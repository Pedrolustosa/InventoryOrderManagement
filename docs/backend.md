# Backend — InventoryOrderManagementServer

.NET 10 (`net10.0`) Clean Architecture solution. Entry point: `src/InventoryOrderManagement.Api/Program.cs`.

## Solution structure

File: `InventoryOrderManagementServer/InventoryOrderManagement.slnx`

Production projects live under `src/`; test projects under `tests/`.

| Project | Role |
|---------|------|
| `InventoryOrderManagement.Api` | HTTP host, controllers, middleware, OpenAPI, DI |
| `InventoryOrderManagement.Application` | Use cases, DTOs, permissions, abstractions |
| `InventoryOrderManagement.Domain` | Entities, enums, value objects, exceptions |
| `InventoryOrderManagement.Infrastructure` | EF Core, Identity, repositories, seed, queries |
| `*.Domain.UnitTests` | Entity rules |
| `*.Application.UnitTests` | Use cases / notifications |
| `*.Infrastructure.IntegrationTests` | Persistence |
| `*.Api.IntegrationTests` | Endpoints via `WebApplicationFactory` |

## How to run

```bash
cd InventoryOrderManagementServer

dotnet restore InventoryOrderManagement.slnx
dotnet build InventoryOrderManagement.slnx
dotnet run --project src/InventoryOrderManagement.Api --launch-profile http
```

| Resource | URL |
|----------|-----|
| API | `http://localhost:5237` |
| Swagger (Development) | `http://localhost:5237/swagger` |
| HTTPS (`https` profile) | `https://localhost:7265` |

In Development, startup:

1. Applies migrations (`Database.Migrate()`).
2. Runs `IdentitySeeder` (roles + claims; optional admin via User Secrets).
3. Runs `DevelopmentDataSeeder` (demo data + accounts).

### Connection string

`src/InventoryOrderManagement.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=inventory.db"
  }
}
```

SQLite file is created in the Api working directory. Optional admin bootstrap:

```text
Identity:Bootstrap:Email
Identity:Bootstrap:Password
Identity:Bootstrap:DisplayName
```

### Migrations

```bash
cd InventoryOrderManagementServer

dotnet ef migrations add MigrationName \
  --project src/InventoryOrderManagement.Infrastructure \
  --startup-project src/InventoryOrderManagement.Api

dotnet ef database update \
  --project src/InventoryOrderManagement.Infrastructure \
  --startup-project src/InventoryOrderManagement.Api
```

## Domain

### Aggregates / entities

| Entity | Responsibility |
|--------|----------------|
| `Product`, `Category`, `Supplier`, `Customer` | Catalog (customer with address VO) |
| `InventoryBalance`, `StockMovement` | Per-product balance + ledger |
| `Order` / `OrderItem` | Sales order |
| `PurchaseOrder` / items, `PurchaseReceipt` | Purchase and receipt |
| `Shipment` / `ShipmentStatusHistory` | Shipping tied to an order |
| `ReturnRequest` / `ReturnItem` | Customer or supplier return |
| `FinancialTitle` (+ installments / settlements) | Payables / receivables |
| `CashAccount`, `CashMovement` | Cash accounts and movements |
| `UserNotification` | In-app notification |

Identity (`ApplicationUser`, `ApplicationRole`, `SecurityAuditLog`) lives in Infrastructure.

### State machines

**Sales order (`OrderStatus`)**

```text
Draft → Confirmed → Shipped → Delivered
  └──────────────→ Cancelled (Draft|Confirmed only)
```

**Purchase order (`PurchaseOrderStatus`)**

```text
Draft → Sent → PartiallyReceived → Received
  └──────────→ Cancelled (Draft|Sent|PartiallyReceived)
```

**Shipment (`ShipmentStatus`)**

```text
Preparing → Shipped → InTransit → Delivered
                └──────→ Failed → (Resume) → InTransit
```

**Returns**

- Customer: Draft → Receive → UnderInspection → Completed | Rejected
- Supplier: Draft → Authorize → Dispatch → Dispatched

### Notable rules

- Confirm requires Draft with ≥1 item; cancel limited to Draft/Confirmed.
- `RegisterCustomerReturn` only on Delivered orders.
- Shipment dispatch requires a carrier; history in `ShipmentStatusHistory`.
- Inventory: `Receive` / `Issue` / `AdjustTo` / `Reserve` / `Release` / `ConsumeReservation`; `InsufficientStockException` and optimistic concurrency.
- Low stock (single rule `Domain/Inventory/StockLevels`): a product is below its minimum when the minimum is
  greater than zero and **Available** (on hand − reserved) is lower than it. The same rule drives `IsLowStock`,
  the balance filter, inventory/low-stock reports, the dashboard count and the web badge.
  - Every `InventoryBalance` change goes through one `ApplyChange` method, which raises
    `StockBelowMinimumDomainEvent(ProductId, Available, MinimumStock, OccurredOnUtc)` only on the downward
    crossing (was not below → is below). Staying below raises nothing; going back up re-arms it. Reservations
    (order confirmation) lower Available; consuming a reservation on dispatch does not.
  - `LowStockNotificationHandler` (`IDomainEventHandler`, **after commit**) notifies active users holding
    `Inventory.LowStockAlerts` (default: Administrator, Manager, InventoryOperator; not Viewer), linking to
    `/inventory?productId={id}`. It skips a user who still has an **unread** low-stock notification for the
    product. Failures are logged and never undo the stock change.
  - Limitation: no outbox. If notifying fails, or the process stops between commit and the handler, the alert
    is lost (only logged) and not retried.
- Domain events: `UnitOfWork` dispatches transactional handlers in rounds until no aggregate has pending events,
  so events raised inside a handler (e.g. stock events while confirming, dispatching or completing a return) are
  handled too. Post-commit handlers are isolated: a failure is logged and never turns a committed operation
  into an error. A synchronous throw from any handler surfaces its own exception, not the reflection wrapper.
- Order confirmation reserves stock (`OrderConfirmedHandler`, `OrderReservation` movements).
- Shipment dispatch (`ShipmentDispatchedDomainEvent` → `ShipmentDispatchedHandler`, transactional, same
  transaction as the Shipment → Dispatched and Order → Shipped changes):
  - A shipment always covers the whole order (one shipment per order).
  - Stock is decided **per order** from its own movements, never from the product-wide reserved total:
    net reservation covers the item → `ConsumeReservation` (OnHand and Reserved drop together);
    no reservation (legacy order) → `Issue` directly, validating `AvailableQuantity`;
    partial reservation → 409.
    Orders confirmed before reservations existed already had an `OrderExit` at confirmation
    (`ShipmentId` null); for them nothing is issued again.
  - Each issue is an `OrderExit` movement linked to `OrderId` and `ShipmentId`.
  - One receivable per sales order (`SourceType = SalesOrder`, `SourceId = OrderId`), for the order subtotal,
    one installment due `Finance:DefaultReceivableDays` (default 30) after issue. Orders and customers have no payment terms.
  - Idempotency: filtered unique indexes on `StockMovements (ShipmentId, ProductId)` and
    `FinancialTitles (Type, SourceType, SourceId)` for sales-order receivables, plus existence checks in the handler.
  - Any failure (stock, receivable, concurrency) rolls everything back and returns 409 ProblemDetails.
- Return completion (`ReturnCompletedDomainEvent` → `ReturnCompletedHandler`, transactional, same transaction
  as the return status change). Raised once, on the physical step only: customer returns and exchanges when
  the inspection completes with accepted units (Rejected raises nothing), supplier returns on dispatch.
  - Quantities: `Order.RegisterCustomerReturn` / `PurchaseOrder.RegisterSupplierReturn` reject more than
    sold/received minus earlier returns.
  - Stock: Restocked → `Receive` + `CustomerReturnEntry`; Quarantined → `CustomerReturnQuarantine` and
    Discarded → `CustomerReturnLoss`, both record only (on-hand/available unchanged); Rejected goes back to the
    customer. Supplier returns → `SupplierReturnExit` validating `AvailableQuantity`.
  - Exchange (`ReturnType.Exchange`, `POST /api/returns/exchange`): customer return plus `ReplacementItems`
    (price snapshot); on completion the returned units enter as above and replacements leave as
    `ExchangeReplacementExit`, validating `AvailableQuantity`.
  - Finance, proportional to accepted (customer) or dispatched (supplier) quantity × unit value snapshot:
    the original title (sales-order receivable / purchase-order payable) is abated with cash-less
    `ReturnCredit` settlements, oldest installment first; a title with no settlements whose whole balance is
    reversed is cancelled instead. The part already paid, or the whole amount without an open original,
    becomes a new title with `SourceType = Return`: payable refund/credit to the customer, receivable from
    the supplier, due `Finance:DefaultReturnTitleDays` (default 30). Exchanges settle only the difference
    (none when equal, receivable when replacements cost more, refund rule when less). Treatment `None` skips
    finance (except exchanges). Financial records are never deleted.
  - `ReturnCredit` and `SourceType = Return` are system-only (rejected on manual settlements/titles).
  - Idempotency: stock runs only when the return has no movement (unique index
    `StockMovements (ReturnRequestId, ProductId, MovementType)`); finance only while
    `FinancialStatus = Pending` → `Processed` (unique `FinancialTitles (SourceType, SourceId)` for returns and
    unique settlement keys `return:{returnId}:{installmentId}`).
  - Any failure rolls back stock, order/purchase return quantities, finance and status (409 ProblemDetails).
- Financial title: derived status; settle creates a settlement and possibly cash movement.

Key files: `Domain/Entities/*.cs`, enums in `Domain/Enums/`.

## Application

Registration: `Application/DependencyInjection/ServiceCollectionExtensions.cs` (`AddApplication`).

Pattern: **one use case per operation** (scoped class), no MediatR.

### Main areas

| Area | Examples |
|------|----------|
| Catalog | Create/Update/Deactivate products, categories, suppliers, customers |
| Inventory | Entries, exits, adjustments, minimum stock, low-stock |
| Orders | Create, UpdateDraft, Confirm, Ship, Deliver, Cancel |
| Purchases | Create/Send/Receive/Cancel + receipts |
| Shipments | Create, Dispatch, InTransit, Deliver, Fail, Resume |
| Returns | Customer/supplier flows + inspection |
| Finance | Payables/Receivables, cash accounts, aging, cash-flow |
| Operational dashboard | Summary, trends, recent orders (`GetOperationalDashboardSummaryUseCase`, …) |
| Reports | Stock/sales dashboard summary, inventory, orders, best-sellers, sales |
| Identity | Login, users, roles, permissions, audit |
| Notifications | List, unread, mark-read |

### Abstractions

- Persistence: `IUnitOfWork`, per-aggregate repositories (`IOrderRepository`, …)
- Identity: `ICurrentUser`, `IUserAccountService`, `IRoleAccountService`, audit
- Dashboard: `IDashboardQuery` → Infrastructure `DashboardQuery`
- Reports: `IInventoryReportsQuery`, `IOrderReportsQuery`, …
- Notifications: `INotificationDispatcher`, `INotificationRecipientResolver`

AuthZ catalog: `Application/Security/Permissions.cs`.

## Infrastructure

Registration: `AddInfrastructure`.

| Component | Detail |
|-----------|--------|
| `InventoryDbContext` | `IdentityDbContext<ApplicationUser, ApplicationRole, Guid>` |
| Provider | SQLite (`UseSqlite`) |
| Configurations | Fluent API under `Persistence/Configurations/` |
| Migrations | `Persistence/Migrations/` |
| Repositories | `Persistence/Repositories/` |
| Reports / dashboard | `Persistence/Queries/` (`DashboardQuery`, report queries) |
| Seed | `IdentitySeeder`, `DevelopmentDataSeeder` |

### Identity

- Password: min 12, digit, upper, lower, non-alphanumeric
- Lockout: 5 attempts / 15 min
- Cookie `.Iom.Auth`, 8h sliding; 401/403 JSON responses (no HTML redirect)
- Principal rejected if user missing or `IsActive == false`
- Claim `must_change_password` via `ApplicationUserClaimsPrincipalFactory`

### System roles

`Administrator`, `Manager`, `InventoryOperator`, `SalesOperator`, `Viewer`

Defaults: `Infrastructure/Identity/RolePermissionDefaults.cs` (Administrator = all permissions).

## Api pipeline (`Program.cs`)

1. Development: OpenAPI + Swagger, migrate + seed
2. `GlobalExceptionHandler` → ProblemDetails
3. CORS `AngularDev` (`localhost:4200` + credentials)
4. HTTPS redirection **outside** Development
5. Authentication → Authorization
6. `MustChangePasswordMiddleware`
7. `MapControllers`

Extras: global `AntiforgeryFilter`; `JsonStringEnumConverter`; FallbackPolicy = authenticated user.

## Authorization

- Claim type: `permission`
- Each string in `Permissions.All` becomes an Authorization Policy with the same name
- Controllers: `[Authorize(Policy = Permissions.X.Y)]`

Full list and endpoint mapping: [API](api.md).

## Tests

```bash
cd InventoryOrderManagementServer
dotnet test InventoryOrderManagement.slnx
```

| Project | Focus |
|---------|-------|
| Domain.UnitTests | Transitions and invariants |
| Application.UnitTests | Use cases and notifications |
| Infrastructure.IntegrationTests | Repos, settlements, rollback |
| Api.IntegrationTests | HTTP (products, purchases, finance, returns, shipments, dashboard, …) |

Example filter:

```bash
dotnet test tests/InventoryOrderManagement.Api.IntegrationTests --filter "FullyQualifiedName~Dashboard"
```

## Entry-point files

| File | Use |
|------|-----|
| `src/.../Api/Program.cs` | Host and pipeline |
| `src/.../Application/DependencyInjection/ServiceCollectionExtensions.cs` | Application DI |
| `src/.../Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs` | Infrastructure DI |
| `src/.../Infrastructure/Persistence/InventoryDbContext.cs` | EF model |
| `src/.../Application/Security/Permissions.cs` | Permissions |
| `src/.../Infrastructure/Persistence/Seeding/DevelopmentDataSeeder.cs` | Demo data |

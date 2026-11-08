# InventoryOrderManagementServer

.NET 10 REST API for inventory, purchasing, sales, shipping, returns, finance, operational dashboard, and access management.

Built with Clean Architecture (Domain, Application, Infrastructure, Api), ASP.NET Core Identity (cookie + CSRF), EF Core 10 + SQLite, and permission-based authorization.

For the full-stack overview, see the [repository README](../README.md). Deeper technical notes: [docs/backend.md](../docs/backend.md), [docs/architecture.md](../docs/architecture.md), [docs/api.md](../docs/api.md).

## Solution structure

```text
InventoryOrderManagementServer/
├── src/
│   ├── InventoryOrderManagement.Api/
│   ├── InventoryOrderManagement.Application/
│   ├── InventoryOrderManagement.Domain/
│   └── InventoryOrderManagement.Infrastructure/
├── tests/
│   ├── InventoryOrderManagement.Domain.UnitTests/
│   ├── InventoryOrderManagement.Application.UnitTests/
│   ├── InventoryOrderManagement.Api.IntegrationTests/
│   └── InventoryOrderManagement.Infrastructure.IntegrationTests/
├── InventoryOrderManagement.slnx
└── README.md
```

| Project | Role |
|---|---|
| `Api` | Controllers, middleware, OpenAPI/Swagger, CORS, composition root |
| `Application` | Use cases, DTOs, permissions, persistence/identity abstractions |
| `Domain` | Entities, enums, value objects, business rules |
| `Infrastructure` | EF Core, Identity, repositories, seeding, dashboard/report queries |
| `*.UnitTests` / `*.IntegrationTests` | xUnit coverage by layer |

Dependency direction: **Api → Application + Infrastructure**; **Infrastructure → Application + Domain**; **Application → Domain**.

## Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download) (`net10.0`)
- Optional: EF Core CLI — `dotnet tool install --global dotnet-ef`

## Run

```bash
cd InventoryOrderManagementServer

dotnet restore InventoryOrderManagement.slnx
dotnet build InventoryOrderManagement.slnx
dotnet run --project src/InventoryOrderManagement.Api --launch-profile http
```

| Resource | URL |
|---|---|
| API | `http://localhost:5237` |
| Swagger (Development) | `http://localhost:5237/swagger` |
| HTTPS profile | `https://localhost:7265` (+ `http://localhost:5237`) |

CORS allows the Angular app at `http://localhost:4200` / `https://localhost:4200` with credentials.

### Development startup behavior

On Api start in Development:

1. Applies EF migrations (`Database.Migrate()`).
2. Seeds Identity roles/claims (`IdentitySeeder`); optional bootstrap admin via User Secrets.
3. Seeds demo catalog/operations/accounts (`DevelopmentDataSeeder`).

## Configuration

`src/InventoryOrderManagement.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=inventory.db"
  }
}
```

SQLite file `inventory.db` is created in the Api working directory (ignored by Git).

Optional bootstrap admin (User Secrets / environment variables — never commit secrets):

```text
Identity:Bootstrap:Email
Identity:Bootstrap:Password
Identity:Bootstrap:DisplayName
```

## Migrations

```bash
cd InventoryOrderManagementServer

dotnet ef migrations add MigrationName \
  --project src/InventoryOrderManagement.Infrastructure \
  --startup-project src/InventoryOrderManagement.Api

dotnet ef database update \
  --project src/InventoryOrderManagement.Infrastructure \
  --startup-project src/InventoryOrderManagement.Api
```

Migrations live under `src/InventoryOrderManagement.Infrastructure/Persistence/Migrations/`.

## Authentication and authorization

- ASP.NET Core Identity cookie (`.Iom.Auth`)
- Antiforgery cookie (`.Iom.Antiforgery`) + `X-XSRF-TOKEN` on mutations
- Permission claims → named policies (e.g. `Orders.Confirm`, `Shipments.Dispatch`)
- Fallback policy requires an authenticated user
- Must-change-password middleware

System roles: `Administrator`, `Manager`, `InventoryOperator`, `SalesOperator`, `Viewer`.

### Demo accounts (Development only)

| Email | Password | Role |
|---|---|---|
| `admin.demo@localhost` | `Admin!Access123` | Administrator |
| `manager@localhost` | `Manager!Access123` | Manager |
| `inventory@localhost` | `Inventory!Op123` | InventoryOperator |
| `sales@localhost` | `Sales!Access123` | SalesOperator |
| `viewer@localhost` | `Viewer!Access123` | Viewer |

## API surface (summary)

Prefix: `/api`. Interactive docs: Swagger in Development.

| Area | Examples |
|---|---|
| Auth | `/api/auth/login`, `/logout`, `/me`, `/csrf` |
| Catalog | `/api/products`, `/categories`, `/suppliers`, `/customers` |
| Operations | `/api/inventory`, `/orders`, `/purchase-orders` |
| Shipping / returns | `/api/shipments`, `/returns` |
| Finance | `/api/finance/payables`, `/receivables`, `/cash-accounts`, … |
| Operational dashboard | `/api/dashboard/summary`, `/trends`, `/recent-orders` |
| Reports / notifications | `/api/reports`, `/notifications` |
| Security | `/api/users`, `/roles`, `/permissions`, `/security/audit-logs` |

### Operational dashboard

Policy: `Reports.View`. Period params: `startDateUtc`, `endDateUtc`, `period` (same presets as reports). Period filters sales/purchase commercial metrics; catalog, stock, shipping, and returns counts are current snapshots.

| Endpoint | Notes |
|---|---|
| `GET /api/dashboard/summary` | Consolidated cards (products, parties, orders, purchases, shipping, returns). `finance` only if the user has `Finance.Reports.View`. `notifications.unread` only for the current user with `Notifications.View`. Shipping counts are **shipment records**, not unique orders. |
| `GET /api/dashboard/trends` | Combined sales (`ConfirmedAtUtc`) + purchases (`SentAtUtc`) by `groupBy` Day/Week/Month |
| `GET /api/dashboard/recent-orders` | Latest orders (`take` 1–50, default 10) |

Drill-down detail stays on existing endpoints: `/api/reports/*`, `/api/finance/dashboard`, `/api/reports/inventory/low-stock`.

Errors use `ProblemDetails`. Enums serialize as strings. Full route and permission map: [docs/api.md](../docs/api.md).

## Tests

```bash
cd InventoryOrderManagementServer
dotnet test InventoryOrderManagement.slnx
```

Example filter:

```bash
dotnet test tests/InventoryOrderManagement.Api.IntegrationTests --filter "FullyQualifiedName~Shipment"
```

| Project | Scope |
|---|---|
| `Domain.UnitTests` | Entity rules and transitions |
| `Application.UnitTests` | Use cases |
| `Api.IntegrationTests` | HTTP via `WebApplicationFactory` |
| `Infrastructure.IntegrationTests` | Persistence scenarios |

## Related

- Frontend: [InventoryOrderManagementClient](../InventoryOrderManagementClient/README.md)
- Architecture: [docs/architecture.md](../docs/architecture.md)

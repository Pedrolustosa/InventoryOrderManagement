# REST API

Base URL (Development): `http://localhost:5237/api`  
Swagger UI: `http://localhost:5237/swagger`

Authentication uses the Identity cookie. Almost all endpoints require an authenticated user (FallbackPolicy). Mutations require antiforgery (`X-XSRF-TOKEN`).

## Authentication and CSRF

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/auth/csrf` | Anonymous | Issues CSRF token |
| POST | `/auth/login` | Anonymous | Login (session cookie) |
| POST | `/auth/logout` | Authenticated | Ends session |
| GET | `/auth/me` | Authenticated | Current user + permissions |
| POST | `/auth/change-password` | Authenticated | Password change |

Relevant cookies: `.Iom.Auth`, `.Iom.Antiforgery`.  
Mutation header: `X-XSRF-TOKEN`.

JSON enums: **string**. Errors: **ProblemDetails**.

## Permissions

Claim type: `permission`. Each value below is also the Authorization Policy name.

```text
Users.View | Users.Create | Users.Update | Users.Deactivate
Roles.View | Roles.Create | Roles.Update | Roles.Delete | Roles.ManagePermissions
Products.View | Products.Create | Products.Update | Products.Deactivate
Categories.View | Categories.Create | Categories.Update | Categories.Deactivate
Suppliers.View | Suppliers.Create | Suppliers.Update | Suppliers.Deactivate
Customers.View | Customers.Create | Customers.Update | Customers.Deactivate
Inventory.View | Inventory.Receive | Inventory.Exit | Inventory.Adjust | Inventory.MinimumStock.Update
Orders.View | Orders.Create | Orders.UpdateDraft | Orders.Confirm | Orders.Ship | Orders.Deliver | Orders.Cancel
Purchases.View | Purchases.Create | Purchases.UpdateDraft | Purchases.Send | Purchases.Receive | Purchases.Cancel
Shipments.View | Shipments.Create | Shipments.Update | Shipments.Dispatch | Shipments.Advance
Returns.View | Returns.Customer.Create | Returns.Customer.Receive | Returns.Customer.Inspect
Returns.Supplier.Create | Returns.Supplier.Authorize | Returns.Supplier.Dispatch | Returns.Cancel
Finance.View | Finance.Payables.Create | Finance.Payables.Update | Finance.Payables.Cancel | Finance.Payables.Settle
Finance.Receivables.Create | Finance.Receivables.Update | Finance.Receivables.Cancel | Finance.Receivables.Settle
Finance.CashAccounts.View | Finance.CashAccounts.Manage | Finance.CashFlow.View | Finance.Reports.View
Reports.View
Security.Audit.View
Notifications.View | Notifications.MarkRead
```

System roles: `Administrator`, `Manager`, `InventoryOperator`, `SalesOperator`, `Viewer`.

## Catalog

### Products — `/products` · Policies `Products.*`

| Method | Route | Policy |
|--------|-------|--------|
| POST | `/` | Create |
| GET | `/` | View |

> Note: Product Update/Deactivate may exist in Domain/Application without a matching HTTP endpoint yet.

### Categories — `/categories` · `Categories.*`

| Method | Route | Policy |
|--------|-------|--------|
| POST | `/` | Create |
| GET | `/` | View |
| GET | `/{id}` | View |
| PUT | `/{id}` | Update |
| PATCH | `/{id}/deactivate` | Deactivate |

### Suppliers — `/suppliers` · `Suppliers.*`

Same pattern as Categories (POST, GET, GET id, PUT, PATCH deactivate).

### Customers — `/customers` · `Customers.*`

Same pattern as Categories.

## Inventory — `/inventory`

| Method | Route | Policy |
|--------|-------|--------|
| POST | `/entries` | Inventory.Receive |
| POST | `/exits` | Inventory.Exit |
| POST | `/adjustments` | Inventory.Adjust |
| GET | `/balances` | Inventory.View |
| GET | `/balances/{productId}` | Inventory.View |
| PUT | `/balances/{productId}/minimum-stock` | Inventory.MinimumStock.Update |
| GET | `/low-stock` | Inventory.View |
| GET | `/movements` | Inventory.View |
| GET | `/products/{productId}/movements` | Inventory.View |

## Sales orders — `/orders`

| Method | Route | Policy |
|--------|-------|--------|
| POST | `/` | Orders.Create |
| GET | `/` | Orders.View |
| GET | `/{id}` | Orders.View |
| PUT | `/{id}` | Orders.UpdateDraft |
| POST | `/{id}/confirm` | Orders.Confirm |
| POST | `/{id}/ship` | Orders.Ship |
| POST | `/{id}/deliver` | Orders.Deliver |
| POST | `/{id}/cancel` | Orders.Cancel |
| GET | `/{id}/timeline` | Orders.View (+ Finance.View / Returns.View per module) |

With an active shipment, legacy `ship`/`deliver` may return **409** — use the `/shipments` flow.

### Order timeline — `GET /orders/{id}/timeline`

Read-only, chronological history of a sales order across **Order, Stock, Shipment, Finance and Return**.

- **200** `OrderTimelineDto`; **404** ProblemDetails for an unknown order; **403** without `Orders.View`.
- `Finance` entries are returned only with `Finance.View`, and `Return` entries only with `Returns.View`; the other
  modules are always included. Without those permissions the data is not even queried.
- Ascending by `occurredAtUtc` (UTC, `Z`); events recorded at the same instant follow the business flow
  (e.g. `order.confirmed` → `stock.reserved` → `shipment.dispatched` → `stock.issued` → `finance.receivable_created`).
- No translated text: `eventType` and `status` are **stable codes** for the client to translate. `summary` holds
  structured data and omits fields that do not apply.
- Links: `referenceType` + `referenceId` is one of `Order`, `Shipment`, `Product` (stock entries), `FinancialTitle`
  (`summary.titleType` says receivable/payable), `ReturnRequest`.
- `performedBy` (`userId`, `displayName`) is filled when the source records the user: shipment status changes,
  settlements, manual titles and return creation. Order transitions and stock movements do not record a user, so
  it is `null` for them.
- Performance: no-tracking projections with a fixed number of queries regardless of timeline size (at most 7:
  order, shipment history, stock movements, returns, titles, settlements, user names).

**Event types**

| Module | `eventType` | `status` (resulting) |
|--------|-------------|----------------------|
| Order | `order.created`, `order.confirmed`, `order.shipped`, `order.delivered`, `order.cancelled` | Order status |
| Stock | `stock.reserved`, `stock.reservation_released`, `stock.issued`, `stock.cancellation_returned` (legacy), `stock.return_restocked`, `stock.return_quarantined`, `stock.return_lost`, `stock.exchange_issued` | — |
| Shipment | `shipment.created`, `shipment.dispatched`, `shipment.in_transit`, `shipment.failed`, `shipment.resumed`, `shipment.delivered` | Shipment status |
| Finance | `finance.receivable_created`, `finance.refund_created` (payable to the customer from a return), `finance.difference_created` (exchange price difference receivable), `finance.settled` (cash settlement), `finance.abated` (return credit), `finance.title_cancelled` | Title status after the event (`PartiallyPaid`/`Paid` on settlements) |
| Return | `return.requested` (`summary.returnType` tells an exchange), `return.received`, `return.completed`, `return.rejected`, `return.cancelled` | Return status |

Customer returns have no separate approval step: completing the inspection is the approval (`return.completed`) or
the refusal (`return.rejected`).

**Example** (real response from the integration test, trimmed to some entries):

```json
{
  "orderId": "f1f73a6e-6781-4d96-a594-38dd4cfe7464",
  "orderNumber": "ORD-20261108-B3893D26",
  "currentStatus": "Delivered",
  "entries": [
    {
      "occurredAtUtc": "2026-11-08T15:33:43.7014316Z",
      "module": "Order",
      "eventType": "order.confirmed",
      "status": "Confirmed",
      "summary": { "amount": 200 },
      "referenceType": "Order",
      "referenceId": "f1f73a6e-6781-4d96-a594-38dd4cfe7464",
      "performedBy": null
    },
    {
      "occurredAtUtc": "2026-11-08T15:33:43.7083509Z",
      "module": "Stock",
      "eventType": "stock.reserved",
      "status": null,
      "summary": {
        "productId": "7071c5de-dd64-4872-a7db-18f0a40e2252",
        "productSku": "TIMELINE-FLOW",
        "productName": "Product timeline-flow",
        "quantity": 4
      },
      "referenceType": "Product",
      "referenceId": "7071c5de-dd64-4872-a7db-18f0a40e2252",
      "performedBy": null
    },
    {
      "occurredAtUtc": "2026-11-08T15:33:43.9492705Z",
      "module": "Shipment",
      "eventType": "shipment.dispatched",
      "status": "Shipped",
      "summary": { "carrierName": "Acme" },
      "referenceType": "Shipment",
      "referenceId": "fc967bf3-bf09-49ba-a51b-0f76fe5951e0",
      "performedBy": { "userId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "displayName": null }
    },
    {
      "occurredAtUtc": "2026-11-08T15:33:44.0340329Z",
      "module": "Finance",
      "eventType": "finance.receivable_created",
      "status": "Pending",
      "summary": {
        "amount": 200,
        "dueDate": "2026-12-08",
        "titleNumber": "FT-20261108-3C524145",
        "titleType": "Receivable"
      },
      "referenceType": "FinancialTitle",
      "referenceId": "3895fbb7-b13a-4fd3-b0a1-534eff899873",
      "performedBy": null
    },
    {
      "occurredAtUtc": "2026-11-08T15:33:44.5028116Z",
      "module": "Finance",
      "eventType": "finance.settled",
      "status": "PartiallyPaid",
      "summary": {
        "amount": 80,
        "titleNumber": "FT-20261108-3C524145",
        "titleType": "Receivable",
        "paymentMethod": "Pix"
      },
      "referenceType": "FinancialTitle",
      "referenceId": "3895fbb7-b13a-4fd3-b0a1-534eff899873",
      "performedBy": { "userId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "displayName": null }
    },
    {
      "occurredAtUtc": "2026-11-08T15:33:45.1605594Z",
      "module": "Return",
      "eventType": "return.completed",
      "status": "Completed",
      "summary": {
        "amount": 50,
        "returnNumber": "RET-20261108-C450EBE2",
        "returnType": "CustomerReturn"
      },
      "referenceType": "ReturnRequest",
      "referenceId": "04da0abc-931d-4771-845e-7d526e72c2f6",
      "performedBy": null
    },
    {
      "occurredAtUtc": "2026-11-08T15:33:45.2166002Z",
      "module": "Finance",
      "eventType": "finance.abated",
      "status": "PartiallyPaid",
      "summary": {
        "amount": 50,
        "titleNumber": "FT-20261108-3C524145",
        "titleType": "Receivable",
        "paymentMethod": "ReturnCredit"
      },
      "referenceType": "FinancialTitle",
      "referenceId": "3895fbb7-b13a-4fd3-b0a1-534eff899873",
      "performedBy": null
    }
  ]
}
```

## Purchases

### Purchase orders — `/purchase-orders`

| Method | Route | Policy |
|--------|-------|--------|
| POST | `/` | Purchases.Create |
| GET | `/` | Purchases.View |
| GET | `/{id}` | Purchases.View |
| PUT | `/{id}` | Purchases.UpdateDraft |
| POST | `/{id}/send` | Purchases.Send |
| POST | `/{id}/receipts` | Purchases.Receive |
| GET | `/{id}/receipts` | Purchases.View |
| POST | `/{id}/cancel` | Purchases.Cancel |

### Receipts — `/purchase-receipts`

| Method | Route | Policy |
|--------|-------|--------|
| GET | `/{receiptId}` | Purchases.View |

## Shipping — `/shipments`

| Method | Route | Policy |
|--------|-------|--------|
| POST | `/` | Shipments.Create |
| GET | `/` | Shipments.View |
| GET | `/{id}` | Shipments.View |
| GET | `/{id}/history` | Shipments.View |
| PUT | `/{id}` | Shipments.Update |
| POST | `/{id}/dispatch` | Shipments.Dispatch |
| POST | `/{id}/in-transit` | Shipments.Advance |
| POST | `/{id}/deliver` | Shipments.Advance |
| POST | `/{id}/fail` | Shipments.Advance |
| POST | `/{id}/resume` | Shipments.Advance |

## Returns — `/returns`

| Method | Route | Policy |
|--------|-------|--------|
| POST | `/customer` | Returns.Customer.Create |
| POST | `/supplier` | Returns.Supplier.Create |
| GET | `/` | Returns.View |
| GET | `/{id}` | Returns.View |
| PUT | `/{id}` | (depends on type/flow) |
| GET | `/customer/eligible-orders/{orderId}/items` | Returns.View |
| GET | `/supplier/eligible-orders/{purchaseOrderId}/items` | Returns.View |
| GET | `/{id}/history` | Returns.View |
| GET | `/{id}/stock-movements` | Returns.View |
| POST | `/{id}/receive` | Returns.Customer.Receive |
| POST | `/{id}/inspection/start` | Returns.Customer.Inspect |
| POST | `/{id}/inspection/complete` | Returns.Customer.Inspect |
| POST | `/{id}/authorize` | Returns.Supplier.Authorize |
| POST | `/{id}/dispatch` | Returns.Supplier.Dispatch |
| POST | `/{id}/cancel` | Returns.Cancel |

## Finance

### Payables / Receivables — `/finance/payables` and `/finance/receivables`

| Method | Route | Policy (payables example) |
|--------|-------|---------------------------|
| POST | `/` | Finance.Payables.Create |
| GET | `/` | Finance.View |
| GET | `/{id}` | Finance.View |
| PUT | `/{id}` | Finance.Payables.Update |
| POST | `/{id}/cancel` | Finance.Payables.Cancel |
| POST | `/{id}/installments/{installmentId}/settlements` | Finance.Payables.Settle |

Receivables use the equivalent `Finance.Receivables.*` policies.

### Cash accounts — `/finance/cash-accounts`

| Method | Route | Policy |
|--------|-------|--------|
| POST | `/` | Finance.CashAccounts.Manage |
| GET | `/` | Finance.CashAccounts.View |
| GET | `/{id}` | Finance.CashAccounts.View |
| PUT | `/{id}` | Finance.CashAccounts.Manage |
| PATCH | `/{id}/deactivate` | Finance.CashAccounts.Manage |
| GET | `/{id}/balance` | Finance.CashAccounts.View |

### Finance queries — `/finance`

| Method | Route | Policy |
|--------|-------|--------|
| GET | `/settlements` | Finance.View |
| GET | `/cash-movements` | Finance.CashFlow.View |
| GET | `/dashboard` | Finance.Reports.View |
| GET | `/cash-flow/realized` | Finance.CashFlow.View |
| GET | `/cash-flow/projected` | Finance.CashFlow.View |
| GET | `/reports/aging` | Finance.Reports.View |

## Reports — `/reports` · `Reports.View`

| Method | Route |
|--------|-------|
| GET | `/dashboard` |
| GET | `/inventory` |
| GET | `/inventory/low-stock` |
| GET | `/inventory/movements` |
| GET | `/orders/summary` |
| GET | `/orders/trend` |
| GET | `/orders/cancellations` |
| GET | `/products/best-sellers` |
| GET | `/sales/by-customer` |

## Operational dashboard — `/dashboard` · `Reports.View`

Consolidated home KPIs for the Angular `/dashboard` feature. Does not replace Stock Reports (`/reports`) or `GET /finance/dashboard`.

| Method | Route | Notes |
|--------|-------|-------|
| GET | `/summary` | Nested cards. Optional `startDateUtc`, `endDateUtc`, `period` (same presets as reports). Period applies to sales/purchase commercial metrics; stock/parties/shipping/returns are snapshots. `finance` omitted without `Finance.Reports.View`. `notifications` is unread for the current user only (`Notifications.View`). Shipping counts are shipment records. `products.lowStock` is positive qty at/below minimum (excludes out-of-stock). |
| GET | `/trends` | Requires `groupBy` (`Day`/`Week`/`Month`). Sales by `ConfirmedAtUtc`; purchases by `SentAtUtc`. |
| GET | `/recent-orders` | Optional `take` (1–50, default 10). |

Low-stock table rows on the SPA use `GET /reports/inventory/low-stock` (not a `/dashboard/*` sibling).

## Security / IAM

### Users — `/users`

| Method | Route | Policy |
|--------|-------|--------|
| POST | `/` | Users.Create |
| GET | `/` | Users.View |
| GET | `/{id}` | Users.View |
| PUT | `/{id}` | Users.Update |
| PATCH | `/{id}/deactivate` | Users.Deactivate |
| PUT | `/{id}/roles` | Users.Update |

### Roles — `/roles`

| Method | Route | Policy |
|--------|-------|--------|
| POST | `/` | Roles.Create |
| GET | `/` | Roles.View |
| GET | `/{id}` | Roles.View |
| PUT | `/{id}` | Roles.Update |
| DELETE | `/{id}` | Roles.Delete |
| PUT | `/{id}/permissions` | Roles.ManagePermissions |

### Permissions — `/permissions`

| Method | Route | Policy |
|--------|-------|--------|
| GET | `/` | Roles.View |
| GET | `/by-role/{roleId}` | Roles.View |

### Audit — `/security/audit-logs`

| Method | Route | Policy |
|--------|-------|--------|
| GET | `/` | Security.Audit.View |

## Notifications — `/notifications`

| Method | Route | Policy |
|--------|-------|--------|
| GET | `/` | Notifications.View |
| GET | `/unread-count` | Notifications.View |
| PATCH | `/{id}/read` | Notifications.MarkRead |
| PATCH | `/read-all` | Notifications.MarkRead |

## Typical HTTP status codes

| Code | Situation |
|------|-----------|
| 200 / 201 | Success |
| 400 | Validation / domain rule |
| 401 | Unauthenticated |
| 403 | Missing permission or must-change-password |
| 404 | Resource not found |
| 409 | State conflict (invalid transition, Order×Shipment, concurrency) |

Central mapping in `GlobalExceptionHandler`.

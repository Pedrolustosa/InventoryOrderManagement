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

With an active shipment, legacy `ship`/`deliver` may return **409** — use the `/shipments` flow.

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

Consolidated home KPIs. Does not replace Stock Reports or `GET /finance/dashboard`.

| Method | Route | Notes |
|--------|-------|-------|
| GET | `/summary` | Nested cards. Optional `startDateUtc`, `endDateUtc`, `period`. Period applies to sales/purchase commercial metrics; stock/parties/shipping/returns are snapshots. `finance` omitted without `Finance.Reports.View`. `notifications` is unread for the current user only (`Notifications.View`). Shipping counts are shipment records. `products.lowStock` is positive qty at/below minimum (excludes out-of-stock). |
| GET | `/trends` | Requires `groupBy` (`Day`/`Week`/`Month`). Sales by `ConfirmedAtUtc`; purchases by `SentAtUtc`. |
| GET | `/recent-orders` | Optional `take` (1–50, default 10). |

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

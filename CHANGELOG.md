# Changelog

All notable changes to this project are documented in this file.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses
[Conventional Commits](https://www.conventionalcommits.org/).

## [Unreleased]

### Breaking changes

- **Orders ship and deliver only through their shipment.** `POST /api/orders/{id}/ship` and
  `POST /api/orders/{id}/deliver` no longer change the order: they answer **410 Gone** with a ProblemDetails that
  points to `/api/shipments`. Migrate clients to:
  1. `POST /api/shipments` (create the shipment for the confirmed order);
  2. `POST /api/shipments/{id}/dispatch` (order → Shipped, stock issued, receivable created);
  3. `POST /api/shipments/{id}/deliver` (order → Delivered).

  The 410 endpoints are kept for one version and will then be removed.
- **Permissions `Orders.Ship` and `Orders.Deliver` were removed.** The migration
  `RemoveLegacyOrderShippingPermissions` deletes them from every stored role, leaving all other permissions as they
  are. Shipping is governed by `Shipments.Create`, `Shipments.Dispatch` and `Shipments.Advance`.

### Changed

- `Order` no longer exposes public ship/deliver transitions. The `OrderFulfillment` domain service moves the order
  together with its own shipment, so an order cannot become Shipped or Delivered without one.
- The development seed creates shipped and delivered orders through their shipment. It also fixes a first-run issue
  where those orders were left without a shipment.
- Web: the order page replaces the *Ship* / *Deliver* buttons with **Create shipment** (confirmed order without a
  shipment, `Shipments.Create`) and **Open shipment** (`Shipments.View`). The shipment form accepts `?orderId=` to
  preselect the order.

### Unchanged

- Orders that were shipped or delivered by the removed endpoints have no shipment. They are not migrated: their
  details and timeline still load, and no shipping action is offered for them.

### Removed

- `ShipOrderUseCase`, `DeliverOrderUseCase`, `OrdersService.shipOrder/deliverOrder`, the order list ship/deliver
  actions and their translation keys.

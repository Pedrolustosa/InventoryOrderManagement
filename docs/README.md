# Documentation — InventoryOrderManagement

Technical documentation for the full-stack inventory, purchasing, sales, finance, returns, shipping, and operational dashboard management system.

The [root README](../README.md) covers the overview, stack, and how to run the apps. This folder goes deeper into architecture, backend, frontend, and API.

## Index

| Document | Content |
|----------|---------|
| [Architecture](architecture.md) | System overview, layers, flows, and design decisions |
| [Backend](backend.md) | .NET 10 solution, domain, Application, Infrastructure, auth, and tests |
| [Frontend](frontend.md) | Angular 22 SPA, features, guards, i18n, and UI |
| [API](api.md) | REST routes, authentication, permissions, and HTTP status codes |

## Projects

```text
InventoryOrderManagement/
├── InventoryOrderManagementServer/   # .NET 10 API (Clean Architecture)
├── InventoryOrderManagementClient/   # Angular 22 SPA
└── docs/                             # This documentation
```

| Project | Stack | Port (dev) |
|---------|-------|------------|
| Server | ASP.NET Core, EF Core, SQLite, Identity | `http://localhost:5237` |
| Client | Angular 22, Tailwind 4, Vitest | `http://localhost:4200` |

## Audience

Developers who maintain or extend the codebase, and portfolio reviewers who need more than the root README.

## Demo accounts (Development)

Created by `DevelopmentDataSeeder` — **local Development only**:

| Email | Password | Role |
|-------|----------|------|
| `admin.demo@localhost` | `Admin!Access123` | Administrator |
| `manager@localhost` | `Manager!Access123` | Manager |
| `inventory@localhost` | `Inventory!Op123` | InventoryOperator |
| `sales@localhost` | `Sales!Access123` | SalesOperator |
| `viewer@localhost` | `Viewer!Access123` | Viewer |

## Package READMEs

- [Root README](../README.md) — overview, quick start, demo accounts
- [Server README](../InventoryOrderManagementServer/README.md) — API runbook
- [Client README](../InventoryOrderManagementClient/README.md) — SPA runbook

## Quick links

- SPA (Development): `http://localhost:4200`
- Operational dashboard: `http://localhost:4200/dashboard` (`Reports.View`)
- Swagger (Development): `http://localhost:5237/swagger`
- API prefix: `/api`
- Default UI language: `pt-BR` (runtime switch to `en`)

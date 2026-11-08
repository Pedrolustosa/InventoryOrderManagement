# InventoryOrderManagementClient

Angular 22 SPA for the Inventory Order Management system.

Standalone components, lazy-loaded features, cookie + CSRF auth against the .NET API, Tailwind CSS 4, bilingual UI (`pt-BR` / `en`), and Vitest unit tests.

For the full-stack overview, see the [repository README](../README.md). Deeper technical notes: [docs/frontend.md](../docs/frontend.md), [docs/architecture.md](../docs/architecture.md).

## Layout

```text
InventoryOrderManagementClient/
└── inventory-order-management-web/
    ├── src/
    │   ├── app/
    │   │   ├── core/          # auth, i18n, theme, toast, ProblemDetails
    │   │   ├── features/      # domain features + shell
    │   │   ├── shared/        # HasPermissionDirective
    │   │   ├── app.routes.ts
    │   │   └── app.config.ts
    │   ├── environments/
    │   └── styles.css
    ├── public/
    │   ├── assets/i18n/       # pt-BR.json, en.json
    │   └── assets/branding/
    ├── package.json
    └── angular.json
```

The runnable app lives in `inventory-order-management-web/`.

## Prerequisites

- Node.js compatible with Angular 22 (LTS recommended, typically Node 20+)
- npm (project declares `packageManager: npm@12.2.0`)
- Backend API running at `http://localhost:5237` (see [Server README](../InventoryOrderManagementServer/README.md))

## Run

```bash
cd InventoryOrderManagementClient/inventory-order-management-web

npm install
npm start
```

`npm start` runs `ng serve`.

| Item | Value |
|---|---|
| App URL | `http://localhost:4200` |
| API base (dev) | `http://localhost:5237/api` |
| Config | `src/environments/environment.ts` |

Keep Backend and Frontend running together. CORS on the Api allows `localhost:4200` with credentials.

### Scripts

| Script | Command |
|---|---|
| Start | `npm start` |
| Build | `npm run build` |
| Watch build | `npm run watch` |
| Unit tests | `npm test` |

## Stack

| Technology | Purpose |
|---|---|
| Angular 22 (standalone) | SPA framework |
| TypeScript | Static typing |
| Tailwind CSS 4 | Utility styling + design tokens |
| Inter (`@fontsource/inter`) | Typography |
| `@ngx-translate` | Runtime i18n (`pt-BR` default / `en`) |
| Reactive Forms | Create/edit and workflow forms |
| Signals + RxJS | UI state and HTTP streams |
| Chart.js | Dashboard, reports, and finance charts |
| ngx-toastr | Toast feedback |
| Vitest + jsdom | Unit tests |

## Features and routes

Authenticated area uses `AppShell` with `authGuard` and `passwordChangedGuard`. Each module is gated by `permissionGuard`.

| Path | Feature |
|---|---|
| `/login`, `/change-password`, `/access-denied` | Auth pages |
| `/dashboard` | Operational dashboard (KPIs, charts, low stock, recent orders) |
| `/products`, `/categories`, `/suppliers`, `/customers` | Catalog |
| `/inventory` | Stock balances and movements |
| `/orders` | Sales orders |
| `/purchases` | Purchase orders and receipts |
| `/shipping` | Shipments |
| `/returns` | Customer / supplier returns |
| `/finance/*` | Payables, receivables, cash, aging |
| `/reports/*` | Stock and sales reports (separate from `/dashboard`) |
| `/notifications` | In-app notification center |
| `/users`, `/roles`, `/permissions`, `/audit` | Access management |

Home redirects to the first module allowed by the signed-in user's permissions. Users with `Reports.View` prefer `/dashboard` over `/reports` when that is their first matching module.

## Authentication

- Cookie session against `/api/auth/*` (`withCredentials`)
- CSRF: `GET /auth/csrf` then `X-XSRF-TOKEN` on POST/PUT/PATCH/DELETE
- Guards: `authGuard`, `passwordChangedGuard`, `permissionGuard`, `homeGuard`
- UI gating: `*hasPermission` directive (server policies remain the security boundary)

Demo accounts are seeded by the backend in Development — see [Server README](../InventoryOrderManagementServer/README.md#demo-accounts-development-only).

## Environment

`src/environments/environment.ts`:

```ts
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5237/api',
};
```

There is no separate `environment.prod.ts` in the current tree; point `apiUrl` at the target Api when deploying.

## i18n and theme

- Catalogs: `public/assets/i18n/pt-BR.json`, `en.json`
- Language persisted in `localStorage` (`app-language`); default `pt-BR`
- Currency remains **BRL** regardless of UI language
- Light/dark theme via `ThemeService` (`app-theme`)

## Tests

```bash
cd InventoryOrderManagementClient/inventory-order-management-web
npm test
```

Example include filter:

```bash
npx ng test --watch=false --include=**/dashboard/**/*.spec.ts
```

E2E is not configured in this project.

## Scaffolding (optional)

Generated with [Angular CLI](https://github.com/angular/angular-cli) 22.2.x.

```bash
ng generate component component-name
ng generate --help
```

## Related

- Backend: [InventoryOrderManagementServer](../InventoryOrderManagementServer/README.md)
- API reference: [docs/api.md](../docs/api.md)
- Architecture: [docs/architecture.md](../docs/architecture.md)

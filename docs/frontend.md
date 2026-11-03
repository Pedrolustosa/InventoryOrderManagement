# Frontend — InventoryOrderManagementClient

Angular **22.2** standalone SPA under `InventoryOrderManagementClient/inventory-order-management-web`.

## How to run

```bash
cd InventoryOrderManagementClient/inventory-order-management-web

npm install
npm start
```

| Item | Value |
|------|-------|
| URL | `http://localhost:4200` |
| API (dev) | `http://localhost:5237/api` (`src/environments/environment.ts`) |
| Scripts | `start`, `build`, `watch`, `test` |
| packageManager | `npm@12.2.0` |

Keep the Api running in parallel. CORS allows `localhost:4200` with credentials.

## Layout

```text
inventory-order-management-web/
├── src/
│   ├── app/
│   │   ├── core/          # auth, i18n, theme, toast, ProblemDetails
│   │   ├── features/      # domains + shell
│   │   ├── shared/        # HasPermissionDirective
│   │   ├── app.routes.ts
│   │   └── app.config.ts
│   ├── environments/
│   └── styles.css
└── public/
    ├── assets/i18n/       # pt-BR.json, en.json
    └── assets/branding/
```

Bootstrap: `main.ts` → `bootstrapApplication(App, appConfig)`. No classic `NgModule`.

## UI stack

| Technology | Use |
|------------|-----|
| Angular 22 + TypeScript ~6 | Framework / typing |
| Tailwind CSS 4 | Utility-first + `--iom-*` tokens |
| Inter (`@fontsource/inter`) | Typography |
| `@ngx-translate` | Bilingual UI (`pt-BR` default / `en`) |
| Chart.js | Reports and finance |
| ngx-toastr | Feedback |
| Signals + RxJS | Local state and HTTP |
| Reactive Forms | Forms and validation |
| Vitest + jsdom | Unit tests |

Light/dark theme via `ThemeService` (`data-theme` on `<html>`, key `app-theme`).

## Routing and shell

Routes in `src/app/app.routes.ts`:

| Path | Access |
|------|--------|
| `/login` | Public |
| `/change-password` | Authenticated |
| `/access-denied` | Authenticated |
| Shell (`AppShell`) | `authGuard` + `passwordChangedGuard` |
| Lazy features | `permissionGuard('X.Y')` per module |

Home (`homeGuard`) redirects to the first module allowed by the user's permissions.

Filtered navigation: `features/shell/navigation.ts` (`NAVIGATION_GROUPS`).

### Features and main routes

| Feature | Base | Highlights |
|---------|------|------------|
| `products` | `/products` | List + form |
| `categories` / `suppliers` / `customers` | `/…` | List, detail, deactivate |
| `inventory` | `/inventory` | Receive, exit, adjust, minimum, movements |
| `orders` | `/orders` | List, new, edit, details, status actions |
| `purchases` | `/purchases` | Draft, send, receive, receipts |
| `shipping` | `/shipping` | Create, details, dispatch/advance |
| `returns` | `/returns` | Customer/supplier flows |
| `finance` | `/finance/*` | Dashboard, titles, cash, aging |
| `reports` | `/reports/*` | Overview, inventory, orders, best-sellers |
| `notifications` | `/notifications` | Center + Navbar bell |
| `access-management` | `/users`, `/roles`, `/permissions`, `/audit` | IAM |

Each feature typically has `{feature}.routes.ts`, pages, components, and an HTTP `*.service.ts`.

## Client authentication and authorization

### Flow

1. Startup: `AuthService.loadSession()` → `GET /api/auth/me`
2. Login: `ensureCsrf()` → `POST /api/auth/login`
3. If `mustChangePassword` → `/change-password`
4. Otherwise → landing / validated `returnUrl`
5. Logout: `POST /api/auth/logout` + clear signal

### Guards (`core/auth/guards/auth.guards.ts`)

| Guard | Behavior |
|-------|----------|
| `authGuard` | No session → `/login?returnUrl=` |
| `passwordChangedGuard` | Forces password change |
| `homeGuard` | First allowed path |
| `permissionGuard` | Missing claim → `/access-denied` |

### Interceptors

| Interceptor | Role |
|-------------|------|
| Credentials | `withCredentials`; mutations send `X-XSRF-TOKEN` from cookie |
| Error | 401 → login; 403 must-change → change-password; other 403 → access-denied |

### UI gating

Directive `*hasPermission="'Orders.Confirm'"` in `shared/directives/has-permission.directive.ts`.

The UI hides actions; the **security boundary** remains Api policies.

Client password policy: ≥12, upper, lower, digit, symbol (`password-policy.ts`).

## HTTP services

Pattern: `@Injectable({ providedIn: 'root' })` + `HttpClient` + `environment.apiUrl`.

| Service | Prefix |
|---------|--------|
| AuthService | `/auth` |
| Products / Categories / Suppliers / Customers | `/products`, … |
| InventoryService | `/inventory` |
| OrdersService | `/orders` |
| PurchasesService | `/purchase-orders`, `/purchase-receipts` |
| ShipmentsService | `/shipments` |
| ReturnsService | `/returns` |
| FinanceService | `/finance` |
| ReportsService | `/reports` |
| NotificationsService | `/notifications` |
| Users / Roles / Permissions / Audit | `/users`, `/roles`, `/permissions`, `/security/audit-logs` |

Typed errors via `ProblemDetails` (`core/models/problem-details.model.ts`).

**Note:** only `environment.ts` (dev) exists. There is no versioned `environment.prod.ts` in the current tree.

## i18n

- HTTP loader: `./assets/i18n/{lang}.json`
- Default / fallback: `pt-BR`
- Persistence: `localStorage` key `app-language`
- Dynamic Angular locale (`DynamicLocaleId`) + shell remount
- Namespaces: `COMMON`, `NAVIGATION`, `AUTH`, `ORDERS`, `FINANCE`, `REPORTS`, etc.
- Currency remains **BRL** even when the UI is English

## State

- **Signals** for auth, loading, filters, theme/language
- **No NgRx** — root services + light stores (`NotificationsStateService`, `ReportPeriodStore`)
- Reactive forms with `FormBuilder` / `inject()`

## Tests

```bash
cd InventoryOrderManagementClient/inventory-order-management-web
npm test
```

- Runner: Vitest via `@angular/build:unit-test`
- Coverage across services, pages, guards, interceptors, shell, i18n (~88 specs)
- Helpers: `permitted-auth`, `provide-test-translate`, `toast-testing`

Example:

```bash
npx ng test --watch=false --include=**/shipping/**/*.spec.ts
```

E2E is not configured in this project.

## Critical paths

| Concept | Path |
|---------|------|
| Routes | `src/app/app.routes.ts` |
| Config | `src/app/app.config.ts` |
| Auth | `src/app/core/auth/` |
| Environment | `src/environments/environment.ts` |
| Styles / tokens | `src/styles.css` |
| i18n | `public/assets/i18n/` |
| Features | `src/app/features/` |

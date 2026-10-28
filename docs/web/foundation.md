# Web Foundation

Status: **Application foundation implemented.** CRM, Sales and Inventory screens are not
implemented in this slice.

The web foundation turns `BSX.Web` from a host into a SaaS application shell: authentication,
workspace selection, permission-aware navigation, a responsive layout and a reusable design
system. It deliberately stops before business screens.

---

## Scope

Implemented:

- Sign-in, sign-out, session expiry and workspace selection.
- Protected routes, authenticated API separation, 401 and 403 experiences.
- Responsive application shell with sidebar, header, breadcrumbs, tenant badge and user menu.
- Permission-aware CRM, Sales and Administration navigation.
- Shared design-system components.
- Toasts, loading, empty, error and confirmation patterns.
- Component, navigation, authorization, accessibility and endpoint tests.

Not implemented:

- Customer, organization, lead or opportunity business screens.
- Inventory UI.
- Reporting, analytics or dashboards.
- Tenant creation or tenant settings.

---

## Layout Architecture

`Components/App.razor` hosts the Blazor application and wraps routing in
`CascadingAuthenticationState`. `Components/Routes.razor` uses `AuthorizeRouteView`:

- Unauthenticated requests are redirected to `/login`.
- Authenticated users without the required permission see the friendly access-denied experience.
- The default layout for protected pages is `MainLayout`.
- Authentication pages use `AuthLayout`.

`MainLayout` provides:

- A skip link to `#main-content`.
- A sticky sidebar with brand and permission-filtered navigation.
- A sticky header with mobile navigation trigger, breadcrumbs, workspace badge and user menu.
- A `main` landmark with focus target.
- The toast host and Blazor unhandled-error surface.

`AuthLayout` provides the same skip link and main landmark in a centered authentication shell.

Responsive behavior:

- Desktop: fixed sidebar and content grid.
- Tablet/mobile: off-canvas sidebar, overlay dismissal, header navigation trigger.
- Authentication pages collapse to a single panel on small screens.

---

## Authentication UX

### Browser and API schemes

BSX.Web uses a smart authentication policy:

- `/identity`, `/crm`, `/sales` and `/health` continue to authenticate with **JWT bearer**.
- The Blazor application (`/`, `/app/...`, `/login`, `/select-tenant`) uses a **cookie** session.

The cookie is HttpOnly and contains identity claims, permissions, the active tenant and access
token expiry. Identity access and refresh tokens are never exposed to the browser UI. The refresh
token lives in a separate HttpOnly cookie.

The auth flow is a server-side adapter over the existing Identity application commands:

- `DiscoverTenantOptionsCommand` verifies credentials and returns active workspace memberships.
- `LoginCommand` creates the Identity session and tokens.
- `RefreshTokenCommand` rotates tokens during cookie validation.
- `LogoutCommand` revokes the session owning the refresh token.

No authentication rule is reimplemented in the web layer.

### Login flow

1. The user opens `/login`.
2. Credentials are verified through the Identity application.
3. No memberships: the user signs in without a workspace and sees only self-service navigation.
4. One active membership: the workspace is selected automatically.
5. Multiple active memberships: the user is redirected to `/select-tenant`.
6. The selected workspace signs the user in with that active tenant.

The workspace-selection payload is held server-side for five minutes behind an opaque token in the
selection URL. It is not placed in the browser history as credentials, and the application sends
`Referrer-Policy: no-referrer` to avoid leaking the token through outbound links. The pending store
is in-process; a multi-instance deployment needs a distributed store for workspace selection.

### Session lifecycle

- The web cookie has a seven-day sliding session.
- Before the access token expires, cookie validation calls the Identity refresh command.
- A successful refresh replaces the claims and rotates the refresh cookie.
- A failed refresh rejects the cookie and redirects to `/login?expired=true`.
- Sign-out revokes the Identity session, clears the cookie and redirects to the signed-out state.

### Authentication routes

| Route | Purpose |
| --- | --- |
| `/login` | Credential entry, error and session-expiry experience. |
| `/select-tenant` | Workspace chooser when multiple memberships exist. |
| `/auth/login` | Server-side sign-in adapter. |
| `/auth/select-tenant` | Server-side workspace selection adapter. |
| `/auth/logout` | Server-side sign-out adapter. |
| `/access-denied` | Friendly authenticated 403 page. |

---

## Authorization UX

- Access tokens carry permission claims that are copied into the web session.
- `PermissionPolicyProvider` creates `Permission:<code>` policies on demand.
- `PermissionAuthorizationHandler` checks the permission claim and fails closed.
- Route attributes use permission policies for each business area.
- `NavigationCatalog.For(permissions)` removes inaccessible items and empty sections.
- `/app/admin/profile` and `/app/admin/sessions` require authentication only.
- Inventory navigation is intentionally hidden.
- Unauthenticated protected page requests are challenged to `/login` with a return URL.
- Authenticated requests without a permission receive `/access-denied`, never raw JSON.

---

## Tenant Experience

- Login validates the requested tenant through Identity membership resolution.
- Multi-tenant users receive a workspace chooser with an explicit active membership list.
- The shell shows the current workspace in the header badge.
- The tenant identifier is shown in shortened form in the badge and fully in the tooltip.
- Tenant creation, renaming, branding and switching without re-authentication are out of scope.

---

## Navigation Strategy

The navigation catalog is the single source of truth:

```text
CRM
  Customers       /app/crm/customers       crm.customers.read
  Organizations   /app/crm/organizations   crm.organizations.read

Sales
  Leads           /app/sales/leads         sales.leads.read
  Opportunities   /app/sales/opportunities sales.opportunities.read

Administration
  Profile         /app/admin/profile       authenticated
  Sessions        /app/admin/sessions      authenticated
```

Business routes live under `/app` so they cannot collide with the `/crm` and `/sales` APIs.
Sections with no visible items are omitted. The first accessible destination is available through
`NavigationCatalog.GetLandingRoute`.

---

## Design System

| Component | Purpose |
| --- | --- |
| `PageHeader` | Page title, subtitle, breadcrumb slot and actions. |
| `Card` | Surface with optional header, actions and footer. |
| `DataTable<T>` | Accessible table with loading and empty states. |
| `FormSection` | Grouped form controls with legend and description. |
| `SearchBox` | Accessible search input with clear action. |
| `Pagination` | Previous/next paging with current-page status. |
| `EmptyState` | Friendly zero-data state with optional action. |
| `ErrorState` | Recoverable error surface with optional action. |
| `LoadingState` | Live status and spinner for asynchronous work. |
| `ConfirmDialog` | Accessible confirmation dialog with Escape handling. |
| `ToastHost` | Live success, info, warning and error notifications. |

Design tokens live in `wwwroot/app.css`: surfaces, borders, text hierarchy, primary/success/warning/
danger tones, spacing, radii, shadows, focus rings and responsive breakpoints. The visual language
is a dark enterprise surface with restrained gradients and high-contrast content.

---

## UX States

The foundation defines patterns for future business screens:

- **Loading:** `LoadingState` and `DataTable.Loading`.
- **Empty:** `EmptyState` and `DataTable` zero rows.
- **Validation:** field labels, `.validation-message`, alert surface and server error mapping.
- **API errors:** `ErrorState`, friendly problem responses and retry actions.
- **Concurrency conflicts:** the existing `409` conflict response maps to a recoverable error
  surface; business screens must re-read before retrying.
- **Success feedback:** `IToastService.Success` through `ToastHost`.
- **Destructive actions:** `ConfirmDialog` with explicit confirmation copy.

---

## Accessibility

- Skip links in both layouts.
- Header, navigation, main and footer landmarks.
- Visible focus rings for keyboard users.
- Semantic headings and table captions.
- Accessible labels for search, login, workspace selection and icon buttons.
- `aria-current`, `aria-expanded`, `aria-live`, `role="dialog"` and `aria-modal` where relevant.
- Escape closes the confirmation dialog.
- Mobile navigation is removed from the accessibility tree while closed.
- No reliance on colour alone for error/success meaning.

---

## Testing

`tests/Integration/BSX.Web.IntegrationTests/WebFoundation` covers:

- Design-system component rendering.
- Navigation visibility by permission and Inventory exclusion.
- Permission policy and handler behavior.
- Login, tenant selection, logout and protected-route redirects.
- Friendly access-denied behavior.
- Accessibility smoke checks for the sign-in page.

The existing 100 API-level integration tests continue to validate JWT-protected Identity, CRM and
Sales endpoints after the smart authentication scheme change.

---

## Future Work

- Business screens for CRM and Sales.
- Session list/revoke UI, profile editing and password changes.
- Tenant settings, display names and branding.
- Distributed pending-login storage for multi-instance deployments.
- A server-side API client/token accessor for business screens that call module APIs.
- Broader browser, mobile and assistive-technology testing.

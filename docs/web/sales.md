# Sales Web Experience

Status: **Lead and opportunity workflows implemented.**

This document covers the first complete Sales user experience in `BSX.Web`, including manual
verification. Inventory, reporting and analytics are intentionally out of scope.

---

## Scope

Implemented:

- Lead list, search, paging, detail, create and edit.
- Lead lifecycle: mark contacted, qualify and disqualify.
- Opportunity list, search, paging, detail, create from a qualified lead, and edit.
- Opportunity lifecycle: advance stage, mark won and mark lost.
- Searchable CRM customer and organization pickers.
- Revenue, probability, expected close date and weighted forecast display.
- Stage timeline, lead status timeline, permission-aware actions and confirmations.
- Loading, empty, validation, success, conflict and failure experiences.
- Responsive layouts and keyboard-accessible forms, dialogs and action groups.

Not implemented:

- Quotes, orders, invoices, pricing or forecasting engines.
- Reporting, analytics or dashboards.
- Inventory.
- Bulk Sales actions or opportunity deletion.

---

## Routes

| Route | Purpose |
| --- | --- |
| `/app/sales/leads` | Lead search and paging. |
| `/app/sales/leads/new` | Create lead. |
| `/app/sales/leads/{id}` | Lead detail, lifecycle actions and CRM context. |
| `/app/sales/leads/{id}/edit` | Edit lead details and notes. |
| `/app/sales/opportunities` | Opportunity search and paging. |
| `/app/sales/opportunities/new` | Convert a qualified lead into an opportunity. |
| `/app/sales/opportunities/new?leadId={id}` | Prefilled conversion from lead detail. |
| `/app/sales/opportunities/{id}` | Opportunity detail, forecast and pipeline actions. |
| `/app/sales/opportunities/{id}/edit` | Edit an open opportunity. |

Navigation is provided by the existing permission-aware catalog:

```text
Sales
  Leads
  Opportunities
```

---

## Permissions

| Capability | Permission |
| --- | --- |
| View leads | `sales.leads.read` |
| Create leads | `sales.leads.create` |
| Edit leads and run lead lifecycle actions | `sales.leads.update` |
| View opportunities | `sales.opportunities.read` |
| Create opportunities from qualified leads | `sales.opportunities.create` |
| Edit opportunities and run lifecycle actions | `sales.opportunities.update` |
| Search/select CRM customers and organizations | `crm.customers.read` / `crm.organizations.read` |

Events and commands are re-checked in the Sales application layer. Hidden actions are not the
security boundary.

---

## Lead Workflow

### List

- Case-insensitive title search with shareable paging URLs.
- Status badges for New, Contacted, Qualified and Disqualified.
- Compact links to the linked CRM customer and organization.
- New lead is visible only with `sales.leads.create`.

### Create and edit

- Title is required and limited to 200 characters.
- Notes are optional and limited to 2,000 characters.
- Customer and organization links are optional while the lead is a prospect.
- CRM lookups are searchable and available only when the user can read the corresponding CRM
  resource.
- Details are editable only while the lead is New or Contacted. Only notes remain editable after
  qualification or disqualification.
- Invalid CRM references, validation failures and optimistic concurrency conflicts are surfaced as
  readable messages.

### Lifecycle

| Action | Allowed from | Confirmation |
| --- | --- | --- |
| Mark contacted | New | No |
| Qualify | Contacted | Yes |
| Disqualify | New or Contacted | Yes |

- Qualification enables conversion to an opportunity.
- Disqualified and qualified are terminal. Disqualified leads cannot be qualified later.
- Status and lifecycle steps are shown as a progress timeline.

---

## Opportunity Workflow

### List

- Title search, or customer ID search when the term is a GUID.
- Stage badges, revenue, probability, expected close date and weighted forecast.
- New opportunity is visible only with `sales.opportunities.create`.

### Create

1. Open a qualified lead and choose **Create opportunity**, or open
   **Sales > Opportunities > New opportunity** and select a qualified lead.
2. The lead title and existing customer/organization links are prefilled.
3. A customer is required.
4. Enter amount, currency, probability and expected close date.
5. The weighted forecast preview updates from amount × probability.
6. Only qualified leads can be converted, and each qualified lead can produce one opportunity.

### Detail and timeline

The opportunity detail shows:

- Amount and currency.
- Probability.
- Expected close date.
- Weighted forecast.
- Customer, organization and source-lead links.
- A stage timeline: Discovery → Qualification → Proposal → Negotiation → Won/Lost.

### Lifecycle

| Action | Allowed from | Confirmation |
| --- | --- | --- |
| Advance stage | Discovery, Qualification or Proposal | No |
| Mark won | Negotiation only | Yes |
| Mark lost | Any open stage | Yes |

- Negotiation cannot advance further; it must be won or lost.
- Won sets probability to 100%. Lost sets probability to 0%.
- Won and Lost are terminal. Edit, advance and further lifecycle actions are hidden.
- The timeline marks completed, current and terminal stages and exposes the current step with
  `aria-current`.

---

## CRM Integration

The shared CRM lookup picker provides:

- Search by customer name/code or organization name/address.
- Permission-aware behavior when CRM read access is missing.
- Selection and clearing for optional lead links.
- Required customer selection for opportunities.
- Clear validation when a selected CRM record no longer exists.

CRM reference validation remains in Sales Infrastructure through `CRM.Contracts`. Invalid
references are mapped to `SALES.Customer.NotFound` or `SALES.Organization.NotFound` in the web
layer rather than surfacing an unhandled exception.

---

## UX and Accessibility

- Form fields use labels, hints and field-level validation messages.
- Searchable results are keyboard-accessible buttons with `aria-pressed`.
- Lead and stage timelines expose `aria-current="step"`.
- Confirmation dialogs use `role="dialog"`, `aria-modal`, Escape-to-cancel, focus on open and a busy
  state while the mutation runs.
- Status is not conveyed by colour alone.
- Tables use captions and scoped headers; narrow layouts scroll the table container.
- All lifecycle actions are available in source order to keyboard users.
- Success operations use toasts; recoverable failures use alerts with reload actions.

---

## Errors and Concurrency

`BSX.Web` dispatches Sales requests in-process through `WebCurrentUser`, which supplies the circuit
principal, active tenant and permissions.

`SalesWebResults` maps:

- `ConcurrencyConflictException` → `SALES.ConcurrencyConflict`.
- `CrmReferenceValidationException` → `SALES.Customer.NotFound` / `SALES.Organization.NotFound`.

Forms and lifecycle actions show reload links for conflicts. No stale write is silently retried.
Domain conflicts such as invalid transitions, terminal opportunities or duplicate conversion are
shown as readable messages.

---

## Testing

`tests/Integration/BSX.Web.IntegrationTests/WebFoundation` covers:

- Lead and opportunity list/detail/create/edit rendering.
- Lead lifecycle: contact, qualify, terminal editing and disqualification rules.
- Opportunity lifecycle: conversion, staged advancement, won and lost terminal behavior.
- CRM lookup resolution and invalid-reference handling.
- Optimistic-concurrency mapping through `SalesWebResults`.
- Sales reader permissions and absence of mutation controls.
- Authentication, authorization and not-found routes.
- Form component rendering, forecast preview, stage timeline and form-model validation.

Run the web suite:

```powershell
dotnet test tests/Integration/BSX.Web.IntegrationTests/BSX.Web.IntegrationTests.csproj -c Release
```

Run everything:

```powershell
dotnet test BSX.Platform.sln -c Release
```

---

## Manual Verification

### 1. Prerequisites

- .NET 10 SDK.
- PostgreSQL 15+.
- EF Core tool restored from the repository manifest.

Optional local PostgreSQL container:

```powershell
docker run --name bsx-postgres `
  -e POSTGRES_PASSWORD=postgres `
  -p 5432:5432 `
  -d postgres:16
```

### 2. Configure the database

```powershell
$connection = "Host=localhost;Port=5432;Database=bsx_identity;Username=postgres;Password=postgres"

$env:ConnectionStrings__Identity = $connection
$env:ConnectionStrings__Crm = $connection
$env:ConnectionStrings__Sales = $connection
```

### 3. Apply migrations

```powershell
dotnet tool restore

dotnet ef database update `
  --project src/Modules/Identity/BSX.Modules.Identity.Infrastructure `
  -- --connection $connection

dotnet ef database update `
  --project src/Modules/Crm/BSX.Modules.Crm.Infrastructure `
  -- --connection $connection

dotnet ef database update `
  --project src/Modules/Sales/BSX.Modules.Sales.Infrastructure `
  -- --connection $connection
```

### 4. Enable demo identity and start

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:Identity__Permissions__SynchronizeOnStartup = "true"
$env:Demo__SeedIdentity = "true"

dotnet run --project src/BSX.Web --launch-profile http
```

Open `http://localhost:5272`.

### 5. Demo credentials

| Field | Value |
| --- | --- |
| Email | `admin@bsx.local` |
| Password | `Passw0rd!` |
| Workspace | Single seeded demo workspace, selected automatically |

The demo seed is Development-only, idempotent and grants the Administrator system role all
registered module permissions, including Sales.

### 6. Navigation

```text
/login
  -> workspace home
  -> Sales > Leads
  -> Sales > Opportunities
```

### 7. Sales verification checklist

#### Lead lifecycle

- [ ] Open **Sales > Leads**; list, search and **New lead** are visible.
- [ ] Create a lead with title, notes and a CRM customer/organization from the searchable pickers.
- [ ] Search by title and verify paging/query-string behavior.
- [ ] Open the lead detail; verify the customer/organization names and notes.
- [ ] Use **Mark contacted**; verify the status timeline advances.
- [ ] Use **Qualify lead**, cancel the confirmation once, then confirm; verify **Qualified** and
      **Create opportunity**.
- [ ] Create another lead and disqualify it; verify terminal state and hidden lifecycle actions.
- [ ] Edit a New or Contacted lead; verify title, notes and CRM links.
- [ ] Qualify a lead, edit it, and verify only notes can be changed.
- [ ] Try qualifying a New lead through the API or a stale tab; verify a readable conflict.

#### Opportunity lifecycle

- [ ] From a qualified lead, choose **Create opportunity**.
- [ ] Verify the lead title and CRM context are prefilled, select a customer if needed.
- [ ] Enter amount, currency, probability and expected close date; verify the weighted preview.
- [ ] Create the opportunity and verify revenue, probability, close date and stage timeline.
- [ ] Advance Discovery → Qualification → Proposal → Negotiation.
- [ ] Verify Negotiation cannot advance again.
- [ ] Mark **Won**, cancel once, then confirm; verify terminal state, 100% probability and timeline.
- [ ] Create another opportunity and mark it **Lost** from an open stage; verify 0% probability.
- [ ] Attempt to edit a Won or Lost opportunity; verify editing/lifecycle actions are unavailable.
- [ ] Attempt to convert the same qualified lead twice; verify the duplicate conversion message.
- [ ] Open an opportunity edit form in two tabs, save tab A, then save tab B; verify tab B shows the
      concurrency message and does not overwrite silently.

#### CRM pickers

- [ ] Search a customer by name or code and select it.
- [ ] Search an organization by name and select/clear it.
- [ ] Verify no CRM records are exposed to a user without the corresponding CRM read permission.
- [ ] Enter an opportunity without a customer; verify the required field validation.

#### UX and accessibility

- [ ] Tab through the lead and opportunity forms; labels and validation remain associated.
- [ ] Use Escape to close confirmation dialogs and verify focus is visible.
- [ ] Verify status is understandable without colour.
- [ ] Verify the stage timeline exposes the current step to assistive technology.
- [ ] Resize to a narrow viewport and verify forms, tables and timelines remain usable.
- [ ] Sign out and verify Sales routes return to `/login`.

### 8. Read-only verification

The integration test factory seeds `sales-reader@example.com` with read-only Sales and CRM
permissions. Automated tests verify that this user sees Sales data but no create, edit, stage or
lifecycle controls. The local demo seed intentionally creates only the administrator.

---

## Limitations

- Automated tests cover workflows through the web application context; browser-level interaction
  automation for interactive Blazor forms is still future work.
- Opportunity create from the list uses a searchable qualified-lead picker rather than a remote
  type-ahead.
- Sales has no ETag/If-Match contract; optimistic concurrency is server-side and represented as a
  conflict result.
- Quotes, orders, invoices, pricing, reporting and analytics remain out of scope.

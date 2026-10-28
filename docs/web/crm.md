# CRM Web Experience

Status: **Customer, organization, contact and address workflows implemented.**

This document is the functional and manual-verification guide for the CRM web experience in
`BSX.Web`. It extends the original customer journey documented in
[crm-customers.md](crm-customers.md).

---

## Scope

Implemented:

- Customer list, detail, create and edit.
- Organization list, detail, create and edit, including an optional primary address.
- Customer organization assignment and clearing.
- Contact add, edit, remove and set-primary.
- Address add, edit, remove and set-default with Billing/Shipping/Other.
- Search and paging for customers and organizations.
- Permission-aware actions, loading, empty, validation, success, failure, conflict and confirmation
  experiences.
- Keyboard-accessible controls, labelled forms, focus-managed dialogs and responsive layouts.

Not implemented:

- Inventory screens.
- Customer activation/deactivation UI.
- Organization deletion or organization-to-customer lists.
- Bulk operations, reporting, analytics or dashboards.

---

## Routes

| Route | Purpose |
| --- | --- |
| `/app/crm/customers` | Customer search and paging. |
| `/app/crm/customers/new` | Create customer. |
| `/app/crm/customers/{id}` | Customer detail with assignment, contacts and addresses. |
| `/app/crm/customers/{id}/edit` | Edit customer name and tax identifier. |
| `/app/crm/customers/{id}/contacts/new` | Add contact. |
| `/app/crm/customers/{id}/contacts/{contactId}/edit` | Edit contact. |
| `/app/crm/customers/{id}/addresses/new` | Add Billing/Shipping/Other address. |
| `/app/crm/customers/{id}/addresses/{addressId}/edit` | Edit address. |
| `/app/crm/organizations` | Organization search and paging. |
| `/app/crm/organizations/new` | Create organization. |
| `/app/crm/organizations/{id}` | Organization detail and primary address. |
| `/app/crm/organizations/{id}/edit` | Edit organization and primary address. |

Organization assignment, contact removal, set-primary, address removal and set-default are actions
on the customer detail screen. Removal and unassignment require confirmation.

---

## Permissions

| Capability | Permission |
| --- | --- |
| View customers, contacts and addresses | `crm.customers.read` |
| Create customers | `crm.customers.create` |
| Edit customers, assign/clear organizations, manage contacts and addresses | `crm.customers.update` |
| View organizations and the organization selector | `crm.organizations.read` |
| Create organizations | `crm.organizations.create` |
| Edit organizations | `crm.organizations.update` |

Permissions gate route access, action visibility and application commands. The application layer
re-checks every command and query; hidden UI is not the security boundary.

---

## Organization Workflows

### List and search

- Case-insensitive search by organization name or tax identifier.
- 20-row default paging with shareable query URLs.
- Shows the primary address city and country when present.
- New organization is visible only with `crm.organizations.create`.

### Create and edit

- Name is required and limited to 200 characters.
- Tax identifier is optional, normalized to upper-case and limited to 50 characters.
- The primary address is optional. Address line 1, city and country are required when included.
- Unchecking **Include a primary address** on edit clears the organization address.
- Duplicate values, invalid input and optimistic-concurrency conflicts are mapped to readable
  messages.
- Success shows a toast and returns to the organization detail.

---

## Customer Organization Assignment

From customer detail, the Organization card:

- Shows the assigned organization name and link when the user can read organizations.
- Offers a searchable selector for assignment or reassignment.
- Limits the selector to the first 10 matches and asks the user to refine the search.
- Disables assignment for deactivated customers.
- Clears the assignment only after an explicit confirmation dialog.
- Shows the current assignment and prevents selecting the already-assigned organization.

Assignment and clearing call the existing application commands, so tenant isolation, active-state
rules and concurrency remain authoritative in CRM.

---

## Contact Workflows

From customer detail:

- Contacts display name, email, phone and a prominent **Primary** badge.
- **Add contact** is available only for active customers.
- A contact must have an email address or phone number; the form surfaces reachability errors
  before and after submission.
- Duplicate email for the same customer is reported as a conflict.
- **Set primary** moves the single primary marker to the selected contact.
- **Remove** requires confirmation.
- Contact edit shows current details and keeps the primary assignment intact.

The contact limit is 50 per customer; the domain returns a readable error when reached.

---

## Address Workflows

From customer detail:

- Addresses show type, full postal address and a **Default** badge.
- **Add address** supports Billing, Shipping and Other.
- Address line 1, city and country are required.
- The `(address, type)` uniqueness rule is enforced by the domain; duplicates show a readable
  conflict instead of a retry loop.
- The first address becomes the default automatically.
- **Set default** moves the single default marker.
- **Remove** requires confirmation, and the domain promotes another address to default when needed.
- Address editing supports changing both the postal values and the type.
- The address limit is 20 per customer.

---

## UX and Accessibility

- Loading states for pages, searches and mutations.
- Empty states for no customers, organizations, contacts or addresses.
- Success toasts for create, edit, assignment, contact and address mutations.
- Validation messages are associated with their fields.
- Confirm dialogs use `role="dialog"`, `aria-modal`, focus on open, Escape-to-cancel and a busy
  state that disables actions while the mutation is running.
- Searchable organization results use buttons with `aria-pressed` state.
- Primary/default/status indicators do not rely on colour alone.
- Tables use captions, scoped headers and labelled action buttons.
- Layouts collapse to one column on narrow screens; tables keep a horizontal scroll container.
- All actions are reachable by keyboard in source order.

---

## Errors and Concurrency

`BSX.Web` dispatches CRM application requests in-process through `WebCurrentUser`, which supplies
the circuit principal, active tenant and permissions to the application pipeline.

- Domain/application failures remain `Result` values and are mapped to concise web messages.
- Optimistic concurrency exceptions are converted by `CrmWebResults` to the stable
  `CRM.ConcurrencyConflict` result because interactive Blazor has no HTTP response to map.
- Forms and inline actions show a reload action for conflicts; nothing is silently retried.
- Not-found customer/organization resources return a friendly not-found experience and a `404` for
  direct requests.
- Missing permissions route to `/access-denied`.
- Unauthenticated requests redirect to `/login` with a return URL.

---

## Data Ownership

CRM remains the authority for customers, organizations, contacts and addresses. The web layer does
not duplicate validation or tenant rules. It only:

- collects user input,
- dispatches the existing commands and queries,
- presents the result,
- reloads the affected projection after a mutation.

---

## Testing

`tests/Integration/BSX.Web.IntegrationTests/WebFoundation` covers:

- Organization list, search, detail, create-page and edit-page rendering.
- Customer detail assignment, contact and address management actions.
- Read-only permission behavior and absence of mutation controls.
- Organization route authentication, authorization and not-found behavior.
- Form component rendering and conditional address/primary fields.
- Form model validation: reachability, phone digits, address type, required address fields.
- Organization lifecycle through the web application context.
- Assignment/unassignment lifecycle.
- Contact lifecycle: add, set primary, update, remove.
- Address lifecycle: add, duplicate rejection, set default, update type, remove.
- Optimistic-concurrency mapping through `CrmWebResults`.
- Existing customer journey regression coverage.
- Confirmation dialog busy and accessibility semantics.

Run the web tests:

```powershell
dotnet test tests/Integration/BSX.Web.IntegrationTests/BSX.Web.IntegrationTests.csproj -c Release
```

Run the full suite:

```powershell
dotnet test BSX.Platform.sln -c Release
```

---

## Manual Verification

### 1. Prerequisites

- .NET 10 SDK.
- PostgreSQL 15 or later.
- EF Core tool restored from the repo manifest.
- PowerShell examples below; use equivalent environment-variable syntax in other shells.

Optional local PostgreSQL container:

```powershell
docker run --name bsx-postgres `
  -e POSTGRES_PASSWORD=postgres `
  -p 5432:5432 `
  -d postgres:16
```

### 2. Configure the database

Use one local database for Identity and CRM; CRM uses its own `crm` schema.

```powershell
$connection = "Host=localhost;Port=5432;Database=bsx_identity;Username=postgres;Password=postgres"

$env:ConnectionStrings__Identity = $connection
$env:ConnectionStrings__Crm = $connection
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
```

CRM migrations are always applied out of band; the host does not apply them at startup.

### 4. Enable demo identity seed

The development-only seeder creates one administrator, one demo tenant and the membership needed
to sign in. It is idempotent and refuses to seed outside the Development environment.

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:Demo__SeedIdentity = "true"
```

Permission synchronization must be enabled so the `Administrator` system role exists. It is enabled
by default:

```powershell
$env:Identity__Permissions__SynchronizeOnStartup = "true"
```

### 5. Start the application

```powershell
dotnet run --project src/BSX.Web --launch-profile http
```

Open:

```text
http://localhost:5272
```

### 6. Demo credentials

| Field | Value |
| --- | --- |
| Email | `admin@bsx.local` |
| Password | `Passw0rd!` |
| Workspace | The single seeded demo workspace; selected automatically |

The demo tenant identifier is `11111111-1111-1111-1111-111111111111`.

### 7. Navigation path

```text
/login
  -> workspace home
  -> CRM > Organizations
  -> CRM > Customers
```

The shell shows the current workspace, CRM navigation and the user menu. Sign out from the user
menu when finished.

### 8. CRM verification checklist

#### Organizations

- [ ] Open **CRM > Organizations**; the list, search box and **New organization** are visible.
- [ ] Create an organization with name, tax identifier and a primary address.
- [ ] Confirm the success toast and organization detail, including the formatted address.
- [ ] Search by name and tax identifier; verify paging query strings are preserved.
- [ ] Edit the organization, change the name/tax identifier and save.
- [ ] Edit again, uncheck **Include a primary address**, save, and confirm the address is cleared.
- [ ] Open the same organization edit form in two tabs; save tab A, then save tab B. Tab B must
      show the concurrency message and a reload action, not overwrite silently.

#### Customer organization assignment

- [ ] Create or open a customer.
- [ ] With no organization, use **Assign organization**, search by name or tax identifier, select a
      result and assign it.
- [ ] Confirm the organization name appears on the card and links to organization detail.
- [ ] Use **Change organization**, choose another organization and confirm the update.
- [ ] Use **Clear organization**, cancel the confirmation once, then confirm it; verify the
      assignment is removed.
- [ ] Deactivate a customer through the API or an existing data fixture and confirm assignment is
      disabled while clearing remains available.

#### Contacts

- [ ] Open a customer and click **Add contact**.
- [ ] Submit with no email or phone; confirm the reachability validation message.
- [ ] Submit a phone with fewer than 5 digits; confirm the phone validation message.
- [ ] Add a primary contact with email and phone; verify the **Primary** badge.
- [ ] Add a second contact with a different email; verify it is not primary.
- [ ] Use **Set primary** on the second contact; verify exactly one primary remains.
- [ ] Edit a contact and verify the values persist.
- [ ] Try adding a contact with the same email; verify a readable duplicate-email error.
- [ ] Remove a contact, cancel once, then confirm; verify removal and the success toast.
- [ ] On a deactivated customer, verify **Add contact** is hidden.

#### Addresses

- [ ] Add a Billing address; verify it becomes the default automatically.
- [ ] Add the same address and type again; verify the duplicate-address error.
- [ ] Add the same address as Shipping; verify it is accepted.
- [ ] Use **Set default** on Shipping; verify the default badge moves.
- [ ] Edit the Billing address, change its line and type to Other, and save.
- [ ] Remove Shipping, cancel once, then confirm; verify removal and default promotion behavior.
- [ ] Verify long addresses wrap without page-level horizontal scrolling.

#### Customers and regressions

- [ ] Customer search by code and name still works.
- [ ] Customer create/edit still works with immutable code and concurrency handling.
- [ ] Organization and Customer pages remain usable on a narrow browser viewport.
- [ ] Tab through forms and dialogs; focus is visible and Escape closes confirmation dialogs.
- [ ] Sign out and confirm protected CRM routes return to `/login`.

### 9. Read-only permission verification

The integration test factory seeds `crm-reader@example.com` with only customer and organization
read permissions. Automated tests verify that this user sees CRM data but no create, edit, assign,
contact or address mutation controls. Manual browser verification with the seeded demo does not
include a read-only user because the demo seed intentionally creates only the administrator.

---

## Limitations

- Customer contact/address forms use interactive Blazor Server events; browser-level automation is
  not part of the current test suite, although the underlying web application workflows are covered
  through integration tests.
- The organization selector shows the first 10 matches and requires search refinement for larger
  lists; it is not a virtualized type-ahead.
- Organization deletion, assignment history, bulk operations and reporting are out of scope.
- CRM has no ETag/If-Match contract; optimistic concurrency is server-side and represented as a
  conflict result.

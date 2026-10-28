# CRM Customers

Status: **First customer-facing CRM journey implemented.**

This slice delivers the customer experience on top of the web foundation and the existing CRM
application layer. Organizations, contacts, addresses and Sales remain out of scope.

---

## Routes

| Route | Purpose | Permission |
| --- | --- | --- |
| `/app/crm/customers` | Search and page the customer directory. | `crm.customers.read` |
| `/app/crm/customers/new` | Create a customer. | `crm.customers.create` |
| `/app/crm/customers/{id}` | Customer overview with contacts and addresses. | `crm.customers.read` |
| `/app/crm/customers/{id}/edit` | Edit the customer name and tax identifier. | `crm.customers.update` |

The New customer action is hidden without `crm.customers.create`; the Edit action is hidden
without `crm.customers.update`. Route policies and application authorization both enforce access.

---

## Customer List

- Server-side search across customer code and name.
- Twenty-row default paging with shareable page URLs.
- Permissions-aware New customer action.
- Status badge, customer name, code and compact organization assignment.
- Loading, empty, error and no-search-results states.
- Search is a normal GET form, so it works with browser reloads and shareable URLs.

The list calls `ListCustomersQuery` through the in-process dispatcher. No aggregate is loaded;
the existing CRM read repository projects the summaries.

---

## Customer Detail

- Name, code, status, tax identifier and organization assignment.
- Contacts table with email, phone and primary indicator.
- Address cards with type and default marker.
- Success banners for `created=true` and `updated=true`.
- Friendly not-found and access-denied experiences.
- Edit action is permission-aware.

Contact and address management is intentionally read-only in this slice.

---

## Create and Edit

Both forms use the shared design-system inputs and field validation.

- Customer code: required, maximum 32 characters, letters/numbers/hyphens/underscores/periods.
- Customer name: required, maximum 200 characters.
- Tax identifier: optional, maximum 50 characters, letters/numbers/periods/slashes/hyphens.
- The code is immutable after creation and is shown as permanent context on the edit screen.
- The customer is created as active.
- Duplicate code, not-found, tenant-required and concurrency conflicts are mapped to actionable,
  non-technical messages.
- A successful create redirects to the detail screen with a confirmation banner.
- A successful edit redirects to the detail screen with a confirmation banner.
- Concurrency conflicts offer a reload action instead of retrying automatically.

The forms dispatch the existing CRM commands, so domain validation, tenant isolation, permission
checks, auditing, outbox writes and concurrency handling remain authoritative in the module.

---

## Data Access and Identity

`BSX.Web` hosts Blazor Server and invokes the CRM application layer directly. The web app is the
composition root and already references the module application assemblies.

The circuit-safe `WebCurrentUser` bridge seeds the authenticated principal into the scoped
application context before a page dispatches a query or command. CRM handlers therefore receive:

- the user identifier and email,
- the active workspace (`tenant_id`),
- the granted permission claims,
- the authenticated state required by the authorization behavior.

HTTP API requests continue to resolve `ICurrentUser` from `HttpContext.User`. No access or refresh
token is exposed to browser JavaScript or stored in component state.

---

## Error Handling

| Condition | Experience |
| --- | --- |
| Duplicate customer code | Inline alert with a request to choose another code. |
| Customer not found | Not-found page; direct requests return `404`. |
| Missing workspace | Request cannot proceed; users are directed back to workspace selection. |
| Concurrent modification | Alert with a reload link; the form is not silently retried. |
| Missing permission | `/access-denied`. |
| Unauthenticated | Challenge to `/login` with a return URL. |

---

## Accessibility and Responsive Behavior

- Landmark navigation and page headings are inherited from the shell.
- Field labels, hints and validation messages are associated with inputs.
- Status is not communicated by colour alone.
- Tables use captions and scoped column headers.
- Success and error banners use live/alert semantics.
- The list and detail layouts collapse to one column on narrow screens.
- Long names, codes and identifiers wrap without horizontal page scrolling.

---

## Tests

`BSX.Web.IntegrationTests/WebFoundation/CrmCustomerWebTests` covers:

- Login and the full customer read journey through the web session.
- List search and result counts.
- Detail rendering, contacts/addresses empty states and success banners.
- Create-page form labels and fields.
- Edit-page prefill and immutable-code context.
- Permission-aware actions and route protection.
- Access denied, unauthenticated redirects and not-found behavior.
- Organizations and Sales placeholders remaining unchanged.

The existing CRM API and application test suites continue to cover command/query behavior,
validation, tenant isolation, concurrency and persistence.

---

## Limitations

- Organizations are not implemented in the UI.
- Contact and address editing is not implemented.
- Customer activation/deactivation is not exposed in this slice.
- Sales and Inventory remain outside the CRM journey.
- Organization assignment is displayed as a compact identifier until an organization picker ships.
- The create/edit forms use interactive Blazor Server event handling; automated browser-level
  interaction testing is still future work.

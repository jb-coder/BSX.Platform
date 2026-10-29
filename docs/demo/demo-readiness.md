# Demo and Pilot Readiness

Status: **Customer-demo and pilot-ready for CRM, Sales and Administration.**

This guide covers the seeded demonstration workspace, the first-run experience and a repeatable
customer walkthrough. The demo seeder is development-only and opt-in. It never runs in Production.

Implemented for this release:

- Idempotent demo data for customers, organizations, leads and opportunities.
- Two active users that are ready to invite into the workspace.
- A guided home experience that explains the customer → lead → opportunity path.
- First-run progress based on real workspace records.
- Contextual next actions from empty states and created records.
- Home navigation and invitation shortcuts.
- Automated coverage for first-run guidance, navigation and demo safety.

Deliberately not implemented:

- Inventory
- Purchasing
- Billing
- Reporting or analytics dashboards

---

## Demo Environment Setup

### 1. Start PostgreSQL

```powershell
docker run --name bsx-postgres `
  -e POSTGRES_PASSWORD=postgres `
  -p 5432:5432 `
  -d postgres:16
```

If the container already exists, start it instead:

```powershell
docker start bsx-postgres
```

### 2. Configure the local database

```powershell
$connection = "Host=localhost;Port=5432;Database=bsx_identity;Username=postgres;Password=postgres"

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ConnectionStrings__Identity = $connection
$env:ConnectionStrings__Crm = $connection
$env:ConnectionStrings__Sales = $connection
```

### 3. Apply the module migrations

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

### 4. Enable the demo workspace

```powershell
$env:Identity__Permissions__SynchronizeOnStartup = "true"
$env:Demo__SeedIdentity = "true"
$env:Demo__SeedSampleData = "true"
```

| Setting | Purpose |
| --- | --- |
| `Demo:SeedIdentity` | Creates the demo administrator, workspace and membership. |
| `Demo:SeedSampleData` | Creates the sample CRM, Sales and invite-ready users. |
| `Identity:Permissions:SynchronizeOnStartup` | Ensures the system roles contain the current permission catalog. |

Both demo flags are ignored outside the Development environment. The sample seeder waits for the
demo administrator, is idempotent and uses the normal application commands.

### 5. Start the application

```powershell
dotnet run --project src/BSX.Web --launch-profile http
```

Open `http://localhost:5272`.

### Demo credentials

| Account | Email | Password | Workspace access |
| --- | --- | --- | --- |
| Demo administrator | `admin@bsx.local` | `Passw0rd!` | Member with Administrator system role |
| Demo invitee | `alex.rivera@bsx.local` | `Passw0rd!` | No workspace membership |
| Demo invitee | `priya.shah@bsx.local` | `Passw0rd!` | No workspace membership |

The invitee accounts are active platform users without workspace membership, so they appear in the
invitation picker without failing with an “already a member” conflict.

---

## Seeded Demo Data

| Area | Records |
| --- | --- |
| Organizations | Northwind Group, Contoso Holdings |
| Customers | Northwind Traders (`DEMO-NW-001`), Contoso Ltd (`DEMO-CT-002`), Fabrikam (`DEMO-FB-003`) |
| Leads | Northwind annual services renewal (Qualified), Contoso analytics workshop (New), Fabrikam warehouse expansion (Qualified), Tailspin onboarding project (Contacted) |
| Opportunities | Northwind annual services renewal (Discovery), Fabrikam warehouse expansion (Proposal) |
| Invite-ready users | Alex Rivera, Priya Shah |

Each seeded customer includes a primary contact and billing address. The two qualified leads already
have opportunities, so the Opportunities page demonstrates stage and forecast behavior immediately.

On restart, existing records are detected and left unchanged. The seeder does not reset or overwrite
records edited during a demo.

---

## First-Run Experience

The authenticated home page is `/`.

It provides:

1. A short explanation of what BSX does.
2. The customer → lead → opportunity → team workflow.
3. A four-step Getting Started checklist backed by real workspace data.
4. A direct action for the next incomplete step.
5. Permission-aware workspace areas.

The four steps are:

1. **Add your first customer** — opens `/app/crm/customers/new`.
2. **Capture a lead** — opens `/app/sales/leads/new`.
3. **Create an opportunity** — points to a qualified lead when one is ready, otherwise explains how
   to contact and qualify a lead.
4. **Invite your team** — opens `/app/admin/memberships/invite`.

A step is complete only when the corresponding data exists. The invitation step is complete when the
workspace has more than the current member.

Additional guidance in the product:

- Empty customer, lead and opportunity lists explain the business purpose and next action.
- A customer creation confirmation links directly to a pre-linked “Create lead” form.
- The opportunity form explains that an opportunity starts from a qualified lead.
- A user detail page offers “Invite to workspace”.
- The invitation page links to user creation when the person does not exist yet.
- The sidebar contains a permission-aware **Home** destination.

---

## Ten-Minute Customer Walkthrough

### 1. Sign in and orient

1. Open `/login`.
2. Sign in as `admin@bsx.local` / `Passw0rd!`.
3. Review the home workflow and Getting Started checklist.
4. Point out that the checklist state comes from the workspace, not a fixed demo script.

### 2. Create a customer

1. Select **Create customer** from home, or **CRM > Customers > New customer**.
2. Use a memorable code such as `DEMO-ACME-004`.
3. Name the customer `Acme Demonstration`.
4. Save.
5. On the confirmation, use **Next step: capture a lead for this customer**.

### 3. Capture and qualify a lead

1. The new lead form is pre-linked to the customer.
2. Enter a title such as `Acme annual rollout`.
3. Add qualification context in Notes.
4. Save the lead.
5. Select **Mark contacted**.
6. Select **Qualify lead** and confirm.

### 4. Create the opportunity

1. From the qualified lead, select **Create opportunity**.
2. Use the lead title or a customer-specific title.
3. Set an amount, currency, probability and expected close date.
4. Save.
5. Advance the stage and show the stage timeline and weighted forecast.

### 5. Invite a teammate

1. Open **Administration > Memberships**.
2. Select **Invite member**.
3. Choose `Alex Rivera` or `Priya Shah`.
4. Create the invitation.
5. Accept the invitation from the membership list to activate access.
6. Optionally open **Roles** to show how permissions are grouped and assigned.

### 6. Show the remaining experience

1. **CRM > Organizations** — group related customers.
2. **CRM > Customers** — open a customer, add contacts and addresses.
3. **Sales > Opportunities** — show both seeded stages.
4. **Administration > Users, Roles, Sessions** — show access and session controls.
5. Refresh the home page and show that Getting Started progress has updated.

---

## Pilot Handoff Checklist

Before a pilot:

- [ ] Use a dedicated PostgreSQL database.
- [ ] Apply Identity, CRM and Sales migrations out of band.
- [ ] Keep `Demo:SeedIdentity` and `Demo:SeedSampleData` disabled.
- [ ] Configure production JWT signing keys and `AllowedHosts`.
- [ ] Set `Identity:Permissions:SynchronizeOnStartup=true` for the release that introduces permissions.
- [ ] Verify `/health/live` and `/health/ready`.
- [ ] Create the pilot administrator through an approved operational process.
- [ ] Create pilot users and invite them through Administration.
- [ ] Confirm memberships, roles and session visibility with the pilot owner.
- [ ] Take a database backup before onboarding pilot data.
- [ ] Agree on support contacts and data-retention expectations.

See [production readiness](../operations/production-readiness.md), [configuration](../operations/configuration.md)
and [administration](../web/administration.md) for operational detail.

---

## Success Criteria Verification

A first-time user can complete the following without external guidance:

- [ ] Sign in.
- [ ] Understand what the product does from the home workflow.
- [ ] Create a customer from home or CRM.
- [ ] Create a lead, including from the created customer.
- [ ] Contact and qualify the lead.
- [ ] Convert the qualified lead into an opportunity.
- [ ] Invite a user and accept the membership.

Automated coverage:

- `HomeExperienceTests` verifies first-run step ordering, progress, permission filtering and
  qualified-lead conversion links.
- `NavigationCatalogTests` verifies Home, permission filtering and Inventory exclusion.
- `WebFoundationEndpointsTests` verifies the rendered home experience and authenticated shell.
- Existing CRM, Sales and Administration workflow suites verify the underlying create and lifecycle
  journeys.

---

## Troubleshooting

### The home page has no demo records

1. Confirm `ASPNETCORE_ENVIRONMENT=Development`.
2. Confirm `Demo__SeedIdentity=true` and `Demo__SeedSampleData=true`.
3. Confirm all three module schemas are migrated.
4. Check the application log for `Demo sample data is ready` or the corresponding warning.
5. Confirm the signed-in account has CRM, Sales and membership read permissions.

### A demo invitee cannot be invited

- The user must be active.
- The user must not already have a membership in the active workspace.
- Use `admin@bsx.local` for the demonstration.
- If someone already invited Alex or Priya, use the other seeded invitee or create a new user.

### The opportunity page says a qualified lead is required

This is expected. Opportunities are created from qualified leads. Open **Sales > Leads**, contact
and qualify a lead, then select **Create opportunity** from the lead.

### Demo data should be reset

For a clean demonstration, use a separate database rather than deleting live rows:

```powershell
docker exec bsx-postgres createdb -U postgres bsx_demo_clean
```

Point the three connection strings at `bsx_demo_clean`, apply the migrations again and restart with
the demo flags enabled. Keep the original database for reference.

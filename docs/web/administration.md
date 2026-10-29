# Administration Web Experience

Status: **User, role, membership and session administration implemented.**

This document covers the administration experience in `BSX.Web` and the manual verification steps
for local evaluation.

---

## Scope

Implemented:

- User list, search, paging, detail, create, edit, activate and deactivate.
- Role list, detail, create, edit, permission assignment and system-role protection.
- Workspace membership list, invitation, role change, invitation acceptance and removal.
- Current-user session list, current-session marker, individual revocation and revoke-all.
- Permission-aware navigation and actions.
- Loading, empty, validation, success, error and concurrency feedback.
- Accessible forms, permission selectors, dialogs and responsive layouts.

Not implemented:

- Service accounts and API keys.
- User lock/unlock and administrative password reset UI.
- Platform-role assignment directly to users.
- Role deletion UI.
- Tenant creation, branding or tenant switching without re-authentication.
- Inventory.

---

## Routes

| Route | Purpose |
| --- | --- |
| `/app/admin/users` | User search and paging. |
| `/app/admin/users/new` | Create user. |
| `/app/admin/users/{id}` | User detail and lifecycle actions. |
| `/app/admin/users/{id}/edit` | Edit user name and email. |
| `/app/admin/roles` | Role list, including system and workspace roles. |
| `/app/admin/roles/new` | Create workspace role and assign permissions. |
| `/app/admin/roles/{id}` | Role detail and grouped permissions. |
| `/app/admin/roles/{id}/edit` | Edit a non-system role. |
| `/app/admin/roles/{id}/permissions` | Search and assign role permissions. |
| `/app/admin/memberships` | Active workspace membership list. |
| `/app/admin/memberships/invite` | Invite an existing user. |
| `/app/admin/memberships/{id}/role` | Change a member's role. |
| `/app/admin/sessions` | Current user's sessions and revoke-all action. |

Administration navigation:

```text
Administration
  Users          identity.users.read
  Roles          identity.roles.read
  Memberships    identity.memberships.read
  Sessions       authenticated
```

---

## Permission Model

Administration uses the existing Identity permission catalog:

```text
identity.users.read
identity.users.create
identity.users.update
identity.users.manage-status
identity.users.assign-role

identity.roles.read
identity.roles.create
identity.roles.update
identity.roles.set-permissions

identity.permissions.read

identity.memberships.read
identity.memberships.manage
```

The platform `Administrator` system role receives all catalog permissions during startup
synchronization. The `Auditor` system role receives read permissions according to the existing
system-role rules.

System roles are displayed but cannot be renamed, re-described or have permissions changed. Role
authorization is re-checked by Identity application commands; hiding controls is not the security
boundary.

---

## Users

### List and detail

- Search by name or email with server-side paging.
- Status badge for `Pending`, `Active`, `Locked` and `Disabled`.
- Detail shows email, status, MFA state, registration timestamp and direct platform roles.
- The current user is marked with a **You** badge.

### Create

- Name, email and initial password are required.
- New accounts start in `Pending` status.
- Passwords are stored only through the Identity password hasher.
- The create action requires `identity.users.create`.

### Edit

- Name and email can be changed.
- Email uniqueness is enforced by Identity.
- The edit action requires `identity.users.update`.

### Activate and deactivate

- Activation requires `identity.users.manage-status`.
- Deactivation requires confirmation and revokes refresh sessions through Identity.
- Administrators cannot deactivate their own account from the UI.
- Conflicts and credential problems are reported without losing the page context.

---

## Roles

### List and detail

- Shows both workspace roles and platform/system roles.
- System roles are marked and read-only.
- Search filters by role name and description on the client.
- Detail groups the role's permissions by module and shows human-readable descriptions from the
  permission catalog.

### Create

- Name and optional description.
- Permissions are selected from the catalog, grouped by module.
- Permission search filters by code or description.
- Requires `identity.roles.create`.

### Edit

- Name and description for non-system roles.
- Requires `identity.roles.update`.
- System roles cannot be edited.

### Permission assignment

- Full permission catalog grouped by module.
- Search by permission code or description.
- Selected count is visible.
- Replaces the role's permission set through `SetRolePermissionsCommand`.
- Requires `identity.roles.set-permissions`.

---

## Workspace Memberships

### List

- Shows member name, email, membership status, assigned role names and joined date.
- Current-user membership is marked.
- Search filters by member name or email.
- Client-side paging keeps the list manageable.

### Invite

- Select an existing user using a searchable user picker.
- Sends `InviteUserToTenantCommand` for the active workspace.
- Membership starts as `Invited`; it does not grant workspace access until accepted.
- Requires `identity.memberships.manage` and user read access for the picker.

### Accept invitation

- The administration UI can accept an `Invited` membership after confirmation, activating workspace
  access.
- Requires `identity.memberships.manage`.

### Change role

- Replaces the member's current role assignment with one selected workspace or platform role.
- Supports removing all roles by selecting **No role**.
- Requires `identity.memberships.manage`.

### Remove

- Requires confirmation.
- Marks the membership `Removed` and clears its role assignments.
- Self-removal is hidden in the UI.
- Requires `identity.memberships.manage`.

---

## Sessions

- Lists sessions for the current user only.
- The current session is marked **Current**.
- Device user agent and IP are displayed when available.
- Individual active sessions other than the current one can be revoked after confirmation.
- **Revoke all sessions** signs out every device, including the current one, through
  `/auth/logout-all` and clears the web cookies.

---

## Scope Isolation

Administration commands and queries use `AdministrationDispatcher`, which creates a fresh DI scope
per operation and seeds `WebCurrentUser` from the circuit principal. This avoids stale EF tracked
entities across administration actions while keeping the existing tenant and permission pipeline
authoritative.

---

## Manual Verification

### Prerequisites

1. PostgreSQL running and Identity, CRM and Sales migrations applied.
2. `ASPNETCORE_ENVIRONMENT=Development`.
3. `Identity__Permissions__SynchronizeOnStartup=true`.
4. `Demo__SeedIdentity=true`.
5. `Demo__SeedSampleData=true` for the invite-ready demo users and sample CRM/Sales data.
6. Application started with `dotnet run --project src/BSX.Web --launch-profile http`.

Sign in at `http://localhost:5272` with:

```text
Email:    admin@bsx.local
Password: Passw0rd!
```

### Navigation paths

```text
/app/admin/users
/app/admin/roles
/app/admin/memberships
/app/admin/sessions
```

### Demo scenarios

1. **User lifecycle**
   - Create a user; verify Pending status.
   - Open the detail, activate the user, edit the name/email, then deactivate with confirmation.
2. **Role lifecycle**
   - Create a workspace role with a mix of CRM, Sales and Identity permissions.
   - Verify permission grouping/search and descriptions.
   - Edit the role name and description, then change its permission set.
   - Open the system `Administrator` role and verify it is read-only.
3. **Membership lifecycle**
   - Create an unaffiliated user.
   - Invite the user as a workspace member; verify Invited status.
   - Accept the invitation.
   - Change the member role; verify the role name updates.
   - Remove the member with confirmation; verify Removed status.
4. **Sessions**
   - Open Sessions and verify the current session badge.
   - Revoke another active session if available.
   - Use Revoke all sessions, verify logout, and sign in again.

### Test checklist

#### Users

- [ ] Search and paging return expected users.
- [ ] New user is created as Pending.
- [ ] Activation and deactivation update the status.
- [ ] Edit changes name/email with validation.
- [ ] Current admin cannot deactivate itself.
- [ ] Duplicate email shows a readable conflict.

#### Roles

- [ ] Role list shows system and workspace roles.
- [ ] Create validates name and description.
- [ ] Permission selector groups by module and supports search.
- [ ] Role edit persists name and description.
- [ ] Permission assignment persists the exact selected set.
- [ ] System roles show no edit/manage-permission actions and application commands reject changes.

#### Memberships

- [ ] Invite creates an Invited membership.
- [ ] Invitation acceptance changes status to Active.
- [ ] Change role replaces the role and updates the list.
- [ ] Remove requires confirmation and marks the membership Removed.
- [ ] Self-removal is not offered.
- [ ] Duplicate membership and cross-tenant errors are readable.

#### Sessions

- [ ] Current session is marked and cannot be individually revoked from the list.
- [ ] Revoking another session updates its status.
- [ ] Revoke all sessions clears the web session and returns to login.
- [ ] Revoking all sessions invalidates session refresh.

#### Authorization and accessibility

- [ ] Anonymous access to `/app/admin/users` redirects to login.
- [ ] A user without Identity read permissions sees `/access-denied`.
- [ ] The administration reader sees lists/details but no create, edit, lifecycle or membership
      mutation actions.
- [ ] Keyboard navigation reaches every form control and action.
- [ ] Confirmation dialogs use Escape to cancel and expose dialog semantics.
- [ ] Status and permission information remain understandable without colour.

---

## Testing

Web integration tests cover:

- Permission selector grouping, descriptions and read-only system-role rendering.
- User create/edit/activate/deactivate workflow.
- Role create/update/permission workflow and system-role immutability.
- Membership invite/change-role/accept/remove workflow.
- Session list/revoke/revoke-all workflow.
- Concurrent write mapping through `IdentityWebResults`.
- Administration list/detail/create/edit routes and permission-aware controls.
- Navigation filtering for Users, Roles, Memberships and Sessions.

Run:

```powershell
dotnet test tests\Integration\BSX.Web.IntegrationTests\BSX.Web.IntegrationTests.csproj -c Release
```

---

## Limitations

- User platform-role assignment, lock/unlock and administrative password reset are not exposed in
  the UI.
- Role deletion is not exposed.
- Session revocation is self-service; an administrator cannot revoke another user's sessions from
  this screen.
- The local demo seeds only the administrator. Read-only administration behavior is covered by the
  test factory user `admin-reader@example.com`; it is not created in the local PostgreSQL database.
- The web administration UI uses in-process application dispatch, not the HTTP Identity API.

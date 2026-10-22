# Deployment Checklist (D8)

## Before deployment

- [ ] CI is green (build with warnings-as-errors + full test suite, including PostgreSQL parity when
      Docker is available).
- [ ] Release tag/artifact produced by the release job.
- [ ] Database backup taken (mandatory before destructive migrations).
- [ ] **Identity** migrations reviewed and ready to apply (expand/contract for rolling deployments).
- [ ] **CRM** migrations reviewed and ready to apply (`InitialCrm` on first deployment;
      expand/contract thereafter).
- [ ] **Sales** migrations reviewed and ready to apply (`InitialSales` on first deployment;
      expand/contract thereafter).
- [ ] `Identity:Jwt:SigningKeys` configured in the shared secret store for **all** instances.
- [ ] `ConnectionStrings:Identity` configured via secret.
- [ ] `ConnectionStrings:Crm` configured (or intentionally omitted to share the Identity database;
      CRM then uses the `crm` schema in that database).
- [ ] `ConnectionStrings:Sales` configured (or intentionally omitted to share the Identity database;
      Sales then uses the `sales` schema in that database).
- [ ] `AllowedHosts` set to the production host name(s).
- [ ] Identity database flags set correctly for the topology:
      `Identity:Database:ApplyMigrationsOnStartup=false` (HA) or `true` (single instance);
      `Identity:Database:VerifySchema=true`.
- [ ] `Crm:Database:VerifySchema=true` where the deployment owns the `crm` schema (recommended).
- [ ] `Sales:Database:VerifySchema=true` where the deployment owns the `sales` schema (recommended).
- [ ] `Identity:Permissions:SynchronizeOnStartup` set for the topology (`true` seeds the system
      roles from module catalogs; requires the Identity schema to be present).
- [ ] Liveness/readiness probe paths configured in the orchestrator.

## During deployment

- [ ] Apply **Identity** migrations as a gated step (before rolling out the new application version).
- [ ] Apply **CRM** migrations as a gated step (before rolling out the new application version).
- [ ] Apply **Sales** migrations as a gated step (before rolling out the new application version).
- [ ] Roll out instances gradually; wait for `/health/ready` to be green before continuing.
- [ ] Monitor error rates, 401/403 rates, 409 concurrency conflicts and latency during rollout.

## After deployment

- [ ] `/health/live` and `/health/ready` are green on all instances (`crm-database`,
      `sales-database`, and the schema checks when enabled).
- [ ] Smoke test Identity: login, refresh, list sessions, logout; exercise one authorized and one
      unauthorized call.
- [ ] Smoke test CRM: create a customer, read it back, list it with search, update it, and exercise
      one unauthorized call.
- [ ] Smoke test Sales: create a lead, contact and qualify it, create an opportunity, and exercise
      one unauthorized call. Verify that a lead referencing an unknown CRM customer returns `409`.
- [ ] Verify password-reset behaviour matches the deployment (delivery configured → `200`; not
      configured → `503`).
- [ ] Confirm no unexpected configuration warnings/errors in logs.

## Rollback

- [ ] Keep the previous artifact available for immediate rollback.
- [ ] Ensure the previous version is compatible with the current schema (expand/contract), or
      restore the pre-migration backup.
- [ ] If a CRM or Sales migration must be rolled back, apply the migration bundle/`database update`
      to the previous migration and restore data only if the rollback is destructive.

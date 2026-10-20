# Deployment Checklist (D8)

## Before deployment

- [ ] CI is green (build with warnings-as-errors + full test suite).
- [ ] Release tag/artifact produced by the release job.
- [ ] Database backup taken (mandatory before destructive migrations).
- [ ] Migrations reviewed and ready to apply (expand/contract for rolling deployments).
- [ ] `Identity:Jwt:SigningKeys` configured in the shared secret store for **all** instances.
- [ ] `ConnectionStrings:Identity` configured via secret.
- [ ] `AllowedHosts` set to the production host name(s).
- [ ] `Identity:Database` flags set correctly for the topology:
      `ApplyMigrationsOnStartup=false` (HA) or `true` (single instance); `VerifySchema=true`.
- [ ] Liveness/readiness probe paths configured in the orchestrator.

## During deployment

- [ ] Apply migrations as a gated step (before rolling out the new application version).
- [ ] Roll out instances gradually; wait for `/health/ready` to be green before continuing.
- [ ] Monitor error rates, 401/403 rates and latency during rollout.

## After deployment

- [ ] `/health/live` and `/health/ready` are green on all instances.
- [ ] Smoke test: login, refresh, list sessions, logout; exercise one authorized and one
      unauthorized call.
- [ ] Verify password-reset behaviour matches the deployment (delivery configured → `200`; not
      configured → `503`).
- [ ] Confirm no unexpected configuration warnings/errors in logs.

## Rollback

- [ ] Keep the previous artifact available for immediate rollback.
- [ ] Ensure the previous version is compatible with the current schema (expand/contract), or
      restore the pre-migration backup.

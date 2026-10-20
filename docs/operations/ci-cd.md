# CI/CD Pipeline (D5)

The pipeline is defined in [`.github/workflows/ci.yml`](../../.github/workflows/ci.yml) and runs on
every push and pull request, plus tags.

## Jobs

| Job | Trigger | Purpose |
| --- | --- | --- |
| **Build validation** | push, PR | Restore and build in `Release` with `TreatWarningsAsErrors=true`. Warnings fail the build. |
| **Test validation** | after build | Run `dotnet test --no-build` with TRX + coverage, upload results. Depends on build. |
| **Release validation** | tags `v*` | Rebuild, re-test, publish `BSX.Web`, upload the release artifact. Depends on tests. |

## Gates

1. **Warnings fail builds.** `Directory.Build.props` sets `TreatWarningsAsErrors=true`; the CI build
   also passes `-p:TreatWarningsAsErrors=true` explicitly. Code-style analyzers (`IDE0005`,
   `EnforceCodeStyleInBuild=true`) are enforced.
2. **Tests run automatically** for the whole solution on every change; a failing test fails the
   pipeline.
3. **Release job is tag-gated.** Artifacts are only produced for `v*` tags after build and tests
   pass.

## Recommended branch protection

On `main` (and `develop`):

- Require the **Build validation** and **Test validation** checks to pass before merge.
- Require pull-request review.
- Disallow force pushes.

## Extending the pipeline

- Add PostgreSQL-backed (Testcontainers) tests to the test job when provider parity is required.
- Add container build/push steps to the release job when an image registry is adopted.
- Add a deployment job (environment-gated) that applies migrations and rolls out the artifact.

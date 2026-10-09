# ADR-008 - Central Package Management and Build Governance

## Status

Accepted

---

## Context

As the platform grows across many modules, unmanaged NuGet versions lead to drift, conflicting
transitive dependencies and difficult upgrades. The build must also fail fast on correctness
issues.

---

## Decision

Adopt:

- **Central Package Management** via `Directory.Packages.props`. Every version is declared once;
  projects reference packages without a version.
- **Shared build properties** via `Directory.Build.props`. Nullable reference types, implicit
  usings, deterministic builds, XML documentation generation and warnings-as-errors.
- **SDK pinning** via `global.json` targeting .NET 10.
- **Code style enforcement** via `.editorconfig`, evaluated during build.

Compiler warnings are errors. Analyzer warnings are not, to avoid blocking on stylistic noise.

---

## Alternatives Considered

### Per-project package versions

Pros

- Simple for a single project.

Cons

- Version drift across modules.
- No single upgrade point.

### Treat all analyzer warnings as errors

Pros

- Extremely strict.

Cons

- High friction with generated code and evolving analyzers.

---

## Consequences

### Positive

- Deterministic, reproducible builds.
- One place to manage and audit dependencies.
- Early detection of correctness issues.

### Negative

- Contributors must update the central file when adding packages.

---

## Notes

Adding a package requires a version entry in `Directory.Packages.props`. Direct versions in
project files are not permitted.

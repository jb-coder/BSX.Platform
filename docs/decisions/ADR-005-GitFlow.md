# ADR-005 - GitFlow

## Status

Accepted

---

## Context

A branching strategy is required to support long-term development.

---

## Decision

Use GitFlow.

Main branches:

- main
- develop

Supporting branches:

- feature/*
- release/*
- hotfix/*

---

## Consequences

### Positive

- Predictable release management
- Controlled evolution

### Negative

- Additional branching overhead

---

## Notes

Semantic Versioning is mandatory.
# BSX Platform Testing Skill

## Purpose

Defines testing standards for BSX Platform.

Testing is mandatory.

Every feature must be testable.

---

# Testing Pyramid

1. Unit Tests
2. Integration Tests
3. Architecture Tests
4. End-to-End Tests

Prioritize unit and integration tests.

---

# Unit Tests

Purpose:

Validate business rules.

Examples:

Customer creation

Order submission

Invoice generation

Requirements:

- Fast
- Deterministic
- Independent

---

# Integration Tests

Purpose:

Validate infrastructure.

Examples:

Database operations

Endpoints

Authentication

Authorization

Real dependencies are preferred.

Avoid excessive mocking.

---

# Architecture Tests

Purpose:

Enforce architectural rules.

Examples:

- Domain must not reference Infrastructure
- Modules must not depend directly on other modules
- CQRS conventions must be respected

Architecture tests are mandatory.

---

# End-to-End Tests

Purpose:

Validate user workflows.

Examples:

Create customer

Create order

Generate invoice

Keep scenarios business focused.

---

# Naming Conventions

Method format:

Should_DoSomething_WhenCondition

Examples:

Should_CreateCustomer_WhenRequestIsValid

Should_RejectOrder_WhenStockIsUnavailable

---

# Assertions

Use:

FluentAssertions

Avoid weak assertions.

Preferred:

result.IsSuccess.Should().BeTrue();

---

# Coverage Philosophy

Do not chase percentages.

Prioritize business-critical paths.

The goal is confidence, not metrics.
`
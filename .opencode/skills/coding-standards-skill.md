# BSX Platform Coding Standards

## General Rules

Write code for humans.

Code is read more often than it is written.

---

## Naming

Classes:

Customer

CustomerService

CustomerRepository

Methods:

CreateCustomerAsync

GetCustomerAsync

Variables:

customerName

customerId

Avoid abbreviations.

---

## Files

One public type per file.

---

## Records

Prefer records for:

- Requests
- Responses
- DTOs

---

## Classes

Mark classes as sealed whenever inheritance is not required.

---

## Async

Use async/await consistently.

Avoid synchronous database access.

---

## Dependency Injection

Depend on abstractions.

Avoid service locators.

---

## Validation

Use FluentValidation.

Validation must exist before business logic execution.

---

## Documentation

Public APIs should include XML documentation.

Complex business rules require comments explaining the WHY, not the WHAT.

---

## Clean Code

Avoid:

- God Classes
- Static Helpers
- Deep Nesting
- Long Methods

Prefer small focused units.
# ADR-019 - Inventory Quantity Strategy

## Status

Accepted

**Amends:** the Inventory v1 domain scope; resolves the quantity-strategy condition from the
Inventory Domain Principal Architect Review.

---

## Context

Inventory v1 models stock as the number of discrete units of a product in a warehouse. The
Principal Architect Review identified a SaaS-readiness question: should the domain support
continuous quantities (weight, volume) and a unit of measure from the start, or remain
integer-only?

The decision must be made before Infrastructure creates the database schema because changing
`int` to `decimal` later affects:

- the domain `Quantity` value object and aggregate arithmetic,
- persisted columns and unique/index behavior,
- serialized domain-event payloads stored in the outbox,
- the future Application/API contract,
- historical data migration.

The platform currently has no requirements for weight-, volume- or length-based products.

---

## Decision

**Inventory v1 supports discrete units only.**

- `Quantity` is a non-negative 32-bit integer from `0` to `1,000,000,000`.
- No `UnitOfMeasure` is modeled in v1.
- A product is stocked, reserved, received, released and adjusted only in whole units.
- Stock is measured per warehouse; there is no unit conversion.
- Fractional quantities and units of measure require a future ADR and a deliberate migration.

Inventory is therefore optimized for countable goods (pieces, boxes, units) and explicitly does
not support weight/volume-based inventory in v1.

---

## Alternatives Considered

### Alternative A - UnitOfMeasure + decimal quantities now

Pros

- Supports retail, manufacturing and process industries from day one.
- Avoids a future breaking change.
- Makes reservations and conversions explicit.

Cons

- Adds a `UnitOfMeasure` model, validation and conversion policy before a requirement exists.
- Decimal arithmetic requires rounding rules, scale decisions and database precision choices.
- Multiplies the Application/API surface (unit selection, conversion, display).
- Violates the pragmatic, scope-driven approach (ADR-003) and "no speculative features".

### Alternative B - Decimal quantities without a unit of measure

Pros

- Supports fractional quantities immediately.

Cons

- A quantity without a unit is ambiguous (0.5 of what?).
- Still requires a future breaking change to add units.
- Introduces precision/rounding complexity with little benefit.

### Alternative C - Discrete units only (chosen)

Pros

- Matches the current product scope and existing Inventory v1 requirements.
- Keeps arithmetic deterministic and the schema simple.
- Keeps the domain and outbox payloads stable.
- Leaves room for a future ADR without contaminating the current model.

Cons

- Cannot represent weight/volume-based stock.
- Requires a migration when continuous units become a requirement.

---

## Consequences

### Positive

- Inventory v1 is simple, predictable and fully testable with integer arithmetic.
- PostgreSQL columns, uniqueness checks and event payloads stay stable.
- The Application layer does not need unit conversion or rounding rules.

### Negative

- Inventory cannot onboard tenants that sell by weight, volume or length until the future
  migration is completed.
- Product/API consumers must not assume that a future `Quantity` will remain an integer.

### Future migration path

When a unit-of-measure requirement is approved:

1. Write a superseding ADR defining `UnitOfMeasure`, precision/scale and conversion rules.
2. Add the unit of measure to `Product` (the stock-keeping definition) and to `InventoryItem`
   where stocking units differ from purchasing/selling units.
3. Convert `Quantity` to a decimal-backed value object and migrate `OnHandQuantity` and
   `ReservedQuantity` columns to `numeric`.
4. Replay/translate historical outbox payloads or version the event contracts.
5. Version the API and migration scripts; do not silently reinterpret existing integer data.

---

## Notes

- The stock semantics (`OnHandQuantity`, `ReservedQuantity`, `AvailableQuantity`) are documented
  in [domain-model.md](../modules/inventory/domain-model.md).
- Related: [ADR-003](ADR-003-DDD.md), [ADR-009](ADR-009-Domain-Event-Dispatching-Strategy.md).

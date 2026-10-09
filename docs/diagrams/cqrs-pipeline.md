# CQRS Pipeline

Canonical order of the request pipeline. Every command and query passes through it.

```mermaid
flowchart TD

    REQ[Command / Query] --> LOG[LoggingBehavior]
    LOG --> VAL[ValidationBehavior]
    VAL -->|invalid| ERRV["Result<br/>(Validation errors, field-level)"]
    VAL -->|valid| AUTH[AuthorizationBehavior]
    AUTH -->|"not authenticated"| ERR401["Result<br/>(Authentication)"]
    AUTH -->|"not permitted"| ERR403["Result<br/>(Permission)"]
    AUTH -->|allowed| ISCMD{"Command?"}
    ISCMD -->|"yes"| TXB[TransactionBehavior]
    ISCMD -->|"no"| HND[Handler]
    TXB --> HND
    HND --> EVT["Persist + dispatch domain events<br/>(ADR-009)"]
    HND --> RES["Result / Result&lt;T&gt;"]
    EVT --> RES
    RES --> MAP["Transport mapping<br/>(IErrorMapper)"]
    ERRV --> MAP
    ERR401 --> MAP
    ERR403 --> MAP
```

| Order | Behavior | Runs for | Responsibility |
| --- | --- | --- | --- |
| 1 | `LoggingBehavior` | All | Structured start/completion logging. |
| 2 | `ValidationBehavior` | All | FluentValidation; short-circuits with structured validation errors. |
| 3 | `AuthorizationBehavior` | Requests with a requirement | Permission gate; fail-closed default. |
| 4 | `TransactionBehavior` | Commands only | Opens a transaction and commits via the unit of work. |
| 5 | Handler | All | Business logic. |

Notes:

- Queries never open a transaction.
- Behaviors are registered in this order; the first registered is the outermost.
- Further behaviors (for example, caching for queries) are inserted without reordering the four
  canonical ones.

# Request Flow

How a command or query flows through the platform pipeline. See
[cqrs-pipeline.md](cqrs-pipeline.md) for the canonical behavior order.

```mermaid
flowchart TD

    Request["Command / Query (Result)"]

    Request --> Logging["LoggingBehavior"]

    Logging --> Validation["ValidationBehavior<br/>(FluentValidation)"]

    Validation -->|"invalid"| ValidationError["Result<br/>(structured validation errors)"]

    Validation -->|"valid"| Authorization["AuthorizationBehavior<br/>(permission gate)"]

    Authorization -->|"denied"| AuthError["Result<br/>(Authentication / Permission)"]

    Authorization -->|"allowed"| IsCommand{"Command?"}

    IsCommand -->|"yes"| Transaction["TransactionBehavior"]

    IsCommand -->|"no"| Handler["Handler"]

    Transaction --> Handler

    Handler --> Domain["Domain<br/>(Aggregate + business rules)"]

    Domain --> Repository["Repository<br/>(aggregate specific)"]

    Repository --> Database["PostgreSQL<br/>(EF Core)"]

    Handler --> Events["Persist + dispatch domain events<br/>(ADR-009)"]

    Domain -->|"Result"| Response["Result / Result&lt;T&gt;"]
    Repository --> Response
    Handler --> Response

    Response --> Transport["Endpoint / API<br/>(IErrorMapper)"]
    ValidationError --> Transport
    AuthError --> Transport
```

Notes:

- Authorization runs after validation and before the handler.
- Read operations (queries) never start transactions.
- Write operations (commands) run inside a transaction and persist domain events atomically.
- Expected failures are returned as `Result` and mapped to transport by the adapter.

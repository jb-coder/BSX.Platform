# Error Mapping

Errors are semantic in the kernel and mapped to transport only in the adapter (ADR-011).

## From Handler to Transport

```mermaid
flowchart TD

    HND[Handler] --> RES["Result / Result&lt;T&gt;<br/>(0..n Errors)"]
    RES -->|success| OK[Value]
    RES -->|failure| ERR["Error collection<br/>Code, Message, Kind, Field?"]

    ERR --> VAL{Kind}
    VAL -->|Validation| E400["400 ProblemDetails<br/>+ errors dictionary"]
    VAL -->|Authentication| E401[401]
    VAL -->|Permission| E403[403]
    VAL -->|NotFound| E404[404]
    VAL -->|Conflict| E409[409]
    VAL -->|Failure| E422[422]
    VAL -->|Unexpected| E500[500]

    EXC[Unhandled exception] --> GEH[Global exception handler]
    GEH --> E500
```

## Taxonomy

```mermaid
flowchart LR
    subgraph Kernel
        E["Error<br/>(no HTTP knowledge)"]
        K["ErrorKind<br/>(semantic category)"]
        R["Result / Result&lt;T&gt;<br/>Errors: 0..n"]
    end
    subgraph Adapter
        MAP[IErrorMapper]
        PD["ProblemDetails<br/>(RFC 9457)"]
    end
    R --> E
    E --> K
    E --> MAP
    MAP --> PD
```

## ProblemDetails Shape (validation)

| Field | Source |
| --- | --- |
| `type` | Derived from `ErrorKind`. |
| `title` | Localized summary. |
| `status` | Mapped HTTP status. |
| `detail` | Aggregated `Error.Message` values. |
| `errors` | Dictionary keyed by `Error.Field` → `(Code, Message)` list. |

- Codes are stable and namespaced (`MODULE.Subject.Problem`).
- Messages are localized; codes are not.
- `Result` is never serialized directly to the wire.

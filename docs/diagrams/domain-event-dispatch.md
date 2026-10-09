# Domain Event Dispatch

Visual reference for [ADR-009](../decisions/ADR-009-Domain-Event-Dispatching-Strategy.md).

---

## 1. Event Lifecycle

```mermaid
stateDiagram-v2
    [*] --> Raised
    Raised --> Collected: SaveChanges collects pending events
    Collected --> Persisted: written to outbox in same transaction
    Persisted --> Dispatched: after commit
    Dispatched --> Handled: handler succeeds
    Dispatched --> Failed: any handler fails
    Failed --> Retry: exponential backoff
    Retry --> Dispatched
    Failed --> DeadLettered: max attempts exceeded
    Handled --> Completed: outbox row marked processed
    Completed --> [*]
    DeadLettered --> [*]
```

---

## 2. SaveChanges to Dispatch (request scope)

```mermaid
sequenceDiagram
    autonumber
    participant EP as Endpoint / Command
    participant TB as Transaction Behavior
    participant UoW as Unit of Work
    participant DB as DbContext
    participant PG as PostgreSQL
    participant DSP as DomainEventDispatcher
    participant H as DomainEventHandler
    participant OBP as Outbox Publisher
    participant BUS as Message Broker

    EP->>TB: SendAsync(command)
    TB->>UoW: BeginTransaction()
    TB->>H: handler modifies aggregate
    Note over H: Aggregate raises domain events (in memory)
    TB->>UoW: SaveChangesAndDispatchAsync()
    UoW->>DB: SaveChanges()
    DB->>DB: collect events via IHasDomainEvents
    DB->>DB: translate + write outbox rows (Domain / Integration)
    DB->>PG: COMMIT  (state + events, atomic)
    Note over PG: Transaction closed before dispatch
    UoW->>DSP: DispatchAsync(persisted domain events)
    DSP->>H: HandleAsync(event)
    H->>UoW: mark processed / enqueue integration events
    H-->>DSP: success or isolated failure
    DSP-->>UoW: dispatch summary
    UoW-->>TB: Result
    TB-->>EP: Result

    loop background processor
        OBP->>PG: poll pending Integration rows
        OBP->>BUS: publish
        OBP->>PG: mark processed / schedule retry
    end
```

---

## 3. Two Independent Loops

```mermaid
flowchart TD

    CMD[Command Handler] --> AGG[Aggregate raises DomainEvents]

    AGG --> UOW[Unit of Work . SaveChangesAndDispatchAsync]
    UOW --> TX[(Single Transaction)]
    TX --> DOMAIN[(outbox.messages : kind = Domain)]
    TX --> INTEG[(outbox.messages : kind = Integration)]

    DOMAIN -->|after commit| DSP[DomainEventDispatcher in-process]
    DSP --> DH[Domain Event Handler]
    DH -->|may enqueue| INTEG

    INTEG --> PUB[Background Outbox Publisher]
    PUB --> BUS[IEventBus]
    BUS --> BROKER[RabbitMQ / Azure Service Bus / Kafka]
    BROKER --> CONSUMER[Consumer Module + Client]
    CONSUMER --> DEDUP[(Inbox / dedup by EventId)]
```

---

## 4. Choosing Domain vs Integration Event

```mermaid
flowchart TD

    EVT[Reaction to a business occurrence] --> Q{Does it belong to the same module?}

    Q -->|Yes| DOM[Domain Event]
    DOM --> DOM1[In-process, after commit]
    DOM --> DOM2[Internal contract, freely changeable]
    DOM --> DOM3[At-least-once, idempotent handler]

    Q -->|No| INT[Integration Event]
    INT --> INT1[Outbox + broker]
    INT --> INT2[Published, versioned contract]
    INT --> INT3[At-least-once + inbox dedup]
```

---

## 5. Outbox Table (conceptual)

```mermaid
erDiagram
    OUTBOX_MESSAGES {
        bigint  Id PK "ordered FIFO"
        uuid    EventId "idempotency key"
        string  Kind "Domain | Integration"
        string  EventType
        jsonb   Payload
        timestamptz OccurredOnUtc
        timestamptz CreatedOnUtc
        timestamptz ProcessedOnUtc
        string  Status "Pending | Processed | Failed | DeadLettered"
        int     RetryCount
        string  LastError
        uuid    CorrelationId
        uuid    TenantId "nullable, reserved"
    }
```

---

## 6. Layer Responsibilities

```mermaid
flowchart LR

    subgraph Domain
        AR[AggregateRoot] -->|raises| DE[IDomainEvent]
    end

    subgraph Infrastructure
        DBX[DbContext / Interceptor] -->|collect + persist| OB[(Outbox table)]
        UOW[Unit of Work] --> DBX
        DISP[DomainEventDispatcher] -->|post-commit| HND[Handlers]
        OBP[Outbox Publisher] -->|background| BUS[IEventBus]
    end

    subgraph Application
        TXB[Transaction Behavior] --> UOW
        HND -->|Result| TXB
    end

    DBX -. reads .-> AR
    OB -. feeds .-> DISP
    OB -. feeds .-> OBP
```

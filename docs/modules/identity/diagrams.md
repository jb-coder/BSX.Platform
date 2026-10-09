# Identity - Diagrams

Visual reference for the Identity module. Links: [domain-model.md](domain-model.md),
[authentication](jwt-strategy.md), [refresh tokens](refresh-token-strategy.md),
[module boundaries](module-boundaries.md).

---

## 1. Module Context

```mermaid
flowchart TD
    CLIENT["Client / Blazor UI"] --> HOST["BSX.Web (host + composition root)"]
    HOST --> IDENTITY["Identity module"]
    HOST --> OTHERS["Other modules"]

    IDENTITY --> DB[("PostgreSQL<br/>identity schema")]

    IDENTITY -. "integration events (Outbox)" .-> NOTIF["Notifications"]
    IDENTITY -. "integration events (Outbox)" .-> CRM["CRM"]

    IDENTITY --> AB["ICurrentUser / IPermissionChecker<br/>(BuildingBlocks abstractions)"]
    OTHERS --> AB
```

---

## 2. Aggregate Model

```mermaid
classDiagram
    class User {
        +UserId Id
        +Email Email
        +PersonName Name
        +PasswordCredential Credential
        +UserStatus Status
        +SecurityStamp SecurityStamp
        +UserSession sessions
        +Register()
        +ChangePassword()
        +AssignRole()
        +Deactivate()
    }
    class Role {
        +RoleId Id
        +RoleName Name
        +Permission[] Permissions
        +bool IsSystem
        +Grant()
        +Revoke()
    }
    class UserSession {
        +SessionId Id
        +UserId UserId
        +DateTimeOffset AbsoluteExpiresOnUtc
        +Start()
        +Rotate()
        +RevokeChain()
    }
    class RefreshToken {
        +RefreshTokenId Id
        +RefreshTokenHash Hash
        +DateTimeOffset ExpiresOnUtc
    }
    class PasswordResetToken {
        +PasswordResetTokenId Id
        +RefreshTokenHash Hash
        +DateTimeOffset ExpiresOnUtc
    }

    User "1" ..> "*" Role : assigned by id
    User "1" *-- "0..1" PasswordResetToken : owns
    UserSession "1" *-- "1..*" RefreshToken : owns
    User "1" ..> "*" UserSession : identifies
```

---

## 3. Entity Relationships (conceptual)

```mermaid
erDiagram
    USERS {
        uuid Id PK
        string Email
        string NormalizedEmail
        string Name
        string PasswordHash
        string Status
        string SecurityStamp
        int AccessFailedCount
    }
    ROLES {
        uuid Id PK
        string Name
        bool IsSystem
    }
    PERMISSIONS {
        string Code PK
        string Description
    }
    ROLE_PERMISSIONS {
        uuid RoleId FK
        string PermissionCode FK
    }
    USER_ROLES {
        uuid UserId FK
        uuid RoleId FK
    }
    USER_SESSIONS {
        uuid Id PK
        uuid UserId FK
        timestamptz AbsoluteExpiresOnUtc
        timestamptz RevokedOnUtc
    }
    REFRESH_TOKENS {
        uuid Id PK
        uuid SessionId FK
        string Hash
        timestamptz ExpiresOnUtc
        timestamptz UsedOnUtc
        timestamptz RevokedOnUtc
        uuid ReplacedByTokenId
    }
    PASSWORD_RESET_TOKENS {
        uuid Id PK
        uuid UserId FK
        string Hash
        timestamptz ExpiresOnUtc
        timestamptz UsedOnUtc
    }

    USERS ||--o{ USER_ROLES : "has"
    ROLES ||--o{ USER_ROLES : "assigned"
    ROLES ||--o{ ROLE_PERMISSIONS : "grants"
    PERMISSIONS ||--o{ ROLE_PERMISSIONS : "in"
    USERS ||--o{ USER_SESSIONS : "opens"
    USER_SESSIONS ||--o{ REFRESH_TOKENS : "contains"
    USERS ||--o{ PASSWORD_RESET_TOKENS : "requests"
```

Auditing and soft-delete columns (`CreatedOnUtc`, `UpdatedOnUtc`, `IsDeleted`, `TenantId`, ...) are
shadow properties (ADR-010) and are omitted here.

---

## 4. Login Flow

```mermaid
sequenceDiagram
    autonumber
    participant C as Client
    participant API as Identity.Endpoints
    participant S as ISender
    participant H as AuthenticateUserHandler
    participant U as User
    participant SES as UserSession
    participant DB as identity schema

    C->>API: POST /identity/auth/login
    API->>S: AuthenticateUserCommand
    S->>H: HandleAsync
    H->>U: verify credentials
    alt valid and not disabled/locked
        H->>U: ResetAccessFailures
        H->>U: Raise UserSignedIn
        H->>SES: Start
        H->>SES: Raise SessionStarted + RefreshTokenIssued
        H->>DB: SaveChangesAndDispatchAsync
        DB-->>H: commit
        H-->>C: 200 access + refresh tokens
    else invalid
        H->>U: RecordAccessFailure
        H->>DB: SaveChangesAndDispatchAsync
        H-->>C: 401 Authentication
    end
```

---

## 5. Refresh with Rotation and Reuse Detection

```mermaid
sequenceDiagram
    autonumber
    participant C as Client
    participant H as RefreshTokenHandler
    participant SES as UserSession
    participant DB as identity schema

    C->>H: RefreshTokenCommand(refreshToken)
    H->>SES: locate token by hash
    alt token active
        H->>SES: Rotate
        SES-->>H: new refresh token
        H->>DB: commit
        H-->>C: 200 new access + refresh tokens
    else token used or revoked
        H->>SES: RevokeChain
        H->>SES: Raise RefreshTokenReuseDetected + SessionRevoked
        H->>DB: commit
        H-->>C: 401 Authentication (session revoked)
    end
```

---

## 6. Vertical Slice - Register User

```mermaid
flowchart LR
    E["POST /identity/users"] --> CMD[RegisterUserCommand]
    CMD --> VAL[ValidationBehavior]
    VAL --> AUT["AuthorizationBehavior<br/>identity.users.create"]
    AUT --> TX[TransactionBehavior]
    TX --> H[RegisterUserHandler]
    H --> AGG["User.Register"]
    H --> REPO[IUserRepository]
    H --> OUT["Outbox<br/>UserRegisteredIntegrationEvent"]
    REPO --> DB[("identity schema")]
    H --> RES["Result UserId"]
    RES --> E
```

# Authorization

Permission-based authorization evaluated in the pipeline (ADR-012).

## Components

```mermaid
flowchart LR

    subgraph Host
        PRIN["ClaimsPrincipal<br/>(post-authentication)"]
    end

    subgraph BuildingBlocks["BuildingBlocks.Authorization"]
        CU[ICurrentUser]
        PC[IPermissionChecker]
        RQ[RequirePermissionAttribute]
        AB[AuthorizationBehavior]
    end

    subgraph Identity["Identity (future)"]
        STORE["Roles + Permissions"]
        CHECKER[PermissionCheckerImpl]
    end

    subgraph Module
        HND["Command / Query Handler"]
    end

    PRIN --> CU
    CU --> AB
    RQ --> AB
    AB --> PC
    PC --> CHECKER
    CHECKER --> STORE
    AB -->|allowed| HND
```

## Pipeline Decision

```mermaid
flowchart TD
    REQ[Request] --> HAS{"RequirePermission<br/>declared?"}
    HAS -->|no| ALLOW[Continue to handler]
    HAS -->|yes| AUTH{Authenticated?}
    AUTH -->|no| E401["Result<br/>(Authentication)"]
    AUTH -->|yes| PERM{Has permission?}
    PERM -->|no| E403["Result<br/>(Permission)"]
    PERM -->|yes| ALLOW
```

## Permission Scope (ADR-020)

```mermaid
flowchart TD
    DEF["PermissionDefinition<br/>(module catalog)"] --> SCOPE{"Scope"}
    SCOPE -->|Platform| PLAT["Platform roles only<br/>(TenantId is null)"]
    SCOPE -->|Tenant| TEN["Tenant roles and platform roles"]
    PLAT --> RES["PermissionResolver"]
    TEN --> RES
    RES -->|"tenant role + platform scope"| DROP["Dropped (fail closed)"]
```

- A tenant role can never grant a platform-scoped permission.
- Unknown permission codes contribute nothing.
- The web UI hides disallowed permissions, but the backend and the resolver remain authoritative.

## Authorization Revalidation (ADR-021)

```mermaid
flowchart TD
    REQ[Authenticated request] --> SNAP["AuthorizationSnapshotReader<br/>(session + user + membership + permissions)"]
    SNAP -->|invalid| DENY["Denied<br/>(Authorization.SessionExpired)"]
    SNAP -->|valid| PERM["Fresh permission gate"]
    PERM --> HND[Handler]

    CIRCUIT["Blazor circuit"] -->|"every CircuitIntervalSeconds"| SNAP
    CIRCUIT -->|invalid| SIGNOUT["Sign out circuit<br/>(redirect to /login?expired=true)"]
    COOKIE["Cookie request"] -->|"every CookieIntervalSeconds"| SNAP
    COOKIE -->|invalid| EXPIRE["Delete cookies<br/>(redirect to /login?expired=true)"]
```

- The snapshot is authoritative; cached token claims are never trusted for the gate.
- A missing, revoked, expired, deactivated or tenant-mismatched principal is denied.
- A store failure is treated as denied.

## Defaults Before Identity

```mermaid
flowchart LR
    NOTREG[No Identity registered] --> CU["ICurrentUser = anonymous<br/>(always registered)"]
    NOTREG --> PC["IPermissionChecker = fail-closed"]
    PC -->|"request has requirement"| DENY["Denied (Permission)"]
    PC -->|"request has no requirement"| PASS[Unaffected]
```

- The gate is the permission (`resource.action`), not the role.
- Resource-level rules (ownership, tenant, entity state) are evaluated by the handler using
  `ICurrentUser`.
- Authentication (issuance/validation) is owned by Identity; the reserved permission claim type
  is `permission`.

# Request Flow

```mermaid
flowchart TD

Request

Request --> Validator

Validator --> Handler

Handler --> Domain

Domain --> Repository

Repository --> Database

Database --> Result

Result --> Response
```
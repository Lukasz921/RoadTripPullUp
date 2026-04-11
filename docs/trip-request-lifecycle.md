## Trip request lifecycle

```mermaid
stateDiagram-v2
    [*] --> Pending : create request
    Pending --> Rejected : driver rejects
    Pending --> Cancelled : passenger cancels
    Rejected  --> [*] : when tip is achied
    Cancelled --> [*] : when tip is achied
```


# Notes
When an offer is created, the system needs to check whether there is already an existing record in the database. If the offer is pending or rejected, it should not be possible to create another request.

When a trip is archived, the request should be deleted.
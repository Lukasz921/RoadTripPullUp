## Trip lifecycle

```mermaid
stateDiagram-v2
    [*] --> InActive : create draft
    InActive --> Active : publish
    Active --> Full : max passengers reached
    Full --> Active : passenger leaves
    Active --> Cancelled : cancel
    Full --> Cancelled : cancel
    Active --> Done : trip completed
    Full --> Done : trip completed
    Cancelled --> Archived : archive
    Done --> Archived : archive
```

# Notes
- Inactive — trip entity is created but not yet published.
- Active — trip is published and still has available seats.
- Full — the maximum number of passengers has been reached.
- Cancelled — trip was cancelled by the driver.
- Done — trip has been completed.
- Archived — a cancelled or completed trip has been archived.

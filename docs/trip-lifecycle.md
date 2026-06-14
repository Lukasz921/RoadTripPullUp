## Trip lifecycle

```mermaid
stateDiagram-v2
    [*] --> ACTIVE : POST /api/v1/trips (driver creates trip)
    ACTIVE --> [*] : DELETE /api/v1/trips/{id} (driver deletes)\nDELETE /api/admin/trips/{id} (admin deletes)
```

## Notes
- The only status in the system is **ACTIVE** — a trip is created immediately as active, with no draft phase.
- A trip disappears from active listings automatically once `departure_time` has passed (date filter applied in queries).
- Deleting a trip is a hard delete (removed from the database).
- Past trips remain accessible via `GET /api/v1/trips/history`.

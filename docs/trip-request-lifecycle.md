## Passenger flow

The driver adds a passenger directly to a trip — there is no separate request/accept flow.

```mermaid
sequenceDiagram
    participant Driver
    participant API
    participant Passenger

    Driver->>API: POST /api/v1/trips/{tripId}/passengers
    Note right of API: Validations:<br/>- trip exists and is ACTIVE<br/>- passenger is a different user than the driver<br/>- passenger is not already on the trip<br/>- a seat is available<br/>- passenger exists in the system
    API-->>Driver: 204 No Content
    Note over Driver,Passenger: Passenger is added to the trip<br/>and to the trip group chat
```

### Endpoint

`POST /api/v1/trips/{tripId}/passengers`

Body:
```json
{ "passengerId": "<uuid>" }
```

Responses:
- `204` — added successfully
- `400` — no seats available / passenger already on trip / invalid UUID
- `403` — caller is not the driver of this trip
- `404` — trip not found

# Database Schema

The system uses three separate databases.

---

## app_db (PostgreSQL 16)

Managed by `AppDbContext` (EF Core migrations in `src/Infrastructure/Migrations/`).

```mermaid
erDiagram
    MESSAGES {
        uuid Id PK
        uuid SenderId
        uuid ReceiverId
        text Content
        datetime Timestamp
    }
```

> **Note:** `app_db` previously contained `Trips`, `Routes`, and `TripRequests` tables from the old TripPlanner layer. Those entities have been removed from the code. The tables still exist in the DB from historical migrations but are no longer used.

---

## users_db (PostgreSQL 16)

Managed by `UsersDbContext` (EF Core migrations in `src/Users/Migrations/`).

```mermaid
erDiagram
    USERS {
        uuid Id PK
        text Name
        text Surname
        text Email
        text PasswordHash
        text PhoneNumber
        datetime DateOfBirth
        int Role
        int Sex
    }
```

| Enum | Values |
|---|---|
| `Role` | `0 = REGULAR_USER`, `1 = ADMIN` |
| `Sex` | `0 = MALE`, `1 = FEMALE`, `2 = OTHER` |

---

## trip_db (PostGIS 16)

Managed by raw SQL init scripts in `docker/trip-db/init/`. No EF Core — direct Npgsql queries via `TripsV1Service`.

```mermaid
erDiagram
    TRIP {
        uuid id PK
        uuid driver_user_id
        geography source_geog
        geography target_geog
        geography route_polyline
        int route_distance_m
        int route_duration_s
        int max_detour_m
        timestamptz departure_time
        numeric price_per_seat
        smallint available_seats
        trip_status status
        timestamptz created_at
    }

    TRIP_PASSENGER {
        uuid trip_id FK
        uuid passenger_user_id
        timestamptz joined_at
    }

    TRIP ||--o{ TRIP_PASSENGER : includes
```

`status` ∈ `ACTIVE` | `COMPLETED`

`route_polyline` is a `geography(LINESTRING, 4326)` — full road geometry computed by Valhalla on trip creation.

**Spatial indexes:**
- `idx_trip_route_polyline` — GiST on `route_polyline` (used by `ST_DWithin` in search Phase 1)
- `idx_trip_departure_active` — on `departure_time` filtered to `status = 'ACTIVE'`
- `idx_trip_driver_active` — on `driver_user_id` filtered to `status = 'ACTIVE'`
- `idx_trip_passenger_user` — on `trip_passenger(passenger_user_id)`

---

## MessageService DB (PostgreSQL)

Managed by `MessageService.Infrastructure.AppDbContext` (EF Core, separate from `app_db`).

```mermaid
erDiagram
    CONVERSATIONS {
        uuid id PK
        int type
        uuid trip_id
        text title
        datetime date
        datetime created_at
    }

    CONVERSATION_MEMBERS {
        uuid conversation_id FK
        uuid user_id
        int role
        datetime joined_at
    }

    MESSAGES {
        uuid id PK
        uuid conversation_id FK
        uuid sender_id
        int type
        jsonb payload
        datetime created_at
    }

    MESSAGE_READS {
        uuid message_id FK
        uuid user_id
        datetime read_at
    }

    CONVERSATIONS ||--o{ CONVERSATION_MEMBERS : has
    CONVERSATIONS ||--o{ MESSAGES : contains
    MESSAGES ||--o{ MESSAGE_READS : read_by
```

`ConversationType` ∈ `0 = Direct` | `1 = Group`

A group conversation is created automatically when a trip is created (`trip_id` links the conversation to the trip).

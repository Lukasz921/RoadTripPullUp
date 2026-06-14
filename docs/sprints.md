### Przemysław Borczak, Bartosz Cichomski, Marek Makochon, Łukasz Przybylski

**Technologies used:** C#, .NET 10, PostgreSQL, PostGIS, Entity Framework Core, React, Docker, Redis, Valhalla, SignalR

---

### Sprint 1: Foundation

**Auth, project skeleton, database setup.**

- Clean Architecture solution structure: `Core`, `Application`, `Infrastructure`, `API`
- `POST /api/auth/register` — register with email/password
- `POST /api/auth/login` — returns JWT
- `POST /api/auth/google` — Google OAuth login
- JWT validation middleware (issuer, audience, signing key)
- `UsersDbContext` with EF Core migrations, PostgreSQL

---

### Sprint 2: Trips — Core

**Drivers can create trips; passengers can search.**

- `POST /api/v1/trips` — create trip (computes route via Valhalla or mock engine)
- `GET /api/v1/trips/search` — synchronous search (Phase 1: PostGIS `ST_DWithin`, Phase 2: Valhalla matrix detour)
- `POST /api/v1/trips/search` — async search (enqueue to Redis)
- `GET /api/v1/trips/search/{jobId}` — poll async search job
- PostGIS `trip_db` with spatial indexes; raw Npgsql (no EF Core for trips)
- Mock routing engine for debug mode (Haversine × 1.3, no Valhalla needed)

---

### Sprint 3: Passengers & Messaging

**Driver adds passengers; group chat created automatically.**

- `POST /api/v1/trips/{tripId}/passengers` — driver directly adds a passenger (no request/accept flow)
- `GET /api/v1/trips/me` — driver's upcoming trips
- `GET /api/v1/trips/joined` — passenger's upcoming trips
- `GET /api/v1/trips/history` — all past trips (driver + passenger combined)
- `GET /api/v1/trips/{tripId}` — trip detail
- `DELETE /api/v1/trips/{tripId}` — driver deletes own trip
- Group conversation created automatically on trip creation (`TripsOrchestratorController` coordinates TripService + MessageService)
- Passenger added to group chat when driver adds them to the trip
- MessageService: conversations, messages, SignalR hub, read receipts (see `MessageService-API.md`)

---

### Sprint 4: Users & Ratings

**Profile management, ratings, banning.**

- `GET /api/users/me` — current user profile
- `PATCH /api/users/me` — update profile
- `GET /api/users/me/integration-data` — user data for cross-service use
- `POST /api/v1/trips/{tripId}/rate-user` — rate a trip participant (only allowed after trip departure time has passed)
- `GET /api/users/{id}/ratings` — get ratings for a user
- `GET /api/users/ratings/{ratingId}` — get single rating
- `DELETE /api/users/ratings/{ratingId}` — delete own rating
- `AvgRating` and `RatingsCount` stored on the `users` table

---

### Sprint 5: Admin Panel

**Admin endpoints for moderation.**

- `GET /api/admin/trips` — list all trips with optional date range filter *(ADMIN only)*
- `DELETE /api/admin/trips/{tripId}` — delete any trip *(ADMIN only)*
- `POST /api/users/{id}/ban` — ban user with reason and optional expiry *(ADMIN only)*
- `POST /api/users/{id}/unban` — lift ban *(ADMIN only)*
- `POST /api/users/{id}/role` — change user role *(ADMIN only)*
- JWT `ClaimTypes.Role` carries `REGULAR_USER` or `ADMIN`

---

### Not implemented (out of scope)

- Request/accept passenger flow (`/api/trips/{id}/request`, `/api/requests/{id}/accept`) — replaced by direct driver-adds-passenger
- Trip states beyond `ACTIVE` (Full, Cancelled, Done, Archived) — not implemented; trips are hard-deleted
- Payment gateway integration
- Google Maps / external geo-location service (Valhalla used instead)
- Report/moderation system (`Report` entity, `IReportRepository`)

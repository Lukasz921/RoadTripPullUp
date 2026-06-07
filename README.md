# RoadTripPullUp

Carpooling app built with .NET 10 (ASP.NET Core) and React.

---

## Running the project

There are two modes: **debug** (no Valhalla required) and **release** (real routing with Valhalla).

---

### Debug mode — no Valhalla needed

In debug mode the app uses a mock routing engine that calculates straight-line distances (Haversine × 1.3 road factor). No tiles to download, starts in seconds.

```bash
docker compose up --build
```

- Frontend: http://localhost:5173
- API: http://localhost:8080
- API docs (Scalar): http://localhost:8080/scalar/v1

Trip routes will appear as straight lines on the map instead of real roads — everything else (search, passengers, chat) works normally.

---

### Release mode — real routes with Valhalla

Release mode uses the [Valhalla](https://github.com/valhalla/valhalla) routing engine with real OpenStreetMap road data. Routes follow actual roads and search results are calculated with real driving distances.

#### Step 1 — Download Poland tiles (one-time, ~1.5 GB download, ~40 min to build)

The easiest way is to let Valhalla download and build tiles automatically on first start. The `tile_urls` environment variable in `docker-compose.prod.yml` already points to the Poland extract from Geofabrik.

> **Note:** Tile building only happens once. The result is stored in the `valhalla_data` Docker volume and reused on all future starts.

#### Step 2 — Start in release mode

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up --build
```

Valhalla starts building tiles automatically. Watch progress:

```bash
docker logs -f valhalla
```

Wait until you see:

```
Starting valhalla service!
```

This takes around **40 minutes** on the first run. Subsequent starts are instant (tiles are cached in the volume).

#### Check Valhalla is ready

```bash
curl http://localhost:8002/status
```

Should return something like `{"version":"3.x.x","tileset_last_modified":...}`.

Test a route (Warsaw → Kraków):

```bash
curl -s -X POST http://localhost:8002/route \
  -H "Content-Type: application/json" \
  -d '{
    "locations": [
      {"lon": 21.0122, "lat": 52.2297},
      {"lon": 19.9450, "lat": 50.0647}
    ],
    "costing": "auto"
  }' | python3 -m json.tool
```

#### Troubleshooting Valhalla

| Error | Cause | Fix |
|---|---|---|
| `error_code 171` — No suitable edges | Tiles not ready yet | Wait for `Starting valhalla service!` in logs |
| Connection refused on port 8002 | Container not running | `docker compose ... up -d valhalla` |
| Tile build interrupted | Container restarted mid-build | Run `docker volume rm roadtrippullup_valhalla_data` then start again |

---

## Stopping the project

```bash
# Stop containers but keep all data (trips, users, Valhalla tiles)
docker compose down

# Stop and delete ALL data (fresh start — you'll need to rebuild Valhalla tiles)
docker compose down -v
```

> **Important:** Always use `docker compose down` without `-v` to keep your data between sessions.

---

## Local development (without Docker)

Run only the databases in Docker, and run API + frontend locally.

```bash
# Start databases and Redis
docker compose up -d db trip_db redis

# Run backend (debug mode — mock routing)
cd src/API
dotnet restore
dotnet watch run

# Run frontend (separate terminal)
cd src/frontend
npm install
npm run dev
```

---

## Architecture

| Service | Port | Description |
|---|---|---|
| Frontend | 5173 | React + Vite |
| API | 8080 | ASP.NET Core .NET 10 |
| app_db | 5432 | PostgreSQL 16 — users, messages |
| trip_db | 5433 | PostGIS 16 — trips (spatial data) |
| Redis | 6379 | Async search job queue |
| Valhalla | 8002 | Routing engine (release mode only) |

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

### Database migrations (trip_db)

The `trip_db` schema is plain SQL in `docker/trip-db/init/`. These scripts run **automatically only on a fresh volume** (first `docker compose up` on a new machine, or after `docker compose down -v`). If you already have a `trip_db` volume, apply new scripts manually:

```bash
docker exec -i trip_db psql -U postgres -d trip_db < docker/trip-db/init/004_add_trip_requests.sql
docker exec -i trip_db psql -U postgres -d trip_db < docker/trip-db/init/005_add_base_route_distance.sql
```

(`app_db` / `messages_db` use EF Core migrations and are applied automatically by the API on startup.)

---

### Release mode — real routes with Valhalla

Release mode uses the [Valhalla](https://github.com/valhalla/valhalla) routing engine with real OpenStreetMap road data. Routes follow actual roads and search results are calculated with real driving distances.

#### Step 1 — Download Poland tiles (one-time, ~1.5 GB download, ~40 min to build)

There are two ways to get the tiles. Try automatic first; if it fails, use manual.

**Option A — automatic download (Valhalla downloads the file itself)**

The `tile_urls` environment variable in `docker-compose.prod.yml` already points to the Poland extract from Geofabrik. Just start the stack and Valhalla will download and build tiles on its own.

Skip to Step 2.

---

**Option B — manual download (if automatic fails or is too slow)**

Download the Poland PBF file yourself and copy it into the container. This is more reliable on slow or restricted connections.

**1. Download the file**

Linux / macOS:
```bash
wget -O poland-latest.osm.pbf https://download.geofabrik.de/europe/poland-latest.osm.pbf
```

Windows (PowerShell):
```powershell
Invoke-WebRequest -Uri "https://download.geofabrik.de/europe/poland-latest.osm.pbf" -OutFile "poland-latest.osm.pbf"
```

Or just open the URL in a browser and save the file: https://download.geofabrik.de/europe/poland-latest.osm.pbf

The file is ~1.5 GB and only needs to be downloaded once.

**2. Edit `docker-compose.prod.yml` — disable the auto-download**

Change `tile_urls` so Valhalla does not try to download anything:

```yaml
environment:
  - tile_urls=
  - serve_tiles=True
  - build_admins=False
  - build_time_zones=False
```

**3. Start only the Valhalla container**

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d valhalla
```

**4. Copy the PBF file into the container**

```bash
docker cp poland-latest.osm.pbf valhalla:/custom_files/
```

**5. Restart Valhalla so it picks up the file and starts building**

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml restart valhalla
```

Watch the build:
```bash
docker logs -f valhalla
```

Tile building takes ~40 minutes. When done you will see `Starting valhalla service!`.

> **Note:** Tile building only happens once. The result is stored in the `valhalla_data` Docker volume and reused on all future starts.

---

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

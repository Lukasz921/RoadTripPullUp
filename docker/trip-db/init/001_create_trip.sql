CREATE EXTENSION IF NOT EXISTS postgis;

CREATE TYPE trip_status AS ENUM ('ACTIVE', 'COMPLETED');

CREATE TABLE trip (
    id                UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    driver_user_id    UUID         NOT NULL,
    source_geog       geography(POINT, 4326)      NOT NULL,
    target_geog       geography(POINT, 4326)      NOT NULL,
    route_polyline    geography(LINESTRING, 4326),
    route_distance_m  INTEGER,
    route_duration_s  INTEGER,
    max_detour_m      INTEGER      NOT NULL,
    departure_time    TIMESTAMPTZ  NOT NULL,
    price_per_seat    NUMERIC(10,2) NOT NULL,
    available_seats   SMALLINT     NOT NULL,
    status            trip_status  NOT NULL DEFAULT 'ACTIVE',
    created_at        TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

-- Spatial index for ST_DWithin queries in search
CREATE INDEX idx_trip_route_polyline
    ON trip USING GIST (route_polyline);

-- Time-range filtering for search and GET /trips/me
CREATE INDEX idx_trip_departure_active
    ON trip (departure_time)
    WHERE status = 'ACTIVE';

-- Driver lookup for GET /trips/me
CREATE INDEX idx_trip_driver_active
    ON trip (driver_user_id)
    WHERE status = 'ACTIVE';

CREATE TYPE trip_request_status AS ENUM ('PENDING', 'ACCEPTED');

CREATE TABLE trip_request (
    id                 UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    trip_id            UUID         NOT NULL REFERENCES trip(id) ON DELETE CASCADE,
    requester_user_id  UUID         NOT NULL,
    conversation_id    UUID         NOT NULL,
    pickup_geog        geography(POINT, 4326)      NOT NULL,
    dropoff_geog       geography(POINT, 4326)      NOT NULL,
    preview_polyline   geography(LINESTRING, 4326),
    detour_m           INTEGER      NOT NULL,
    status             trip_request_status NOT NULL DEFAULT 'PENDING',
    created_at         TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    accepted_at        TIMESTAMPTZ
);

-- All requests for a trip (driver's inbox) and ordered replay of accepted stops.
CREATE INDEX idx_trip_request_trip ON trip_request(trip_id);

-- 1:1 lookup from a direct conversation to its request (chat panel).
CREATE INDEX idx_trip_request_conversation ON trip_request(conversation_id);

-- At most one open request per (trip, requester); a second click reuses it.
CREATE UNIQUE INDEX uq_trip_request_pending
    ON trip_request(trip_id, requester_user_id)
    WHERE status = 'PENDING';

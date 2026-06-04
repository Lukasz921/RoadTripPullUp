CREATE TABLE trip_passenger (
    trip_id            UUID        NOT NULL REFERENCES trip(id) ON DELETE CASCADE,
    passenger_user_id  UUID        NOT NULL,
    joined_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (trip_id, passenger_user_id)
);

-- Lookup: all trips a given user has joined
CREATE INDEX idx_trip_passenger_user ON trip_passenger(passenger_user_id);

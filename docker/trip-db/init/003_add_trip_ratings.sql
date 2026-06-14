CREATE TABLE trip_rating (
    id                UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    trip_id           UUID         NOT NULL REFERENCES trip(id) ON DELETE CASCADE,
    rater_user_id     UUID         NOT NULL,
    rated_user_id     UUID         NOT NULL,
    rating            SMALLINT     NOT NULL CHECK (rating >= 1 AND rating <= 5),
    created_at        TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    UNIQUE (trip_id, rater_user_id, rated_user_id)
);

-- Index for lookup: all ratings given to a user
CREATE INDEX idx_trip_rating_rated_user ON trip_rating(rated_user_id);

-- Index for lookup: all ratings given by a user
CREATE INDEX idx_trip_rating_rater_user ON trip_rating(rater_user_id);

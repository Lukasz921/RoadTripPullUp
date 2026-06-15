-- The immutable original (driver-only) route distance, used as the baseline for
-- detour calculations in search and trip requests. route_distance_m grows as
-- passengers are added; base_route_distance_m never changes after trip creation,
-- so detours stay measured against the driver's original route.
ALTER TABLE trip ADD COLUMN base_route_distance_m INTEGER;

-- Backfill: for existing trips, the current route distance is the best available
-- baseline (trips without accepted passengers have route_distance_m == base).
UPDATE trip SET base_route_distance_m = route_distance_m WHERE base_route_distance_m IS NULL;

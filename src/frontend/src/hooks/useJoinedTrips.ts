import { useEffect, useState } from 'react';
import { getJoinedTrips } from '../api/trips';
import type { TripSummary } from '../types/trip';

interface UseJoinedTripsResult {
  trips: TripSummary[];
  loading: boolean;
  error: string;
}

export function useJoinedTrips(): UseJoinedTripsResult {
  const [trips, setTrips] = useState<TripSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    getJoinedTrips()
      .then((data) =>
        setTrips(
          (data.items ?? []).map((t) => ({
            id: t.id,
            driverId: t.driverId,
            source: t.source,
            target: t.target,
            departureTime: t.departureTime,
            pricePerSeat: t.pricePerSeat,
            availableSeats: t.availableSeats,
            maxDetourMeters: t.maxDetourMeters,
            actualDetourMeters: 0,
          })),
        ),
      )
      .catch(() => setError('Failed to load joined rides.'))
      .finally(() => setLoading(false));
  }, []);

  return { trips, loading, error };
}

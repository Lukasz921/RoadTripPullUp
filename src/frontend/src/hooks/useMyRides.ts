import { useEffect, useState } from 'react';
import { getMyTrips } from '../api/trips';
import type { TripSummary } from '../types/trip';

interface UseMyRidesResult {
  trips: TripSummary[];
  loading: boolean;
  error: string;
}

export function useMyRides(): UseMyRidesResult {
  const [trips, setTrips] = useState<TripSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    getMyTrips()
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
      .catch(() => setError('Failed to load your rides.'))
      .finally(() => setLoading(false));
  }, []);

  return { trips, loading, error };
}

import { useEffect, useState } from 'react';
import { getJoinedTrips, type TripDTO } from '../api/trips';

interface UseJoinedTripsResult {
  trips: TripDTO[];
  loading: boolean;
  error: string;
}

export function useJoinedTrips(): UseJoinedTripsResult {
  const [trips, setTrips] = useState<TripDTO[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    getJoinedTrips()
      .then((data) => setTrips(data.items ?? []))
      .catch(() => setError('Failed to load joined rides.'))
      .finally(() => setLoading(false));
  }, []);

  return { trips, loading, error };
}

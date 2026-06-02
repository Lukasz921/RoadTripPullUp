import { useEffect, useState } from 'react';
import { getMyTrips, type TripDTO } from '../api/trips';

interface UseMyRidesResult {
  trips: TripDTO[];
  loading: boolean;
  error: string;
}

export function useMyRides(): UseMyRidesResult {
  const [trips, setTrips] = useState<TripDTO[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    getMyTrips()
      .then((data) => setTrips(data.items ?? []))
      .catch(() => setError('Failed to load your rides.'))
      .finally(() => setLoading(false));
  }, []);

  return { trips, loading, error };
}

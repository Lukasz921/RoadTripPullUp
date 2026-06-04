import { useEffect, useState } from 'react';
import { getMyTrips, type TripDTO } from '../api/trips';

interface UseMyRidesResult {
  trips: TripDTO[];
  totalCount: number;
  loading: boolean;
  error: string;
}

export function useMyRides(page: number, pageSize = 10): UseMyRidesResult {
  const [trips, setTrips] = useState<TripDTO[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    setLoading(true);
    setError('');
    getMyTrips(page, pageSize)
      .then((data) => {
        setTrips(data.items ?? []);
        setTotalCount(data.totalCount ?? 0);
      })
      .catch(() => setError('Failed to load your rides.'))
      .finally(() => setLoading(false));
  }, [page, pageSize]);

  return { trips, totalCount, loading, error };
}

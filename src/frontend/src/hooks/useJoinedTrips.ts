import { useEffect, useState } from 'react';
import { getJoinedTrips, type TripDTO } from '../api/trips';

interface UseJoinedTripsResult {
  trips: TripDTO[];
  totalCount: number;
  loading: boolean;
  error: string;
}

export function useJoinedTrips(page: number, pageSize = 10): UseJoinedTripsResult {
  const [trips, setTrips] = useState<TripDTO[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    setLoading(true);
    setError('');
    getJoinedTrips(page, pageSize)
      .then((data) => {
        setTrips(data.items ?? []);
        setTotalCount(data.totalCount ?? 0);
      })
      .catch(() => setError('Failed to load joined rides.'))
      .finally(() => setLoading(false));
  }, [page, pageSize]);

  return { trips, totalCount, loading, error };
}

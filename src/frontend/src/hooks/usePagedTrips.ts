import { useEffect, useState } from 'react';
import type { PagedTripsDTO, TripDTO } from '../api/trips';

interface UsePagedTripsResult {
  trips: TripDTO[];
  totalCount: number;
  loading: boolean;
  error: string;
}

/**
 * Generic paged-trip loader. Pass any endpoint that returns a PagedTripsDTO
 * (getMyTrips, getJoinedTrips, getTripHistory, …).
 */
export function usePagedTrips(
  fetcher: (page: number, pageSize: number) => Promise<PagedTripsDTO>,
  page: number,
  pageSize = 10,
): UsePagedTripsResult {
  const [trips, setTrips] = useState<TripDTO[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError('');
    fetcher(page, pageSize)
      .then((data) => {
        if (cancelled) return;
        setTrips(data.items ?? []);
        setTotalCount(data.totalCount ?? 0);
      })
      .catch(() => {
        if (!cancelled) setError('Failed to load rides.');
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [fetcher, page, pageSize]);

  return { trips, totalCount, loading, error };
}

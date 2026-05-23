import { useEffect, useState } from 'react';
import { tripApi } from '../api/axiosConfig';
import type { TripSummary } from '../types/trip';

interface UseMyTripsResult {
  trips: TripSummary[];
  loading: boolean;
  error: string;
}

export function useMyTrips(): UseMyTripsResult {
  const [trips, setTrips] = useState<TripSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    tripApi
      .get('/trips/me')
      .then((res) => setTrips(res.data.trips ?? []))
      .catch((err) => {
        console.error('Failed to load trips:', err);
        setError('Failed to load your trips.');
      })
      .finally(() => setLoading(false));
  }, []);

  return { trips, loading, error };
}

import { useEffect, useState } from 'react';
import { tripApi } from '../api/axiosConfig';
import type { Trip } from '../pages/profile/components/TripList';

interface UseMyTripsResult {
  trips: Trip[];
  loading: boolean;
  error: string;
}

export function useMyTrips(): UseMyTripsResult {
  const [trips, setTrips] = useState<Trip[]>([]);
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

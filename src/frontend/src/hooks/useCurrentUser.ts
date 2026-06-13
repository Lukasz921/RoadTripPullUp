import { useEffect, useState } from 'react';
import { getCurrentUser, type CurrentUser } from '../api/user';

interface UseCurrentUserResult {
  user: CurrentUser | null;
  loading: boolean;
  error: string;
}

export function useCurrentUser(): UseCurrentUserResult {
  const [user, setUser] = useState<CurrentUser | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    getCurrentUser()
      .then((data) => setUser(data))
      .catch((err) => {
        console.error('Failed to load user:', err);
        setError('Failed to load profile.');
      })
      .finally(() => setLoading(false));
  }, []);

  return { user, loading, error };
}

import { useCallback, useEffect, useState } from 'react';
import { getCurrentUser, type CurrentUser } from '../api/user';

interface UseCurrentUserResult {
  user: CurrentUser | null;
  loading: boolean;
  error: string;
  refetch: () => Promise<void>;
}

export function useCurrentUser(): UseCurrentUserResult {
  const [user, setUser] = useState<CurrentUser | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const data = await getCurrentUser();
      setUser(data);
    } catch (err) {
      console.error('Failed to load user:', err);
      setError('Failed to load profile.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  return { user, loading, error, refetch: load };
}

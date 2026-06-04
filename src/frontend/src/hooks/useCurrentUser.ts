import { useEffect, useState } from 'react';
import { authApi } from '../api/axiosConfig';

interface CurrentUser {
  id: string;
  name: string;
  surname: string;
  email: string;
  phoneNumber?: string;
  dateOfBirth: string;
  sex: string;
}

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
    authApi
      .get<CurrentUser>('/users/me')
      .then((res) => setUser(res.data))
      .catch((err) => {
        console.error('Failed to load user:', err);
        setError('Failed to load profile.');
      })
      .finally(() => setLoading(false));
  }, []);

  return { user, loading, error };
}

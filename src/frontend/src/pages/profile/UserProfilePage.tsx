import { useEffect, useState } from 'react';
import { useParams, useLocation } from 'react-router-dom';
import Navbar from '../../components/layout/Navbar';
import Footer from '../../components/layout/Footer';
import ProfileRow from './components/ProfileRow';
import { getUserById, type CurrentUser } from '../../api/user';

function calcAge(dateOfBirth: string): number {
  const birth = new Date(dateOfBirth);
  const today = new Date();
  let age = today.getFullYear() - birth.getFullYear();
  const monthDiff = today.getMonth() - birth.getMonth();
  if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birth.getDate())) age--;
  return Math.max(0, age);
}

export default function UserProfilePage() {
  const { id } = useParams<{ id: string }>();
  const { state } = useLocation();
  // The user is normally passed in via navigation state (props); fall back to a
  // fetch so the page also works on a direct URL / refresh.
  const passedUser = (state as { user?: CurrentUser } | null)?.user ?? null;

  const [user, setUser] = useState<CurrentUser | null>(passedUser);
  const [loading, setLoading] = useState(!passedUser);
  const [error, setError] = useState('');

  useEffect(() => {
    if (passedUser || !id) return;
    setLoading(true);
    setError('');
    getUserById(id)
      .then(setUser)
      .catch(() => setError('Failed to load user.'))
      .finally(() => setLoading(false));
  }, [id, passedUser]);

  return (
    <main className="flex min-h-screen flex-col bg-[#f3faee] text-[#12351f]">
      <Navbar />

      <div className="mx-auto w-full max-w-3xl flex-1 px-6 pb-16 pt-28">
        <h1 className="mb-6 text-3xl font-bold">User profile</h1>

        <section className="rounded-2xl bg-white p-6 shadow-sm">
          {loading && <p className="text-sm text-[#5d7056]">Loading profile...</p>}
          {error && <p className="rounded-lg bg-red-50 px-4 py-2 text-sm text-red-600">{error}</p>}
          {user && (
            <div className="grid gap-4 sm:grid-cols-2">
              <ProfileRow label="Name" value={user.name} />
              <ProfileRow label="Surname" value={user.surname} />
              <ProfileRow label="Age" value={calcAge(user.dateOfBirth)} />
              <ProfileRow label="Sex" value={user.sex} />
              <ProfileRow label="Email" value={user.email} />
              {user.phoneNumber && <ProfileRow label="Phone" value={user.phoneNumber} />}
              <ProfileRow label="Rating" value={user.avgRating > 0 ? user.avgRating.toFixed(2) : 'No rating'} />
              <ProfileRow label="Ratings count" value={user.ratingsCount} />
            </div>
          )}
        </section>
      </div>

      <Footer />
    </main>
  );
}

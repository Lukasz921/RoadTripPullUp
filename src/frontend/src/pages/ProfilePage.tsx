import { useNavigate } from 'react-router-dom';
import Navbar from '../components/layout/Navbar';
import Footer from '../components/layout/Footer';
import ProfileRow from './profile/components/ProfileRow';
import TripList from './profile/components/TripList';
import { useMyTrips } from '../hooks/useMyTrips';
import { useCurrentUser } from '../hooks/useCurrentUser';

function calcAge(dateOfBirth: string): number {
  const birth = new Date(dateOfBirth);
  const today = new Date();
  let age = today.getFullYear() - birth.getFullYear();
  const monthDiff = today.getMonth() - birth.getMonth();
  if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birth.getDate())) age--;
  return Math.max(0, age);
}

export default function ProfilePage() {
  const navigate = useNavigate();
  const { trips: publishedTrips, loading, error } = useMyTrips();
  const { user, loading: userLoading, error: userError } = useCurrentUser();

  return (
    <main className="flex min-h-screen flex-col bg-[#f3faee] text-[#12351f]">
      <Navbar />

      <div className="mx-auto w-full max-w-3xl flex-1 px-6 pb-16 pt-28">
        <h1 className="mb-6 text-3xl font-bold">User profile</h1>

        <header className="rounded-2xl bg-white p-6 shadow-sm">
          {userLoading && <p className="text-sm text-[#5d7056]">Loading profile...</p>}
          {userError && <p className="rounded-lg bg-red-50 px-4 py-2 text-sm text-red-600">{userError}</p>}
          {user && (
            <div className="grid gap-4 sm:grid-cols-2">
              <ProfileRow label="Name" value={user.name} />
              <ProfileRow label="Surname" value={user.surname} />
              <ProfileRow label="Age" value={calcAge(user.dateOfBirth)} />
              <ProfileRow label="Sex" value={user.sex} />
              <ProfileRow label="Email" value={user.email} />
              {user.phoneNumber && <ProfileRow label="Phone" value={user.phoneNumber} />}
            </div>
          )}
        </header>

        <section className="mt-10">
          <div className="mb-4 flex items-center justify-between">
            <div className="flex gap-2">
              <button
                onClick={() => navigate('/joined-rides')}
                className="rounded-xl bg-white px-4 py-2 text-sm font-semibold text-[#12351f] shadow-sm ring-1 ring-[#d7e8c8] hover:bg-[#f3faee]"
              >
                Joined rides
              </button>
              <button
                onClick={() => navigate('/add-trip')}
                className="rounded-xl bg-[#8cc63f] px-4 py-2 text-sm font-semibold text-[#12351f] hover:bg-[#a6dd55]"
              >
                Add trip
              </button>
            </div>
          </div>

        </section>
      </div>

      <Footer />
    </main>
  );
}

import { useNavigate } from 'react-router-dom';
import Navbar from '../components/layout/Navbar';
import Footer from '../components/layout/Footer';
import ProfileDetails from './profile/components/ProfileDetails';
import { useCurrentUser } from '../hooks/useCurrentUser';

export default function ProfilePage() {
  const navigate = useNavigate();
  const { user, loading: userLoading, error: userError, refetch } = useCurrentUser();

  return (
    <main className="flex min-h-screen flex-col bg-[#f3faee] text-[#12351f]">
      <Navbar />

      <div className="mx-auto w-full max-w-3xl flex-1 px-6 pb-16 pt-28">
        <h1 className="mb-6 text-3xl font-bold">User profile</h1>

        <header className="rounded-2xl bg-white p-6 shadow-sm">
          {userLoading && <p className="text-sm text-[#5d7056]">Loading profile...</p>}
          {userError && <p className="rounded-lg bg-red-50 px-4 py-2 text-sm text-red-600">{userError}</p>}
          {user && <ProfileDetails user={user} onUpdated={refetch} />}
        </header>

        <div className="mt-10 flex gap-2">
          <button
            onClick={() => navigate('/joined-rides')}
            className="rounded-xl bg-white px-4 py-2 text-sm font-semibold text-[#12351f] shadow-sm ring-1 ring-[#d7e8c8] hover:bg-[#f3faee]"
          >
            Joined rides
          </button>
          <button
            onClick={() => navigate('/my-rides')}
            className="rounded-xl bg-white px-4 py-2 text-sm font-semibold text-[#12351f] shadow-sm ring-1 ring-[#d7e8c8] hover:bg-[#f3faee]"
          >
            My rides
          </button>
          <button
            onClick={() => navigate('/my-conversations')}
            className="rounded-xl bg-white px-4 py-2 text-sm font-semibold text-[#12351f] shadow-sm ring-1 ring-[#d7e8c8] hover:bg-[#f3faee]"
          >
            My conversations
          </button>
        </div>

        {user?.isBanned && (
          <div className="mt-6 rounded-2xl bg-red-50 p-6 shadow-sm ring-1 ring-red-200">
            <p className="font-semibold text-red-700">Your account is banned</p>
            {user.banReason && (
              <p className="mt-1 text-sm text-red-600">Reason: {user.banReason}</p>
            )}
            <p className="mt-1 text-sm text-red-600">
              {user.bannedUntil
                ? `Until: ${new Date(user.bannedUntil).toLocaleString()}`
                : 'This ban is permanent.'}
            </p>
          </div>
        )}
      </div>

      <Footer />
    </main>
  );
}

import { useEffect, useState } from 'react';
import { useLocation } from 'react-router-dom';
import Navbar from '../../components/layout/Navbar';
import Footer from '../../components/layout/Footer';
import { banUser, type ComplaintResponseDTO } from '../../api/admin';
import { getUserById, type CurrentUser } from '../../api/user';
import { formatDate } from '../../utils/format';

// Fallback used when a user can't be fetched (e.g. mock ids), so the card and
// ban button still render with the id we do have.
function placeholderUser(id: string): CurrentUser {
  return {
    id,
    name: 'Unknown',
    surname: 'user',
    email: id,
    dateOfBirth: '',
    sex: '',
    avgRating: 0,
    ratingsCount: 0,
    isBanned: false,
  };
}

function UserCard({
  title,
  user,
  banButton,
}: {
  title: string;
  user: CurrentUser | null;
  banButton?: React.ReactNode;
}) {
  return (
    <section className="rounded-2xl bg-white p-6 shadow-sm">
      <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-[#5d7056]">{title}</p>
      {user ? (
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-lg font-semibold text-[#12351f]">
              {user.name} {user.surname}
              {user.isBanned && (
                <span className="ml-2 rounded-full bg-red-100 px-2 py-0.5 text-xs font-semibold text-red-600">
                  Banned
                </span>
              )}
            </p>
            <p className="text-sm text-[#5d7056]">{user.email}</p>
            <p className="mt-1 text-xs text-[#5d7056]">
              Rating: {user.avgRating.toFixed(1)} ({user.ratingsCount})
            </p>
          </div>
          {banButton}
        </div>
      ) : (
        <p className="text-sm text-[#5d7056]">Could not load user.</p>
      )}
    </section>
  );
}

export default function ComplaintDetailsPage() {
  const location = useLocation();
  // The complaint is passed via navigation state from the list (no by-id endpoint).
  const complaint = (location.state as { complaint?: ComplaintResponseDTO } | null)?.complaint ?? null;
  const [complainer, setComplainer] = useState<CurrentUser | null>(null);
  const [complained, setComplained] = useState<CurrentUser | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [banning, setBanning] = useState(false);

  useEffect(() => {
    if (!complaint) {
      setLoading(false);
      return;
    }
    let cancelled = false;
    setLoading(true);
    setError('');
    // Load both involved users. Tolerate one failing — fall back to a
    // minimal placeholder so the card (and ban button) still render.
    Promise.allSettled([
      getUserById(complaint.complainerId),
      getUserById(complaint.complainedUserId),
    ])
      .then(([complainerRes, complainedRes]) => {
        if (cancelled) return;
        setComplainer(
          complainerRes.status === 'fulfilled'
            ? complainerRes.value
            : placeholderUser(complaint.complainerId),
        );
        setComplained(
          complainedRes.status === 'fulfilled'
            ? complainedRes.value
            : placeholderUser(complaint.complainedUserId),
        );
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [complaint]);

  async function handleBan() {
    if (!complained || !complaint) return;
    setBanning(true);
    try {
      await banUser(complained.id, { reason: complaint.reason });
      setComplained({ ...complained, isBanned: true, banReason: complaint.reason });
    } catch {
      setError('Failed to ban user.');
    } finally {
      setBanning(false);
    }
  }

  return (
    <main className="flex min-h-screen flex-col bg-[#f3faee] text-[#12351f]">
      <Navbar />

      <div className="mx-auto w-full max-w-3xl flex-1 px-6 pb-16 pt-28">
        <h1 className="mb-6 text-3xl font-bold">Complaint</h1>

        {loading && <p className="text-sm text-[#5d7056]">Loading...</p>}

        {error && (
          <p className="mb-4 rounded-lg bg-red-50 px-4 py-2 text-sm text-red-600">{error}</p>
        )}

        {!complaint && !loading && (
          <p className="text-sm text-[#5d7056]">Open a complaint from the complaints list.</p>
        )}

        {complaint && (
          <div className="flex flex-col gap-6">
            <section className="rounded-2xl bg-white p-6 shadow-sm">
              <p className="text-xs text-[#5d7056]">Reason</p>
              <p className="text-sm font-semibold text-[#12351f]">{complaint.reason}</p>
              <p className="mt-3 text-xs text-[#5d7056]">{formatDate(complaint.createdAt)}</p>
            </section>

            <UserCard title="Complainer" user={complainer} />

            <UserCard
              title="Reported user"
              user={complained}
              banButton={
                complained && (
                  <button
                    type="button"
                    onClick={handleBan}
                    disabled={banning || complained.isBanned}
                    className="whitespace-nowrap rounded-xl border border-red-300 px-4 py-2 text-sm font-semibold text-red-600 transition hover:bg-red-50 disabled:opacity-40"
                  >
                    {complained.isBanned ? 'Banned' : banning ? 'Banning...' : 'Ban user'}
                  </button>
                )
              }
            />
          </div>
        )}
      </div>

      <Footer />
    </main>
  );
}

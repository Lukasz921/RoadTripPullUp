import { useNavigate } from 'react-router-dom';
import Navbar from '../components/layout/Navbar';
import Footer from '../components/layout/Footer';
import TripCard from '../components/TripCard';
import { useMyRides } from '../hooks/useMyRides';

export default function MyRidesPage() {
  const navigate = useNavigate();
  const { trips, loading, error } = useMyRides();

  return (
    <main className="flex min-h-screen flex-col bg-[#f3faee] text-[#12351f]">
      <Navbar />

      <div className="mx-auto w-full max-w-3xl flex-1 px-6 pb-16 pt-28">
        <div className="mb-6 flex items-center justify-between">
          <h1 className="text-3xl font-bold">My rides</h1>
          <button
            onClick={() => navigate('/add-trip')}
            className="rounded-xl bg-[#8cc63f] px-4 py-2 text-sm font-semibold text-[#12351f] hover:bg-[#a6dd55]"
          >
            Add trip
          </button>
        </div>

        {loading && <p className="text-sm text-[#5d7056]">Loading rides...</p>}

        {error && (
          <p className="rounded-lg bg-red-50 px-4 py-2 text-sm text-red-600">{error}</p>
        )}

        {!loading && !error && trips.length === 0 && (
          <p className="text-sm text-[#5d7056]">You haven't published any rides yet.</p>
        )}

        {!loading && !error && trips.length > 0 && (
          <div className="flex flex-col gap-3">
            {trips.map((trip) => (
              <TripCard key={trip.id} trip={trip} />
            ))}
          </div>
        )}
      </div>

      <Footer />
    </main>
  );
}

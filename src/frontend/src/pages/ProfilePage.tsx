import Navbar from '../components/layout/Navbar';
import Footer from '../components/layout/Footer';
import ProfileRow from './profile/components/ProfileRow';
import TripList from './profile/components/TripList';
import { goTo } from '../utils/scroll';
import { useMyTrips } from '../hooks/useMyTrips';

const user = {
  name: 'Imie',
  surname: 'Nazwisko',
  age: 22,
  sex: 'Not specified',
};

export default function ProfilePage() {
  const { trips: publishedTrips, loading, error } = useMyTrips();

  return (
    <main className="flex min-h-screen flex-col bg-[#f3faee] text-[#12351f]">
      <Navbar />

      <div className="mx-auto w-full max-w-3xl flex-1 px-6 pb-16 pt-28">
        <h1 className="mb-6 text-3xl font-bold">User profile</h1>

        <header className="rounded-2xl bg-white p-6 shadow-sm">
          <div className="grid gap-4 sm:grid-cols-2">
            <ProfileRow label="Name" value={user.name} />
            <ProfileRow label="Surname" value={user.surname} />
            <ProfileRow label="Age" value={user.age} />
            <ProfileRow label="Sex" value={user.sex} />
          </div>
        </header>

        <section className="mt-10">
          <div className="mb-4 flex items-center justify-between">
            <h2 className="text-2xl font-bold text-[#12351f]">Trips published</h2>
            <button
              onClick={() => goTo('add-trip')}
              className="rounded-xl bg-[#8cc63f] px-4 py-2 text-sm font-semibold text-[#12351f] hover:bg-[#a6dd55]"
            >
              Add trip
            </button>
          </div>

          {loading && (
            <p className="text-sm text-[#5d7056]">Loading trips...</p>
          )}

          {error && (
            <p className="rounded-lg bg-red-50 px-4 py-2 text-sm text-red-600">{error}</p>
          )}

          {!loading && !error && (
            <TripList
              title=""
              trips={publishedTrips}
            />
          )}
        </section>
      </div>

      <Footer />
    </main>
  );
}

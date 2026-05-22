import Navbar from '../components/layout/Navbar';
import Footer from '../components/layout/Footer';
import ProfileRow from './profile/components/ProfileRow';
import TripList, { type Trip } from './profile/components/TripList';
import { goTo } from '../utils/scroll';

const user = {
  name: 'Imie',
  surname: 'Nazwisko',
  age: 22,
  sex: 'Not specified',
};

const joinedTrips: Trip[] = [
  { id: 1, from: 'Warsaw', to: 'Kraków', date: '2026-02-14' },
  { id: 2, from: 'Warsaw', to: 'Gdańsk', date: '2026-02-22' },
  { id: 3, from: 'Łódź', to: 'Warsaw', date: '2026-03-03' },
];

const publishedTrips: Trip[] = [
  { id: 1, from: 'Warsaw', to: 'Poznań', date: '2026-02-18' },
  { id: 2, from: 'Warsaw', to: 'Wrocław', date: '2026-02-27' },
];

export default function ProfilePage() {
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

        <TripList
          title="Trips joined"
          trips={joinedTrips}
          actionLabel="Search trips"
          onAction={() => goTo('find-ride')}
        />

        <TripList
          title="Trips published"
          trips={publishedTrips}
          actionLabel="Publish trip"
          onAction={() => goTo('add-route')}
          actionVariant="green"
        />
      </div>

      <Footer />
    </main>
  );
}

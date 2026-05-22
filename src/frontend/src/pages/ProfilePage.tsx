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
  {
    id: 'a1b2c3d4-0001',
    driverId: 'drv-0001',
    source: { lat: 52.2297, lng: 21.0122 },
    target: { lat: 50.0647, lng: 19.9450 },
    departureTime: '2026-02-14T08:30:00',
    pricePerSeat: 45,
    availableSeats: 2,
    maxDetourMeters: 10000,
    actualDetourMeters: 3200,
  },
  {
    id: 'a1b2c3d4-0002',
    driverId: 'drv-0002',
    source: { lat: 52.2297, lng: 21.0122 },
    target: { lat: 54.3520, lng: 18.6466 },
    departureTime: '2026-02-22T06:00:00',
    pricePerSeat: 80,
    availableSeats: 1,
    maxDetourMeters: 5000,
    actualDetourMeters: 1500,
  },
];

const publishedTrips: Trip[] = [
  {
    id: 'a1b2c3d4-0003',
    driverId: 'drv-self',
    source: { lat: 52.2297, lng: 21.0122 },
    target: { lat: 52.4064, lng: 16.9252 },
    departureTime: '2026-02-18T07:00:00',
    pricePerSeat: 60,
    availableSeats: 3,
    maxDetourMeters: 8000,
    actualDetourMeters: 0,
  },
  {
    id: 'a1b2c3d4-0004',
    driverId: 'drv-self',
    source: { lat: 52.2297, lng: 21.0122 },
    target: { lat: 51.1079, lng: 17.0385 },
    departureTime: '2026-02-27T09:15:00',
    pricePerSeat: 55,
    availableSeats: 2,
    maxDetourMeters: 12000,
    actualDetourMeters: 0,
  },
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
          actionLabel="Join trip"
          onAction={() => goTo('join-trip')}
        />

        <TripList
          title="Trips published"
          trips={publishedTrips}
          actionLabel="Add trip"
          onAction={() => goTo('add-trip')}
          actionVariant="green"
        />
      </div>

      <Footer />
    </main>
  );
}

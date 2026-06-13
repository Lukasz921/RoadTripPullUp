import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import Navbar from '../components/layout/Navbar';
import Footer from '../components/layout/Footer';
import TripRouteMap from '../components/TripRouteMap';
import { getTripById, type TripDTO } from '../api/trips';
import { getUserById } from '../api/user';
import { reverseGeocode } from '../api/reverseGeocode';
import { useCurrentUser } from '../hooks/useCurrentUser';
import type { Place } from '../utils/geoapify';
import { formatDate, metersToKm, secondsToTime } from '../utils/format';

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-xs text-[#5d7056]">{label}</p>
      <p className="text-sm font-semibold text-[#12351f]">{value}</p>
    </div>
  );
}

export default function TripDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useCurrentUser();
  const [trip, setTrip] = useState<TripDTO | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [origin, setOrigin] = useState<Place | null>(null);
  const [destination, setDestination] = useState<Place | null>(null);
  const [nameById, setNameById] = useState<Record<string, string>>({});

  useEffect(() => {
    if (!id) return;
    getTripById(id)
      .then((data) => {
        setTrip(data);
        reverseGeocode(data.source.lat, data.source.lng).then((label) =>
          setOrigin({ label, lat: data.source.lat, lng: data.source.lng, placeId: '', city: '', country: '' })
        );
        reverseGeocode(data.target.lat, data.target.lng).then((label) =>
          setDestination({ label, lat: data.target.lat, lng: data.target.lng, placeId: '', city: '', country: '' })
        );
      })
      .catch(() => setError('Failed to load trip details.'))
      .finally(() => setLoading(false));
  }, [id]);

  // Resolve driver + passenger ids to "Name Surname".
  useEffect(() => {
    if (!trip) return;
    let cancelled = false;
    const ids = [trip.driverId, ...trip.passengerIds];
    Promise.all(
      ids.map(async (uid) => {
        const u = await getUserById(uid);
        return [uid, `${u.name} ${u.surname}`] as const;
      }),
    )
      .then((entries) => {
        if (!cancelled) setNameById(Object.fromEntries(entries));
      })
      .catch(() => {});
    return () => {
      cancelled = true;
    };
  }, [trip]);

  return (
    <main className="flex min-h-screen flex-col bg-[#f3faee] text-[#12351f]">
      <Navbar />

      <div className="mx-auto w-full max-w-3xl flex-1 px-6 pb-16 pt-28">
        <h1 className="mb-6 text-3xl font-bold">Trip details</h1>

        {loading && <p className="text-sm text-[#5d7056]">Loading...</p>}

        {error && (
          <p className="rounded-lg bg-red-50 px-4 py-2 text-sm text-red-600">{error}</p>
        )}

        {trip && (
          <div className="flex flex-col gap-6">
            <section className="rounded-2xl bg-white p-6 shadow-sm">
              <div className="grid gap-x-6 gap-y-4 sm:grid-cols-2">
                <Field
                  label="From"
                  value={origin?.label ?? `${trip.source.lat.toFixed(4)}, ${trip.source.lng.toFixed(4)}`}
                />
                <Field
                  label="To"
                  value={destination?.label ?? `${trip.target.lat.toFixed(4)}, ${trip.target.lng.toFixed(4)}`}
                />
                <Field label="Departure" value={formatDate(trip.departureTime)} />
                <Field label="Price per seat" value={`${trip.pricePerSeat} PLN`} />
                <Field label="Available seats" value={String(trip.availableSeats)} />
                <Field label="Max detour" value={metersToKm(trip.maxDetourMeters)} />
                <Field label="Route distance" value={metersToKm(trip.routeDistanceM)} />
                <Field label="Estimated duration" value={secondsToTime(trip.routeDurationS)} />
                <Field label="Passengers" value={String(trip.passengerIds.length)} />
                <Field label="Created" value={formatDate(trip.createdAt)} />
              </div>
            </section>

            {!!user && (trip.driverId === user.id || trip.passengerIds.includes(user.id)) && (
              <div className="flex gap-3">
                <button
                  type="button"
                  onClick={() => navigate(`/trip/${id}/chats`)}
                  className="flex-1 rounded-xl bg-[#12351f] px-4 py-3 text-sm font-semibold text-white hover:bg-[#1d4a2d]"
                >
                  Trip chats
                </button>
              </div>
            )}

            <section className="rounded-2xl bg-white p-6 shadow-sm">
              <h2 className="mb-4 text-lg font-semibold text-[#12351f]">People</h2>

              <p className="text-xs text-[#5d7056]">Driver</p>
              <button
                type="button"
                onClick={() => {
                  /* TODO: clickable target not defined yet */
                }}
                className="text-left text-sm font-semibold text-[#12351f] hover:underline"
              >
                {nameById[trip.driverId] ?? '…'}
              </button>

              <p className="mt-4 text-xs text-[#5d7056]">Passengers</p>
              {trip.passengerIds.length === 0 ? (
                <p className="text-sm text-[#5d7056]">No passengers yet.</p>
              ) : (
                <div className="flex flex-col items-start gap-1">
                  {trip.passengerIds.map((pid) => (
                    <button
                      key={pid}
                      type="button"
                      onClick={() => {
                        /* TODO: clickable target not defined yet */
                      }}
                      className="text-left text-sm font-semibold text-[#12351f] hover:underline"
                    >
                      {nameById[pid] ?? '…'}
                    </button>
                  ))}
                </div>
              )}
            </section>

            <section className="rounded-2xl bg-white p-4 shadow-sm" style={{ height: '420px' }}>
              <h2 className="mb-3 text-lg font-semibold text-[#12351f]">Route</h2>
              <div className="h-[calc(100%-2.5rem)]">
                <TripRouteMap origin={origin} destination={destination} polylinePoints={trip.routePolylinePoints} />
              </div>
            </section>
          </div>
        )}
      </div>

      <Footer />
    </main>
  );
}

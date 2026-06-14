import { useEffect, useState } from 'react';
import { useParams, useNavigate, useLocation } from 'react-router-dom';
import Navbar from '../components/layout/Navbar';
import Footer from '../components/layout/Footer';
import TripRouteMap from '../components/TripRouteMap';
import { getTripById, rateUser, fileComplaint, type TripDTO } from '../api/trips';
import { getUserById, type CurrentUser } from '../api/user';
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
  const location = useLocation();
  const canRate = (location.state as { canRate?: boolean } | null)?.canRate ?? false;
  const { user } = useCurrentUser();
  const [trip, setTrip] = useState<TripDTO | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [origin, setOrigin] = useState<Place | null>(null);
  const [destination, setDestination] = useState<Place | null>(null);
  const [userById, setUserById] = useState<Record<string, CurrentUser>>({});
  const [rateOpen, setRateOpen] = useState(false);
  const [complaintOpen, setComplaintOpen] = useState(false);

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

  // Resolve driver + passenger ids to full user records (used for display and passed on click).
  useEffect(() => {
    if (!trip) return;
    let cancelled = false;
    const ids = [trip.driverId, ...trip.passengerIds];
    Promise.all(
      ids.map(async (uid) => {
        const u = await getUserById(uid);
        return [uid, u] as const;
      }),
    )
      .then((entries) => {
        if (!cancelled) setUserById(Object.fromEntries(entries));
      })
      .catch(() => {});
    return () => {
      cancelled = true;
    };
  }, [trip]);

  // Clicking yourself goes to the editable profile; clicking anyone else to their read-only profile.
  function openUser(uid: string) {
    if (uid === user?.id) {
      navigate('/profile');
    } else {
      navigate(`/user/${uid}`, { state: { user: userById[uid] } });
    }
  }

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
                {canRate && trip.driverId !== user?.id && (
                  <button
                    type="button"
                    onClick={() => setRateOpen(true)}
                    className="flex-1 rounded-xl border border-[#12351f] px-4 py-3 text-sm font-semibold text-[#12351f] hover:bg-[#e8f5e0]"
                  >
                    Rate trip
                  </button>
                )}
                <button
                  type="button"
                  onClick={() => setComplaintOpen(true)}
                  className="flex-1 rounded-xl border border-red-300 px-4 py-3 text-sm font-semibold text-red-600 hover:bg-red-50"
                >
                  File complaint
                </button>
              </div>
            )}

            <section className="rounded-2xl bg-white p-6 shadow-sm">
              <h2 className="mb-4 text-lg font-semibold text-[#12351f]">People</h2>

              <p className="text-xs text-[#5d7056]">Driver</p>
              <button
                type="button"
                onClick={() => openUser(trip.driverId)}
                className="text-left text-sm font-semibold text-[#12351f] hover:underline"
              >
                {userById[trip.driverId]
                  ? `${userById[trip.driverId].name} ${userById[trip.driverId].surname}`
                  : '…'}
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
                      onClick={() => openUser(pid)}
                      className="text-left text-sm font-semibold text-[#12351f] hover:underline"
                    >
                      {userById[pid] ? `${userById[pid].name} ${userById[pid].surname}` : '…'}
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

      {rateOpen && trip && (
        <RateDriverModal
          tripId={trip.id}
          driverId={trip.driverId}
          driverName={
            userById[trip.driverId]
              ? `${userById[trip.driverId].name} ${userById[trip.driverId].surname}`
              : 'the driver'
          }
          onClose={() => setRateOpen(false)}
        />
      )}

      {complaintOpen && trip && user && (
        <FileComplaintModal
          tripId={trip.id}
          participants={[trip.driverId, ...trip.passengerIds].filter((uid) => uid !== user.id)}
          userById={userById}
          onClose={() => setComplaintOpen(false)}
        />
      )}

      <Footer />
    </main>
  );
}

function RateDriverModal({
  tripId,
  driverId,
  driverName,
  onClose,
}: {
  tripId: string;
  driverId: string;
  driverName: string;
  onClose: () => void;
}) {
  const [value, setValue] = useState(0);
  const [hover, setHover] = useState(0);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [done, setDone] = useState(false);

  async function submit() {
    if (value < 1 || value > 5) return;
    setSubmitting(true);
    setError('');
    try {
      await rateUser(tripId, { userId: driverId, value });
      setDone(true);
    } catch (err) {
      const status = (err as { response?: { status?: number } }).response?.status;
      if (status === 409) setError('You have already rated this driver for this trip.');
      else if (status === 400) setError('This trip cannot be rated.');
      else setError('Failed to submit rating. Please try again.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4"
      onClick={onClose}
    >
      <div
        className="w-full max-w-sm rounded-2xl bg-white p-6 shadow-lg"
        onClick={(e) => e.stopPropagation()}
      >
        {done ? (
          <div className="text-center">
            <h2 className="mb-2 text-lg font-semibold text-[#12351f]">Thanks for your rating!</h2>
            <button
              type="button"
              onClick={onClose}
              className="mt-2 w-full rounded-xl bg-[#12351f] px-4 py-3 text-sm font-semibold text-white hover:bg-[#1d4a2d]"
            >
              Close
            </button>
          </div>
        ) : (
          <>
            <h2 className="mb-1 text-lg font-semibold text-[#12351f]">Rate {driverName}</h2>
            <p className="mb-4 text-sm text-[#5d7056]">How was your ride with the driver?</p>

            <div className="mb-4 flex justify-center gap-2">
              {[1, 2, 3, 4, 5].map((star) => (
                <button
                  key={star}
                  type="button"
                  onMouseEnter={() => setHover(star)}
                  onMouseLeave={() => setHover(0)}
                  onClick={() => setValue(star)}
                  className={`text-3xl leading-none transition ${
                    star <= (hover || value) ? 'text-[#f5b301]' : 'text-[#d7e8c8]'
                  }`}
                  aria-label={`${star} star${star > 1 ? 's' : ''}`}
                >
                  ★
                </button>
              ))}
            </div>

            {error && (
              <p className="mb-3 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-600">{error}</p>
            )}

            <div className="flex gap-3">
              <button
                type="button"
                onClick={onClose}
                className="flex-1 rounded-xl border border-[#12351f] px-4 py-3 text-sm font-semibold text-[#12351f] hover:bg-[#e8f5e0]"
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={submit}
                disabled={value < 1 || submitting}
                className="flex-1 rounded-xl bg-[#12351f] px-4 py-3 text-sm font-semibold text-white hover:bg-[#1d4a2d] disabled:opacity-40"
              >
                {submitting ? 'Submitting...' : 'Submit'}
              </button>
            </div>
          </>
        )}
      </div>
    </div>
  );
}

function FileComplaintModal({
  tripId,
  participants,
  userById,
  onClose,
}: {
  tripId: string;
  participants: string[];
  userById: Record<string, CurrentUser>;
  onClose: () => void;
}) {
  const [complainedUserId, setComplainedUserId] = useState('');
  const [reason, setReason] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [done, setDone] = useState(false);

  async function submit() {
    if (!complainedUserId || reason.trim().length === 0) return;
    setSubmitting(true);
    setError('');
    try {
      await fileComplaint(tripId, { complainedUserId, reason: reason.trim() });
      setDone(true);
    } catch (err) {
      const status = (err as { response?: { status?: number } }).response?.status;
      if (status === 403) setError('You can only file complaints for trips you took part in.');
      else if (status === 400) setError('This complaint could not be filed.');
      else setError('Failed to file complaint. Please try again.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4" onClick={onClose}>
      <div className="w-full max-w-sm rounded-2xl bg-white p-6 shadow-lg" onClick={(e) => e.stopPropagation()}>
        {done ? (
          <div className="text-center">
            <h2 className="mb-2 text-lg font-semibold text-[#12351f]">Complaint submitted</h2>
            <p className="mb-4 text-sm text-[#5d7056]">Thanks — our team will review it.</p>
            <button
              type="button"
              onClick={onClose}
              className="w-full rounded-xl bg-[#12351f] px-4 py-3 text-sm font-semibold text-white hover:bg-[#1d4a2d]"
            >
              Close
            </button>
          </div>
        ) : (
          <>
            <h2 className="mb-4 text-lg font-semibold text-[#12351f]">File a complaint</h2>

            <label className="mb-1 block text-xs text-[#5d7056]">Who is this about?</label>
            <select
              value={complainedUserId}
              onChange={(e) => setComplainedUserId(e.target.value)}
              className="mb-4 w-full rounded-xl border border-[#d7e8c8] px-3 py-2 text-sm text-[#12351f] focus:border-[#12351f] focus:outline-none"
            >
              <option value="">Select a person…</option>
              {participants.map((uid) => (
                <option key={uid} value={uid}>
                  {userById[uid] ? `${userById[uid].name} ${userById[uid].surname}` : uid}
                </option>
              ))}
            </select>

            <label className="mb-1 block text-xs text-[#5d7056]">Reason</label>
            <textarea
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              rows={4}
              placeholder="Describe what happened…"
              className="mb-4 w-full resize-none rounded-xl border border-[#d7e8c8] px-3 py-2 text-sm text-[#12351f] focus:border-[#12351f] focus:outline-none"
            />

            {error && (
              <p className="mb-3 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-600">{error}</p>
            )}

            <div className="flex gap-3">
              <button
                type="button"
                onClick={onClose}
                className="flex-1 rounded-xl border border-[#12351f] px-4 py-3 text-sm font-semibold text-[#12351f] hover:bg-[#e8f5e0]"
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={submit}
                disabled={!complainedUserId || reason.trim().length === 0 || submitting}
                className="flex-1 rounded-xl bg-red-600 px-4 py-3 text-sm font-semibold text-white hover:bg-red-700 disabled:opacity-40"
              >
                {submitting ? 'Submitting...' : 'Submit'}
              </button>
            </div>
          </>
        )}
      </div>
    </div>
  );
}

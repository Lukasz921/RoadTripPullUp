import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import Navbar from '../../components/layout/Navbar';
import Footer from '../../components/layout/Footer';
import { getConversation, type ConversationDTO } from '../../api/messages';
import { acceptTripRequest, getTripById, getTripRequestByConversation, type TripDTO, type TripRequestDTO } from '../../api/trips';
import { getUserById } from '../../api/user';
import { reverseGeocode } from '../../api/reverseGeocode';
import { metersToKm } from '../../utils/format';
import { useCurrentUser } from '../../hooks/useCurrentUser';
import TripRequestMap from '../../components/TripRequestMap';
import Chat from './Chat';

export default function ChatPage() {
  const { id: conversationId } = useParams<{ id: string }>();
  const { user } = useCurrentUser();
  const [conversation, setConversation] = useState<ConversationDTO | null>(null);
  const [trip, setTrip] = useState<TripDTO | null>(null);
  const [startPlace, setStartPlace] = useState('');
  const [endPlace, setEndPlace] = useState('');
  const [nameById, setNameById] = useState<Record<string, string>>({});
  const [request, setRequest] = useState<TripRequestDTO | null>(null);
  const [pickupPlace, setPickupPlace] = useState('');
  const [dropoffPlace, setDropoffPlace] = useState('');
  const [joining, setJoining] = useState(false);
  const [added, setAdded] = useState(false);
  const [joinError, setJoinError] = useState('');

  // Fetch the conversation.
  useEffect(() => {
    if (!conversationId) return;
    getConversation(conversationId).then(setConversation).catch(() => {});
  }, [conversationId]);

  // Fetch the trip request behind this direct conversation (if any) and label its endpoints.
  useEffect(() => {
    if (!conversationId) return;
    let cancelled = false;
    getTripRequestByConversation(conversationId)
      .then(async (req) => {
        if (cancelled || !req) return;
        setRequest(req);
        const [pu, dof] = await Promise.all([
          reverseGeocode(req.pickup.lat, req.pickup.lng),
          reverseGeocode(req.dropoff.lat, req.dropoff.lng),
        ]);
        if (cancelled) return;
        setPickupPlace(pu);
        setDropoffPlace(dof);
      })
      .catch(() => {});
    return () => {
      cancelled = true;
    };
  }, [conversationId]);

  // Fetch the trip this conversation belongs to and resolve its endpoints to place names.
  useEffect(() => {
    if (!conversation) return;
    let cancelled = false;
    getTripById(conversation.tripId)
      .then(async (t) => {
        if (cancelled) return;
        setTrip(t);
        const [start, end] = await Promise.all([
          reverseGeocode(t.source.lat, t.source.lng),
          reverseGeocode(t.target.lat, t.target.lng),
        ]);
        if (cancelled) return;
        setStartPlace(start);
        setEndPlace(end);
      })
      .catch(() => {});
    return () => {
      cancelled = true;
    };
  }, [conversation]);

  // Resolve every participant id to "Name Surname" once; reuse the current user's own data for self.
  useEffect(() => {
    if (!conversation || !user) return;
    let cancelled = false;
    Promise.all(
      conversation.participants.map(async (id) => {
        if (id === user.id) return [id, `${user.name} ${user.surname}`] as const;
        const u = await getUserById(id);
        return [id, `${u.name} ${u.surname}`] as const;
      }),
    )
      .then((entries) => {
        if (!cancelled) setNameById(Object.fromEntries(entries));
      })
      .catch(() => {});
    return () => {
      cancelled = true;
    };
  }, [conversation, user]);

  const otherNames =
    conversation && user
      ? conversation.participants
          .filter((id) => id !== user.id)
          .map((id) => nameById[id])
          .filter(Boolean)
      : [];

  const isGroup = conversation?.type?.toLowerCase() === 'group';
  const isDriver = !!trip && !!user && trip.driverId === user.id;
  // The participant who would be added (the other person in a direct chat).
  const passengerId = conversation && user
    ? conversation.participants.find((id) => id !== user.id)
    : undefined;
  const alreadyPassenger = !!trip && !!passengerId && trip.passengerIds.includes(passengerId);
  const requestPending = request?.status === 'PENDING';
  // Only the driver can accept, and only while there is a pending request for a seat that's still free.
  const showAddToTrip = isDriver && !isGroup && !!request && requestPending && !alreadyPassenger;

  async function addToTripHandler() {
    if (!trip || !request) return;
    setJoining(true);
    setJoinError('');
    try {
      await acceptTripRequest(trip.id, request.id);
      setAdded(true);
      setRequest({ ...request, status: 'ACCEPTED' });
    } catch {
      setJoinError('Failed to add to trip. Please try again.');
    } finally {
      setJoining(false);
    }
  }

  return (
    <main className="flex min-h-screen flex-col bg-[#f3faee] text-[#12351f]">
      <Navbar />
      <div className="mx-auto flex w-full max-w-3xl flex-1 flex-col px-6 pb-16 pt-28">
        {conversation && (
          <div className="mb-6 flex items-start justify-between gap-4 rounded-2xl bg-white p-6 shadow-sm">
            <div>
              <h1 className="text-2xl font-bold">
                {isGroup ? 'Group chat' : otherNames.length > 0 ? otherNames.join(', ') : 'Chat'}
              </h1>
              {trip && (
                <p className="mt-1 text-sm text-[#5d7056]">
                  {startPlace || '…'} → {endPlace || '…'}
                </p>
              )}
            </div>
            {showAddToTrip && (
              <div className="shrink-0 text-right">
                <button
                  type="button"
                  onClick={addToTripHandler}
                  disabled={joining || added}
                  className="rounded-xl bg-[#8cc63f] px-4 py-2 text-sm font-semibold text-[#12351f] hover:bg-[#a6dd55] disabled:opacity-60"
                >
                  {added ? 'Added' : joining ? 'Joining…' : 'Add to trip'}
                </button>
                {joinError && (
                  <p className="mt-2 text-sm text-red-600">{joinError}</p>
                )}
              </div>
            )}
          </div>
        )}

        {request && !isGroup && (
          <div className="mb-6 rounded-2xl bg-white p-6 shadow-sm">
            <div className="mb-3 flex items-center justify-between gap-4">
              <h2 className="text-lg font-semibold">Trip request</h2>
              <span
                className={`rounded-full px-3 py-1 text-xs font-semibold ${
                  request.status === 'ACCEPTED'
                    ? 'bg-[#dff0c8] text-[#12351f]'
                    : 'bg-[#fff3cd] text-[#7a5b00]'
                }`}
              >
                {request.status === 'ACCEPTED' ? 'Added to trip' : 'Pending'}
              </span>
            </div>
            <p className="text-sm text-[#5d7056]">
              Wants a ride:{' '}
              <span className="font-semibold text-[#12351f]">{pickupPlace || '…'}</span> →{' '}
              <span className="font-semibold text-[#12351f]">{dropoffPlace || '…'}</span>
            </p>
            <p className="mt-1 text-sm text-[#5d7056]">
              Extra detour:{' '}
              <span className="font-semibold text-[#12351f]">{metersToKm(request.detourMeters)}</span>
            </p>
            {trip && (
              <div className="mt-4 h-80">
                <TripRequestMap
                  tripStart={trip.source}
                  tripEnd={trip.target}
                  pickup={request.pickup}
                  dropoff={request.dropoff}
                  tripPolyline={trip.routePolylinePoints}
                  requestPolyline={request.previewPolyline}
                  labels={{ tripStart: startPlace, tripEnd: endPlace, pickup: pickupPlace, dropoff: dropoffPlace }}
                />
              </div>
            )}
          </div>
        )}

        {conversation && user && (
          <Chat conversation={conversation} currentUserId={user.id} nameById={nameById} />
        )}
      </div>
      <Footer />
    </main>
  );
}

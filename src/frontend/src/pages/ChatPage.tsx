import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import Navbar from '../components/layout/Navbar';
import Footer from '../components/layout/Footer';
import { getConversation, type ConversationDTO } from '../api/messages';
import { addToTrip, getTripById, type TripDTO } from '../api/trips';
import { getUserById } from '../api/user';
import { reverseGeocode } from '../api/reverseGeocode';
import { useCurrentUser } from '../hooks/useCurrentUser';

export default function ChatPage() {
  const { id: conversationId } = useParams<{ id: string }>();
  const { user } = useCurrentUser();
  const [conversation, setConversation] = useState<ConversationDTO | null>(null);
  const [trip, setTrip] = useState<TripDTO | null>(null);
  const [startPlace, setStartPlace] = useState('');
  const [endPlace, setEndPlace] = useState('');
  const [participantNames, setParticipantNames] = useState<string[]>([]);
  const [joining, setJoining] = useState(false);
  const [joinError, setJoinError] = useState('');

  // Fetch the conversation.
  useEffect(() => {
    if (!conversationId) return;
    getConversation(conversationId).then(setConversation).catch(() => {});
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

  // Fetch names of the participants other than the logged-in user.
  useEffect(() => {
    if (!conversation || !user) return;
    const otherIds = conversation.participants.filter((id) => id !== user.id);
    let cancelled = false;
    Promise.all(otherIds.map((id) => getUserById(id)))
      .then((users) => {
        if (!cancelled) setParticipantNames(users.map((u) => `${u.name} ${u.surname}`));
      })
      .catch(() => {});
    return () => {
      cancelled = true;
    };
  }, [conversation, user]);

  const isGroup = conversation?.type?.toLowerCase() === 'group';
  const isDriver = !!trip && !!user && trip.driverId === user.id;
  const showAddToTrip = isDriver && !isGroup;

  async function addToTripHandler() {
    if (!conversation || !user) return;
    const passengerId = conversation.participants.find((id) => id !== user.id);
    if (!passengerId) return;
    setJoining(true);
    setJoinError('');
    try {
      await addToTrip(conversation.tripId, passengerId);
    } catch {
      setJoinError('Failed to add to trip. Please try again.');
    } finally {
      setJoining(false);
    }
  }

  return (
    <main className="flex min-h-screen flex-col bg-[#f3faee] text-[#12351f]">
      <Navbar />
      <div className="mx-auto w-full max-w-3xl flex-1 px-6 pb-16 pt-28">
        {conversation && (
          <div className="mb-6 flex items-start justify-between gap-4 rounded-2xl bg-white p-6 shadow-sm">
            <div>
              <h1 className="text-2xl font-bold">
                {participantNames.length > 0 ? participantNames.join(', ') : 'Chat'}
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
                  disabled={joining}
                  className="rounded-xl bg-[#8cc63f] px-4 py-2 text-sm font-semibold text-[#12351f] hover:bg-[#a6dd55] disabled:opacity-60"
                >
                  {joining ? 'Joining…' : 'Add to trip'}
                </button>
                {joinError && (
                  <p className="mt-2 text-sm text-red-600">{joinError}</p>
                )}
              </div>
            )}
          </div>
        )}
      </div>
      <Footer />
    </main>
  );
}

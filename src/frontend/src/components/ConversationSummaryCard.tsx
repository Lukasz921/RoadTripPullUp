import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import type { ConversationDTO } from '../api/messages';
import { getTripById, type TripDTO } from '../api/trips';
import { getUserById } from '../api/user';
import { reverseGeocode } from '../api/reverseGeocode';
import { useCurrentUser } from '../hooks/useCurrentUser';
import { formatDate } from '../utils/format';

interface ConversationSummaryCardProps {
  conversation: ConversationDTO;
  navigationState?: Record<string, unknown>;
}

// Conversations with no messages get an epoch (1970) last-message date — treat those as "no last message".
function hasRealLastMessage(iso?: string): boolean {
  return !!iso && new Date(iso).getFullYear() > 1970;
}

export default function ConversationSummaryCard({ conversation, navigationState }: ConversationSummaryCardProps) {
  const navigate = useNavigate();
  const { user } = useCurrentUser();
  const isGroup = conversation.type?.toLowerCase() === 'group';

  const [trip, setTrip] = useState<TripDTO | null>(null);
  const [startPlace, setStartPlace] = useState('');
  const [endPlace, setEndPlace] = useState('');
  const [otherName, setOtherName] = useState('');

  // Fetch the trip this conversation belongs to, then resolve its endpoints to place names.
  useEffect(() => {
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
  }, [conversation.tripId]);

  // For direct chats, show the other person's name + surname (the participant that isn't the logged-in user).
  useEffect(() => {
    if (isGroup || !user) return;
    const otherId = conversation.participants.find((id) => id !== user.id);
    if (!otherId) return;
    let cancelled = false;
    getUserById(otherId)
      .then((u) => {
        if (!cancelled) setOtherName(`${u.name} ${u.surname}`);
      })
      .catch(() => {});
    return () => {
      cancelled = true;
    };
  }, [isGroup, conversation.participants, user]);

  const title = isGroup
    ? 'Group chat'
    : otherName
      ? `Private chat with ${otherName}`
      : 'Private chat';

  // Direct chats are between the driver and one other user. If that user isn't a
  // passenger on the trip, flag it as "New" (e.g. someone asking to join).
  const otherParticipantId = isGroup
    ? undefined
    : conversation.participants.find((id) => id !== trip?.driverId);
  const isNonPassenger =
    !isGroup && !!trip && !!otherParticipantId && !trip.passengerIds.includes(otherParticipantId);

  const showLastMessageDate = hasRealLastMessage(conversation.lastMessageCreatedAt);

  return (
    <div
      onClick={() => navigate(`/conversation/${conversation.conversationId}`, { state: navigationState })}
      className={`cursor-pointer rounded-xl border bg-white px-5 py-4 transition hover:bg-[#f0f9e8] ${
        isGroup ? 'border-[#8cc63f] border-l-4' : 'border-[#d7e8c8]'
      }`}
    >
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-center gap-2">
          {isGroup && (
            <span className="rounded-full bg-[#8cc63f] px-2 py-0.5 text-xs font-semibold text-white">
              Group
            </span>
          )}
          {isNonPassenger && (
            <span className="rounded-full bg-amber-500 px-2 py-0.5 text-xs font-semibold text-white">
              New
            </span>
          )}
          <p className="font-semibold text-[#12351f]">{title}</p>
        </div>
        {showLastMessageDate && (
          <p className="shrink-0 text-xs text-[#5d7056]">
            {formatDate(conversation.lastMessageCreatedAt)}
          </p>
        )}
      </div>

      {trip && (
        <div className="mt-2 text-sm text-[#5d7056]">
          <p>{formatDate(trip.departureTime)}</p>
          <p className="font-medium text-[#12351f]">
            {startPlace || '…'} → {endPlace || '…'}
          </p>
        </div>
      )}

      {conversation.lastMessagePreview && (
        <p className="mt-1 truncate text-sm text-[#5d7056]">{conversation.lastMessagePreview}</p>
      )}

      <p className="mt-2 text-xs text-[#5d7056]">
        {conversation.participants.length} participant{conversation.participants.length !== 1 ? 's' : ''}
      </p>
    </div>
  );
}

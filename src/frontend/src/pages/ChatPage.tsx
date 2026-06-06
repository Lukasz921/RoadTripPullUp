import { useEffect, useState } from 'react';
import { useParams, useLocation } from 'react-router-dom';
import Navbar from '../components/layout/Navbar';
import Footer from '../components/layout/Footer';
import { getConversation, type ConversationDTO } from '../api/messages';
import { joinTrip } from '../api/trips';

export default function ChatPage() {
  const { id: conversationId } = useParams<{ id: string }>();
  const { state } = useLocation();
  const showAddToTrip: boolean = state?.showAddToTrip ?? false;
  const [conversation, setConversation] = useState<ConversationDTO | null>(null);
  const [joining, setJoining] = useState(false);
  const [joinError, setJoinError] = useState('');

  async function handleAddToTrip() {
    if (!conversation) return;
    setJoining(true);
    setJoinError('');
    try {
      await joinTrip(conversation.TripId);
    } catch {
      setJoinError('Failed to join trip. Please try again.');
    } finally {
      setJoining(false);
    }
  }

  useEffect(() => {
    if (!conversationId) return;
    getConversation(conversationId).then(setConversation).catch(() => {});
  }, [conversationId]);

  return (
    <main className="flex min-h-screen flex-col bg-[#f3faee] text-[#12351f]">
      <Navbar />
      <div className="mx-auto w-full max-w-3xl flex-1 px-6 pb-16 pt-28">
        {conversation && (
          <div className="mb-6 rounded-2xl bg-white p-6 shadow-sm">
            <h1 className="text-2xl font-bold">
              {conversation.Name ?? (conversation.Type === 'Group' ? 'Group chat' : 'Direct chat')}
            </h1>
            <p className="mt-1 text-sm text-[#5d7056]">
              {conversation.Participants.length} participant{conversation.Participants.length !== 1 ? 's' : ''}
            </p>
            {showAddToTrip && (
              <div className="mt-4">
                <button
                  type="button"
                  onClick={handleAddToTrip}
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

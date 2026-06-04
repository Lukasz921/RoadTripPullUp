import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import Navbar from '../components/layout/Navbar';
import Footer from '../components/layout/Footer';
import ConversationSummaryCard from '../components/ConversationSummaryCard';
import { getGroupConversationByTrip, getDirectConversationsByTrip, type ConversationDTO } from '../api/messages';

export default function ChatsPage() {
  const { id: tripId } = useParams<{ id: string }>();

  const [groupConversation, setGroupConversation] = useState<ConversationDTO | null>(null);
  const [directConversations, setDirectConversations] = useState<ConversationDTO[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!tripId) return;
    setLoading(true);
    setError('');

    Promise.all([
      getGroupConversationByTrip(tripId),
      getDirectConversationsByTrip(tripId),
    ])
      .then(([group, directs]) => {
        setGroupConversation(group);
        setDirectConversations(directs);
      })
      .catch(() => setError('Failed to load conversations.'))
      .finally(() => setLoading(false));
  }, [tripId]);

  return (
    <main className="flex min-h-screen flex-col bg-[#f3faee] text-[#12351f]">
      <Navbar />
      <div className="mx-auto w-full max-w-3xl flex-1 px-6 pb-16 pt-28">
        <h1 className="mb-6 text-3xl font-bold">Chats</h1>

        {loading && <p className="text-sm text-[#5d7056]">Loading conversations...</p>}

        {error && (
          <p className="rounded-lg bg-red-50 px-4 py-2 text-sm text-red-600">{error}</p>
        )}

        {!loading && !error && (
          <div className="flex flex-col gap-3">
            {groupConversation && (
              <ConversationSummaryCard conversation={groupConversation} isGroup />
            )}

            {directConversations.length > 0 && (
              <>
                {groupConversation && (
                  <div className="my-1 border-t border-[#d7e8c8]" />
                )}
                {directConversations.map((conv) => (
                  <ConversationSummaryCard key={conv.ConversationId} conversation={conv} />
                ))}
              </>
            )}

            {!groupConversation && directConversations.length === 0 && (
              <p className="text-sm text-[#5d7056]">No conversations yet.</p>
            )}
          </div>
        )}
      </div>
      <Footer />
    </main>
  );
}

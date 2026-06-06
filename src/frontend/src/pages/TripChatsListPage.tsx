import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import Navbar from '../components/layout/Navbar';
import Footer from '../components/layout/Footer';
import ConversationList from '../components/ConversationList';
import { getGroupConversationByTrip, getDirectConversationsByTrip, type ConversationDTO } from '../api/messages';

export default function TripChatsListPage() {
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

        <ConversationList
          conversations={[
            ...(groupConversation ? [groupConversation] : []),
            ...directConversations,
          ]}
          loading={loading}
          error={error}
        />
      </div>
      <Footer />
    </main>
  );
}

import { useEffect, useState } from 'react';
import Navbar from '../components/layout/Navbar';
import Footer from '../components/layout/Footer';
import ConversationList from '../components/ConversationList';
import { getConversations, type ConversationDTO } from '../api/messages';

export default function MyConversationsPage() {
  const [conversations, setConversations] = useState<ConversationDTO[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    getConversations()
      .then(setConversations)
      .catch(() => setError('Failed to load conversations.'))
      .finally(() => setLoading(false));
  }, []);

  return (
    <main className="flex min-h-screen flex-col bg-[#f3faee] text-[#12351f]">
      <Navbar />
      <div className="mx-auto w-full max-w-3xl flex-1 px-6 pb-16 pt-28">
        <h1 className="mb-6 text-3xl font-bold">My conversations</h1>
        <ConversationList conversations={conversations} loading={loading} error={error} />
      </div>
      <Footer />
    </main>
  );
}

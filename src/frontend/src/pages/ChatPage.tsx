import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import Navbar from '../components/layout/Navbar';
import Footer from '../components/layout/Footer';
import { getConversation, type ConversationDTO } from '../api/messages';

export default function ChatPage() {
  const { id: conversationId } = useParams<{ id: string }>();
  const [conversation, setConversation] = useState<ConversationDTO | null>(null);

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
          </div>
        )}
      </div>
      <Footer />
    </main>
  );
}

import { useEffect, useRef, useState } from 'react';
import { getMessages, sendMessage, type ConversationDTO, type MessageDTO } from '../api/messages';
import { formatDate } from '../utils/format';

interface ChatProps {
  conversation: ConversationDTO;
  currentUserId: string;
  // id -> "Name Surname", resolved once by the parent so we don't refetch users here.
  nameById: Record<string, string>;
}

export default function Chat({ conversation, currentUserId, nameById }: ChatProps) {
  const [messages, setMessages] = useState<MessageDTO[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [draft, setDraft] = useState('');
  const [sending, setSending] = useState(false);
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError('');
    getMessages(conversation.conversationId)
      .then((msgs) => {
        if (!cancelled) setMessages(msgs);
      })
      .catch(() => {
        if (!cancelled) setError('Failed to load messages.');
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [conversation.conversationId]);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  async function handleSend(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const text = draft.trim();
    if (!text) return;
    setSending(true);
    setError('');
    try {
      await sendMessage({ conversationId: conversation.conversationId, payload: { text } });
      setDraft('');
      const msgs = await getMessages(conversation.conversationId);
      setMessages(msgs);
    } catch {
      setError('Failed to send message.');
    } finally {
      setSending(false);
    }
  }

  // Oldest first so the newest message ends up at the bottom.
  const orderedMessages = [...messages].sort(
    (a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime(),
  );

  return (
    <div className="flex min-h-0 flex-1 flex-col rounded-2xl bg-white p-6 shadow-sm">
      <div className="min-h-0 flex-1 overflow-y-auto">
        <div className="flex min-h-full flex-col justify-end gap-3">
        {loading && <p className="text-sm text-[#5d7056]">Loading messages...</p>}
        {error && <p className="rounded-lg bg-red-50 px-4 py-2 text-sm text-red-600">{error}</p>}
        {!loading && messages.length === 0 && (
          <p className="text-sm text-[#5d7056]">No messages yet. Say hi!</p>
        )}

        {orderedMessages.map((m) => {
          const mine = m.senderId === currentUserId;
          const text = String(m.payload?.text ?? '');
          return (
            <div key={m.messageId} className={`flex ${mine ? 'justify-end' : 'justify-start'}`}>
              <div
                className={`max-w-[75%] rounded-2xl px-4 py-2 ${
                  mine ? 'bg-[#8cc63f]' : 'bg-[#f3faee] ring-1 ring-[#d7e8c8]'
                }`}
              >
                {!mine && (
                  <p className="text-xs font-semibold text-[#5d7056]">
                    {nameById[m.senderId] ?? 'Unknown'}
                  </p>
                )}
                <p className="text-sm text-[#12351f]">{text}</p>
                <p className="mt-1 text-[10px] text-[#5d7056]">{formatDate(m.createdAt)}</p>
              </div>
            </div>
          );
        })}
        <div ref={bottomRef} />
        </div>
      </div>

      <form onSubmit={handleSend} className="mt-4 flex shrink-0 gap-2">
        <input
          type="text"
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          placeholder="Write a message…"
          className="h-12 flex-1 rounded-xl border border-[#d7e8c8] bg-white px-4 text-[#12351f] outline-none focus:border-[#8cc63f]"
        />
        <button
          type="submit"
          disabled={sending || !draft.trim()}
          className="rounded-xl bg-[#12351f] px-5 font-semibold text-white hover:bg-[#1d4a2d] disabled:opacity-60"
        >
          {sending ? 'Sending…' : 'Send'}
        </button>
      </form>
    </div>
  );
}

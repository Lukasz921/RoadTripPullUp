import { useEffect, useLayoutEffect, useRef, useState } from 'react';
import { getMessages, sendMessage, type ConversationDTO, type MessageDTO } from '../../api/messages';
import {
  createChatConnection,
  eventToMessage,
  isConnected,
  joinConversation,
  leaveConversation,
  sendMessageOverHub,
  type MessageCreatedEvent,
} from '../../api/chatHub';
import type { HubConnection } from '@microsoft/signalr';
import { formatDate } from '../../utils/format';

interface ChatProps {
  conversation: ConversationDTO;
  currentUserId: string;
  // id -> "Name Surname", resolved once by the parent so we don't refetch users here.
  nameById: Record<string, string>;
}

const PAGE_SIZE = 20; // matches the backend's default window size

function mergeUnique(a: MessageDTO[], b: MessageDTO[]): MessageDTO[] {
  const byId = new Map<string, MessageDTO>();
  for (const m of a) byId.set(m.messageId, m);
  for (const m of b) byId.set(m.messageId, m);
  return Array.from(byId.values());
}

export default function Chat({ conversation, currentUserId, nameById }: ChatProps) {
  const conversationId = conversation.conversationId;

  const [messages, setMessages] = useState<MessageDTO[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [hasMore, setHasMore] = useState(true);
  const [error, setError] = useState('');
  const [draft, setDraft] = useState('');
  const [sending, setSending] = useState(false);

  const scrollRef = useRef<HTMLDivElement>(null);
  const connectionRef = useRef<HubConnection | null>(null);
  const loadingMoreRef = useRef(false);
  // When set, the next layout pass restores scroll position after prepending older messages.
  const prevScrollHeightRef = useRef<number | null>(null);
  // Whether the view should stick to the bottom on the next message append.
  const stickToBottomRef = useRef(true);

  // Initial page (newest messages) whenever the conversation changes.
  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError('');
    setHasMore(true);
    stickToBottomRef.current = true;
    prevScrollHeightRef.current = null;
    getMessages(conversationId)
      .then((msgs) => {
        if (cancelled) return;
        setMessages(msgs);
        if (msgs.length < PAGE_SIZE) setHasMore(false);
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
  }, [conversationId]);

  // Live updates via SignalR.
  useEffect(() => {
    const conn = createChatConnection();
    connectionRef.current = conn;
    let active = true;

    conn.on('MessageCreated', (ev: MessageCreatedEvent) => {
      if (!active) return;
      const msg = eventToMessage(ev);
      if (msg.conversationId !== conversationId) return;
      setMessages((prev) =>
        prev.some((m) => m.messageId === msg.messageId) ? prev : [...prev, msg],
      );
    });

    conn
      .start()
      .then(() => joinConversation(conn, conversationId))
      .catch(() => {
        /* socket is best-effort; sending still falls back to REST */
      });

    return () => {
      active = false;
      conn.off('MessageCreated');
      leaveConversation(conn, conversationId).catch(() => {});
      conn.stop().catch(() => {});
      connectionRef.current = null;
    };
  }, [conversationId]);

  // Scroll management: preserve position after prepending, otherwise stick to the bottom.
  useLayoutEffect(() => {
    const el = scrollRef.current;
    if (!el) return;
    if (prevScrollHeightRef.current != null) {
      el.scrollTop = el.scrollHeight - prevScrollHeightRef.current;
      prevScrollHeightRef.current = null;
    } else if (stickToBottomRef.current) {
      el.scrollTop = el.scrollHeight;
    }
  }, [messages]);

  async function loadMore() {
    if (loadingMoreRef.current || !hasMore) return;
    loadingMoreRef.current = true;
    setLoadingMore(true);
    const el = scrollRef.current;
    prevScrollHeightRef.current = el ? el.scrollHeight : null;
    try {
      const older = await getMessages(conversationId, { fromConversation: messages.length });
      setMessages((prev) => mergeUnique(prev, older));
      if (older.length < PAGE_SIZE) setHasMore(false);
    } catch {
      prevScrollHeightRef.current = null;
    } finally {
      loadingMoreRef.current = false;
      setLoadingMore(false);
    }
  }

  function handleScroll() {
    const el = scrollRef.current;
    if (!el) return;
    stickToBottomRef.current = el.scrollHeight - el.scrollTop - el.clientHeight < 120;
    if (el.scrollTop < 60 && hasMore && !loadingMoreRef.current && !loading) {
      void loadMore();
    }
  }

  async function handleSend(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const text = draft.trim();
    if (!text) return;
    setSending(true);
    setError('');
    stickToBottomRef.current = true;
    try {
      const conn = connectionRef.current;
      if (conn && isConnected(conn)) {
        // Server broadcasts "MessageCreated" back to us, which appends the message.
        await sendMessageOverHub(conn, { conversationId, payload: { text }, type: 'text' });
      } else {
        await sendMessage({ conversationId, payload: { text } });
        const msgs = await getMessages(conversationId);
        setMessages((prev) => mergeUnique(prev, msgs));
      }
      setDraft('');
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
    <div className="flex h-[70vh] flex-col rounded-2xl bg-white p-6 shadow-sm">
      <div ref={scrollRef} onScroll={handleScroll} className="min-h-0 flex-1 overflow-y-auto">
        <div className="flex min-h-full flex-col justify-end gap-3">
          {loadingMore && (
            <p className="text-center text-xs text-[#5d7056]">Loading older messages…</p>
          )}
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

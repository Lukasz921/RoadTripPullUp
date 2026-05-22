import React, { useState, useEffect, useRef } from 'react';
import { useParams, Link } from 'react-router-dom';
import { getConversations, getConversation, sendMessage } from '../../api/messages';
import type { ConversationSummaryDTO, MessageDTO } from '../../api/messages';
import { jwtDecode } from 'jwt-decode';

interface DecodedToken {
  nameid?: string;
  unique_name?: string;
  sub?: string;
}

function getCurrentUserId(): string | null {
  const token = localStorage.getItem('token');
  if (!token) return null;
  try {
    const decoded = jwtDecode<DecodedToken>(token);
    return decoded.nameid || decoded.sub || decoded.unique_name || null;
  } catch {
    return null;
  }
}

const Messages: React.FC = () => {
  const { partnerId } = useParams<{ partnerId?: string }>();
  const [conversations, setConversations] = useState<ConversationSummaryDTO[]>([]);
  const [messages, setMessages] = useState<MessageDTO[]>([]);
  const [newMessage, setNewMessage] = useState('');
  const [loading, setLoading] = useState(true);
  const [sending, setSending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const currentUserId = getCurrentUserId();
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    getConversations()
      .then(setConversations)
      .catch((e: any) => setError(e?.response?.data?.detail || e.message || 'Błąd pobierania konwersacji'))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    if (!partnerId) {
      setMessages([]);
      return;
    }
    getConversation(partnerId)
      .then(setMessages)
      .catch((e: any) => setError(e?.response?.data?.detail || e.message || 'Błąd pobierania wiadomości'));
  }, [partnerId]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const handleSend = async () => {
    if (!partnerId || !newMessage.trim()) return;
    setSending(true);
    try {
      const sent = await sendMessage(partnerId, newMessage.trim());
      setMessages((prev) => [...prev, sent]);
      setNewMessage('');
      getConversations().then(setConversations);
    } catch (e: any) {
      alert(e?.response?.data?.detail || 'Błąd wysyłania wiadomości');
    } finally {
      setSending(false);
    }
  };

  return (
    <div style={{ display: 'flex', height: 'calc(100vh - 40px)', maxWidth: 900, margin: '20px auto', border: '1px solid #ddd', borderRadius: 10, overflow: 'hidden' }}>
      <div style={{ width: 260, borderRight: '1px solid #ddd', overflowY: 'auto', background: '#fafafa' }}>
        <div style={{ padding: '12px 16px', fontWeight: 600, borderBottom: '1px solid #ddd' }}>
          <Link to="/" style={{ fontSize: 12, color: '#888' }}>← Powrót</Link>
          <div>Wiadomości</div>
        </div>
        {loading && <div style={{ padding: 16, fontSize: 13 }}>Ładowanie...</div>}
        {!loading && conversations.length === 0 && <div style={{ padding: 16, fontSize: 13, color: '#888' }}>Brak konwersacji</div>}
        {conversations.map((c) => (
          <Link
            key={c.partnerId}
            to={`/messages/${c.partnerId}`}
            style={{
              display: 'block',
              padding: '12px 16px',
              borderBottom: '1px solid #eee',
              background: partnerId === c.partnerId ? '#e8f0fe' : 'transparent',
              textDecoration: 'none',
              color: '#333',
            }}
          >
            <div style={{ fontSize: 13, fontWeight: 500 }}>{c.partnerId.substring(0, 8)}...</div>
            <div style={{ fontSize: 12, color: '#888', marginTop: 2, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
              {c.lastMessage}
            </div>
          </Link>
        ))}
      </div>

      <div style={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
        {!partnerId ? (
          <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#888' }}>
            Wybierz konwersację
          </div>
        ) : (
          <>
            <div style={{ flex: 1, overflowY: 'auto', padding: 16, display: 'flex', flexDirection: 'column', gap: 8 }}>
              {error && <div style={{ color: 'red', fontSize: 13 }}>{error}</div>}
              {messages.map((m) => {
                const isMine = m.senderId === currentUserId;
                return (
                  <div key={m.id} style={{ display: 'flex', justifyContent: isMine ? 'flex-end' : 'flex-start' }}>
                    <div style={{
                      maxWidth: '70%',
                      padding: '8px 12px',
                      borderRadius: 12,
                      background: isMine ? '#0084ff' : '#f0f0f0',
                      color: isMine ? '#fff' : '#333',
                      fontSize: 14,
                    }}>
                      {m.content}
                    </div>
                  </div>
                );
              })}
              <div ref={messagesEndRef} />
            </div>
            <div style={{ padding: 12, borderTop: '1px solid #ddd', display: 'flex', gap: 8 }}>
              <input
                value={newMessage}
                onChange={(e) => setNewMessage(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && !e.shiftKey && handleSend()}
                placeholder="Napisz wiadomość..."
                style={{ flex: 1, padding: '8px 12px', borderRadius: 20, border: '1px solid #ddd', fontSize: 14 }}
              />
              <button
                onClick={handleSend}
                disabled={sending || !newMessage.trim()}
                style={{ padding: '8px 16px', borderRadius: 20, background: '#0084ff', color: '#fff', border: 'none', cursor: 'pointer' }}
              >
                Wyślij
              </button>
            </div>
          </>
        )}
      </div>
    </div>
  );
};

export default Messages;

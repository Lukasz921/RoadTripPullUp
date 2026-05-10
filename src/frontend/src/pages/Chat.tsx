import React, { useState, useEffect, useRef } from 'react';
import { useParams, Link } from 'react-router-dom';
import { getConversation, sendMessage } from '../api/messages';
import type { MessageDTO } from '../api/messages';
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

const Chat: React.FC = () => {
  const { receiverId } = useParams<{ receiverId: string }>();
  const [messages, setMessages] = useState<MessageDTO[]>([]);
  const [newMessage, setNewMessage] = useState('');
  const [loading, setLoading] = useState(true);
  const [sending, setSending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const currentUserId = getCurrentUserId();
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!receiverId) return;
    getConversation(receiverId)
      .then(setMessages)
      .catch((e: any) => setError(e?.response?.data?.detail || e.message || 'Błąd pobierania wiadomości'))
      .finally(() => setLoading(false));
  }, [receiverId]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const handleSend = async () => {
    if (!receiverId || !newMessage.trim()) return;
    setSending(true);
    try {
      const sent = await sendMessage(receiverId, newMessage.trim());
      setMessages((prev) => [...prev, sent]);
      setNewMessage('');
    } catch (e: any) {
      alert(e?.response?.data?.detail || 'Błąd wysyłania wiadomości');
    } finally {
      setSending(false);
    }
  };

  return (
    <div style={{ maxWidth: 600, margin: '0 auto', height: '100vh', display: 'flex', flexDirection: 'column' }}>
      <div style={{ padding: '12px 16px', borderBottom: '1px solid #ddd', display: 'flex', alignItems: 'center', gap: 12 }}>
        <Link to="/messages" style={{ fontSize: 13, color: '#555' }}>← Wiadomości</Link>
        <span style={{ fontWeight: 600 }}>Czat</span>
      </div>

      <div style={{ flex: 1, overflowY: 'auto', padding: 16, display: 'flex', flexDirection: 'column', gap: 8 }}>
        {loading && <div style={{ color: '#888', fontSize: 13 }}>Ładowanie...</div>}
        {error && <div style={{ color: 'red', fontSize: 13 }}>{error}</div>}
        {!loading && !error && messages.length === 0 && (
          <div style={{ color: '#888', fontSize: 13, textAlign: 'center', marginTop: 40 }}>Brak wiadomości. Napisz pierwszą!</div>
        )}
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
    </div>
  );
};

export default Chat;

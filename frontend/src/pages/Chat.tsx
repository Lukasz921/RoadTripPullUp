import React, { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { sendMessage, getConversation } from '../api/messages';
import type { MessageResponseDTO } from '../api/messages';

function getCurrentUserId(): string | null {
  const token = localStorage.getItem('token');
  if (!token) return null;
  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    return payload.sub ?? null;
  } catch {
    return null;
  }
}

const Chat: React.FC = () => {
  const { receiverId } = useParams<{ receiverId: string }>();
  const navigate = useNavigate();
  const currentUserId = getCurrentUserId();

  const [messages, setMessages] = useState<MessageResponseDTO[]>([]);
  const [content, setContent] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!currentUserId) {
      navigate('/login');
      return;
    }
    if (!receiverId) return;

    const fetchMessages = async () => {
      try {
        const data = await getConversation(receiverId);
        setMessages(data);
      } catch {
        setError('Nie udało się załadować wiadomości.');
      } finally {
        setLoading(false);
      }
    };

    fetchMessages();
    const interval = setInterval(fetchMessages, 5000);
    return () => clearInterval(interval);
  }, [receiverId, currentUserId, navigate]);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const handleSend = async (e: React.SyntheticEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!content.trim() || !receiverId) return;

    const optimistic: MessageResponseDTO = {
      id: crypto.randomUUID(),
      senderId: currentUserId!,
      receiverId,
      content: content.trim(),
      timestamp: new Date().toISOString(),
      isRead: false,
    };

    setMessages((prev) => [...prev, optimistic]);
    setContent('');

    try {
      const saved = await sendMessage({ receiverId, content: optimistic.content });
      setMessages((prev) => prev.map((m) => (m.id === optimistic.id ? saved : m)));
    } catch {
      setMessages((prev) => prev.filter((m) => m.id !== optimistic.id));
      setError('Nie udało się wysłać wiadomości.');
    }
  };

  if (!currentUserId) return null;

  return (
    <div style={{ display: 'flex', flexDirection: 'column', height: '100vh', maxWidth: 600, margin: '0 auto' }}>
      <div style={{ padding: '12px 16px', borderBottom: '1px solid #ddd', display: 'flex', alignItems: 'center', gap: 8 }}>
        <button onClick={() => navigate(-1)}>← Wróć</button>
        <strong>Rozmowa</strong>
      </div>

      <div style={{ flex: 1, overflowY: 'auto', padding: 16, display: 'flex', flexDirection: 'column', gap: 8 }}>
        {loading && <div>Ładowanie...</div>}
        {error && <div style={{ color: 'red' }}>{error}</div>}
        {!loading && messages.length === 0 && <div style={{ color: '#999', textAlign: 'center' }}>Brak wiadomości. Napisz pierwszą!</div>}

        {messages.map((msg) => {
          const isMine = msg.senderId === currentUserId;
          return (
            <div
              key={msg.id}
              style={{
                alignSelf: isMine ? 'flex-end' : 'flex-start',
                maxWidth: '75%',
                backgroundColor: isMine ? '#0084ff' : '#e4e6eb',
                color: isMine ? '#fff' : '#000',
                padding: '8px 12px',
                borderRadius: isMine ? '18px 18px 4px 18px' : '18px 18px 18px 4px',
              }}
            >
              <div>{msg.content}</div>
              <div style={{ fontSize: 11, opacity: 0.7, marginTop: 2, textAlign: isMine ? 'right' : 'left' }}>
                {new Date(msg.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
              </div>
            </div>
          );
        })}
        <div ref={bottomRef} />
      </div>

      <form onSubmit={handleSend} style={{ display: 'flex', gap: 8, padding: 12, borderTop: '1px solid #ddd' }}>
        <input
          style={{ flex: 1, padding: '8px 12px', borderRadius: 20, border: '1px solid #ddd' }}
          placeholder="Napisz wiadomość..."
          value={content}
          onChange={(e) => setContent(e.target.value)}
          maxLength={1000}
        />
        <button
          type="submit"
          disabled={!content.trim()}
          style={{ padding: '8px 16px', borderRadius: 20, background: '#0084ff', color: '#fff', border: 'none', cursor: 'pointer' }}
        >
          Wyślij
        </button>
      </form>
    </div>
  );
};

export default Chat;

import React, { useState, useEffect, useRef } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { getConversations, getConversation, sendMessage } from '../api/messages';
import type { ConversationSummaryDTO, MessageResponseDTO } from '../api/messages';

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

const Messages: React.FC = () => {
  const navigate = useNavigate();
  const { partnerId } = useParams<{ partnerId?: string }>();
  const currentUserId = getCurrentUserId();

  const [conversations, setConversations] = useState<ConversationSummaryDTO[]>([]);
  const [messages, setMessages] = useState<MessageResponseDTO[]>([]);
  const [content, setContent] = useState('');
  const [loadingConvs, setLoadingConvs] = useState(true);
  const [loadingMsgs, setLoadingMsgs] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!currentUserId) {
      navigate('/login');
      return;
    }
    const fetchConversations = async () => {
      try {
        const data = await getConversations();
        setConversations(data);
      } catch {
        // brak rozmów = pusta lista, nie błąd krytyczny
      } finally {
        setLoadingConvs(false);
      }
    };
    fetchConversations();
    const interval = setInterval(fetchConversations, 10000);
    return () => clearInterval(interval);
  }, [currentUserId, navigate]);

  useEffect(() => {
    if (!partnerId) {
      setMessages([]);
      return;
    }
    setLoadingMsgs(true);
    setError(null);

    const fetchMessages = async () => {
      try {
        const data = await getConversation(partnerId);
        setMessages(data);
      } catch {
        setError('Nie udało się załadować wiadomości.');
      } finally {
        setLoadingMsgs(false);
      }
    };

    fetchMessages();
    const interval = setInterval(fetchMessages, 5000);
    return () => clearInterval(interval);
  }, [partnerId]);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const handleSend = async (e: React.SyntheticEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!content.trim() || !partnerId) return;

    const optimistic: MessageResponseDTO = {
      id: crypto.randomUUID(),
      senderId: currentUserId!,
      receiverId: partnerId,
      content: content.trim(),
      timestamp: new Date().toISOString(),
      isRead: false,
    };

    setMessages((prev) => [...prev, optimistic]);
    setContent('');

    try {
      const saved = await sendMessage({ receiverId: partnerId, content: optimistic.content });
      setMessages((prev) => prev.map((m) => (m.id === optimistic.id ? saved : m)));
      const convData = await getConversations();
      setConversations(convData);
    } catch {
      setMessages((prev) => prev.filter((m) => m.id !== optimistic.id));
      setError('Nie udało się wysłać wiadomości.');
    }
  };

  if (!currentUserId) return null;

  return (
    <div style={{ display: 'flex', height: '100vh', fontFamily: 'sans-serif' }}>
      {/* Sidebar */}
      <div style={{
        width: 300,
        borderRight: '1px solid #e0e0e0',
        display: 'flex',
        flexDirection: 'column',
        background: '#fff',
        flexShrink: 0,
      }}>
        <div style={{
          padding: '16px',
          borderBottom: '1px solid #e0e0e0',
          display: 'flex',
          alignItems: 'center',
          gap: 8,
        }}>
          <button
            onClick={() => navigate('/')}
            style={{ background: 'none', border: 'none', cursor: 'pointer', fontSize: 18, padding: 4 }}
          >←</button>
          <strong style={{ fontSize: 18 }}>Wiadomości</strong>
        </div>

        <div style={{ flex: 1, overflowY: 'auto' }}>
          {loadingConvs && (
            <div style={{ padding: 16, color: '#999' }}>Ładowanie...</div>
          )}
          {!loadingConvs && conversations.length === 0 && (
            <div style={{ padding: 16, color: '#999', textAlign: 'center', fontSize: 14 }}>
              Brak rozmów.<br />Napisz do kogoś ze strony przejazdu.
            </div>
          )}
          {conversations.map((conv) => {
            const isActive = conv.partnerId === partnerId;
            return (
              <div
                key={conv.partnerId}
                onClick={() => navigate(`/messages/${conv.partnerId}`)}
                style={{
                  padding: '12px 16px',
                  cursor: 'pointer',
                  background: isActive ? '#e8f0fe' : 'transparent',
                  borderLeft: isActive ? '3px solid #0084ff' : '3px solid transparent',
                  transition: 'background 0.15s',
                }}
                onMouseEnter={(e) => {
                  if (!isActive) (e.currentTarget as HTMLDivElement).style.background = '#f5f5f5';
                }}
                onMouseLeave={(e) => {
                  if (!isActive) (e.currentTarget as HTMLDivElement).style.background = 'transparent';
                }}
              >
                <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                  <div style={{
                    width: 40, height: 40, borderRadius: '50%',
                    background: '#0084ff', color: '#fff',
                    display: 'flex', alignItems: 'center', justifyContent: 'center',
                    fontWeight: 600, fontSize: 14, flexShrink: 0,
                  }}>
                    {conv.partnerId.substring(0, 2).toUpperCase()}
                  </div>
                  <div style={{ overflow: 'hidden' }}>
                    <div style={{ fontWeight: 500, fontSize: 14, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                      {conv.partnerId}
                    </div>
                    <div style={{ fontSize: 12, color: '#666', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                      {conv.lastMessage}
                    </div>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </div>

      {/* Chat window */}
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', background: '#f9f9f9' }}>
        {!partnerId ? (
          <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#999' }}>
            <div style={{ textAlign: 'center' }}>
              <div style={{ fontSize: 48, marginBottom: 12 }}>💬</div>
              <div>Wybierz rozmowę lub napisz do kogoś z oferty przejazdu</div>
            </div>
          </div>
        ) : (
          <>
            <div style={{
              padding: '12px 16px',
              borderBottom: '1px solid #e0e0e0',
              background: '#fff',
              display: 'flex',
              alignItems: 'center',
              gap: 10,
            }}>
              <div style={{
                width: 36, height: 36, borderRadius: '50%',
                background: '#0084ff', color: '#fff',
                display: 'flex', alignItems: 'center', justifyContent: 'center',
                fontWeight: 600, fontSize: 13,
              }}>
                {partnerId.substring(0, 2).toUpperCase()}
              </div>
              <div>
                <div style={{ fontWeight: 600, fontSize: 14 }}>Użytkownik</div>
                <div style={{ fontSize: 12, color: '#666', fontFamily: 'monospace' }}>{partnerId}</div>
              </div>
            </div>

            <div style={{ flex: 1, overflowY: 'auto', padding: 16, display: 'flex', flexDirection: 'column', gap: 8 }}>
              {loadingMsgs && <div style={{ color: '#999', textAlign: 'center' }}>Ładowanie...</div>}
              {error && <div style={{ color: 'red', textAlign: 'center' }}>{error}</div>}
              {!loadingMsgs && messages.length === 0 && (
                <div style={{ color: '#999', textAlign: 'center', marginTop: 32 }}>
                  Brak wiadomości. Napisz pierwszą!
                </div>
              )}
              {messages.map((msg) => {
                const isMine = msg.senderId === currentUserId;
                return (
                  <div
                    key={msg.id}
                    style={{
                      alignSelf: isMine ? 'flex-end' : 'flex-start',
                      maxWidth: '70%',
                      backgroundColor: isMine ? '#0084ff' : '#e4e6eb',
                      color: isMine ? '#fff' : '#000',
                      padding: '8px 12px',
                      borderRadius: isMine ? '18px 18px 4px 18px' : '18px 18px 18px 4px',
                      wordBreak: 'break-word',
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

            <form
              onSubmit={handleSend}
              style={{
                display: 'flex',
                gap: 8,
                padding: '12px 16px',
                borderTop: '1px solid #e0e0e0',
                background: '#fff',
              }}
            >
              <input
                style={{
                  flex: 1,
                  padding: '10px 14px',
                  borderRadius: 24,
                  border: '1px solid #ddd',
                  fontSize: 14,
                  outline: 'none',
                }}
                placeholder="Napisz wiadomość..."
                value={content}
                onChange={(e) => setContent(e.target.value)}
                maxLength={1000}
              />
              <button
                type="submit"
                disabled={!content.trim()}
                style={{
                  padding: '10px 20px',
                  borderRadius: 24,
                  background: content.trim() ? '#0084ff' : '#c0d8ff',
                  color: '#fff',
                  border: 'none',
                  cursor: content.trim() ? 'pointer' : 'default',
                  fontWeight: 600,
                  fontSize: 14,
                }}
              >
                Wyślij
              </button>
            </form>
          </>
        )}
      </div>
    </div>
  );
};

export default Messages;

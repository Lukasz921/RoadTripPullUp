import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from '@microsoft/signalr';
import type { CreateMessageDTO, MessageDTO, MessageType } from './messages';

// The SignalR hub lives at /hub/chat on the message service host (not under the /api/v1/message base path).
const HUB_URL = new URL(import.meta.env.VITE_MESSAGE_SERVICE_URL).origin + '/hub/chat';

// Shape broadcast by the backend on the "MessageCreated" event (Hub + Redis notifier both send this).
export interface MessageCreatedEvent {
  eventType: string;
  data: {
    id: string;
    conversationId: string;
    senderId: string;
    type: string;
    payload: Record<string, unknown> | null;
    createdAt: string;
  };
}

export function createChatConnection(): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(HUB_URL, {
      accessTokenFactory: () => localStorage.getItem('token') ?? '',
    })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build();
}

// The socket payload uses `id`; normalise it to the REST MessageDTO shape (`messageId`).
export function eventToMessage(ev: MessageCreatedEvent): MessageDTO {
  return {
    messageId: ev.data.id,
    conversationId: ev.data.conversationId,
    senderId: ev.data.senderId,
    type: ev.data.type as MessageType,
    payload: ev.data.payload ?? {},
    createdAt: ev.data.createdAt,
  };
}

export const isConnected = (conn: HubConnection) => conn.state === HubConnectionState.Connected;

export const joinConversation = (conn: HubConnection, conversationId: string) =>
  conn.invoke('JoinConversation', conversationId);

export const leaveConversation = (conn: HubConnection, conversationId: string) =>
  conn.invoke('LeaveConversation', conversationId);

export const sendMessageOverHub = (conn: HubConnection, dto: CreateMessageDTO & { type?: MessageType }) =>
  conn.invoke<{ messageId: string }>('SendMessage', {
    conversationId: dto.conversationId,
    type: dto.type ?? 'text',
    payload: dto.payload,
  });

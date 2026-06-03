import { messageApi } from './axiosConfig';

export type ConversationType = 'Direct' | 'Group';
export type MessageType = 'Text' | 'PriceOffer' | 'PriceAccept' | 'OfferApproval' | 'Location';

// --- Conversations ---

export interface CreateConversationDTO {
  TripId: string;
  Title?: string;
  Date?: string;
  Participants: string[];
}

export interface ConversationDTO {
  ConversationId: string;
  Type: ConversationType;
  TripId: string;
  Name?: string;
  Date?: string;
  Participants: string[];
  LastMessageId: string;
  LastMessagePreview: string;
  LastMessageCreatedAt: string;
}

export interface ConversationListParams {
  fromConversation?: number;
  toConversation?: number;
}

export const createConversation = async (dto: CreateConversationDTO): Promise<{ conversationId: string }> => {
  const response = await messageApi.post('/conversations', { ...dto, Type: 'Direct' });
  return response.data;
};

export const getConversations = async (params?: ConversationListParams): Promise<ConversationDTO[]> => {
  const response = await messageApi.get('/conversations', { params });
  return response.data;
};

export const getConversation = async (conversationId: string): Promise<ConversationDTO> => {
  const response = await messageApi.get(`/conversations/${conversationId}`);
  return response.data;
};


export const getGroupConversationByTrip = async (tripId: string): Promise<ConversationDTO> => {
  const response = await messageApi.get(`/conversations/byTripId/group/${tripId}`);
  return response.data;
};

export const getDirectConversationsByTrip = async (tripId: string): Promise<ConversationDTO[]> => {
  const response = await messageApi.get(`/conversations/byTripId/direct/${tripId}`);
  return response.data;
};










// --- Messages ---

export interface CreateMessageDTO {
  ConversationId: string;
  Payload: Record<string, unknown>;
}

export interface MessageDTO {
  MessageId: string;
  ConversationId: string;
  SenderId: string;
  Type: MessageType;
  Payload: Record<string, unknown>;
  CreatedAt: string;
}

export interface MessageListParams {
  fromConversation?: number;
  toConversation?: number;
}

export interface SyncResponseDTO {
  messages: MessageDTO[];
  serverTimestamp: string;
}

export interface ReadReceiptDTO {
  ConversationId: string;
  LastReadMessageId?: string;
  LastReadTimestamp?: string;
}

export const sendMessage = async (dto: CreateMessageDTO): Promise<{ messageId: string }> => {
  const response = await messageApi.post('/messages', { ...dto, Type: 'Text' });
  return response.data;
};

export const getMessages = async (conversationId: string, params?: MessageListParams): Promise<MessageDTO[]> => {
  const response = await messageApi.get(`/conversations/${conversationId}/messages`, { params });
  return response.data;
};

export const getMessage = async (messageId: string): Promise<MessageDTO> => {
  const response = await messageApi.get(`/messages/${messageId}`);
  return response.data;
};

export const syncMessages = async (lastReceivedAt?: string): Promise<SyncResponseDTO> => {
  const response = await messageApi.get('/messages/sync', {
    params: lastReceivedAt ? { lastReceivedAt } : undefined,
  });
  return response.data;
};

export const markAsRead = async (dto: ReadReceiptDTO): Promise<void> => {
  await messageApi.post('/messages/read', dto);
};

// --- Users ---

export interface MessageUserDTO {
  id: string;
  username: string;
  displayName: string;
}

export const getMessageUser = async (userId: string): Promise<MessageUserDTO> => {
  const response = await messageApi.get(`/users/${userId}`);
  return response.data;
};

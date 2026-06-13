import { messageApi } from './axiosConfig';

export type ConversationType = 'direct' | 'group';
export type MessageType = 'text' | 'priceOffer' | 'priceAccept' | 'offerApproval' | 'location';

// --- Conversations ---

export interface CreateConversationDTO {
  tripId: string;
  title?: string;
  date?: string;
  participants: string[];
}

export interface ConversationDTO {
  conversationId: string;
  type: ConversationType;
  tripId: string;
  name?: string;
  date?: string;
  participants: string[];
  lastMessageId: string;
  lastMessagePreview: string;
  lastMessageCreatedAt: string;
}

export interface ConversationListParams {
  fromConversation?: number;
  toConversation?: number;
}

export const createConversation = async (dto: CreateConversationDTO): Promise<{ conversationId: string }> => {
   const response = await messageApi.post('/conversations', { ...dto, type: 'direct' });
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
  conversationId: string;
  payload: Record<string, unknown>;
}

export interface MessageDTO {
  messageId: string;
  conversationId: string;
  senderId: string;
  type: MessageType;
  payload: Record<string, unknown>;
  createdAt: string;
}

export interface MessageListParams {
  fromConversation?: number;
  toConversation?: number;
}

export const sendMessage = async (dto: CreateMessageDTO): Promise<{ messageId: string }> => {
  const response = await messageApi.post('/messages', { ...dto, type: 'text' });
  return response.data;
};

export const getMessages = async (conversationId: string, params?: MessageListParams): Promise<MessageDTO[]> => {
  const response = await messageApi.get(`/conversations/${conversationId}/messages`, { params });
  return response.data;
};

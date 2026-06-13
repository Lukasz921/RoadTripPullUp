import { messageApi } from './axiosConfig';

export type ConversationType = 'Direct' | 'Group';
export type MessageType = 'Text' | 'PriceOffer' | 'PriceAccept' | 'OfferApproval' | 'Location';

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
  // const response = await messageApi.post('/conversations', { ...dto, type: 'direct' });
  // return response.data;
  void dto; // TODO: remove mock
  return { conversationId: 'mock-direct-1' };
};

export const getConversations = async (params?: ConversationListParams): Promise<ConversationDTO[]> => {
  // const response = await messageApi.get('/conversations', { params });
  // return response.data;
  void params; // TODO: remove mock
  return [
    {
      conversationId: 'mock-group-1',
      type: 'Group',
      tripId: 'trip-1',
      name: 'Warsaw → Kraków crew',
      participants: ['user-1', 'user-2', 'user-3', 'user-4'],
      lastMessageId: 'msg-1',
      lastMessagePreview: 'Anyone need a stop in Łódź?',
      lastMessageCreatedAt: '2026-06-03T14:22:00Z',
    },
    {
      conversationId: 'mock-direct-1',
      type: 'Direct',
      tripId: 'trip-1',
      name: 'Marek Kowalski',
      participants: ['user-1', 'user-2'],
      lastMessageId: 'msg-2',
      lastMessagePreview: 'Hi, is there still a seat available?',
      lastMessageCreatedAt: '2026-06-03T10:05:00Z',
    },
    {
      conversationId: 'mock-direct-2',
      type: 'Direct',
      tripId: 'trip-2',
      name: 'Anna Nowak',
      participants: ['user-1', 'user-3'],
      lastMessageId: 'msg-3',
      lastMessagePreview: 'Great, see you at 8am!',
      lastMessageCreatedAt: '2026-06-02T18:45:00Z',
    },
  ];
};

export const getConversation = async (conversationId: string): Promise<ConversationDTO> => {
  // const response = await messageApi.get(`/conversations/${conversationId}`);
  // return response.data;
  void conversationId; // TODO: remove mock
  return {
    conversationId: conversationId,
    type: 'Direct',
    tripId: 'trip-1',
    name: 'Marek Kowalski',
    participants: ['user-1', 'user-2'],
    lastMessageId: 'msg-2',
    lastMessagePreview: 'Hi, is there still a seat available?',
    lastMessageCreatedAt: '2026-06-03T10:05:00Z',
  };
};


export const getGroupConversationByTrip = async (tripId: string): Promise<ConversationDTO> => {
  const response = await messageApi.get(`/conversations/byTripId/group/${tripId}`);
  return response.data;
};

export const getDirectConversationsByTrip = async (tripId: string): Promise<ConversationDTO[]> => {
  // const response = await messageApi.get(`/conversations/byTripId/direct/${tripId}`);
  // return response.data;
  void tripId; // TODO: remove mock
  return [
    {
      conversationId: 'mock-direct-1',
      type: 'Direct',
      tripId: tripId,
      name: 'Marek Kowalski',
      participants: ['user-1', 'user-2'],
      lastMessageId: 'msg-2',
      lastMessagePreview: 'Hi, is there still a seat available?',
      lastMessageCreatedAt: '2026-06-03T10:05:00Z',
    },
    {
      conversationId: 'mock-direct-2',
      type: 'Direct',
      tripId: tripId,
      name: 'Anna Nowak',
      participants: ['user-1', 'user-3'],
      lastMessageId: 'msg-3',
      lastMessagePreview: 'Great, see you at 8am!',
      lastMessageCreatedAt: '2026-06-02T18:45:00Z',
    },
  ];
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

export interface SyncResponseDTO {
  messages: MessageDTO[];
  serverTimestamp: string;
}

export interface ReadReceiptDTO {
  conversationId: string;
  lastReadMessageId?: string;
  lastReadTimestamp?: string;
}

export const sendMessage = async (dto: CreateMessageDTO): Promise<{ messageId: string }> => {
  const response = await messageApi.post('/messages', { ...dto, type: 'Text' });
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

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


export const getGroupConversationByTrip = async (_tripId: string): Promise<ConversationDTO> => {
  // TODO: remove mock
  return {
    ConversationId: 'mock-group-1',
    Type: 'Group',
    TripId: _tripId,
    Name: 'Warsaw → Kraków crew',
    Participants: ['user-1', 'user-2', 'user-3', 'user-4'],
    LastMessageId: 'msg-1',
    LastMessagePreview: 'Anyone need a stop in Łódź?',
    LastMessageCreatedAt: '2026-06-03T14:22:00Z',
  };
};

export const getDirectConversationsByTrip = async (_tripId: string): Promise<ConversationDTO[]> => {
  // TODO: remove mock
  return [
    {
      ConversationId: 'mock-direct-1',
      Type: 'Direct',
      TripId: _tripId,
      Name: 'Marek Kowalski',
      Participants: ['user-1', 'user-2'],
      LastMessageId: 'msg-2',
      LastMessagePreview: 'Hi, is there still a seat available?',
      LastMessageCreatedAt: '2026-06-03T10:05:00Z',
    },
    {
      ConversationId: 'mock-direct-2',
      Type: 'Direct',
      TripId: _tripId,
      Name: 'Anna Nowak',
      Participants: ['user-1', 'user-3'],
      LastMessageId: 'msg-3',
      LastMessagePreview: 'Great, see you at 8am!',
      LastMessageCreatedAt: '2026-06-02T18:45:00Z',
    },
  ];
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

import api from './axiosConfig';

export interface SendMessageDTO {
  receiverId: string;
  content: string;
}

export interface MessageResponseDTO {
  id: string;
  senderId: string;
  receiverId: string;
  content: string;
  timestamp: string;
  isRead: boolean;
}

export const sendMessage = async (dto: SendMessageDTO): Promise<MessageResponseDTO> => {
  const response = await api.post<MessageResponseDTO>('/messages', dto);
  return response.data;
};

export const getConversation = async (receiverId: string): Promise<MessageResponseDTO[]> => {
  const response = await api.get<MessageResponseDTO[]>(`/messages/${receiverId}`);
  return response.data;
};

export interface ConversationSummaryDTO {
  partnerId: string;
  lastMessage: string;
  lastTimestamp: string;
}

export const getConversations = async (): Promise<ConversationSummaryDTO[]> => {
  const response = await api.get<ConversationSummaryDTO[]>('/messages/conversations');
  return response.data;
};

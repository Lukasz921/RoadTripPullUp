import api from './axiosConfig';

export interface MessageDTO {
  id: string;
  senderId: string;
  receiverId: string;
  content: string;
  timestamp: string;
  isRead: boolean;
}

export interface ConversationSummaryDTO {
  partnerId: string;
  lastMessage: string;
  lastTimestamp: string;
}

export const sendMessage = async (receiverId: string, content: string) => {
  const response = await api.post<MessageDTO>('/messages', { receiverId, content });
  return response.data;
};

export const getConversation = async (partnerId: string) => {
  const response = await api.get<MessageDTO[]>(`/messages/${partnerId}`);
  return response.data;
};

export const getConversations = async () => {
  const response = await api.get<ConversationSummaryDTO[]>('/messages/conversations');
  return response.data;
};

import { authApi } from './axiosConfig';

export type Sex = 'MALE' | 'FEMALE' | 'OTHER';

export interface RegisterDTO {
  name: string;
  surname: string;
  email: string;
  password: string;
  phoneNumber: string;
  dateOfBirth: string;
  sex: Sex;
}

export interface LoginDTO {
  email: string;
  password: string;
}

export interface LoginResponseDTO {
  token: string;
}

export interface CurrentUser {
  id: string;
  name: string;
  surname: string;
  email: string;
  phoneNumber?: string;
  dateOfBirth: string;
  sex: string;
  avgRating: number;
  ratingsCount: number;
  isBanned: boolean;
  banReason?: string;
  bannedUntil?: string;
}

export interface UpdateUserDTO {
  name?: string;
  surname?: string;
  phoneNumber?: string;
  dateOfBirth?: string;
  sex?: Sex;
}

export interface BanUserDTO {
  reason: string;
  until?: string;
}

// Mirrors Users.Core.UserRole — serialized as a number (no JsonStringEnumConverter on the backend).
export const UserRole = {
  RegularUser: 0,
  Admin: 1,
} as const;

export type UserRole = (typeof UserRole)[keyof typeof UserRole];

// --- Auth ---

export const register = async (dto: RegisterDTO): Promise<void> => {
  await authApi.post('/auth/register', dto);
};

export const login = async (dto: LoginDTO): Promise<LoginResponseDTO> => {
  const response = await authApi.post<LoginResponseDTO>('/auth/login', dto);
  return response.data;
};

// --- Current user ---

export const getCurrentUser = async (): Promise<CurrentUser> => {
  const response = await authApi.get<CurrentUser>('/users/me');
  return response.data;
};

export const updateCurrentUser = async (dto: UpdateUserDTO): Promise<void> => {
  await authApi.patch('/users/me', dto);
};

// --- Other users ---

export const getUserById = async (userId: string): Promise<CurrentUser> => {
  const response = await authApi.get<CurrentUser>(`/users/${userId}`);
  return response.data;
};

// --- Ratings ---

export const rateUser = async (tripId: string, userId: string, value: number, comment?: string): Promise<void> => {
  await authApi.post(`/trips/${tripId}/rate-user`, {
    userId,
    value,
    comment,
  });
};

// --- Admin ---

export const banUser = async (userId: string, dto: BanUserDTO): Promise<void> => {
  await authApi.post(`/users/${userId}/ban`, dto);
};

export const unbanUser = async (userId: string): Promise<void> => {
  await authApi.post(`/users/${userId}/unban`);
};

export const changeRole = async (userId: string, role: UserRole): Promise<void> => {
  await authApi.post(`/users/${userId}/role`, role);
};

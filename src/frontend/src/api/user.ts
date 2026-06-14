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

export interface RatingResponseDTO {
  id: string;
  raterId: string;
  raterName?: string;
  value: number;
  comment?: string;
  createdAt: string;
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

export const getUserRatings = async (userId: string): Promise<RatingResponseDTO[]> => {
  const response = await authApi.get<RatingResponseDTO[]>(`/users/${userId}/ratings`);
  return response.data;
};

export const getRating = async (ratingId: string): Promise<RatingResponseDTO> => {
  const response = await authApi.get<RatingResponseDTO>(`/users/ratings/${ratingId}`);
  return response.data;
};

export const deleteRating = async (ratingId: string): Promise<void> => {
  await authApi.delete(`/users/ratings/${ratingId}`);
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

// Mirrors backend ComplaintResponseDTO.
export interface ComplaintResponseDTO {
  id: string;
  tripId: string;
  complainerId: string;
  complainedUserId: string;
  reason: string;
  createdAt: string;
}

// MOCK: returns fake complaints until the backend endpoint exists.
// Swap the body for `authApi.get<ComplaintResponseDTO[]>('/complaints')` once it's available.
export const getComplaints = async (): Promise<ComplaintResponseDTO[]> => {
  await new Promise((resolve) => setTimeout(resolve, 300));
  return [
    {
      id: '11111111-1111-1111-1111-111111111111',
      tripId: '22222222-2222-2222-2222-222222222222',
      complainerId: '33333333-3333-3333-3333-333333333333',
      complainedUserId: '44444444-4444-4444-4444-444444444444',
      reason: 'Driver was over 30 minutes late and did not communicate.',
      createdAt: '2026-06-10T09:15:00Z',
    },
    {
      id: '55555555-5555-5555-5555-555555555555',
      tripId: '66666666-6666-6666-6666-666666666666',
      complainerId: '77777777-7777-7777-7777-777777777777',
      complainedUserId: '88888888-8888-8888-8888-888888888888',
      reason: 'Passenger cancelled at the pickup point without notice.',
      createdAt: '2026-06-12T17:42:00Z',
    },
  ];
};

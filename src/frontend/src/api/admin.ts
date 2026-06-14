import { authApi } from './axiosConfig';
import type { BanUserDTO, UserRole } from './user';

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

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

export interface PagedComplaintsDTO {
  items: ComplaintResponseDTO[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export const getComplaints = async (page = 1, pageSize = 20): Promise<PagedComplaintsDTO> => {
  const response = await authApi.get<PagedComplaintsDTO>('/admin/complaints', {
    params: { page, pageSize },
  });
  return response.data;
};

// MOCK: no backend delete endpoint exists yet — resolves without doing anything.
// Swap the body for `authApi.delete(`/admin/complaints/${id}`)` once it exists.
export const deleteComplaint = async (_id: string): Promise<void> => {
  await new Promise((resolve) => setTimeout(resolve, 200));
};

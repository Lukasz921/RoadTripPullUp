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

// MOCK: generates a fixed pool of fake complaints and serves them page by page.
// Swap the body for `authApi.get<PagedComplaintsDTO>('/admin/complaints', { params: { page, pageSize } })`
// once the paged backend endpoint exists.
const MOCK_REASONS = [
  'Driver was over 30 minutes late and did not communicate.',
  'Passenger cancelled at the pickup point without notice.',
  'Driver drove recklessly and ignored speed limits.',
  'Passenger was rude to other people in the car.',
  'Driver asked for more money than the agreed price per seat.',
  'Passenger never showed up and did not respond to messages.',
  'The car was in an unsafe condition.',
  'Driver took a much longer route than necessary.',
];

const MOCK_COMPLAINTS: ComplaintResponseDTO[] = Array.from({ length: 23 }, (_, i) => {
  const n = (i + 1).toString(16).padStart(2, '0');
  return {
    id: `${n}000000-0000-0000-0000-000000000000`,
    tripId: `${n}111111-1111-1111-1111-111111111111`,
    complainerId: `${n}222222-2222-2222-2222-222222222222`,
    complainedUserId: `${n}333333-3333-3333-3333-333333333333`,
    reason: MOCK_REASONS[i % MOCK_REASONS.length],
    createdAt: new Date(Date.UTC(2026, 5, 14 - i, 9, 0, 0)).toISOString(),
  };
});

export const getComplaints = async (page = 1, pageSize = 10): Promise<PagedComplaintsDTO> => {
  await new Promise((resolve) => setTimeout(resolve, 300));
  const start = (page - 1) * pageSize;
  return {
    items: MOCK_COMPLAINTS.slice(start, start + pageSize),
    page,
    pageSize,
    totalCount: MOCK_COMPLAINTS.length,
  };
};

// MOCK: returns a single complaint by id.
// Swap the body for `authApi.get<ComplaintResponseDTO>(`/admin/complaints/${id}`)` once wired.
export const getComplaintById = async (id: string): Promise<ComplaintResponseDTO> => {
  await new Promise((resolve) => setTimeout(resolve, 300));
  const complaint = MOCK_COMPLAINTS.find((c) => c.id === id);
  if (!complaint) throw new Error('Complaint not found');
  return complaint;
};

// MOCK: removes a complaint from the pool.
// Swap the body for `authApi.delete(`/admin/complaints/${id}`)` once the backend endpoint exists.
export const deleteComplaint = async (id: string): Promise<void> => {
  await new Promise((resolve) => setTimeout(resolve, 200));
  const index = MOCK_COMPLAINTS.findIndex((c) => c.id === id);
  if (index !== -1) MOCK_COMPLAINTS.splice(index, 1);
};

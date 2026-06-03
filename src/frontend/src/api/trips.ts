import { tripApi } from './axiosConfig';

export interface LatLng {
  lat: number;
  lng: number;
}

export interface TripDTO {
  id: string;
  driverId: string;
  source: LatLng;
  target: LatLng;
  departureTime: string;
  routeDistanceM: number;
  routeDurationS: number;
  maxDetourMeters: number;
  pricePerSeat: number;
  availableSeats: number;
  passengerIds: string[];
  status: string;
  createdAt: string;
}

export interface CreateTripDTO {
  source: LatLng;
  target: LatLng;
  departureTime: string;
  maxDetourMeters: number;
  pricePerSeat: number;
  availableSeats: number;
}

export interface PagedTripsDTO {
  items: TripDTO[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface TripSummaryV1DTO {
  id: string;
  driverId: string;
  source: LatLng;
  target: LatLng;
  departureTime: string;
  pricePerSeat: number;
  availableSeats: number;
  maxDetourMeters: number;
  actualDetourMeters: number;
}

export interface SearchTripsRequestDTO {
  source: LatLng;
  target: LatLng;
  dateFrom: string;
  dateTo: string;
  maxPrice?: number;
  minSeats?: number;
  sortBy?: string;
  page?: number;
  pageSize?: number;
}


export interface SearchJobCreatedDTO {
  jobId: string;
  status: string;
  statusUrl: string;
  estimatedDurationMs: number;
}

export interface SearchJobProgressDTO {
  jobId: string;
  status: string;
  progress: {
    phase: string;
    candidatesFound: number;
    candidatesProcessed: number;
  };
}

export interface SearchJobResultDTO {
  jobId: string;
  status: string;
  completedAt?: string;
  items?: TripSummaryV1DTO[];
  page: number;
  pageSize: number;
  totalCount: number;
  error?: {
    code: string;
    message: string;
  };
}

export const createTrip = async (dto: CreateTripDTO) => {
  const response = await tripApi.post<TripDTO>('/', dto);
  return response.data;
};

export const getMyTrips = async (page = 1, pageSize = 20) => {
  const response = await tripApi.get<PagedTripsDTO>('/me', { params: { page, pageSize } });
  return response.data;
};

export const getJoinedTrips = async (page = 1, pageSize = 20) => {
  const response = await tripApi.get<PagedTripsDTO>('/joined', { params: { page, pageSize } });
  return response.data;
};

export const getTripById = async (tripId: string) => {
  const response = await tripApi.get<TripDTO>(`/${tripId}`);
  return response.data;
};

export const joinTrip = async (tripId: string) => {
  await tripApi.post(`/${tripId}/join`);
};

export const deleteTrip = async (tripId: string) => {
  await tripApi.delete(`/${tripId}`);
};


export const submitSearch = async (dto: SearchTripsRequestDTO) => {
  const response = await tripApi.post<SearchJobCreatedDTO>('/search', dto);
  return response.data;
};

export const pollSearch = async (jobId: string) => {
  const response = await tripApi.get<SearchJobProgressDTO | SearchJobResultDTO>(`/search/${jobId}`);
  return { status: response.status, data: response.data };
};

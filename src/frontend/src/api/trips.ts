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
  conversationId?: string;
  routePolylinePoints?: LatLng[];
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
  const response = await tripApi.post<TripDTO>('', dto);
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

export const addToTrip = async (tripId: string, passengerId: string) => {
  await tripApi.post(`/${tripId}/passengers`, { PassengerId: passengerId });
};

// --- Trip requests ---

export interface TripRequestDTO {
  id: string;
  tripId: string;
  requesterId: string;
  conversationId: string;
  pickup: LatLng;
  dropoff: LatLng;
  previewPolyline?: LatLng[];
  detourMeters: number;
  status: string;
}

export interface CreateTripRequestResult {
  conversationId: string;
  requestId: string;
  detourMeters: number;
}

// Passenger requests a trip: creates the driver chat + a trip request (pickup/dropoff).
export const requestTrip = async (
  tripId: string,
  pickup: LatLng,
  dropoff: LatLng,
): Promise<CreateTripRequestResult> => {
  const response = await tripApi.post<CreateTripRequestResult>(`/${tripId}/requests`, { pickup, dropoff });
  return response.data;
};

// Returns the trip request behind a direct conversation, or null if there isn't one.
export const getTripRequestByConversation = async (conversationId: string): Promise<TripRequestDTO | null> => {
  try {
    const response = await tripApi.get<TripRequestDTO>(`/requests/by-conversation/${conversationId}`);
    return response.data;
  } catch {
    return null;
  }
};

// Driver accepts a request: recomputes the route through the new stops and adds the passenger.
export const acceptTripRequest = async (tripId: string, requestId: string): Promise<void> => {
  await tripApi.post(`/${tripId}/requests/${requestId}/accept`);
};

export interface RateUserDTO {
  userId: string;
  value: number;
  comment?: string;
}

export const rateUser = async (tripId: string, dto: RateUserDTO) => {
  await tripApi.post(`/${tripId}/rate-user`, dto);
};

export interface FileComplaintDTO {
  complainedUserId: string;
  reason: string;
}

export const fileComplaint = async (tripId: string, dto: FileComplaintDTO) => {
  await tripApi.post(`/${tripId}/complaint`, dto);
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

// GET /trips/history — paged list of the user's past trips.
export const getTripHistory = async (page = 1, pageSize = 20) => {
  const response = await tripApi.get<PagedTripsDTO>('/history', { params: { page, pageSize } });
  return response.data;
};

export interface SearchTripsQueryDTO {
  sourceLat: number;
  sourceLng: number;
  targetLat: number;
  targetLng: number;
  dateFrom: string;
  dateTo: string;
  maxPrice?: number;
  minSeats?: number;
  sortBy?: string;
  page?: number;
  pageSize?: number;
}


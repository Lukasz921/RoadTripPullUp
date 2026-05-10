import api from './axiosConfig';

export interface TripSummaryDTO {
  tripId: string;
  driverId: string;
  price: number;
  date: string;
  maxPassengers: number;
  route: {
    routeId: string;
    from: string;
    to: string;
  };
}

export interface TripRequestDTO {
  id: string;
  tripId: string;
  passengerId: string;
  status: string;
}

export const searchTrips = async (params: { from?: string; to?: string; date?: string }) => {
  const response = await api.get<TripSummaryDTO[]>('/trips', { params });
  return response.data;
};

export const requestRide = async (tripId: string) => {
  const response = await api.post(`/trips/${tripId}/request`);
  return response.data;
};

export const getTripRequests = async (tripId: string) => {
  const response = await api.get<TripRequestDTO[]>(`/trips/${tripId}/requests`);
  return response.data;
};

export const acceptRequest = async (requestId: string) => {
  const response = await api.post(`/requests/${requestId}/accept`);
  return response.data;
};

export interface TripDetailsDTO {
  tripId: string;
  driverId: string;
  price: number;
  date: string;
  maxPassengers: number;
  status: string;
  route: {
    routeId: string;
    from: string;
    to: string;
  };
  passengerIds: string[];
}

export const getTripById = async (id: string) => {
  const response = await api.get<TripDetailsDTO>(`/trips/${id}`);
  return response.data;
};

export const getMyTrips = async () => {
  const response = await api.get<TripSummaryDTO[]>('/trips/my');
  return response.data;
};

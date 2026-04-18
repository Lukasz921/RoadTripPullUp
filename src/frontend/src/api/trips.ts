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

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

export const searchTrips = async (params: { from?: string; to?: string; date?: string }) => {
  const response = await api.get<TripSummaryDTO[]>('/trips', { params });
  return response.data;
};


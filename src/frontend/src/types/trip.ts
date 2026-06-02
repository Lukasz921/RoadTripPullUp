export interface LatLng {
  lat: number;
  lng: number;
}

/** Matches TripSummaryV1DTO returned by GET /trips/me */
export interface TripSummary {
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

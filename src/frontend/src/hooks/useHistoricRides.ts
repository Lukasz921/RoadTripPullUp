import { getTripHistory } from '../api/trips';
import { usePagedTrips } from './usePagedTrips';

/** Loads the logged-in user's past trips via GET /trips/history. */
export function useHistoricRides(page: number, pageSize = 10) {
  return usePagedTrips(getTripHistory, page, pageSize);
}

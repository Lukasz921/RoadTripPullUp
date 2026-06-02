import { mapGeoapifyFeature, type Place } from '../utils/geoapify';

const API_KEY = import.meta.env.VITE_GEOAPIFY_API_KEY as string;
const COUNTRY_CODE = import.meta.env.VITE_GEOAPIFY_COUNTRY_CODE as string;

export async function geocodeAutocomplete(query: string, signal: AbortSignal): Promise<Place[]> {
  if (!API_KEY) throw new Error('Geoapify API key is missing.');

  const params = new URLSearchParams({ text: query, limit: '5', lang: 'pl', apiKey: API_KEY });
  if (COUNTRY_CODE) params.set('filter', `countrycode:${COUNTRY_CODE}`);

  const res = await fetch(`https://api.geoapify.com/v1/geocode/autocomplete?${params}`, { signal });
  if (!res.ok) throw new Error('Geoapify request failed.');

  const data = await res.json();
  return (data.features ?? [])
    .map(mapGeoapifyFeature)
    .filter((p: Place) => Number.isFinite(p.lat) && Number.isFinite(p.lng));
}

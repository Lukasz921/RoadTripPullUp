const API_KEY = import.meta.env.VITE_GEOAPIFY_API_KEY as string;

export async function reverseGeocode(lat: number, lng: number): Promise<string> {
  if (!API_KEY) return `${lat.toFixed(4)}, ${lng.toFixed(4)}`;

  const params = new URLSearchParams({ lat: String(lat), lon: String(lng), apiKey: API_KEY });
  const res = await fetch(`https://api.geoapify.com/v1/geocode/reverse?${params}`);
  if (!res.ok) return `${lat.toFixed(4)}, ${lng.toFixed(4)}`;

  const data = await res.json();
  const p = data.features?.[0]?.properties;
  if (!p) return `${lat.toFixed(4)}, ${lng.toFixed(4)}`;

  return p.city && p.country
    ? `${p.city}, ${p.country}`
    : p.formatted || `${lat.toFixed(4)}, ${lng.toFixed(4)}`;
}

export interface Place {
  label: string;
  lat: number;
  lng: number;
  placeId: string;
  city: string;
  country: string;
}

export function mapGeoapifyFeature(feature: any): Place {
  const p = feature.properties || {};
  return {
    label: p.formatted || p.address_line1 || 'Selected place',
    lat: Number(p.lat),
    lng: Number(p.lon),
    placeId: p.place_id ?? '',
    city: p.city || p.town || p.village || p.county || '',
    country: p.country || '',
  };
}

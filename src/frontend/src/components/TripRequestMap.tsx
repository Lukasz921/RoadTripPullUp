import { useEffect, useRef } from 'react';
import { loadLeaflet } from '../utils/leaflet';
import type { LatLng } from '../api/trips';

interface TripRequestMapProps {
  tripStart: LatLng;
  tripEnd: LatLng;
  pickup: LatLng;
  dropoff: LatLng;
  tripPolyline?: LatLng[];     // the trip's current route
  requestPolyline?: LatLng[];  // route through the passenger's pickup + dropoff
  labels?: { tripStart?: string; tripEnd?: string; pickup?: string; dropoff?: string };
}

export default function TripRequestMap({
  tripStart, tripEnd, pickup, dropoff, tripPolyline, requestPolyline, labels,
}: TripRequestMapProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const mapRef = useRef<any>(null);
  const layersRef = useRef<any[]>([]);

  useEffect(() => {
    let cancelled = false;

    loadLeaflet().then((L) => {
      if (cancelled || !containerRef.current) return;

      if (!mapRef.current) {
        mapRef.current = L.map(containerRef.current, { zoomControl: false }).setView([52.0, 19.0], 6);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png').addTo(mapRef.current);
        L.control.zoom({ position: 'bottomright' }).addTo(mapRef.current);
      }

      layersRef.current.forEach((layer) => layer.remove());
      layersRef.current = [];

      const dot = (bg: string, border: string) => L.divIcon({
        className: '',
        html: `<div style="width:14px;height:14px;border-radius:50%;background:${bg};border:3px solid ${border};box-shadow:0 0 0 2px #fff"></div>`,
        iconSize: [14, 14],
        iconAnchor: [7, 7],
      });

      const bounds: [number, number][] = [];

      const addMarker = (p: LatLng, icon: any, label?: string) => {
        const m = L.marker([p.lat, p.lng], { icon }).addTo(mapRef.current);
        if (label) m.bindTooltip(label, { permanent: false, direction: 'top' });
        layersRef.current.push(m);
        bounds.push([p.lat, p.lng]);
      };

      const addLine = (pts: LatLng[] | undefined, color: string, dash?: string) => {
        if (!pts || pts.length < 2) return;
        const coords: [number, number][] = pts.map((p) => [p.lat, p.lng]);
        const line = L.polyline(coords, { color, weight: 4, dashArray: dash }).addTo(mapRef.current);
        layersRef.current.push(line);
        coords.forEach((c) => bounds.push(c));
      };

      // Trip's current route in neutral gray; the with-detour route in green on top.
      addLine(tripPolyline, '#9aa5b1');
      addLine(requestPolyline, '#8cc63f', '6 6');

      addMarker(tripStart, dot('#4b5563', '#1f2937'), labels?.tripStart ?? 'Trip start');
      addMarker(tripEnd,   dot('#4b5563', '#1f2937'), labels?.tripEnd ?? 'Trip end');
      addMarker(pickup,    dot('#8cc63f', '#12351f'), labels?.pickup ?? 'Pickup');
      addMarker(dropoff,   dot('#e05c5c', '#7a1a1a'), labels?.dropoff ?? 'Dropoff');

      if (bounds.length >= 2) {
        mapRef.current.fitBounds(L.latLngBounds(bounds), { padding: [48, 48] });
      } else if (bounds.length === 1) {
        mapRef.current.setView(bounds[0], 11);
      }
    });

    return () => { cancelled = true; };
  }, [tripStart, tripEnd, pickup, dropoff, tripPolyline, requestPolyline, labels]);

  useEffect(() => {
    return () => {
      if (mapRef.current) {
        mapRef.current.remove();
        mapRef.current = null;
      }
    };
  }, []);

  return (
    <div
      ref={containerRef}
      className="h-full w-full rounded-2xl overflow-hidden"
      style={{ minHeight: '320px' }}
    />
  );
}

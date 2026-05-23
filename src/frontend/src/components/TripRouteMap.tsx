import { useEffect, useRef } from 'react';
import { loadLeaflet } from '../utils/leaflet';
import type { Place } from '../utils/geoapify';

interface TripRouteMapProps {
  origin: Place | null;
  destination: Place | null;
}

export default function TripRouteMap({ origin, destination }: TripRouteMapProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const mapRef = useRef<any>(null);
  const layersRef = useRef<any[]>([]);

  useEffect(() => {
    let cancelled = false;

    loadLeaflet().then((L) => {
      if (cancelled || !containerRef.current) return;

      if (!mapRef.current) {
        mapRef.current = L.map(containerRef.current).setView([52.0, 19.0], 6);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png').addTo(mapRef.current);
      }

      layersRef.current.forEach((layer) => layer.remove());
      layersRef.current = [];

      const greenIcon = L.divIcon({
        className: '',
        html: `<div style="width:14px;height:14px;border-radius:50%;background:#8cc63f;border:3px solid #12351f;box-shadow:0 0 0 2px #fff"></div>`,
        iconSize: [14, 14],
        iconAnchor: [7, 7],
      });

      const redIcon = L.divIcon({
        className: '',
        html: `<div style="width:14px;height:14px;border-radius:50%;background:#e05c5c;border:3px solid #7a1a1a;box-shadow:0 0 0 2px #fff"></div>`,
        iconSize: [14, 14],
        iconAnchor: [7, 7],
      });

      const points: [number, number][] = [];

      if (origin) {
        const m = L.marker([origin.lat, origin.lng], { icon: greenIcon }).addTo(mapRef.current);
        m.bindTooltip(origin.label, { permanent: false, direction: 'top' });
        layersRef.current.push(m);
        points.push([origin.lat, origin.lng]);
      }

      if (destination) {
        const m = L.marker([destination.lat, destination.lng], { icon: redIcon }).addTo(mapRef.current);
        m.bindTooltip(destination.label, { permanent: false, direction: 'top' });
        layersRef.current.push(m);
        points.push([destination.lat, destination.lng]);
      }

      if (points.length === 2) {
        const line = L.polyline(points, { color: '#8cc63f', weight: 3, dashArray: '8 6' }).addTo(mapRef.current);
        layersRef.current.push(line);
        mapRef.current.fitBounds(L.latLngBounds(points), { padding: [48, 48] });
      } else if (points.length === 1) {
        mapRef.current.setView(points[0], 10);
      }
    });

    return () => { cancelled = true; };
  }, [origin, destination]);

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

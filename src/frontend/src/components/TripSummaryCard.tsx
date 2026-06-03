import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import type { LatLng } from '../types/trip';
import { reverseGeocode } from '../api/reverseGeocode';

interface Trip {
  id: string;
  source: LatLng;
  target: LatLng;
  departureTime: string;
  pricePerSeat: number;
  availableSeats: number;
  maxDetourMeters: number;
}

interface TripSummaryCardProps {
  trip: Trip;
  actualDetourMeters?: number;
  action?: {
    label: string;
    onClick: (trip: Trip) => void;
  };
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleString('en-GB', {
    day: '2-digit', month: 'short', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  });
}

function metersToKm(meters: number) {
  return (meters / 1000).toFixed(1) + ' km';
}

function formatCoords(lat: number, lng: number) {
  return `${lat.toFixed(4)}, ${lng.toFixed(4)}`;
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-xs text-[#5d7056]">{label}</p>
      <p className="text-sm font-semibold text-[#12351f]">{value}</p>
    </div>
  );
}

export default function TripSummaryCard({ trip, actualDetourMeters, action }: TripSummaryCardProps) {
  const navigate = useNavigate();
  const [fromLabel, setFromLabel] = useState(formatCoords(trip.source.lat, trip.source.lng));
  const [toLabel, setToLabel] = useState(formatCoords(trip.target.lat, trip.target.lng));

  useEffect(() => {
    reverseGeocode(trip.source.lat, trip.source.lng).then(setFromLabel);
    reverseGeocode(trip.target.lat, trip.target.lng).then(setToLabel);
  }, [trip.source.lat, trip.source.lng, trip.target.lat, trip.target.lng]);

  return (
    <div className="rounded-xl border border-[#d7e8c8] bg-white px-5 py-4">
      <div className="grid gap-x-6 gap-y-2 sm:grid-cols-2">
        <Field label="From" value={fromLabel} />
        <Field label="To" value={toLabel} />
        <Field label="Departure" value={formatDate(trip.departureTime)} />
        <Field label="Price per seat" value={`${trip.pricePerSeat} PLN`} />
        <Field label="Available seats" value={String(trip.availableSeats)} />
        <Field label="Max detour" value={metersToKm(trip.maxDetourMeters)} />
        {actualDetourMeters !== undefined && (
          <Field label="Actual detour" value={metersToKm(actualDetourMeters)} />
        )}
      </div>

      <div className="mt-4 flex gap-2">
        <button
          type="button"
          onClick={() => navigate(`/trip/${trip.id}`)}
          className="flex-1 rounded-xl border border-[#12351f] px-4 py-2 text-sm font-semibold text-[#12351f] hover:bg-[#e8f5e0]"
        >
          View details
        </button>

        {action && (
          <button
            type="button"
            onClick={() => action.onClick(trip)}
            className="flex-1 rounded-xl bg-[#12351f] px-4 py-2 text-sm font-semibold text-white hover:bg-[#1d4a2d]"
          >
            {action.label}
          </button>
        )}
      </div>
    </div>
  );
}

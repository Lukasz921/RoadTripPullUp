import type { TripSummary, LatLng } from '../../../types/trip';

interface TripListProps {
  title: string;
  trips: TripSummary[];
  actionLabel?: string;
  onAction?: () => void;
  actionVariant?: 'dark' | 'green';
}

function formatCoords(coords: LatLng) {
  return `${coords.lat.toFixed(4)}, ${coords.lng.toFixed(4)}`;
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

export default function TripList({ title, trips, actionLabel, onAction, actionVariant = 'dark' }: TripListProps) {
  return (
    <section className="mt-10">
      <div className="mb-4 flex items-center justify-between gap-4">
        <h2 className="text-2xl font-bold text-[#12351f]">{title}</h2>

        {actionLabel && (
          <button
            onClick={onAction}
            className={
              actionVariant === 'green'
                ? 'rounded-xl bg-[#8cc63f] px-4 py-2 text-sm font-semibold text-[#12351f] hover:bg-[#a6dd55]'
                : 'rounded-xl bg-[#12351f] px-4 py-2 text-sm font-semibold text-white hover:bg-[#1d4a2d]'
            }
          >
            {actionLabel}
          </button>
        )}
      </div>

      <div className="space-y-3">
        {trips.length === 0 && (
          <p className="text-sm text-[#5d7056]">No trips yet.</p>
        )}
        {trips.map((trip) => (
          <div key={trip.id} className="rounded-xl border border-[#d7e8c8] bg-white px-5 py-4">
            <div className="grid gap-x-6 gap-y-2 sm:grid-cols-2">
              <Field label="From" value={formatCoords(trip.source)} />
              <Field label="To" value={formatCoords(trip.target)} />
              <Field label="Departure" value={formatDate(trip.departureTime)} />
              <Field label="Price per seat" value={`${trip.pricePerSeat} PLN`} />
              <Field label="Available seats" value={String(trip.availableSeats)} />
              <Field label="Max detour" value={metersToKm(trip.maxDetourMeters)} />
              <Field label="Trip ID" value={trip.id} mono />
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}

function Field({ label, value, mono = false }: { label: string; value: string; mono?: boolean }) {
  return (
    <div>
      <p className="text-xs text-[#5d7056]">{label}</p>
      <p className={`text-sm font-semibold text-[#12351f] ${mono ? 'font-mono' : ''}`}>{value}</p>
    </div>
  );
}

import TripCard from '../../../components/TripCard';
import type { TripSummary } from '../../../types/trip';

interface TripListProps {
  trips: TripSummary[];
  actionLabel?: string;
  onAction?: (trip: TripSummary) => void;
}

export default function TripList({ trips, actionLabel, onAction }: TripListProps) {
  if (trips.length === 0) {
    return <p className="text-sm text-[#5d7056]">No trips yet.</p>;
  }

  return (
    <div className="space-y-3">
      {trips.map((trip) => (
        <TripCard
          key={trip.id}
          trip={trip}
          action={actionLabel && onAction ? { label: actionLabel, onClick: onAction } : undefined}
        />
      ))}
    </div>
  );
}

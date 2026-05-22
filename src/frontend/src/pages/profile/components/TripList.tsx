export interface Trip {
  id: number;
  from: string;
  to: string;
  date: string;
}

interface TripListProps {
  title: string;
  trips: Trip[];
  actionLabel?: string;
  onAction?: () => void;
  actionVariant?: 'dark' | 'green';
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
        {trips.map((trip) => (
          <div
            key={trip.id}
            className="flex items-center justify-between rounded-xl border border-[#d7e8c8] bg-white px-5 py-4"
          >
            <div>
              <p className="font-semibold text-[#12351f]">{trip.from} → {trip.to}</p>
              <p className="text-sm text-[#5d7056]">{trip.date}</p>
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}

import type { Place } from '../../utils/geoapify';

interface MapPointProps {
  label: string;
  place: Place | null;
}

export default function MapPoint({ label, place }: MapPointProps) {
  return (
    <div className="rounded-xl bg-[#f3faee] p-4">
      <p className="text-[#5d7056]">{label}</p>
      {place ? (
        <>
          <p className="font-semibold text-[#12351f]">{place.label}</p>
          <p className="mt-1 text-xs text-[#5d7056]">{place.lat}, {place.lng}</p>
        </>
      ) : (
        <p className="font-semibold text-[#12351f]">Not selected</p>
      )}
    </div>
  );
}

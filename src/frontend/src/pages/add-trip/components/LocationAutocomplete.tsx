import { useEffect, useRef, useState } from 'react';
import { mapGeoapifyFeature, type Place } from '../../../utils/geoapify';

const API_KEY = import.meta.env.VITE_GEOAPIFY_API_KEY as string;
const COUNTRY_CODE = import.meta.env.VITE_GEOAPIFY_COUNTRY_CODE as string;

interface LocationAutocompleteProps {
  label: string;
  value: string;
  selectedPlace: Place | null;
  onQueryChange: (value: string) => void;
  onSelect: (place: Place | null) => void;
  placeholder?: string;
}

export default function LocationAutocomplete({ label, value, selectedPlace, onQueryChange, onSelect, placeholder }: LocationAutocompleteProps) {
  const [suggestions, setSuggestions] = useState<Place[]>([]);
  const [isOpen, setIsOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const skipNextFetch = useRef(false);

  useEffect(() => {
    if (skipNextFetch.current) {
      skipNextFetch.current = false;
      return;
    }

    const query = value.trim();
    setError('');

    if (query.length < 3) {
      setSuggestions([]);
      setIsOpen(false);
      return;
    }

    if (!API_KEY) {
      setError('Geoapify API key is missing.');
      return;
    }

    const controller = new AbortController();
    const timeoutId = window.setTimeout(async () => {
      setIsLoading(true);
      try {
        const params = new URLSearchParams({ text: query, limit: '5', lang: 'pl', apiKey: API_KEY });
        if (COUNTRY_CODE) params.set('filter', `countrycode:${COUNTRY_CODE}`);

        const res = await fetch(
          `https://api.geoapify.com/v1/geocode/autocomplete?${params}`,
          { signal: controller.signal },
        );
        if (!res.ok) throw new Error('Geoapify request failed.');

        const data = await res.json();
        const places = (data.features ?? [])
          .map(mapGeoapifyFeature)
          .filter((p: Place) => Number.isFinite(p.lat) && Number.isFinite(p.lng));

        setSuggestions(places);
        setIsOpen(true);
      } catch (err: any) {
        if (err.name !== 'AbortError') {
          setError('Could not load location suggestions.');
          setSuggestions([]);
          setIsOpen(false);
        }
      } finally {
        setIsLoading(false);
      }
    }, 350);

    return () => {
      window.clearTimeout(timeoutId);
      controller.abort();
    };
  }, [value]);

  function handleSelect(place: Place) {
    skipNextFetch.current = true;
    onSelect(place);
    onQueryChange(place.label);
    setSuggestions([]);
    setIsOpen(false);
  }

  return (
    <div className="relative">
      <label className="block">
        <span className="text-sm text-[#5d7056]">{label}</span>
        <input
          type="text"
          value={value}
          onChange={(e) => { onQueryChange(e.target.value); onSelect(null); }}
          onFocus={() => suggestions.length > 0 && setIsOpen(true)}
          placeholder={placeholder}
          className="mt-1 h-12 w-full rounded-xl border border-[#d7e8c8] bg-white px-4 font-semibold text-[#12351f] outline-none focus:border-[#8cc63f]"
        />
      </label>

      {isLoading && <p className="mt-1 text-xs text-[#5d7056]">Searching...</p>}
      {error && <p className="mt-1 text-xs text-red-700">{error}</p>}
      {selectedPlace && (
        <p className="mt-1 text-xs text-[#5d7056]">
          Selected: {selectedPlace.lat}, {selectedPlace.lng}
        </p>
      )}

      {isOpen && suggestions.length > 0 && (
        <div className="absolute left-0 right-0 top-[76px] z-20 overflow-hidden rounded-xl border border-[#d7e8c8] bg-white shadow-xl">
          {suggestions.map((place) => (
            <button
              key={`${place.placeId}-${place.lat}-${place.lng}`}
              type="button"
              onClick={() => handleSelect(place)}
              className="block w-full border-b border-[#eef5e8] px-4 py-3 text-left hover:bg-[#f3faee] last:border-b-0"
            >
              <p className="font-semibold text-[#12351f]">{place.label}</p>
              <p className="mt-1 text-xs text-[#5d7056]">{place.lat}, {place.lng}</p>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}

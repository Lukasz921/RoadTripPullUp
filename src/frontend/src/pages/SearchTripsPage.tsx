import { useEffect, useRef, useState } from 'react';
import Navbar from '../components/layout/Navbar';
import Footer from '../components/layout/Footer';
import LocationAutocomplete from '../components/LocationAutocomplete';
import MapPoint from '../components/ui/MapPoint';
import TripRouteMap from '../components/TripRouteMap';
import NumberInput from '../components/ui/NumberInput';
import Spinner from '../components/ui/Spinner';
import TripCard from '../components/TripCard';
import { submitSearch as submitSearchApi, pollSearch, type SearchJobResultDTO } from '../api/trips';
import type { Place } from '../utils/geoapify';
import type { TripSummary } from '../types/trip';

const PAGE_SIZE = 10;

export default function SearchTripsPage() {
  // Route
  const [originQuery, setOriginQuery] = useState('');
  const [destinationQuery, setDestinationQuery] = useState('');
  const [origin, setOrigin] = useState<Place | null>(null);
  const [destination, setDestination] = useState<Place | null>(null);

  // Filters
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const [maxPrice, setMaxPrice] = useState('');
  const [minSeats, setMinSeats] = useState('1');
  const [page, setPage] = useState(1);

  // State
  const [submitting, setSubmitting] = useState(false);
  const [polling, setPolling] = useState(false);
  const [results, setResults] = useState<TripSummary[] | null>(null);
  const [error, setError] = useState('');
  const pollRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  function stopPolling() {
    if (pollRef.current !== null) {
      clearTimeout(pollRef.current);
      pollRef.current = null;
    }
  }

  function startPolling(jobId: string, estimatedDurationMs: number) {
    setPolling(true);
    setResults(null);

    const pollInterval = Math.max(250, estimatedDurationMs / 4);

    const poll = () => {
      pollSearch(jobId)
        .then(({ status, data }) => {
          if (status === 200) {
            stopPolling();
            setPolling(false);
            const result = data as SearchJobResultDTO;
            if (result.error) {
              setError(result.error.message);
            } else {
              setResults(result.items ?? []);
            }
          } else {
            pollRef.current = setTimeout(poll, pollInterval);
          }
        })
        .catch((err: { response?: { data?: { detail?: string } } }) => {
          stopPolling();
          setPolling(false);
          setError(err.response?.data?.detail ?? 'Failed to fetch results.');
        });
    };

    pollRef.current = setTimeout(poll, estimatedDurationMs);
  }

  function submitSearch(targetPage: number) {
    if (!origin || !destination) return;

    setError('');
    setResults(null);
    stopPolling();

    const payload = {
      source: { lat: origin.lat, lng: origin.lng },
      target: { lat: destination.lat, lng: destination.lng },
      dateFrom,
      dateTo,
      maxPrice: maxPrice ? Number(maxPrice) : undefined,
      minSeats: Number(minSeats),
      pageSize: PAGE_SIZE,
      page: targetPage,
    };

    setSubmitting(true);
    submitSearchApi(payload)
      .then(({ jobId, estimatedDurationMs }) => {
        startPolling(jobId, estimatedDurationMs ?? 0);
      })
      .catch((err: { response?: { data?: { detail?: string } } }) => {
        setError(err.response?.data?.detail ?? 'Search failed. Please try again.');
      })
      .finally(() => setSubmitting(false));
  }

  function handleSearch() {
    if (!origin) { setError('Please select an origin location.'); return; }
    if (!destination) { setError('Please select a destination location.'); return; }
    if (!dateFrom) { setError('Please select a departure date.'); return; }
    if (!dateTo) { setError('Please select an arrival date.'); return; }
    submitSearch(page);
  }

  function handlePrev() {
    const newPage = Math.max(1, page - 1);
    setPage(newPage);
    submitSearch(newPage);
  }

  function handleNext() {
    const newPage = page + 1;
    setPage(newPage);
    submitSearch(newPage);
  }

  useEffect(() => () => stopPolling(), []);

  return (
    <div className="min-h-screen bg-[#eaf6df]">
      <Navbar />

      <main className="mx-auto max-w-3xl px-6 pb-24 pt-32">
        <h1 className="mb-2 text-3xl font-bold text-[#12351f]">Find a ride</h1>
        <p className="mb-10 text-[#5d7056]">Enter your route and we'll find matching trips.</p>

        <div className="flex flex-col gap-6">
          <form onSubmit={(e) => { e.preventDefault(); handleSearch(); }} className="flex flex-col gap-6">

            {/* Route */}
            <section className="rounded-2xl bg-white p-6 shadow-sm">
              <h2 className="mb-4 text-lg font-semibold text-[#12351f]">Route</h2>
              <div className="flex flex-col gap-4">
                <LocationAutocomplete
                  label="From"
                  value={originQuery}
                  selectedPlace={origin}
                  onQueryChange={setOriginQuery}
                  onSelect={setOrigin}
                  placeholder="City or address"
                />
                <LocationAutocomplete
                  label="To"
                  value={destinationQuery}
                  selectedPlace={destination}
                  onQueryChange={setDestinationQuery}
                  onSelect={setDestination}
                  placeholder="City or address"
                />
              </div>
              <div className="mt-4 grid grid-cols-2 gap-3">
                <MapPoint label="Origin" place={origin} />
                <MapPoint label="Destination" place={destination} />
              </div>
            </section>

            {/* Date range */}
            <section className="rounded-2xl bg-white p-6 shadow-sm">
              <h2 className="mb-4 text-lg font-semibold text-[#12351f]">Date range</h2>
              <div className="grid grid-cols-2 gap-4">
                <label className="block">
                  <span className="text-sm text-[#5d7056]">From</span>
                  <input
                    type="date"
                    value={dateFrom}
                    onChange={(e) => setDateFrom(e.target.value)}
                    required
                    className="mt-1 h-12 w-full rounded-xl border border-[#d7e8c8] bg-white px-4 font-semibold text-[#12351f] outline-none focus:border-[#8cc63f]"
                  />
                </label>
                <label className="block">
                  <span className="text-sm text-[#5d7056]">To</span>
                  <input
                    type="date"
                    value={dateTo}
                    onChange={(e) => setDateTo(e.target.value)}
                    required
                    className="mt-1 h-12 w-full rounded-xl border border-[#d7e8c8] bg-white px-4 font-semibold text-[#12351f] outline-none focus:border-[#8cc63f]"
                  />
                </label>
              </div>
            </section>

            {/* Filters */}
            <section className="rounded-2xl bg-white p-6 shadow-sm">
              <h2 className="mb-4 text-lg font-semibold text-[#12351f]">Filters</h2>
              <div className="grid grid-cols-2 gap-4">
                <NumberInput
                  label="Min seats"
                  value={minSeats}
                  onChange={setMinSeats}
                  min={1}
                  step={1}
                  placeholder="1"
                />
                <NumberInput
                  label="Max price (PLN)"
                  value={maxPrice}
                  onChange={setMaxPrice}
                  min={0}
                  step={1}
                  placeholder="Any"
                />
              </div>
            </section>

            {error && (
              <p className="rounded-xl bg-red-50 px-4 py-3 text-sm text-red-700">{error}</p>
            )}

            <button
              type="submit"
              disabled={submitting || polling}
              className="h-14 w-full rounded-2xl bg-[#8cc63f] font-bold text-white transition hover:bg-[#78b030] disabled:opacity-60"
            >
              {submitting ? 'Submitting…' : 'Search rides'}
            </button>

          </form>

          {/* Map */}
          <section className="rounded-2xl bg-white p-4 shadow-sm" style={{ height: '420px' }}>
            <h2 className="mb-3 text-lg font-semibold text-[#12351f]">Preview</h2>
            <div className="h-[calc(100%-2.5rem)]">
              <TripRouteMap origin={origin} destination={destination} />
            </div>
          </section>
          
          {polling && <Spinner label="Finding matching rides…" />}

          {/* Results */}
          {results !== null && (
            <section className="flex flex-col gap-3">
              <h2 className="text-xl font-bold text-[#12351f]">
                {results.length === 0 ? 'No rides found' : `${results.length} ride${results.length !== 1 ? 's' : ''} found`}
              </h2>

              {results.length === 0 && (
                <p className="text-sm text-[#5d7056]">Try adjusting your route or filters.</p>
              )}

              {results.map((trip) => (
                <TripCard
                  key={trip.id}
                  trip={trip}
                  action={{ label: 'Join ride', onClick: (t) => console.log('Join ride:', t.id) }}
                />
              ))}
            </section>
          )}

      

          {/* Pagination */}
          {results !== null && (
            <div className="flex items-center justify-between gap-4 rounded-2xl bg-white px-6 py-4 shadow-sm">
              <button
                type="button"
                onClick={handlePrev}
                disabled={page <= 1 || submitting || polling}
                className="rounded-xl bg-[#8cc63f] px-6 py-2 font-semibold text-white transition hover:bg-[#78b030] disabled:opacity-40"
              >
                Previous
              </button>

              <span className="font-bold text-[#12351f]">{page}</span>

              <button
                type="button"
                onClick={handleNext}
                disabled={submitting || polling}
                className="rounded-xl bg-[#8cc63f] px-6 py-2 font-semibold text-white transition hover:bg-[#78b030] disabled:opacity-40"
              >
                Next
              </button>
            </div>
          )}
        </div>
      </main>

      <Footer />
    </div>
  );
}

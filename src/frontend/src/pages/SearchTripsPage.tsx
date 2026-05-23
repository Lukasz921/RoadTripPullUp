import { useEffect, useRef, useState } from 'react';
import Navbar from '../components/layout/Navbar';
import Footer from '../components/layout/Footer';
import LocationAutocomplete from '../components/LocationAutocomplete';
import MapPoint from '../components/ui/MapPoint';
import TripRouteMap from '../components/TripRouteMap';
import NumberInput from '../components/ui/NumberInput';
import Spinner from '../components/ui/Spinner';
import { tripApi } from '../api/axiosConfig';
import type { Place } from '../utils/geoapify';

const PAGE_SIZE = 10;
const POLL_INTERVAL_MS = 1000;

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

  const [submitting, setSubmitting] = useState(false);
  const [polling, setPolling] = useState(false);
  const [error, setError] = useState('');
  const pollRef = useRef<ReturnType<typeof setInterval> | null>(null);

  function stopPolling() {
    if (pollRef.current !== null) {
      clearInterval(pollRef.current);
      pollRef.current = null;
    }
  }

  function startPolling(statusUrl: string, estimatedDurationMs: number) {
    setPolling(true);

    // strip /api/v1 prefix since tripApi base already includes it
    const path = statusUrl.replace('/api/v1', '');

    const beginPolling = () => {
      pollRef.current = setInterval(() => {
        tripApi
          .get(path)
          .then((res) => {
            const { status, trips } = res.data;

            if (status === 'done') {
              stopPolling();
              setPolling(false);
              console.log('Search results:', trips);
              // TODO: setResults(trips ?? []);
            }

            if (status === 'failed') {
              stopPolling();
              setPolling(false);
              setError('Search job failed. Please try again.');
            }
          })
          .catch((err) => {
            stopPolling();
            setPolling(false);
            console.error('Polling error:', err);
            setError(err.response?.data?.detail ?? 'Failed to fetch results.');
          });
      }, POLL_INTERVAL_MS);
    };

    // wait the estimated duration before first poll to avoid hitting the server too early
    setTimeout(beginPolling, estimatedDurationMs);
  }

  function handleSearch(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    stopPolling();

    if (!origin) { setError('Please select an origin location.'); return; }
    if (!destination) { setError('Please select a destination location.'); return; }

    const payload = {
      source: { lat: origin.lat, lng: origin.lng },
      target: { lat: destination.lat, lng: destination.lng },
      dateFrom,
      dateTo,
      maxPrice: maxPrice ? Number(maxPrice) : undefined,
      minSeats: Number(minSeats),
      limit: PAGE_SIZE,
      page,
    };

    setSubmitting(true);
    tripApi
      .post('/trips/search', payload)
      .then((res) => {
        const { statusUrl, estimatedDurationMs } = res.data;
        startPolling(statusUrl, estimatedDurationMs ?? 0);
      })
      .catch((err) => {
        console.error('Search failed:', err);
        setError(err.response?.data?.detail ?? 'Search failed. Please try again.');
      })
      .finally(() => setSubmitting(false));
  }

  // Clean up interval if user navigates away
  useEffect(() => () => stopPolling(), []);

  return (
    <div className="min-h-screen bg-[#eaf6df]">
      <Navbar />

      <main className="mx-auto max-w-6xl px-6 pb-24 pt-32">
        <h1 className="mb-2 text-3xl font-bold text-[#12351f]">Find a ride</h1>
        <p className="mb-10 text-[#5d7056]">Enter your route and we'll find matching trips.</p>

        <div className="grid gap-8 lg:grid-cols-2">

          {/* ── Left column: form ── */}
          <div className="flex flex-col gap-6">
            <form onSubmit={handleSearch} className="flex flex-col gap-6">

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
                    <span className="text-sm text-[#5d7056]">From (optional)</span>
                    <input
                      type="date"
                      value={dateFrom}
                      onChange={(e) => setDateFrom(e.target.value)}
                      className="mt-1 h-12 w-full rounded-xl border border-[#d7e8c8] bg-white px-4 font-semibold text-[#12351f] outline-none focus:border-[#8cc63f]"
                    />
                  </label>
                  <label className="block">
                    <span className="text-sm text-[#5d7056]">To (optional)</span>
                    <input
                      type="date"
                      value={dateTo}
                      onChange={(e) => setDateTo(e.target.value)}
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

              {/* Page */}
              <section className="rounded-2xl bg-white p-6 shadow-sm">
                <h2 className="mb-4 text-lg font-semibold text-[#12351f]">Page</h2>
                <NumberInput
                  label="Page number"
                  value={String(page)}
                  onChange={(v) => setPage(Math.max(1, Number(v)))}
                  min={1}
                  step={1}
                  placeholder="1"
                />
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

              {polling && <Spinner label="Finding matching rides…" />}
            </form>
          </div>

          {/* ── Right column: map ── */}
          <div>
            <section className="rounded-2xl bg-white p-4 shadow-sm" style={{ height: '520px' }}>
              <h2 className="mb-3 text-lg font-semibold text-[#12351f]">Preview</h2>
              <div className="h-[calc(100%-2.5rem)]">
                <TripRouteMap origin={origin} destination={destination} />
              </div>
            </section>
          </div>

        </div>
      </main>

      <Footer />
    </div>
  );
}

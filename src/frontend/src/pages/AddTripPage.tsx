import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import Navbar from '../components/layout/Navbar';
import Footer from '../components/layout/Footer';
import LocationAutocomplete from '../components/LocationAutocomplete';
import MapPoint from '../components/ui/MapPoint';
import TripRouteMap from '../components/TripRouteMap';
import NumberInput from '../components/ui/NumberInput';
import type { Place } from '../utils/geoapify';
import { tripApi } from '../api/axiosConfig';

export default function AddTripPage() {
  const navigate = useNavigate();

  // Location state
  const [originQuery, setOriginQuery] = useState('');
  const [destinationQuery, setDestinationQuery] = useState('');
  const [origin, setOrigin] = useState<Place | null>(null);
  const [destination, setDestination] = useState<Place | null>(null);

  // Trip details
  const [departureDate, setDepartureDate] = useState('');
  const [departureTime, setDepartureTime] = useState('');
  const [seats, setSeats] = useState('1');
  const [pricePerSeat, setPricePerSeat] = useState('0');
  const [maxDetourMeters, setMaxDetourMeters] = useState('5000');

  const [submitError, setSubmitError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitError('');

    if (!origin) { setSubmitError('Please select an origin location.'); return; }
    if (!destination) { setSubmitError('Please select a destination location.'); return; }
    if (!departureDate || !departureTime) { setSubmitError('Please set a departure date and time.'); return; }

    const payload = {
      source: { lat: origin.lat, lng: origin.lng },
      target: { lat: destination.lat, lng: destination.lng },
      departureTime: `${departureDate}T${departureTime}:00`,
      maxDetourMeters: Number(maxDetourMeters),
      pricePerSeat: Number(pricePerSeat),
      availableSeats: Number(seats),
    };

    setSubmitting(true);
    tripApi
      .post('/trips', payload)
      .then(() => navigate('/profile'))
      .catch((err) => {
        console.error('Failed to create trip:', err);
        setSubmitError(err.response?.data?.detail ?? 'Failed to publish trip. Please try again.');
      })
      .finally(() => setSubmitting(false));
  }

  return (
    <div className="min-h-screen bg-[#eaf6df]">
      <Navbar />

      <main className="mx-auto max-w-6xl px-6 pb-24 pt-32">
        <h1 className="mb-2 text-3xl font-bold text-[#12351f]">Add a trip</h1>
        <p className="mb-10 text-[#5d7056]">Fill in the details and share your ride with others.</p>

        <form onSubmit={handleSubmit} className="grid gap-8 lg:grid-cols-2">

          {/* ── Left column: form ── */}
          <div className="flex flex-col gap-6">

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

              {/* Selected points summary */}
              <div className="mt-4 grid grid-cols-2 gap-3">
                <MapPoint label="Origin" place={origin} />
                <MapPoint label="Destination" place={destination} />
              </div>
            </section>

            {/* Date & Time */}
            <section className="rounded-2xl bg-white p-6 shadow-sm">
              <h2 className="mb-4 text-lg font-semibold text-[#12351f]">Departure</h2>
              <div className="grid grid-cols-2 gap-4">
                <label className="block">
                  <span className="text-sm text-[#5d7056]">Date</span>
                  <input
                    type="date"
                    value={departureDate}
                    onChange={(e) => setDepartureDate(e.target.value)}
                    className="mt-1 h-12 w-full rounded-xl border border-[#d7e8c8] bg-white px-4 font-semibold text-[#12351f] outline-none focus:border-[#8cc63f]"
                  />
                </label>
                <label className="block">
                  <span className="text-sm text-[#5d7056]">Time</span>
                  <input
                    type="time"
                    value={departureTime}
                    onChange={(e) => setDepartureTime(e.target.value)}
                    className="mt-1 h-12 w-full rounded-xl border border-[#d7e8c8] bg-white px-4 font-semibold text-[#12351f] outline-none focus:border-[#8cc63f]"
                  />
                </label>
              </div>
            </section>

            {/* Seats, Price & Detour */}
            <section className="rounded-2xl bg-white p-6 shadow-sm">
              <h2 className="mb-4 text-lg font-semibold text-[#12351f]">Details</h2>
              <div className="grid grid-cols-2 gap-4">
                <NumberInput
                  label="Available seats"
                  value={seats}
                  onChange={setSeats}
                  min={1}
                  step={1}
                  placeholder="1"
                />
                <NumberInput
                  label="Price per seat (PLN)"
                  value={pricePerSeat}
                  onChange={setPricePerSeat}
                  min={0}
                  step={0.5}
                  placeholder="0"
                />
                <div className="col-span-2">
                  <NumberInput
                    label="Max detour (meters)"
                    value={maxDetourMeters}
                    onChange={setMaxDetourMeters}
                    min={0}
                    step={500}
                    placeholder="5000"
                  />
                </div>
              </div>
            </section>

            {submitError && (
              <p className="rounded-xl bg-red-50 px-4 py-3 text-sm text-red-700">{submitError}</p>
            )}

            <button
              type="submit"
              disabled={submitting}
              className="h-14 w-full rounded-2xl bg-[#8cc63f] font-bold text-white transition hover:bg-[#78b030] disabled:opacity-60"
            >
              {submitting ? 'Publishing…' : 'Publish trip'}
            </button>
          </div>

          {/* ── Right column: map ── */}
          <div className="flex flex-col gap-4">
            <section className="rounded-2xl bg-white p-4 shadow-sm" style={{ height: '520px' }}>
              <h2 className="mb-3 text-lg font-semibold text-[#12351f]">Preview</h2>
              <div className="h-[calc(100%-2.5rem)]">
                <TripRouteMap origin={origin} destination={destination} />
              </div>
            </section>
          </div>

        </form>
      </main>

      <Footer />
    </div>
  );
}

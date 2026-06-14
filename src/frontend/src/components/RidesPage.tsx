import { useState } from 'react';
import Navbar from './layout/Navbar';
import Footer from './layout/Footer';
import TripSummaryCard from './TripSummaryCard';
import { usePagedTrips } from '../hooks/usePagedTrips';
import type { PagedTripsDTO, TripDTO } from '../api/trips';

const PAGE_SIZE = 10;

interface RidesPageProps {
  title: string;
  fetchTrips: (page: number, pageSize: number) => Promise<PagedTripsDTO>;
  emptyMessage: string;
  headerButton?: { label: string; onClick: () => void };
  cardAction?: (trip: TripDTO) => { label: string; onClick: () => void } | undefined;
  detailsState?: Record<string, unknown>;
}

export default function RidesPage({ title, fetchTrips, emptyMessage, headerButton, cardAction, detailsState }: RidesPageProps) {
  const [page, setPage] = useState(1);
  const { trips, totalCount, loading, error } = usePagedTrips(fetchTrips, page, PAGE_SIZE);

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  return (
    <main className="flex min-h-screen flex-col bg-[#f3faee] text-[#12351f]">
      <Navbar />

      <div className="mx-auto w-full max-w-3xl flex-1 px-6 pb-16 pt-28">
        <div className="mb-6 flex items-center justify-between">
          <h1 className="text-3xl font-bold">{title}</h1>
          {headerButton && (
            <button
              onClick={headerButton.onClick}
              className="rounded-xl bg-[#8cc63f] px-4 py-2 text-sm font-semibold text-[#12351f] hover:bg-[#a6dd55]"
            >
              {headerButton.label}
            </button>
          )}
        </div>

        {loading && <p className="text-sm text-[#5d7056]">Loading rides...</p>}

        {error && (
          <p className="rounded-lg bg-red-50 px-4 py-2 text-sm text-red-600">{error}</p>
        )}

        {!loading && !error && trips.length === 0 && (
          <p className="text-sm text-[#5d7056]">{emptyMessage}</p>
        )}

        {!loading && !error && trips.length > 0 && (
          <div className="flex flex-col gap-3">
            {trips.map((trip) => (
              <TripSummaryCard key={trip.id} trip={trip} action={cardAction?.(trip)} detailsState={detailsState} />
            ))}
          </div>
        )}

        {!loading && !error && totalCount > 0 && (
          <div className="mt-6 flex items-center justify-between gap-4 rounded-2xl bg-white px-6 py-4 shadow-sm">
            <button
              type="button"
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page <= 1}
              className="rounded-xl bg-[#8cc63f] px-6 py-2 font-semibold text-white transition hover:bg-[#78b030] disabled:opacity-40"
            >
              Previous
            </button>

            <span className="font-bold text-[#12351f]">{page} / {totalPages}</span>

            <button
              type="button"
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              disabled={page >= totalPages}
              className="rounded-xl bg-[#8cc63f] px-6 py-2 font-semibold text-white transition hover:bg-[#78b030] disabled:opacity-40"
            >
              Next
            </button>
          </div>
        )}
      </div>

      <Footer />
    </main>
  );
}

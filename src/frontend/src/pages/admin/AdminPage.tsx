import { useEffect, useState } from 'react';
import Navbar from '../../components/layout/Navbar';
import Footer from '../../components/layout/Footer';
import { getComplaints, deleteComplaint, type ComplaintResponseDTO } from '../../api/admin';
import { formatDate } from '../../utils/format';

const PAGE_SIZE = 10;

function ComplaintCard({
  complaint,
  onDelete,
  deleting,
}: {
  complaint: ComplaintResponseDTO;
  onDelete: () => void;
  deleting: boolean;
}) {
  return (
    <div className="flex items-center justify-between gap-4 rounded-xl border border-[#d7e8c8] bg-white px-5 py-4">
      <div>
        <p className="text-sm font-semibold text-[#12351f]">{complaint.reason}</p>
        <p className="mt-1 text-xs text-[#5d7056]">{formatDate(complaint.createdAt)}</p>
      </div>
      <button
        type="button"
        onClick={onDelete}
        disabled={deleting}
        className="whitespace-nowrap rounded-xl border border-red-300 px-4 py-2 text-sm font-semibold text-red-600 transition hover:bg-red-50 disabled:opacity-40"
      >
        {deleting ? 'Deleting...' : 'Delete'}
      </button>
    </div>
  );
}

export default function AdminPage() {
  const [page, setPage] = useState(1);
  const [complaints, setComplaints] = useState<ComplaintResponseDTO[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError('');
    getComplaints(page, PAGE_SIZE)
      .then((data) => {
        if (cancelled) return;
        setComplaints(data.items);
        setTotalCount(data.totalCount);
      })
      .catch(() => {
        if (!cancelled) setError('Failed to load complaints.');
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [page, reloadKey]);

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  async function handleDelete(id: string) {
    setDeletingId(id);
    try {
      await deleteComplaint(id);
      // If we just removed the last item on a non-first page, step back a page.
      if (complaints.length === 1 && page > 1) {
        setPage((p) => p - 1);
      } else {
        setReloadKey((k) => k + 1);
      }
    } catch {
      setError('Failed to delete complaint.');
    } finally {
      setDeletingId(null);
    }
  }

  return (
    <main className="flex min-h-screen flex-col bg-[#f3faee] text-[#12351f]">
      <Navbar />

      <div className="mx-auto w-full max-w-5xl flex-1 px-6 pb-16 pt-28">
        <h1 className="mb-6 text-3xl font-bold">Complaints</h1>

        {loading && <p className="text-sm text-[#5d7056]">Loading complaints...</p>}

        {error && (
          <p className="rounded-lg bg-red-50 px-4 py-2 text-sm text-red-600">{error}</p>
        )}

        {!loading && !error && complaints.length === 0 && (
          <p className="text-sm text-[#5d7056]">There are no complaints.</p>
        )}

        {!loading && !error && complaints.length > 0 && (
          <div className="flex flex-col gap-3">
            {complaints.map((complaint) => (
              <ComplaintCard
                key={complaint.id}
                complaint={complaint}
                onDelete={() => handleDelete(complaint.id)}
                deleting={deletingId === complaint.id}
              />
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

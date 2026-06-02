import { useNavigate } from 'react-router-dom';
import Navbar from '../components/layout/Navbar';
import Footer from '../components/layout/Footer';

export default function JoinedRidesPage() {
  const navigate = useNavigate();

  return (
    <main className="flex min-h-screen flex-col bg-[#f3faee] text-[#12351f]">
      <Navbar />

      <div className="mx-auto w-full max-w-3xl flex-1 px-6 pb-16 pt-28">
        <h1 className="mb-6 text-3xl font-bold">Joined rides</h1>

        <button
          onClick={() => navigate('/search')}
          className="rounded-xl bg-[#8cc63f] px-4 py-2 text-sm font-semibold text-[#12351f] hover:bg-[#a6dd55]"
        >
          Search rides
        </button>
      </div>

      <Footer />
    </main>
  );
}

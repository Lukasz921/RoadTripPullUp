import { useParams } from 'react-router-dom';
import Navbar from '../components/layout/Navbar';
import Footer from '../components/layout/Footer';

export default function TripDetailsPage() {
  const { id } = useParams<{ id: string }>();

  return (
    <main className="flex min-h-screen flex-col bg-[#f3faee] text-[#12351f]">
      <Navbar />

      <div className="mx-auto w-full max-w-3xl flex-1 px-6 pb-16 pt-28">
        <h1 className="text-3xl font-bold">Trip details</h1>
        <p className="mt-2 text-sm text-[#5d7056]">Trip ID: {id}</p>
      </div>

      <Footer />
    </main>
  );
}

import Navbar from '../../components/layout/Navbar';
import Footer from '../../components/layout/Footer';

export default function AdminPage() {
  return (
    <main className="flex min-h-screen flex-col bg-[#f3faee] text-[#12351f]">
      <Navbar />

      <div className="mx-auto w-full max-w-5xl flex-1 px-6 pb-16 pt-28">
        <h1 className="mb-6 text-3xl font-bold">Admin</h1>

        <p className="text-sm text-[#5d7056]">Admin tools will go here.</p>
      </div>

      <Footer />
    </main>
  );
}

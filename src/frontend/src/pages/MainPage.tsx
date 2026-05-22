import Navbar from '../components/layout/Navbar';
import Footer from '../components/layout/Footer';
import Hero from './main/sections/Hero';
import Benefits from './main/sections/Benefits';
import AddRoute from './main/sections/AddRoute';
import FindRideCTA from './main/sections/FindRideCTA';

export default function MainPage() {
  return (
    <main className="min-h-screen bg-[#eaf6df]">
      <Navbar />
      <Hero />
      <Benefits />
      <AddRoute />
      <FindRideCTA />
      <Footer />
    </main>
  );
}

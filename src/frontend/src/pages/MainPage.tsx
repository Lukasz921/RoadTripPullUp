import Navbar from '../components/layout/Navbar';
import Footer from '../components/layout/Footer';
import Hero from './main/sections/Hero';
import Benefits from './main/sections/Benefits';
import AddTrip from './main/sections/AddTrip';
import JoinTripCTA from './main/sections/JoinTripCTA';

export default function MainPage() {
  return (
    <main className="min-h-screen bg-[#eaf6df]">
      <Navbar />
      <Hero />
      <Benefits />
      <AddTrip />
      <JoinTripCTA />
      <Footer />
    </main>
  );
}

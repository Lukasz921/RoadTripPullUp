import { useState } from 'react';
import Navbar from '../../components/layout/Navbar';
import Footer from '../../components/layout/Footer';
import Hero from './sections/Hero';
import Benefits from './sections/Benefits';
import AddRoute from './sections/AddRoute';
import LoginCTA from './sections/LoginCTA';

export default function LandingPage() {
  const [loggedIn] = useState(false);

  return (
    <main className="min-h-screen bg-[#eaf6df]">
      <Navbar />
      <Hero loggedIn={loggedIn} />
      <Benefits />
      <AddRoute />
      <LoginCTA />
      <Footer />
    </main>
  );
}

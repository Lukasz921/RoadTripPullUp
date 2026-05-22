import Navbar from '../components/layout/Navbar';
import Footer from '../components/layout/Footer';
import Hero from './main/sections/Hero';
import Benefits from './main/sections/Benefits';
import AddRoute from './main/sections/AddRoute';
import LoginCTA from './main/sections/LoginCTA';

export default function MainPage() {
  const loggedIn = !!localStorage.getItem('token');

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

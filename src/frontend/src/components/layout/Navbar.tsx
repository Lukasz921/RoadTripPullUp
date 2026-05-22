import { useNavigate } from 'react-router-dom';
import PullUpLogo from './PullUpLogo';

const NAV_ROUTES: Record<string, string> = {
  home: '/',
  'find-ride': '/search',
  'add-route': '/add-route',
  login: '/login',
};

export default function Navbar() {
  const navigate = useNavigate();

  function goTo(id: string) {
    const element = document.getElementById(id);
    if (element) {
      element.scrollIntoView({ behavior: 'smooth', block: 'start' });
      return;
    }
    navigate(NAV_ROUTES[id] ?? '/');
  }

  return (
    <header className="fixed inset-x-0 top-0 z-50 bg-[#12351f]/90 text-white backdrop-blur">
      <nav className="mx-auto flex h-20 max-w-7xl items-center justify-between px-6">
        <button onClick={() => goTo('home')} className="px-1 py-1" aria-label="Go to home">
          <PullUpLogo />
        </button>

        <div className="ml-auto flex gap-2">
          <button onClick={() => goTo('home')} className="px-4 py-2 text-white/70 hover:text-white">Home</button>
          <button onClick={() => goTo('find-ride')} className="px-4 py-2 text-white/70 hover:text-white">Find ride</button>
          <button onClick={() => goTo('benefits')} className="px-4 py-2 text-white/70 hover:text-white">Benefits</button>
          <button onClick={() => goTo('add-route')} className="px-4 py-2 text-white/70 hover:text-white">Add route</button>
          <button onClick={() => goTo('login')} className="px-4 py-2 text-white/70 hover:text-white">Login</button>
        </div>
      </nav>
    </header>
  );
}

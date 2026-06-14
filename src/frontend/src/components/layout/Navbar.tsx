import { useNavigate } from 'react-router-dom';
import logoSrc from '../../assets/logo.svg';

// Reads the role claim out of the stored JWT without an extra request.
function getRole(): string | null {
  const token = localStorage.getItem('token');
  if (!token) return null;
  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    return (
      payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ??
      payload.role ??
      null
    );
  } catch {
    return null;
  }
}

export default function Navbar() {
  const navigate = useNavigate();
  const loggedIn = !!localStorage.getItem('token');
  const isAdmin = getRole() === 'ADMIN';

  return (
    <header className="fixed inset-x-0 top-0 z-1000 bg-[#12351f]/90 text-white backdrop-blur">
      <nav className="mx-auto flex h-20 max-w-7xl items-center justify-between px-6">
        <button onClick={() => navigate('/')} className="px-1 py-1" aria-label="Go to home">
          <img src={logoSrc} className="h-12 w-36" alt="PullUp logo" />
        </button>

        <div className="ml-auto flex gap-2">
          <button onClick={() => navigate('/search')} className="px-4 py-2 text-white/70 hover:text-white">Find a ride</button>
          <button onClick={() => navigate(loggedIn ? '/add-trip' : '/login')} className="px-4 py-2 text-white/70 hover:text-white">Add trip</button>
          {isAdmin && (
            <button onClick={() => navigate('/admin')} className="px-4 py-2 text-white/70 hover:text-white">Admin</button>
          )}
          {loggedIn ? (
            <button onClick={() => navigate('/profile')} className="px-4 py-2 text-white/70 hover:text-white">Profile</button>
          ) : (
            <button onClick={() => navigate('/login')} className="px-4 py-2 text-white/70 hover:text-white">Login</button>
          )}
        </div>
      </nav>
    </header>
  );
}

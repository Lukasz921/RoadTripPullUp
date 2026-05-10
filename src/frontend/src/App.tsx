import { BrowserRouter, Routes, Route, Link, useNavigate } from 'react-router-dom';
import Login from './pages/Login';
import Register from './pages/Register';
import TripForm from './pages/TripForm';
import Trips from './pages/Trips';
import TripDetails from './pages/TripDetails';
import Chat from './pages/Chat';
import Messages from './pages/Messages';
import MyTrips from './pages/MyTrips';

function getCurrentUserEmail(): string | null {
  const token = localStorage.getItem('token');
  if (!token) return null;
  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    return payload.email ?? payload.sub ?? null;
  } catch {
    return null;
  }
}

function UserBadge() {
  const navigate = useNavigate();
  const email = getCurrentUserEmail();

  if (!email) return null;

  const handleLogout = () => {
    localStorage.removeItem('token');
    navigate('/login');
  };

  return (
    <div style={{
      position: 'fixed',
      bottom: 16,
      right: 16,
      background: '#fff',
      border: '1px solid #e0e0e0',
      borderRadius: 12,
      padding: '8px 12px',
      display: 'flex',
      alignItems: 'center',
      gap: 8,
      boxShadow: '0 2px 8px rgba(0,0,0,0.12)',
      zIndex: 1000,
      fontSize: 13,
    }}>
      <div style={{
        width: 28,
        height: 28,
        borderRadius: '50%',
        background: '#0084ff',
        color: '#fff',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        fontWeight: 700,
        fontSize: 12,
        flexShrink: 0,
      }}>
        {email.charAt(0).toUpperCase()}
      </div>
      <span style={{ color: '#333', maxWidth: 160, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
        {email}
      </span>
      <button
        onClick={handleLogout}
        style={{
          background: 'none',
          border: 'none',
          cursor: 'pointer',
          color: '#888',
          fontSize: 12,
          padding: '2px 4px',
        }}
        title="Wyloguj"
      >
        Wyloguj
      </button>
    </div>
  );
}

function Home() {
  const email = getCurrentUserEmail();

  return (
    <div style={{ maxWidth: 480, margin: '60px auto', padding: '0 16px' }}>
      <h1 style={{ marginBottom: 24 }}>RoadTripPullUp</h1>
      <nav style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
        {!email ? (
          <>
            <Link to="/login">
              <button style={navBtnStyle}>Zaloguj się</button>
            </Link>
            <Link to="/register">
              <button style={navBtnStyle}>Zarejestruj się</button>
            </Link>
          </>
        ) : (
          <>
            <Link to="/trip/create">
              <button style={navBtnStyle}>+ Dodaj ofertę</button>
            </Link>
            <Link to="/my-trips">
              <button style={navBtnStyle}>🚗 Moje oferty</button>
            </Link>
            <Link to="/messages">
              <button style={navBtnStyle}>💬 Wiadomości</button>
            </Link>
          </>
        )}
        <Link to="/trips">
          <button style={navBtnStyle}>🔍 Szukaj przejazdów</button>
        </Link>
      </nav>
    </div>
  );
}

const navBtnStyle: React.CSSProperties = {
  width: '100%',
  padding: '12px 16px',
  fontSize: 15,
  background: '#fff',
  border: '1px solid #ddd',
  borderRadius: 10,
  cursor: 'pointer',
  textAlign: 'left',
};

function App() {
  return (
    <BrowserRouter>
      <UserBadge />
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
        <Route path="/trip/create" element={<TripForm />} />
        <Route path="/trips" element={<Trips />} />
        <Route path="/trips/:id" element={<TripDetails />} />
        <Route path="/chat/:receiverId" element={<Chat />} />
        <Route path="/messages" element={<Messages />} />
        <Route path="/messages/:partnerId" element={<Messages />} />
        <Route path="/my-trips" element={<MyTrips />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;

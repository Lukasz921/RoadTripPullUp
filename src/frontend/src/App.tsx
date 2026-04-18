import { BrowserRouter, Routes, Route, Link } from 'react-router-dom';
import Login from './pages/Login';
import Register from './pages/Register';
import TripForm from './pages/TripForm';
import Trips from './pages/Trips';

function Home() {
  return (
    <div>
      <h1>Strona Główna</h1>
      <nav style={{ display: 'flex', gap: '10px' }}>
        <Link to="/login">Zaloguj</Link>
        <Link to="/register">Zarejestruj</Link>
        <Link to="/trip/create">Dodaj ofertę</Link>
        <Link to="/trips">Szukaj ofert</Link>
      </nav>
    </div>
  );
}

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
        <Route path="/trip/create" element={<TripForm />} />
        <Route path="/trips" element={<Trips />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
import { BrowserRouter, Routes, Route, Link } from 'react-router-dom';
import Login from './pages/Login';
import Register from './pages/Register';

function Home() {
  return (
    <div>
      <h1>Strona Główna</h1>
      <nav style={{ display: 'flex', gap: '10px' }}>
        <Link to="/login">Zaloguj</Link>
        <Link to="/register">Zarejestruj</Link>
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
      </Routes>
    </BrowserRouter>
  );
}

export default App;
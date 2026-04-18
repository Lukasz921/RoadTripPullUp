import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../api/axiosConfig';

export default function Register() {
  const [name, setName] = useState('');
  const [surname, setSurname] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const navigate = useNavigate();

  const handleSubmit = async (e: React.SyntheticEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError('');

    if (!name || !surname || !email || !password) {
      setError('Wypełnij wszystkie pola');
      return;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email)) {
      setError('Niepoprawny format email');
      return;
    }

    try {
      await api.post('/auth/register', { name, surname, email, password });
      navigate('/login');
    } catch (err: any) {
      setError(err.response?.data?.message || 'Wystąpił błąd podczas rejestracji');
    }
  };

  return (
    <div>
      <h2>Rejestracja</h2>
      {error && <p style={{ color: 'red' }}>{error}</p>}
      <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', width: '300px', gap: '10px' }}>
        <input type="text" placeholder="Imię" value={name} onChange={(e) => setName(e.target.value)} />
        <input type="text" placeholder="Nazwisko" value={surname} onChange={(e) => setSurname(e.target.value)} />
        <input type="email" placeholder="Email" value={email} onChange={(e) => setEmail(e.target.value)} />
        <input type="password" placeholder="Hasło" value={password} onChange={(e) => setPassword(e.target.value)} />
        <button type="submit">Zarejestruj</button>
      </form>
    </div>
  );
}
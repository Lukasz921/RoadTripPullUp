import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import api from '../api/axiosConfig';

export default function TripForm() {
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [betweenPoints, setBetweenPoints] = useState('');
  const [price, setPrice] = useState('');
  const [date, setDate] = useState('');
  const [maxPassengers, setMaxPassengers] = useState('');
  const [responseText, setResponseText] = useState('');
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const token = useMemo(() => localStorage.getItem('token') ?? '', []);

  const handleSubmit = async (e: React.SyntheticEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError('');
    setResponseText('');

    if (!from || !to || !price || !date || !maxPassengers) {
      setError('Wypełnij wszystkie wymagane pola.');
      return;
    }

    if (!token) {
      setError('Musisz być zalogowany, aby dodać ofertę.');
      console.error('Brak tokenu JWT w localStorage.');
      return;
    }

    const parsedPrice = Number(price);
    const parsedMaxPassengers = Number(maxPassengers);

    if (Number.isNaN(parsedPrice) || parsedPrice <= 0) {
      setError('Cena musi być większa niż 0.');
      return;
    }

    if (Number.isNaN(parsedMaxPassengers) || parsedMaxPassengers <= 0) {
      setError('Liczba pasażerów musi być większa niż 0.');
      return;
    }

    const payload = {
      route: {
        from,
        to,
        betweenPoints: betweenPoints
          .split(',')
          .map((point) => point.trim())
          .filter((point) => point.length > 0),
      },
      price: parsedPrice,
      date: new Date(date).toISOString(),
      maxPassengers: parsedMaxPassengers,
    };

    try {
      setIsSubmitting(true);
      const response = await api.post('/trips', payload);
      setResponseText(JSON.stringify(response.data, null, 2));
      console.log('Utworzono ofertę:', response.data);
    } catch (err: any) {
      const message = err?.response?.data?.error || err?.response?.data?.message || 'Nie udało się utworzyć oferty.';
      setError(message);
      console.error('Błąd tworzenia oferty:', err?.response?.data || err);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div>
      <h2>Stwórz ofertę</h2>
      <p>
        <Link to="/">Powrót</Link>
      </p>

      {error ? <p style={{ color: 'red' }}>{error}</p> : null}

      <form
        onSubmit={handleSubmit}
        style={{ display: 'flex', flexDirection: 'column', width: '100%', maxWidth: '400px', gap: '10px' }}
      >
        <input type="text" placeholder="Skąd" value={from} onChange={(e) => setFrom(e.target.value)} />
        <input type="text" placeholder="Dokąd" value={to} onChange={(e) => setTo(e.target.value)} />
        <input type="number" step="0.01" placeholder="Cena" value={price} onChange={(e) => setPrice(e.target.value)} />
        <input type="datetime-local" value={date} onChange={(e) => setDate(e.target.value)} />
        <input
          type="number"
          placeholder="Maksymalna liczba pasażerów"
          value={maxPassengers}
          onChange={(e) => setMaxPassengers(e.target.value)}
        />
        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Wysyłanie...' : 'Dodaj ofertę'}
        </button>
      </form>

      {responseText ? (
        <div style={{ marginTop: '16px' }}>
          <h3>Dodano ofertę</h3>
        </div>
      ) : null}
    </div>
  );
}

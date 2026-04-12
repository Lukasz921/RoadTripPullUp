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
      setError('Fill all required fields.');
      return;
    }

    if (!token) {
      setError('You must be logged in to create a trip.');
      console.error('No JWT token in localStorage.');
      return;
    }

    const parsedPrice = Number(price);
    const parsedMaxPassengers = Number(maxPassengers);

    if (Number.isNaN(parsedPrice) || parsedPrice <= 0) {
      setError('Price must be greater than 0.');
      return;
    }

    if (Number.isNaN(parsedMaxPassengers) || parsedMaxPassengers <= 0) {
      setError('Max passengers must be greater than 0.');
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
      console.log('Trip created:', response.data);
    } catch (err: any) {
      const message = err?.response?.data?.error || err?.response?.data?.message || 'Failed to create trip.';
      setError(message);
      console.error('Create trip error:', err?.response?.data || err);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div>
      <h2>Create Trip</h2>
      <p>
        <Link to="/">Back to Home</Link>
      </p>

      {token ? (
        <div>
          <label htmlFor="jwtToken">JWT Token</label>
          <textarea id="jwtToken" value={token} readOnly rows={4} style={{ width: '100%', maxWidth: '600px' }} />
        </div>
      ) : null}

      {error ? <p style={{ color: 'red' }}>{error}</p> : null}

      <form
        onSubmit={handleSubmit}
        style={{ display: 'flex', flexDirection: 'column', width: '100%', maxWidth: '400px', gap: '10px' }}
      >
        <input type="text" placeholder="From" value={from} onChange={(e) => setFrom(e.target.value)} />
        <input type="text" placeholder="To" value={to} onChange={(e) => setTo(e.target.value)} />
        <input
          type="text"
          placeholder="Between points (comma separated)"
          value={betweenPoints}
          onChange={(e) => setBetweenPoints(e.target.value)}
        />
        <input type="number" step="0.01" placeholder="Price" value={price} onChange={(e) => setPrice(e.target.value)} />
        <input type="datetime-local" value={date} onChange={(e) => setDate(e.target.value)} />
        <input
          type="number"
          placeholder="Max passengers"
          value={maxPassengers}
          onChange={(e) => setMaxPassengers(e.target.value)}
        />
        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Sending...' : 'Send create trip form'}
        </button>
      </form>

      {responseText ? (
        <div style={{ marginTop: '16px' }}>
          <h3>Response</h3>
          <textarea value={responseText} readOnly rows={10} style={{ width: '100%', maxWidth: '600px' }} />
        </div>
      ) : null}
    </div>
  );
}

import React, { useState } from 'react';
import { searchTrips } from '../api/trips';
import type { TripSummaryDTO } from '../api/trips';

const TripsPage: React.FC = () => {
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [date, setDate] = useState('');
  const [loading, setLoading] = useState(false);
  const [results, setResults] = useState<TripSummaryDTO[]>([]);
  const [error, setError] = useState<string | null>(null);

  const doSearch = async () => {
    setError(null);
    setLoading(true);
    try {
      const data = await searchTrips({ from: from || undefined, to: to || undefined, date: date || undefined });
      setResults(data);
    } catch (e: any) {
      setError(e?.response?.data?.error || e.message || 'Błąd podczas wyszukiwania');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <h1>Wyszukaj przejazdy</h1>
      <div style={{ display: 'flex', gap: 8, marginBottom: 12 }}>
        <input placeholder="From" value={from} onChange={(e) => setFrom(e.target.value)} />
        <input placeholder="To" value={to} onChange={(e) => setTo(e.target.value)} />
        <input type="date" value={date} onChange={(e) => setDate(e.target.value)} />
        <button onClick={doSearch} disabled={loading}>Szukaj</button>
      </div>

      {loading && <div>Loading...</div>}
      {error && <div style={{ color: 'red' }}>{error}</div>}

      {!loading && !error && results.length === 0 && <div>Brak wyników</div>}

      <ul>
        {results.map((t) => (
          <li key={t.tripId} style={{ marginBottom: 10 }}>
            <strong>{t.route.from} → {t.route.to}</strong>
            <div>Data: {new Date(t.date).toLocaleString()}</div>
            <div>Cena: {t.price} PLN, Miejsca: {t.maxPassengers}</div>
          </li>
        ))}
      </ul>
    </div>
  );
};

export default TripsPage;

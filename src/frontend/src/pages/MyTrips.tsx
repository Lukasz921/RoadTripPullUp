import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { getMyTrips } from '../api/trips';
import type { TripSummaryDTO } from '../api/trips';

const MyTrips: React.FC = () => {
  const [trips, setTrips] = useState<TripSummaryDTO[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getMyTrips()
      .then(setTrips)
      .catch((e: any) => setError(e?.response?.data?.detail || e.message || 'Błąd pobierania ofert'))
      .finally(() => setLoading(false));
  }, []);

  return (
    <div style={{ maxWidth: 600, margin: '40px auto', padding: '0 16px' }}>
      <Link to="/" style={{ fontSize: 14, color: '#555' }}>← Powrót</Link>
      <h2 style={{ marginTop: 16 }}>Moje oferty</h2>

      {loading && <div>Ładowanie...</div>}
      {error && <div style={{ color: 'red' }}>{error}</div>}
      {!loading && !error && trips.length === 0 && <div>Brak ofert. <Link to="/trip/create">Dodaj pierwszą ofertę</Link>.</div>}

      <ul style={{ listStyle: 'none', padding: 0 }}>
        {trips.map((t) => (
          <li key={t.tripId} style={{ marginBottom: 12, padding: 16, border: '1px solid #ddd', borderRadius: 8 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <div>
                <strong>{t.route.from} → {t.route.to}</strong>
                <div style={{ fontSize: 13, color: '#555', marginTop: 4 }}>
                  {new Date(t.date).toLocaleString()} &middot; {t.price} PLN &middot; maks. {t.maxPassengers} pas.
                </div>
              </div>
              <Link to={`/trips/${t.tripId}`} style={{ fontSize: 13 }}>Szczegóły</Link>
            </div>
          </li>
        ))}
      </ul>
    </div>
  );
};

export default MyTrips;

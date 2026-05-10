import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { getMyTrips } from '../api/trips';
import type { TripSummaryDTO } from '../api/trips';

const MyTrips: React.FC = () => {
  const navigate = useNavigate();
  const [trips, setTrips] = useState<TripSummaryDTO[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const isLoggedIn = !!localStorage.getItem('token');

  useEffect(() => {
    if (!isLoggedIn) {
      navigate('/login');
      return;
    }
    getMyTrips()
      .then(setTrips)
      .catch(() => setError('Nie udało się załadować ofert.'))
      .finally(() => setLoading(false));
  }, [isLoggedIn, navigate]);

  return (
    <div style={{ maxWidth: 700, margin: '0 auto', padding: '24px 16px' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 24 }}>
        <button
          onClick={() => navigate('/')}
          style={{ background: 'none', border: 'none', cursor: 'pointer', fontSize: 18 }}
        >←</button>
        <h1 style={{ margin: 0, fontSize: 22 }}>Moje oferty przejazdów</h1>
        <Link to="/trip/create" style={{ marginLeft: 'auto' }}>
          <button style={{
            padding: '8px 16px',
            background: '#0084ff',
            color: '#fff',
            border: 'none',
            borderRadius: 8,
            cursor: 'pointer',
            fontWeight: 600,
          }}>
            + Nowa oferta
          </button>
        </Link>
      </div>

      {loading && <div style={{ color: '#999' }}>Ładowanie...</div>}
      {error && <div style={{ color: 'red' }}>{error}</div>}

      {!loading && !error && trips.length === 0 && (
        <div style={{
          textAlign: 'center',
          padding: '48px 0',
          color: '#666',
          border: '2px dashed #ddd',
          borderRadius: 12,
        }}>
          <div style={{ fontSize: 40, marginBottom: 12 }}>🚗</div>
          <div style={{ fontSize: 16, marginBottom: 8 }}>Nie masz jeszcze żadnych ofert</div>
          <Link to="/trip/create">
            <button style={{
              marginTop: 8,
              padding: '10px 20px',
              background: '#0084ff',
              color: '#fff',
              border: 'none',
              borderRadius: 8,
              cursor: 'pointer',
            }}>
              Dodaj pierwszą ofertę
            </button>
          </Link>
        </div>
      )}

      <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
        {trips.map((trip) => (
          <div
            key={trip.tripId}
            style={{
              background: '#fff',
              border: '1px solid #e0e0e0',
              borderRadius: 12,
              padding: '16px',
              boxShadow: '0 1px 3px rgba(0,0,0,0.06)',
            }}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
              <div>
                <div style={{ fontWeight: 600, fontSize: 16, marginBottom: 4 }}>
                  {trip.route.from} → {trip.route.to}
                </div>
                <div style={{ color: '#555', fontSize: 14, marginBottom: 2 }}>
                  📅 {new Date(trip.date).toLocaleString('pl-PL')}
                </div>
                <div style={{ color: '#555', fontSize: 14 }}>
                  💰 {trip.price} PLN &nbsp;·&nbsp; 👥 maks. {trip.maxPassengers} pasażerów
                </div>
              </div>
              <Link to={`/trips/${trip.tripId}`}>
                <button style={{
                  padding: '6px 14px',
                  background: 'transparent',
                  border: '1px solid #0084ff',
                  color: '#0084ff',
                  borderRadius: 8,
                  cursor: 'pointer',
                  fontSize: 13,
                }}>
                  Szczegóły
                </button>
              </Link>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

export default MyTrips;

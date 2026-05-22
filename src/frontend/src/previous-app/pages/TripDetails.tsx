import React, { useState, useEffect } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { getTripById, requestRide } from '../../api/trips';
import type { TripDetailsDTO } from '../../api/trips';
import { jwtDecode } from 'jwt-decode';

interface DecodedToken {
  nameid?: string;
  unique_name?: string;
  sub?: string;
}

const TripDetails: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [trip, setTrip] = useState<TripDetailsDTO | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [currentUserId, setCurrentUserId] = useState<string | null>(null);

  useEffect(() => {
    const token = localStorage.getItem('token');
    if (token) {
      try {
        const decoded = jwtDecode<DecodedToken>(token);
        setCurrentUserId(decoded.nameid || decoded.sub || decoded.unique_name || null);
      } catch {
        setCurrentUserId(null);
      }
    }
  }, []);

  useEffect(() => {
    if (!id) return;
    getTripById(id)
      .then(setTrip)
      .catch((e: any) => setError(e?.response?.data?.detail || e.message || 'Błąd pobierania danych'))
      .finally(() => setLoading(false));
  }, [id]);

  const handleJoin = async () => {
    if (!id) return;
    try {
      await requestRide(id);
      alert('Wysłano prośbę o dołączenie!');
    } catch (e: any) {
      alert(e?.response?.data?.detail || 'Błąd podczas zgłaszania chęci przejazdu');
    }
  };

  if (loading) return <div style={{ padding: 20 }}>Ładowanie...</div>;
  if (error) return <div style={{ padding: 20, color: 'red' }}>{error}</div>;
  if (!trip) return <div style={{ padding: 20 }}>Nie znaleziono przejazdu.</div>;

  const isDriver = currentUserId && trip.driverId === currentUserId;

  return (
    <div style={{ maxWidth: 600, margin: '40px auto', padding: '0 16px' }}>
      <Link to="/trips" style={{ fontSize: 14, color: '#555' }}>← Powrót do wyników</Link>
      <h2 style={{ marginTop: 16 }}>{trip.route.from} → {trip.route.to}</h2>

      <div style={{ padding: 20, border: '1px solid #ddd', borderRadius: 10, marginTop: 12, display: 'flex', flexDirection: 'column', gap: 10 }}>
        <div><strong>Data:</strong> {new Date(trip.date).toLocaleString()}</div>
        <div><strong>Cena:</strong> {trip.price} PLN</div>
        <div><strong>Wolne miejsca:</strong> {trip.maxPassengers - trip.passengerIds.length} / {trip.maxPassengers}</div>
        <div><strong>Status:</strong> {trip.status}</div>
      </div>

      <div style={{ marginTop: 16, display: 'flex', gap: 10 }}>
        {isDriver ? (
          <span style={{ color: '#888' }}>To Twoja oferta</span>
        ) : (
          <>
            <button
              onClick={handleJoin}
              disabled={!currentUserId}
              style={{ padding: '10px 20px', borderRadius: 8, cursor: 'pointer' }}
            >
              {currentUserId ? 'Dołącz' : 'Zaloguj się, aby dołączyć'}
            </button>
            {currentUserId && (
              <button
                onClick={() => navigate(`/messages/${trip.driverId}`)}
                style={{ padding: '10px 20px', borderRadius: 8, cursor: 'pointer', background: '#f0f0f0', border: '1px solid #ddd' }}
              >
                Napisz do kierowcy
              </button>
            )}
          </>
        )}
      </div>
    </div>
  );
};

export default TripDetails;

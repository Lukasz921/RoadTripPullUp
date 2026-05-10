import React, { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import api from '../api/axiosConfig';
import type { TripSummaryDTO } from '../api/trips';

interface TripDetailsDTO extends TripSummaryDTO {
  status: string;
  passengerIds: string[];
}

const TripDetails: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [trip, setTrip] = useState<TripDetailsDTO | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const isLoggedIn = !!localStorage.getItem('token');

  useEffect(() => {
    if (!id) return;
    api
      .get<TripDetailsDTO>(`/trips/${id}`)
      .then((r) => setTrip(r.data))
      .catch(() => setError('Nie udało się załadować szczegółów przejazdu.'))
      .finally(() => setLoading(false));
  }, [id]);

  if (loading) return <div>Ładowanie...</div>;
  if (error) return <div style={{ color: 'red' }}>{error}</div>;
  if (!trip) return null;

  return (
    <div style={{ maxWidth: 600, margin: '24px auto', padding: '0 16px' }}>
      <button onClick={() => navigate(-1)} style={{ marginBottom: 16 }}>← Wróć</button>

      <h2>Szczegóły przejazdu</h2>
      <p><strong>Trasa:</strong> {trip.route.from} → {trip.route.to}</p>
      <p><strong>Data:</strong> {new Date(trip.date).toLocaleString()}</p>
      <p><strong>Cena:</strong> {trip.price} PLN</p>
      <p><strong>Miejsca:</strong> {trip.maxPassengers}</p>
      <p><strong>Status:</strong> {trip.status}</p>

      <hr />
      <h3>Kierowca</h3>
      <p>ID: {trip.driverId}</p>
      {isLoggedIn ? (
        <Link to={`/chat/${trip.driverId}`}>
          <button style={{ marginTop: 8, padding: '8px 16px', background: '#0084ff', color: '#fff', border: 'none', borderRadius: 6, cursor: 'pointer' }}>
            Napisz wiadomość
          </button>
        </Link>
      ) : (
        <p style={{ color: '#888' }}>
          <Link to="/login">Zaloguj się</Link>, aby napisać do kierowcy.
        </p>
      )}
    </div>
  );
};

export default TripDetails;

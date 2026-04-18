import React, { useState, useEffect } from 'react';
import { searchTrips, requestRide, getTripRequests, acceptRequest } from '../api/trips';
import type { TripSummaryDTO, TripRequestDTO } from '../api/trips';
import { jwtDecode } from 'jwt-decode';

interface DecodedToken {
  nameid?: string;
  unique_name?: string;
  sub?: string;
}

const TripRequests: React.FC<{ tripId: string }> = ({ tripId }) => {
  const [requests, setRequests] = useState<TripRequestDTO[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchRequests = async () => {
    setLoading(true);
    try {
      const data = await getTripRequests(tripId);
      setRequests(data);
    } catch (e: any) {
      setError(e?.response?.data?.detail || e.message || 'Błąd pobierania próśb');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchRequests();
  }, [tripId]);

  const handleAccept = async (requestId: string) => {
    try {
      await acceptRequest(requestId);
      alert('Zaakceptowano pasażera!');
      fetchRequests();
    } catch (e: any) {
      alert(e?.response?.data?.detail || 'Błąd podczas akceptacji');
    }
  };

  if (loading) return <div>Pobieranie próśb...</div>;
  if (error) return <div style={{ color: 'red' }}>{error}</div>;

  return (
    <div style={{ marginLeft: 20, marginTop: 10, padding: 10, borderLeft: '2px solid #ccc' }}>
      <strong>Prośby o dołączenie:</strong>
      {requests.length === 0 && <div>Brak próśb</div>}
      <ul style={{ listStyle: 'none', padding: 0 }}>
        {requests.map(r => (
          <li key={r.id} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 10, marginBottom: 5 }}>
            <span>Pasażer: {r.passengerId.substring(0, 8)}... ({r.status})</span>
            {r.status === 'Pending' && (
              <button onClick={() => handleAccept(r.id)}>Akceptuj</button>
            )}
          </li>
        ))}
      </ul>
    </div>
  );
};

const TripsPage: React.FC = () => {
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [date, setDate] = useState('');
  const [loading, setLoading] = useState(false);
  const [results, setResults] = useState<TripSummaryDTO[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [currentUserId, setCurrentUserId] = useState<string | null>(null);
  const [expandedTripId, setExpandedTripId] = useState<string | null>(null);

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

  const doSearch = async () => {
    setError(null);
    setLoading(true);
    try {
      const data = await searchTrips({ from: from || undefined, to: to || undefined, date: date || undefined });
      setResults(data);
    } catch (e: any) {
      setError(e?.response?.data?.detail || e?.response?.data?.error || e.message || 'Błąd podczas wyszukiwania');
    } finally {
      setLoading(false);
    }
  };

  const handleJoin = async (tripId: string) => {
    try {
      await requestRide(tripId);
      alert('Wysłano prośbę o dołączenie!');
    } catch (e: any) {
      alert(e?.response?.data?.detail || 'Błąd podczas zgłaszania chęci przejazdu');
    }
  };

  return (
    <div>
      <h1>Wyszukaj przejazdy</h1>
      <div style={{ display: 'flex', gap: 8, marginBottom: 12 }}>
        <input placeholder="Skąd" value={from} onChange={(e) => setFrom(e.target.value)} />
        <input placeholder="Dokąd" value={to} onChange={(e) => setTo(e.target.value)} />
        <input type="date" value={date} onChange={(e) => setDate(e.target.value)} />
        <button onClick={doSearch} disabled={loading}>Szukaj</button>
      </div>

      {loading && <div>Ładowanie...</div>}
      {error && <div style={{ color: 'red' }}>{error}</div>}

      {!loading && !error && results.length === 0 && <div>Brak wyników</div>}

      <ul style={{ listStyle: 'none', padding: 0 }}>
        {results.map((t) => {
          const isDriver = currentUserId && t.driverId === currentUserId;
          return (
            <li key={t.tripId} style={{ marginBottom: 20, padding: 15, border: '1px solid #ddd', borderRadius: 8 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                <div>
                  <strong>{t.route.from} → {t.route.to}</strong>
                  <div>Data: {new Date(t.date).toLocaleString()}</div>
                  <div>Cena: {t.price} PLN, Miejsca: {t.maxPassengers}</div>
                </div>
                <div>
                  {isDriver ? (
                    <button onClick={() => setExpandedTripId(expandedTripId === t.tripId ? null : t.tripId)}>
                      {expandedTripId === t.tripId ? 'Ukryj prośby' : 'Zobacz prośby'}
                    </button>
                  ) : (
                    <button onClick={() => handleJoin(t.tripId)} disabled={!currentUserId}>
                      {currentUserId ? 'Dołącz' : 'Zaloguj się, aby dołączyć'}
                    </button>
                  )}
                </div>
              </div>
              {expandedTripId === t.tripId && <TripRequests tripId={t.tripId} />}
            </li>
          );
        })}
      </ul>
    </div>
  );
};

export default TripsPage;

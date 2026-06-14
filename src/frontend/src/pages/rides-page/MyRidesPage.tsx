import { useNavigate } from 'react-router-dom';
import RidesPage from './RidesPage';
import { getMyTrips } from '../../api/trips';

export default function MyRidesPage() {
  const navigate = useNavigate();

  return (
    <RidesPage
      title="My rides"
      fetchTrips={getMyTrips}
      emptyMessage="You haven't published any rides yet."
      headerButton={{ label: 'Add trip', onClick: () => navigate('/add-trip') }}
      cardAction={(trip) => ({
        label: 'Chats',
        onClick: () => navigate(`/trip/${trip.id}/chats`, { state: { showAddToTrip: true } }),
      })}
    />
  );
}

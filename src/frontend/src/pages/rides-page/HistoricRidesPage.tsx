import { useNavigate } from 'react-router-dom';
import RidesPage from '../../components/RidesPage';
import { getTripHistory } from '../../api/trips';

export default function HistoricRidesPage() {
  const navigate = useNavigate();

  return (
    <RidesPage
      title="Ride history"
      fetchTrips={getTripHistory}
      emptyMessage="You have no past rides yet."
    cardAction={(trip) => ({
        label: 'Chats',
        onClick: () => navigate(`/trip/${trip.id}/chats`, { state: { showAddToTrip: true } }),
      })}
    />
  );
}

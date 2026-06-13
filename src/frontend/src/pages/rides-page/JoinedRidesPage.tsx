import { useNavigate } from 'react-router-dom';
import RidesPage from '../../components/RidesPage';
import { getJoinedTrips } from '../../api/trips';

export default function JoinedRidesPage() {
  const navigate = useNavigate();

  return (
    <RidesPage
      title="Joined rides"
      fetchTrips={getJoinedTrips}
      emptyMessage="You haven't joined any rides yet."
      headerButton={{ label: 'Search rides', onClick: () => navigate('/search') }}
    />
  );
}

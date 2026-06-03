import { BrowserRouter, Routes, Route } from 'react-router-dom';
import LoginRegisterPage from './pages/LoginRegisterPage.tsx';
import MainPage from './pages/MainPage';
import ProfilePage from './pages/ProfilePage';
import AddTripPage from './pages/AddTripPage';
import SearchTripsPage from './pages/SearchTripsPage';
import JoinedRidesPage from './pages/JoinedRidesPage';
import MyRidesPage from './pages/MyRidesPage';
import TripDetailsPage from './pages/TripDetailsPage';

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<MainPage />} />
        <Route path="/login" element={<LoginRegisterPage />} />
        <Route path="/register" element={<LoginRegisterPage />} />
        <Route path="/profile" element={<ProfilePage />} />
        <Route path="/add-trip" element={<AddTripPage />} />
        <Route path="/search" element={<SearchTripsPage />} />
        <Route path="/joined-rides" element={<JoinedRidesPage />} />
        <Route path="/my-rides" element={<MyRidesPage />} />
        <Route path="/trip/:id" element={<TripDetailsPage />} />
      </Routes>
    </BrowserRouter>
  );
}

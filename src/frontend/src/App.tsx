import { BrowserRouter, Routes, Route } from 'react-router-dom';
import LoginRegisterPage from './pages/LoginRegisterPage.tsx';
import MainPage from './pages/MainPage';
import ProfilePage from './pages/ProfilePage';
import AddTripPage from './pages/AddTripPage';
import SearchTripsPage from './pages/SearchTripsPage';
import JoinedRidesPage from './pages/JoinedRidesPage';
import MyRidesPage from './pages/MyRidesPage';
import TripDetailsPage from './pages/TripDetailsPage';
import TripChatsListPage from './pages/TripChatsListPage';
import ChatPage from './pages/ChatPage';
import MyConversationsPage from './pages/MyConversationsPage';

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
        <Route path="/trip/:id/chats" element={<TripChatsListPage />} />
        <Route path="/conversation/:id" element={<ChatPage />} />
        <Route path="/my-conversations" element={<MyConversationsPage />} />
      </Routes>
    </BrowserRouter>
  );
}

import { BrowserRouter, Routes, Route } from 'react-router-dom';
import LoginRegisterPage from './pages/LoginRegisterPage.tsx';
import MainPage from './pages/MainPage';
import ProfilePage from './pages/profile/ProfilePage.tsx';
import UserProfilePage from './pages/profile/UserProfilePage.tsx';
import AddTripPage from './pages/add-trip/AddTripPage.tsx';
import SearchTripsPage from './pages/SearchTripsPage';
import JoinedRidesPage from './pages/rides-page/JoinedRidesPage.tsx';
import MyRidesPage from './pages/rides-page/MyRidesPage.tsx';
import TripDetailsPage from './pages/TripDetailsPage';
import TripChatsListPage from './pages/TripChatsListPage';
import ChatPage from './pages/chat/ChatPage.tsx';
import MyConversationsPage from './pages/MyConversationsPage';
import HistoricRidesPage from './pages/rides-page/HistoricRidesPage.tsx'
import AdminPage from './pages/admin/AdminPage.tsx';

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<MainPage />} />
        <Route path="/login" element={<LoginRegisterPage />} />
        <Route path="/register" element={<LoginRegisterPage />} />
        <Route path="/profile" element={<ProfilePage />} />
        <Route path="/user/:id" element={<UserProfilePage />} />
        <Route path="/add-trip" element={<AddTripPage />} />
        <Route path="/search" element={<SearchTripsPage />} />
        <Route path="/joined-rides" element={<JoinedRidesPage />} />
        <Route path="/my-rides" element={<MyRidesPage />} />
        <Route path="/historic-rides" element={<HistoricRidesPage />} />
        <Route path="/trip/:id" element={<TripDetailsPage />} />
        <Route path="/trip/:id/chats" element={<TripChatsListPage />} />
        <Route path="/conversation/:id" element={<ChatPage />} />
        <Route path="/my-conversations" element={<MyConversationsPage />} />
        <Route path="/admin" element={<AdminPage />} />
      </Routes>
    </BrowserRouter>
  );
}

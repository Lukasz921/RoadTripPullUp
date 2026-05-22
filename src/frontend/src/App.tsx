import { BrowserRouter, Routes, Route } from 'react-router-dom';
import LoginRegisterPage from './pages/LoginRegisterPage';
import MainPage from './pages/MainPage';
import TripForm from './previous-app/pages/TripForm';
import Trips from './previous-app/pages/Trips';
import TripDetails from './previous-app/pages/TripDetails';
import Chat from './previous-app/pages/Chat';
import Messages from './previous-app/pages/Messages';
import MyTrips from './previous-app/pages/MyTrips';

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<LoginRegisterPage />} />
        <Route path="/login" element={<LoginRegisterPage />} />
        <Route path="/register" element={<LoginRegisterPage />} />
        <Route path="/home" element={<MainPage />} />
        <Route path="/trip/create" element={<TripForm />} />
        <Route path="/trips" element={<Trips />} />
        <Route path="/trips/:id" element={<TripDetails />} />
        <Route path="/chat/:receiverId" element={<Chat />} />
        <Route path="/messages" element={<Messages />} />
        <Route path="/messages/:partnerId" element={<Messages />} />
        <Route path="/my-trips" element={<MyTrips />} />
      </Routes>
    </BrowserRouter>
  );
}

import { BrowserRouter, Routes, Route } from 'react-router-dom';
import LoginRegisterPage from './pages/LoginRegisterPage.tsx';
import MainPage from './pages/MainPage';
import ProfilePage from './pages/ProfilePage';
import AddTripPage from './pages/AddTripPage';

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<MainPage />} />
        <Route path="/login" element={<LoginRegisterPage />} />
        <Route path="/register" element={<LoginRegisterPage />} />
        <Route path="/profile" element={<ProfilePage />} />
        <Route path="/add-trip" element={<AddTripPage />} />
      </Routes>
    </BrowserRouter>
  );
}

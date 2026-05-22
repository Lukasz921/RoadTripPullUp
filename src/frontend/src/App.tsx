import { BrowserRouter, Routes, Route } from 'react-router-dom';
import LoginRegisterPage from './pages/LoginRegisterPage.tsx';
import MainPage from './pages/MainPage';

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<MainPage />} />
        <Route path="/login" element={<LoginRegisterPage />} />
        <Route path="/register" element={<LoginRegisterPage />} />
      </Routes>
    </BrowserRouter>
  );
}

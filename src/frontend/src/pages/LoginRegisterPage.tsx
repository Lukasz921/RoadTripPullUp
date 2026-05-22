import { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import api from '../api/axiosConfig';
import Navbar from '../components/layout/Navbar';
import Footer from '../components/layout/Footer';
import TextInput from '../components/ui/TextInput';

type Mode = 'login' | 'register';

interface FormState {
  email: string;
  password: string;
  name: string;
  surname: string;
}

const INITIAL_FORM: FormState = {
  email: '',
  password: '',
  name: '',
  surname: '',
};

export default function LoginRegisterPage() {
  const { pathname } = useLocation();
  const [mode, setMode] = useState<Mode>(pathname === '/register' ? 'register' : 'login');
  const [form, setForm] = useState<FormState>(INITIAL_FORM);
  const [error, setError] = useState('');
  const navigate = useNavigate();

  const isRegister = mode === 'register';

  function updateField(field: keyof FormState, value: string) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  function switchMode(next: Mode) {
    setMode(next);
    setError('');
    setForm(INITIAL_FORM);
  }

  async function submitForm(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError('');

    if (isRegister) {
      try {
        await api.post('/auth/register', {
          name: form.name,
          surname: form.surname,
          email: form.email,
          password: form.password,
        });
        navigate('/');
      } catch (err: any) {
        console.error('Register error:', err);
        setError(err.response?.data?.detail ?? 'Registration failed. Please try again.');
      }
      return;
    }

    try {
      const response = await api.post('/auth/login', {
        email: form.email,
        password: form.password,
      });
      localStorage.setItem('token', response.data.token);
      navigate('/');
    } catch (err: any) {
      console.error('Login error:', err);
      setError(err.response?.data?.detail ?? 'Login failed. Please try again.');
    }
  }

  return (
    <main className="flex min-h-screen flex-col bg-[#f3faee] text-[#12351f]">
      <Navbar />

      <section className="flex flex-1 items-center justify-center px-6 py-28">
        <div className="w-full max-w-md rounded-2xl bg-white p-6 shadow-sm">
          <h1 className="text-3xl font-bold">{isRegister ? 'Create account' : 'Login'}</h1>
          <p className="mt-2 text-sm text-[#5d7056]">
            {isRegister ? 'Register to publish and join trips.' : 'Login to your PullUp account.'}
          </p>

          {error && (
            <p className="mt-4 rounded-lg bg-red-50 px-4 py-2 text-sm text-red-600">{error}</p>
          )}

          <form onSubmit={submitForm} className="mt-6 space-y-4">
            {isRegister && (
              <div className="grid gap-4 sm:grid-cols-2">
                <TextInput
                  label="Name"
                  value={form.name}
                  onChange={(v) => updateField('name', v)}
                  placeholder="Name"
                />
                <TextInput
                  label="Surname"
                  value={form.surname}
                  onChange={(v) => updateField('surname', v)}
                  placeholder="Surname"
                />
              </div>
            )}

            <TextInput
              label="Email"
              type="email"
              value={form.email}
              onChange={(v) => updateField('email', v)}
              placeholder="you@example.com"
            />

            <TextInput
              label="Password"
              type="password"
              value={form.password}
              onChange={(v) => updateField('password', v)}
              placeholder="••••••••"
            />

            <button
              type="submit"
              className="h-12 w-full rounded-xl bg-[#12351f] font-semibold text-white hover:bg-[#1d4a2d]"
            >
              {isRegister ? 'Register' : 'Login'}
            </button>
          </form>

          <div className="mt-6 border-t border-[#d7e8c8] pt-5 text-center text-sm text-[#5d7056]">
            {isRegister ? 'Already have an account?' : "Don't have an account?"}{' '}
            <button
              onClick={() => switchMode(isRegister ? 'login' : 'register')}
              className="font-bold text-[#12351f] hover:underline"
            >
              {isRegister ? 'Login' : 'Register'}
            </button>
          </div>
        </div>
      </section>

      <Footer />
    </main>
  );
}

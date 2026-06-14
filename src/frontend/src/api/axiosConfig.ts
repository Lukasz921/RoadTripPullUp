import axios, { type AxiosInstance } from 'axios';

function createApiInstance(baseURL: string): AxiosInstance {
  const instance = axios.create({ baseURL });

  instance.interceptors.request.use((config) => {
    const token = localStorage.getItem('token');
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  });

  instance.interceptors.response.use(
    (response) => response,
    (error) => {
      if (error.response?.status === 401) {
        console.error('Unauthorized:', error.response.config.url);
        window.location.href = '/login';
      }
      return Promise.reject(error);
    }
  );

  return instance;
}

export const authApi = createApiInstance(import.meta.env.VITE_AUTH_SERVICE_URL);
export const tripApi = createApiInstance(import.meta.env.VITE_TRIP_SERVICE_URL);
export const messageApi = createApiInstance(import.meta.env.VITE_MESSAGE_SERVICE_URL);

// default export kept for backwards compatibility
export default authApi;

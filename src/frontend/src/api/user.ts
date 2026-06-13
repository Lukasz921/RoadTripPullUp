import { authApi } from './axiosConfig';

export type Sex = 'MALE' | 'FEMALE' | 'OTHER';

export interface RegisterDTO {
  name: string;
  surname: string;
  email: string;
  password: string;
  phoneNumber: string;
  dateOfBirth: string;
  sex: Sex;
}

export interface LoginDTO {
  email: string;
  password: string;
}

export interface LoginResponseDTO {
  token: string;
}

export interface CurrentUser {
  id: string;
  name: string;
  surname: string;
  email: string;
  phoneNumber?: string;
  dateOfBirth: string;
  sex: string;
}

export const register = async (dto: RegisterDTO): Promise<void> => {
  await authApi.post('/auth/register', dto);
};

export const login = async (dto: LoginDTO): Promise<LoginResponseDTO> => {
  const response = await authApi.post<LoginResponseDTO>('/auth/login', dto);
  return response.data;
};

export const getCurrentUser = async (): Promise<CurrentUser> => {
  const response = await authApi.get<CurrentUser>('/users/me');
  return response.data;
};

import { DI } from 'aurelia';
import { http, ApiError } from './http';

export interface UserInfo {
  userName: string;
  displayName: string;
  role: string;
}

/** Hält den angemeldeten Benutzer. Wird als Singleton im DI-Container registriert. */
export class AuthService {
  public user: UserInfo | null = null;

  async restore(): Promise<void> {
    try {
      this.user = await http.get<UserInfo>('/api/auth/me');
    } catch (e) {
      if (!(e instanceof ApiError) || e.status !== 401) throw e;
      this.user = null;
    }
  }

  getUsers(): Promise<UserInfo[]> {
    return http.get<UserInfo[]>('/api/auth/users');
  }

  async login(userName: string): Promise<void> {
    this.user = await http.post<UserInfo>('/api/auth/login', { userName });
  }

  async logout(): Promise<void> {
    await http.post('/api/auth/logout');
    this.user = null;
  }
}

export const IAuthService = DI.createInterface<AuthService>('IAuthService', x => x.singleton(AuthService));

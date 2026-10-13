import { resolve } from 'aurelia';
import { IRouter } from '@aurelia/router';
import { IAuthService, type UserInfo } from '../../services/auth';

export class LoginPage {
  private readonly auth = resolve(IAuthService);
  private readonly router = resolve(IRouter);

  users: UserInfo[] = [];

  async loading(): Promise<void> {
    this.users = await this.auth.getUsers();
  }

  async login(user: UserInfo): Promise<void> {
    await this.auth.login(user.userName);
    await this.router.load('customers');
  }
}

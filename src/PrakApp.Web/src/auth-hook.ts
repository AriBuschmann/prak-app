import { lifecycleHooks, resolve } from 'aurelia';
import type { IRouteViewModel, Params, RouteNode } from '@aurelia/router';
import { IAuthService } from './services/auth';

/**
 * Läuft vor jedem Seitenwechsel. Nicht angemeldete Benutzer werden zur Anmeldung geschickt,
 * außer die Route ist mit `data: { public: true }` markiert.
 */
@lifecycleHooks()
export class AuthHook {
  private readonly auth = resolve(IAuthService);
  private restored = false;

  async canLoad(_viewModel: IRouteViewModel, _params: Params, next: RouteNode): Promise<boolean | string> {
    if (next.data?.public) {
      return true;
    }
    if (!this.restored) {
      await this.auth.restore();
      this.restored = true;
    }
    return this.auth.user ? true : 'login';
  }
}

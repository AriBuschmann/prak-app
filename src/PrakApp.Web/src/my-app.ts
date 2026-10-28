import { resolve } from 'aurelia';
import { IRouter } from '@aurelia/router';
import { IAuthService } from './services/auth';
import { setUnauthorizedHandler } from './services/http';
import { LoginPage } from './pages/login/login-page';
import { CustomerList } from './pages/customers/customer-list';
import { CustomerDetail } from './pages/customers/customer-detail';
import { ContractDetail } from './pages/contracts/contract-detail';

export class MyApp {
  static routes = [
    { path: '', redirectTo: 'customers' },
    { path: 'login', component: LoginPage, title: 'Anmeldung', data: { public: true } },
    { path: 'customers', component: CustomerList, title: 'Kunden' },
    { path: 'customers/:id', component: CustomerDetail, title: 'Kunde' },
    { path: 'contracts/:id', component: ContractDetail, title: 'Vertrag' },
  ];

  readonly auth = resolve(IAuthService);
  private readonly router = resolve(IRouter);

  language = localStorage.getItem('language') ?? 'de';
  errorMessage = '';

  constructor() {
    // Sitzung abgelaufen (Server antwortet 401): abmelden und zur Anmeldung. Ohne angemeldeten Benutzer schickt auch
    // der AuthHook jeden weiteren Seitenwechsel dorthin. Nur beim ersten 401, falls mehrere Anfragen gleichzeitig scheitern
    setUnauthorizedHandler(() => {
      if (!this.auth.user) return;
      this.auth.user = null;
      void this.router.load('login');
    });

    // Fehler, die nirgends abgefangen werden (z. B. HTTP 500), oben als Hinweis anzeigen
    window.addEventListener('unhandledrejection', event => {
      const reason = event.reason as { status?: number; message?: string } | undefined;
      // 401 ist kein unerwarteter Fehler, sondern eine abgelaufene Sitzung (siehe oben)
      if (reason?.status === 401) {
        event.preventDefault();
        return;
      }
      this.errorMessage = reason?.status ? `${reason.status}: ${reason.message ?? ''}` : String(reason?.message ?? reason);
    });
  }

  dismissError(): void {
    this.errorMessage = '';
  }

  changeLanguage(): void {
    localStorage.setItem('language', this.language);
    // Neu laden, damit auch Grid-Spaltenüberschriften neu übersetzt werden
    location.reload();
  }

  async logout(): Promise<void> {
    await this.auth.logout();
    await this.router.load('login');
  }
}

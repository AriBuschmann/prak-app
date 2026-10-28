import { resolve } from 'aurelia';
import { IRouter } from '@aurelia/router';
import { I18N } from '@aurelia/i18n';
import i18next from 'i18next';
import { IAuthService } from './services/auth';
import { setUnauthorizedHandler } from './services/http';
import { LoginPage } from './pages/login/login-page';
import { CustomerList } from './pages/customers/customer-list';
import { CustomerDetail } from './pages/customers/customer-detail';
import { ContractDetail } from './pages/contracts/contract-detail';

export class MyApp {
  // Tab-Titel als Funktion: Sie wird bei jedem Seitenwechsel aufgerufen und übersetzt den Titel in die gewählte Sprache.
  // i18next direkt, weil "static" zur Klasse gehört und resolve(I18N) dort noch nicht geht (dieselbe Instanz wie I18N)
  static routes = [
    { path: '', redirectTo: 'customers' },
    { path: 'login', component: LoginPage, title: () => i18next.t('pageTitle.login'), data: { public: true } },
    { path: 'customers', component: CustomerList, title: () => i18next.t('pageTitle.customers') },
    { path: 'customers/:id', component: CustomerDetail, title: () => i18next.t('pageTitle.customer') },
    { path: 'contracts/:id', component: ContractDetail, title: () => i18next.t('pageTitle.contract') },
  ];

  readonly auth = resolve(IAuthService);
  private readonly router = resolve(IRouter);
  private readonly i18n = resolve(I18N);

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
      // Für bekannte Statuscodes (404, 500, ...) den übersetzten Text aus "httpStatus" nehmen, sonst den Text vom Server
      const message = reason?.status
        ? this.i18n.tr(`httpStatus.${reason.status}`, { defaultValue: reason.message ?? '' })
        : '';
      this.errorMessage = reason?.status ? `${reason.status}: ${message}` : String(reason?.message ?? reason);
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

import Aurelia from 'aurelia';
import { RouterConfiguration } from '@aurelia/router';
import { I18nConfiguration } from '@aurelia/i18n';

import './jqx';
import './styles.css';
import de from './locales/de.json';
import en from './locales/en.json';

import { MyApp } from './my-app';
import { AuthHook } from './auth-hook';
import { JqxGrid } from './components/jqx-grid';
import { ModalDialog } from './components/modal-dialog';
import { CustomerForm } from './pages/customers/customer-form';

const language = localStorage.getItem('language') ?? 'de';
document.documentElement.lang = language;

Aurelia.register(
  RouterConfiguration,
  I18nConfiguration.customize(options => {
    options.initOptions = {
      lng: language,
      fallbackLng: 'de',
      resources: {
        de: { translation: de },
        en: { translation: en },
      },
    };
  }),
  // Global verfügbare Komponenten und Hooks
  AuthHook,
  JqxGrid,
  ModalDialog,
  CustomerForm,
)
  .app(MyApp)
  .start();

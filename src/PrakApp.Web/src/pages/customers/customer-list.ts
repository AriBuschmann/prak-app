import { resolve } from 'aurelia';
import { IRouter } from '@aurelia/router';
import { I18N } from '@aurelia/i18n';
import { http, ApiError } from '../../services/http';
import { moneyCell } from '../../services/format';
import type { ContractSearchItem, Customer, CustomerDuplicate, CustomerInput, CustomerListItem } from '../../services/models';
import type { GridColumn } from '../../components/jqx-grid';

const emptyCustomer = (): CustomerInput => ({
  firstName: '',
  lastName: '',
  email: null,
  phone: null,
  street: null,
  postalCode: null,
  city: null,
});

// Kurzer Text aus Vorname, Nachname und PLZ, z. B. "erika|albers|24103". Damit merkt sich die Seite, für welche
// Eingaben sie schon vor doppelten Kunden gewarnt hat. Ohne Vor- oder Nachnamen null (dann wird nicht geprüft)
const duplicateKey = (c: CustomerInput): string | null =>
  c.firstName?.trim() && c.lastName?.trim()
    ? [c.firstName, c.lastName, c.postalCode ?? ''].map(v => v.trim().toLowerCase()).join('|')
    : null;

export class CustomerList {
  private readonly router = resolve(IRouter);
  private readonly i18n = resolve(I18N);

  customers: CustomerListItem[] = [];
  // Verträge, deren Nummer den Suchtext enthält (z. B. aus dem Verwendungszweck einer Überweisung)
  contractHits: ContractSearchItem[] = [];
  search = '';

  createOpen = false;
  newCustomer = emptyCustomer();
  errors: Record<string, string[]> = {};
  saving = false;
  // Mögliche doppelte Kunden zu den Eingaben im Dialog (gleicher Name und gleiche PLZ, Handbuch 3.1)
  duplicates: CustomerDuplicate[] = [];
  // Für welche Eingaben die Warnung schon angezeigt wurde (siehe duplicateKey)
  private warnedFor: string | null = null;

  // Warnung und Knopf "Trotzdem anlegen" nur, solange die Eingaben noch dieselben sind wie bei der Prüfung.
  // Ändert man Name oder PLZ, verschwindet die Warnung, und beim nächsten Speichern wird neu geprüft
  get duplicateWarningActive(): boolean {
    return this.duplicates.length > 0 && this.warnedFor === duplicateKey(this.newCustomer);
  }

  readonly columns: GridColumn[] = [
    { text: this.i18n.tr('customer.number'), datafield: 'customerNumber', width: 120 },
    { text: this.i18n.tr('customer.lastName'), datafield: 'lastName' },
    { text: this.i18n.tr('customer.firstName'), datafield: 'firstName' },
    { text: this.i18n.tr('customer.city'), datafield: 'city' },
    { text: this.i18n.tr('customer.contractCount'), datafield: 'contractCount', type: 'number', width: 100, cellsalign: 'right' },
    { text: this.i18n.tr('customer.totalPaid'), datafield: 'totalPaid', type: 'number', width: 140, cellsrenderer: moneyCell },
  ];

  async loading(): Promise<void> {
    await this.load();
  }

  async load(): Promise<void> {
    const q = this.search.trim();
    // Kunden und passende Verträge gleichzeitig laden. Ohne Suchtext gibt es keine Vertragstreffer (leere Liste)
    [this.customers, this.contractHits] = await Promise.all([
      http.get<CustomerListItem[]>(`/api/customers${q ? `?q=${encodeURIComponent(q)}` : ''}`),
      q ? http.get<ContractSearchItem[]>(`/api/contracts?q=${encodeURIComponent(q)}`) : Promise.resolve([]),
    ]);
  }

  open(row: CustomerListItem): void {
    void this.router.load(`customers/${row.id}`);
  }

  showCreate(): void {
    this.newCustomer = emptyCustomer();
    this.errors = {};
    this.duplicates = [];
    this.warnedFor = null;
    this.createOpen = true;
  }

  async create(): Promise<void> {
    // Läuft schon eine Anfrage (z. B. zweiter Klick eines Doppelklicks), nichts tun: sonst entsteht der Kunde doppelt
    if (this.saving) return;
    this.saving = true;
    try {
      // Vor dem Anlegen prüfen, ob es den Kunden schon gibt. Wurde für genau diese Eingaben schon gewarnt,
      // ist dieser Klick "Trotzdem anlegen" und es wird ohne neue Prüfung gespeichert
      const key = duplicateKey(this.newCustomer);
      if (key !== null && key !== this.warnedFor) {
        const { firstName, lastName, postalCode } = this.newCustomer;
        const query = new URLSearchParams({ firstName: firstName ?? '', lastName: lastName ?? '', postalCode: postalCode ?? '' });
        this.duplicates = await http.get<CustomerDuplicate[]>(`/api/customers/duplicates?${query}`);
        if (this.duplicates.length) {
          this.warnedFor = key;
          return;
        }
      }
      const created = await http.post<Customer>('/api/customers', this.newCustomer);
      this.createOpen = false;
      await this.router.load(`customers/${created.id}`);
    } catch (e) {
      if (e instanceof ApiError && e.status === 400) {
        this.errors = e.fieldErrors;
        return;
      }
      throw e;
    } finally {
      this.saving = false;
    }
  }
}

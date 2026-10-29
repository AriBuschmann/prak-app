import { resolve } from 'aurelia';
import { IRouter, type Params } from '@aurelia/router';
import { I18N } from '@aurelia/i18n';
import { http, ApiError } from '../../services/http';
// Neu in dieser Zeile: toIsoDate aus format.ts. Die Funktion macht aus einem Date einen Text "JJJJ-MM-TT"
// (in Ortszeit) und wird unten in emptyContract() für das vorgeschlagene Beginndatum verwendet.
import { dateCell, formatDate, moneyCell, toIsoDate } from '../../services/format';
import type { ContractDetail, ContractListItem, ContractType, Customer } from '../../services/models';
import type { GridColumn } from '../../components/jqx-grid';

interface NewContract {
  contractTypeId: number | null;
  startDate: string;
  endDate: string;
  monthlyFee: number | string;
}

export class CustomerDetail {
  private readonly router = resolve(IRouter);
  private readonly i18n = resolve(I18N);

  customer: Customer | null = null;
  contracts: ContractListItem[] = [];
  contractTypes: ContractType[] = [];

  editOpen = false;
  form: Customer | null = null;
  errors: Record<string, string[]> = {};

  // Zustand des Dialogs "Kunde löschen"
  deleteOpen = false;
  deleteError = '';
  deleting = false;

  contractOpen = false;
  newContract: NewContract = this.emptyContract();
  contractErrors: Record<string, string[]> = {};
  savingContract = false;

  readonly formatDate = formatDate;

  readonly columns: GridColumn[] = [
    { text: this.i18n.tr('contract.number'), datafield: 'contractNumber', width: 130 },
    { text: this.i18n.tr('contract.type'), datafield: 'typeName' },
    { text: this.i18n.tr('contract.startDate'), datafield: 'startDate', type: 'date', width: 110, cellsrenderer: dateCell },
    { text: this.i18n.tr('contract.endDate'), datafield: 'endDate', type: 'date', width: 110, cellsrenderer: dateCell },
    { text: this.i18n.tr('contract.monthlyFee'), datafield: 'monthlyFee', type: 'number', width: 120, cellsrenderer: moneyCell },
    {
      text: this.i18n.tr('contract.status'), datafield: 'status', width: 110,
      cellsrenderer: (_r, _c, value) => `<div class="cell">${this.i18n.tr(`contract.statusValues.${value}`)}</div>`,
    },
    {
      // Zahlungserinnerung fällig (Rückstand über zwei Monatsbeiträge), sonst leer.
      // Das Grid kann den Wert als Text "true" liefern, darum beide Fälle prüfen
      text: this.i18n.tr('contract.reminder'), datafield: 'reminderDue', width: 160,
      cellsrenderer: (_r, _c, value) =>
        value === true || value === 'true'
          ? `<div class="cell reminder-due">${this.i18n.tr('contract.reminderDue')}</div>`
          : '<div class="cell"></div>',
    },
  ];

  async loading(params: Params): Promise<void> {
    const id = Number(params.id);
    [this.customer, this.contracts, this.contractTypes] = await Promise.all([
      http.get<Customer>(`/api/customers/${id}`),
      http.get<ContractListItem[]>(`/api/customers/${id}/contracts`),
      http.get<ContractType[]>('/api/contract-types'),
    ]);
  }

  edit(): void {
    // Ohne geladenen Kunden gibt es nichts zu bearbeiten (gleiche Prüfung wie am Anfang von save())
    if (!this.customer) return;
    // Kopie statt Verweis: Sonst ändert das Formular direkt den angezeigten Kunden, auch wenn man abbricht
    // Vorher stand hier this.form = this.customer: Beide Variablen zeigten auf dasselbe Objekt, jede Eingabe im
    // Formular war sofort in der Überschrift und den Details zu sehen und blieb nach "Abbrechen" stehen.
    // { ...this.customer } legt ein neues Objekt an und kopiert alle Felder hinein.
    // Der angezeigte Kunde ändert sich jetzt erst in save(), wenn der Server die gespeicherten Daten zurückgibt.
    this.form = { ...this.customer };
    this.errors = {};
    this.editOpen = true;
  }

  async save(): Promise<void> {
    if (!this.customer || !this.form) return;
    try {
      this.customer = await http.put<Customer>(`/api/customers/${this.customer.id}`, this.form);
      this.editOpen = false;
    } catch (e) {
      if (e instanceof ApiError && e.status === 400) {
        this.errors = e.fieldErrors;
        return;
      }
      throw e;
    }
  }

  // Hinweis am ausgegrauten Knopf "Löschen". Als Getter, weil "| t" im HTML nicht in einer Bedingung stehen darf (AUR0167)
  get deleteTooltip(): string {
    return this.contracts.length ? this.i18n.tr('customers.deleteNotPossible') : '';
  }

  showDelete(): void {
    this.deleteError = '';
    this.deleteOpen = true;
  }

  // Kunde löschen und zurück zur Kundenliste. Lehnt der Server ab (409, inzwischen gibt es einen Vertrag),
  // steht der Grund im Dialog
  async deleteCustomer(): Promise<void> {
    // Zweiten Klick eines Doppelklicks ignorieren
    if (!this.customer || this.deleting) return;
    this.deleting = true;
    try {
      await http.delete(`/api/customers/${this.customer.id}`);
      this.deleteOpen = false;
      await this.router.load('customers');
    } catch (e) {
      if (e instanceof ApiError && e.status === 409) {
        this.deleteError = e.message;
        return;
      }
      throw e;
    } finally {
      this.deleting = false;
    }
  }

  openContract(row: ContractListItem): void {
    void this.router.load(`contracts/${row.id}`);
  }

  showNewContract(): void {
    this.newContract = this.emptyContract();
    this.contractErrors = {};
    this.contractOpen = true;
  }

  async createContract(): Promise<void> {
    // Läuft schon eine Anfrage (z. B. zweiter Klick eines Doppelklicks), nichts tun: sonst wird der Vertrag zweimal geschickt
    if (!this.customer || this.savingContract) return;
    this.savingContract = true;
    try {
      const created = await http.post<ContractDetail>('/api/contracts', {
        customerId: this.customer.id,
        contractTypeId: this.newContract.contractTypeId ?? 0,
        startDate: this.newContract.startDate || null,
        endDate: this.newContract.endDate || null,
        monthlyFee: Number(this.newContract.monthlyFee) || 0,
      });
      this.contractOpen = false;
      await this.router.load(`contracts/${created.contract.id}`);
    } catch (e) {
      if (e instanceof ApiError && e.status === 400) {
        this.contractErrors = e.fieldErrors;
        return;
      }
      throw e;
    } finally {
      this.savingContract = false;
    }
  }

  private emptyContract(): NewContract {
    // Geändert: Hier wurde das heutige Datum früher in 7 Zeilen selbst aus Jahr, Monat und Tag
    // zusammengebaut. Genau diese Logik steckt jetzt in toIsoDate() in format.ts und wird auch beim Erfassen
    // von Zahlungen benutzt, so steht sie nur noch an einer Stelle. Das Verhalten ist gleich geblieben.
    // So funktioniert die Zeile: new Date() ist "jetzt", toIsoDate() macht daraus z. B. "2026-10-22".
    // Diesen Text erwartet das Datumsfeld <input type="date"> im Dialog "Neuer Vertrag" als Vorbelegung.
    // Die übrigen Felder starten leer: kein Tarif gewählt (null), kein Vertragsende, kein Monatsbeitrag.
    return { contractTypeId: null, startDate: toIsoDate(new Date()), endDate: '', monthlyFee: '' };
  }
}

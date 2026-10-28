import { resolve } from 'aurelia';
import type { Params } from '@aurelia/router';
import { I18N } from '@aurelia/i18n';
import { $, JQX_THEME } from '../../jqx';
import { http, ApiError } from '../../services/http';
// Neu in dieser Zeile: toIsoDate, die Hilfsfunktion aus format.ts, die ein Date in "JJJJ-MM-TT" umwandelt
import { dateCell, formatDate, formatMoney, moneyCell, toIsoDate } from '../../services/format';
import type { BalanceInfo, CancellationPreview, ContractRecord, Payment } from '../../services/models';
import type { GridColumn } from '../../components/jqx-grid';

export class ContractDetail {
  private readonly i18n = resolve(I18N);

  contract: ContractRecord | null = null;
  balance: BalanceInfo | null = null;
  payments: Payment[] = [];

  // Neu (Schritt 4): Zustand des Kündigungsdialogs
  cancelOpen = false;
  cancelReason = '';
  cancelErrors: Record<string, string[]> = {};
  cancelPreview: CancellationPreview | null = null;

  paymentOpen = false;
  payment = { amount: '' as number | string, reference: '' };
  paymentErrors: Record<string, string[]> = {};
  savingPayment = false;
  paymentDateEl!: HTMLElement;

  readonly formatDate = formatDate;
  readonly formatMoney = formatMoney;

  readonly columns: GridColumn[] = [
    { text: this.i18n.tr('payment.date'), datafield: 'paymentDate', type: 'date', width: 120, cellsrenderer: dateCell },
    { text: this.i18n.tr('payment.amount'), datafield: 'amount', type: 'number', width: 120, cellsrenderer: moneyCell },
    { text: this.i18n.tr('payment.reference'), datafield: 'reference' },
    { text: this.i18n.tr('payment.recordedBy'), datafield: 'recordedBy', width: 130 },
  ];

  async loading(params: Params): Promise<void> {
    await this.load(Number(params.id));
  }

  attached(): void {
    $(this.paymentDateEl).jqxDateTimeInput({
      theme: JQX_THEME,
      width: '100%',
      height: 32,
      formatString: 'dd.MM.yyyy',
    });
  }

  detaching(): void {
    $(this.paymentDateEl).jqxDateTimeInput('destroy');
  }

  showPayment(): void {
    if (!this.contract) return;
    this.payment = { amount: this.contract.monthlyFee, reference: '' };
    this.paymentErrors = {};
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    $(this.paymentDateEl).jqxDateTimeInput('setDate', today);
    this.paymentOpen = true;
  }

  async savePayment(): Promise<void> {
    // Läuft schon eine Anfrage (z. B. zweiter Klick eines Doppelklicks), nichts tun: sonst entsteht die Zahlung doppelt
    if (!this.contract || this.savingPayment) return;
    // Das Datum aus dem jQWidgets-Datumsfeld holen: ein Date-Objekt oder null, wenn das Feld leer ist
    const paymentDate = $(this.paymentDateEl).jqxDateTimeInput('getDate') as Date | null;
    this.savingPayment = true;
    try {
      await http.post(`/api/contracts/${this.contract.id}/payments`, {
        amount: Number(this.payment.amount) || 0,
        // Ist ein Datum da, als Text "JJJJ-MM-TT" schicken (ohne Umrechnung nach UTC), sonst null.
        // Leeres Feld bleibt null, damit der Server "Zahlungsdatum ist erforderlich." meldet
        paymentDate: paymentDate ? toIsoDate(paymentDate) : null,
        reference: this.payment.reference,
      });
      this.paymentOpen = false;
      await this.load(this.contract.id);
    } catch (e) {
      if (e instanceof ApiError && e.status === 400) {
        this.paymentErrors = e.fieldErrors;
        return;
      }
      throw e;
    } finally {
      this.savingPayment = false;
    }
  }

  // Neu (Schritt 4): Dialog öffnen. Das voraussichtliche Vertragsende berechnet der Server
  // (dieselben Regeln wie beim Kündigen, siehe docs/features/kuendigung.md)
  async cancelContract(): Promise<void> {
    if (!this.contract) return;
    this.cancelPreview = await http.get<CancellationPreview>(`/api/contracts/${this.contract.id}/cancellation-preview`);
    this.cancelReason = '';
    this.cancelErrors = {};
    this.cancelOpen = true;
  }

  // Neu (Schritt 4): Kündigung speichern. Bei falscher Eingabe (400) die Meldungen am Feld zeigen,
  // sonst Dialog schließen und die Seite neu laden (Status, Kündigungsdatum, Grund und Vertragsende)
  async saveCancellation(): Promise<void> {
    if (!this.contract) return;
    try {
      await http.post(`/api/contracts/${this.contract.id}/cancel`, { reason: this.cancelReason });
      this.cancelOpen = false;
      await this.load(this.contract.id);
    } catch (e) {
      if (e instanceof ApiError && e.status === 400) {
        this.cancelErrors = e.fieldErrors;
        return;
      }
      throw e;
    }
  }

  private async load(id: number): Promise<void> {
    const [detail, payments] = await Promise.all([
      http.get<{ contract: ContractRecord; balance: BalanceInfo }>(`/api/contracts/${id}`),
      http.get<Payment[]>(`/api/contracts/${id}/payments`),
    ]);
    this.contract = detail.contract;
    this.balance = detail.balance;
    this.payments = payments;
  }
}

// Datentypen, wie sie die API liefert (JSON, camelCase). Datumswerte kommen als ISO-String.

export interface CustomerListItem {
  id: number;
  customerNumber: string;
  firstName: string;
  lastName: string;
  city: string | null;
  contractCount: number;
  totalPaid: number;
}

export interface Customer {
  id: number;
  customerNumber: string;
  firstName: string;
  lastName: string;
  email: string | null;
  phone: string | null;
  street: string | null;
  postalCode: string | null;
  city: string | null;
  createdAt: string;
  createdBy: string;
}

/** Möglicher doppelter Kunde: gleicher Name und gleiche PLZ (GET /api/customers/duplicates) */
export interface CustomerDuplicate {
  id: number;
  customerNumber: string;
  firstName: string;
  lastName: string;
  postalCode: string | null;
  city: string | null;
}

export type CustomerInput = Omit<Customer, 'id' | 'customerNumber' | 'createdAt' | 'createdBy'>;

export interface ContractType {
  id: number;
  code: string;
  name: string;
  minTermMonths: number;
  noticePeriodMonths: number;
}

export interface ContractListItem {
  id: number;
  contractNumber: string;
  typeName: string;
  startDate: string;
  endDate: string | null;
  monthlyFee: number;
  status: ContractStatus;
  paidTotal: number;
  reminderDue: boolean;
}

export type ContractStatus = 'Active' | 'Cancelled' | 'Ended';

/** Treffer der Suche nach Vertragsnummer (GET /api/contracts?q=...) */
export interface ContractSearchItem {
  id: number;
  contractNumber: string;
  customerName: string;
  endDate: string | null;
  status: ContractStatus;
}

export interface ContractRecord {
  id: number;
  contractNumber: string;
  customerId: number;
  customerName: string;
  contractTypeId: number;
  typeName: string;
  minTermMonths: number;
  noticePeriodMonths: number;
  startDate: string;
  endDate: string | null;
  monthlyFee: number;
  status: ContractStatus;
  cancelledAt: string | null;
  cancellationReason: string | null;
  /** Kündigung kann zurückgenommen werden (nur bei Kündigungen ab Patch 1.1.0/0004) */
  cancellationUndoable: boolean;
  /** Vertragsende, das nach der Rücknahme wieder gilt (null = unbefristet) */
  endDateBeforeCancellation: string | null;
}

export interface BalanceInfo {
  monthsDue: number;
  expectedTotal: number;
  paidTotal: number;
  balance: number;
  /** Zahlungserinnerung fällig: Rückstand über zwei Monatsbeiträge (Handbuch 3.5) */
  reminderDue: boolean;
}

export interface ContractDetail {
  contract: ContractRecord;
  balance: BalanceInfo;
}

/** Neu (Schritt 4): Antwort von GET /api/contracts/{id}/cancellation-preview */
export interface CancellationPreview {
  cancelledAt: string;
  endDate: string;
}

export interface Payment {
  id: number;
  contractId: number;
  amount: number;
  paymentDate: string;
  reference: string | null;
  recordedAt: string;
  recordedBy: string;
}

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
}

export type ContractStatus = 'Active' | 'Cancelled' | 'Ended';

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
}

export interface BalanceInfo {
  monthsDue: number;
  expectedTotal: number;
  paidTotal: number;
  balance: number;
}

export interface ContractDetail {
  contract: ContractRecord;
  balance: BalanceInfo;
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

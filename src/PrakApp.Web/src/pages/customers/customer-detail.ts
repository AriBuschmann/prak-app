import { resolve } from 'aurelia';
import { IRouter, type Params } from '@aurelia/router';
import { I18N } from '@aurelia/i18n';
import { http, ApiError } from '../../services/http';
import { dateCell, formatDate, moneyCell } from '../../services/format';
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

  contractOpen = false;
  newContract: NewContract = this.emptyContract();
  contractErrors: Record<string, string[]> = {};

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
    this.form = this.customer;
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

  openContract(row: ContractListItem): void {
    void this.router.load(`contracts/${row.id}`);
  }

  showNewContract(): void {
    this.newContract = this.emptyContract();
    this.contractErrors = {};
    this.contractOpen = true;
  }

  async createContract(): Promise<void> {
    if (!this.customer) return;
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
    }
  }

  private emptyContract(): NewContract {
    const today = new Date();
    const startDate = [
      today.getFullYear(),
      String(today.getMonth() + 1).padStart(2, '0'),
      String(today.getDate()).padStart(2, '0'),
    ].join('-');
    return { contractTypeId: null, startDate, endDate: '', monthlyFee: '' };
  }
}

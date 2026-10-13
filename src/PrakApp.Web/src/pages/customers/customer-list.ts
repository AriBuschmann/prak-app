import { resolve } from 'aurelia';
import { IRouter } from '@aurelia/router';
import { I18N } from '@aurelia/i18n';
import { http, ApiError } from '../../services/http';
import { moneyCell } from '../../services/format';
import type { Customer, CustomerInput, CustomerListItem } from '../../services/models';
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

export class CustomerList {
  private readonly router = resolve(IRouter);
  private readonly i18n = resolve(I18N);

  customers: CustomerListItem[] = [];
  search = '';

  createOpen = false;
  newCustomer = emptyCustomer();
  errors: Record<string, string[]> = {};

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
    this.customers = await http.get<CustomerListItem[]>(`/api/customers${q ? `?q=${encodeURIComponent(q)}` : ''}`);
  }

  open(row: CustomerListItem): void {
    void this.router.load(`customers/${row.id}`);
  }

  showCreate(): void {
    this.newCustomer = emptyCustomer();
    this.errors = {};
    this.createOpen = true;
  }

  async create(): Promise<void> {
    try {
      const created = await http.post<Customer>('/api/customers', this.newCustomer);
      this.createOpen = false;
      await this.router.load(`customers/${created.id}`);
    } catch (e) {
      if (e instanceof ApiError && e.status === 400) {
        this.errors = e.fieldErrors;
        return;
      }
      throw e;
    }
  }
}

import { bindable } from 'aurelia';
import type { CustomerInput } from '../../services/models';

/** Eingabefelder eines Kunden. Wird beim Anlegen und beim Bearbeiten verwendet. */
export class CustomerForm {
  @bindable customer!: CustomerInput;
  @bindable errors: Record<string, string[]> = {};
}

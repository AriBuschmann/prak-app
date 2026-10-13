import { bindable, BindingMode } from 'aurelia';
import { $, JQX_THEME } from '../jqx';

/**
 * Dialog auf Basis von jqxWindow. Der Inhalt wird per <au-slot> übergeben.
 *
 *   <modal-dialog heading="Kunde bearbeiten" open.bind="editOpen"> ... </modal-dialog>
 */
export class ModalDialog {
  @bindable heading = '';
  @bindable({ mode: BindingMode.twoWay }) open = false;
  @bindable width = 520;

  windowEl!: HTMLElement;
  private isAttached = false;

  attached(): void {
    $(this.windowEl).jqxWindow({
      theme: JQX_THEME,
      autoOpen: false,
      isModal: true,
      resizable: false,
      width: this.width,
      maxHeight: 900,
      position: 'center',
    });
    // Schließen über das X im Fenster
    $(this.windowEl).on('close', () => {
      this.open = false;
    });
    this.isAttached = true;
    this.openChanged(this.open);
  }

  openChanged(open: boolean): void {
    if (!this.isAttached) return;
    $(this.windowEl).jqxWindow(open ? 'open' : 'close');
  }

  detaching(): void {
    this.isAttached = false;
    $(this.windowEl).off('close');
    $(this.windowEl).jqxWindow('close');
  }
}

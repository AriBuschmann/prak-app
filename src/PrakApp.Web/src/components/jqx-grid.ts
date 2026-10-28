import { bindable } from 'aurelia';
import { $, JQX_THEME } from '../jqx';
import { gridLocalization } from '../jqx/localization';

export interface GridColumn {
  text: string;
  datafield: string;
  type?: 'string' | 'number' | 'date';
  width?: string | number;
  cellsformat?: string;
  cellsalign?: 'left' | 'center' | 'right';
  cellsrenderer?: (row: number, column: string, value: unknown) => string;
}

/**
 * Aurelia-Wrapper um das jqxGrid.
 *
 *   <jqx-grid columns.bind="columns" rows.bind="customers" row-click.bind="row => open(row)"></jqx-grid>
 */
export class JqxGrid<T = unknown> {
  @bindable columns: GridColumn[] = [];
  @bindable rows: T[] = [];
  @bindable rowClick?: (row: T) => void;
  @bindable pageSize = 15;

  gridEl!: HTMLElement;
  private isAttached = false;

  attached(): void {
    $(this.gridEl).jqxGrid({
      theme: JQX_THEME,
      width: '100%',
      autoheight: true,
      pageable: true,
      pagesize: this.pageSize,
      pagesizeoptions: ['10', '15', '25', '50'],
      sortable: true,
      columnsresize: true,
      altrows: true,
      selectionmode: 'singlerow',
      localization: gridLocalization(),
      source: this.createAdapter(),
      columns: this.columns.map(({ type: _type, ...column }) => column),
    });
    $(this.gridEl).on('rowclick', (event: JQuery.TriggeredEvent) => {
      // Das Grid kennt nur die Spalten-Felder (z. B. keine "id"). Deshalb das Original-Objekt
      // über seine Position in den Daten holen – auch nach dem Sortieren stimmt "boundindex".
      const args = (event as unknown as { args: { row: { boundindex: number } } }).args;
      const row = this.rows[args.row.boundindex];
      if (row) {
        this.rowClick?.(row);
      }
    });
    this.isAttached = true;
  }

  rowsChanged(): void {
    if (this.isAttached) {
      $(this.gridEl).jqxGrid({ source: this.createAdapter() });
    }
  }

  detaching(): void {
    this.isAttached = false;
    $(this.gridEl).off('rowclick');
    $(this.gridEl).jqxGrid('destroy');
  }

  private createAdapter(): unknown {
    return new $.jqx.dataAdapter({
      localdata: this.rows ?? [],
      datatype: 'array',
      datafields: this.columns.map(c => ({ name: c.datafield, type: c.type ?? 'string' })),
    });
  }
}

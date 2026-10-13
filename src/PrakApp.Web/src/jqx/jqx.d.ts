// Minimale Typen für die verwendeten jQWidgets-Plugins.
// jQWidgets bringt für das jQuery-Paket keine eigenen TypeScript-Typen mit.
interface JQuery {
  jqxGrid(options: Record<string, unknown>): JQuery;
  jqxGrid(method: string, ...args: unknown[]): any;
  jqxWindow(options: Record<string, unknown>): JQuery;
  jqxWindow(method: string, ...args: unknown[]): any;
  jqxDateTimeInput(options: Record<string, unknown>): JQuery;
  jqxDateTimeInput(method: string, ...args: unknown[]): any;
}

interface JQueryStatic {
  jqx: {
    dataAdapter: new (source: Record<string, unknown>, settings?: Record<string, unknown>) => unknown;
  };
}

declare module 'jqwidgets-framework/jqwidgets/*';

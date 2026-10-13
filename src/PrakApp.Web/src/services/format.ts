const locale = () => (localStorage.getItem('language') === 'en' ? 'en-GB' : 'de-DE');

export function formatMoney(value: number | null | undefined): string {
  if (value === null || value === undefined) return '';
  return new Intl.NumberFormat(locale(), { style: 'currency', currency: 'EUR' }).format(value);
}

/** Formatiert ein Datum aus der API (z. B. "2026-10-05T00:00:00") für die Anzeige. */
export function formatDate(value: string | Date | null | undefined): string {
  if (!value) return '';
  return new Date(value).toLocaleDateString(locale(), { day: '2-digit', month: '2-digit', year: 'numeric' });
}

// Zellen-Renderer für das jqxGrid
export const moneyCell = (_row: number, _column: string, value: unknown) =>
  `<div class="cell cell-right">${formatMoney(value as number)}</div>`;

export const dateCell = (_row: number, _column: string, value: unknown) =>
  `<div class="cell">${formatDate(value as Date | string | null)}</div>`;

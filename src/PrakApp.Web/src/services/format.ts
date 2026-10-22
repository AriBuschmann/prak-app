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

/**
 * Kalenderdatum für die API als "JJJJ-MM-TT" in Ortszeit, z. B. "2026-10-22".
 * Nicht das Date-Objekt selbst schicken: JSON.stringify rechnet es nach UTC um,
 * und aus dem 22.10. 00:00 Uhr (MESZ) würde "2026-10-21T22:00:00.000Z".
 */
// Die Funktion bekommt ein Date-Objekt ("date") und gibt einen Text (string) zurück
export function toIsoDate(date: Date): string {
  // Liste (Array) aus drei Teilen Jahr, Monat und Tag bauen und zurückgeben
  return [
    // Das Jahr in Ortszeit, vierstellig, z. B. 2026
    date.getFullYear(),
    // Den Monat in Ortszeit holen, +1 rechnen, in Text umwandeln und vorne mit "0" auf 2 Stellen auffüllen
    String(date.getMonth() + 1).padStart(2, '0'), // getMonth() zählt ab 0 (Januar = 0)
    // Den Tag des Monats in Ortszeit holen (zählt ganz normal ab 1), in Text umwandeln und auf 2 Stellen auffüllen
    String(date.getDate()).padStart(2, '0'),
  // Die drei Teile mit "-" verbinden: [2026, "10", "22"] → "2026-10-22"
  ].join('-');
}

// Zellen-Renderer für das jqxGrid
export const moneyCell = (_row: number, _column: string, value: unknown) =>
  `<div class="cell cell-right">${formatMoney(value as number)}</div>`;

export const dateCell = (_row: number, _column: string, value: unknown) =>
  `<div class="cell">${formatDate(value as Date | string | null)}</div>`;

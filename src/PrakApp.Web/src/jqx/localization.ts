// Texte für jQWidgets in der Sprache der Oberfläche. jQWidgets bringt nur englische Texte mit,
// die Übersetzungen stehen wie alle anderen in src/locales (Gruppen "grid" und "calendar").
// i18next ist dieselbe Instanz, die @aurelia/i18n verwendet (in main.ts eingerichtet).
import i18next from 'i18next';

const list = (key: string) => i18next.t(key, { returnObjects: true }) as unknown as string[];

/** Für die Option "localization" von jqxGrid: Texte der Blätterleiste und der leeren Tabelle. */
export function gridLocalization(): Record<string, string> {
  return {
    pagershowrowsstring: i18next.t('grid.showRows'),
    pagergotopagestring: i18next.t('grid.goToPage'),
    pagerrangestring: i18next.t('grid.range'),
    emptydatastring: i18next.t('grid.empty'),
    loadtext: i18next.t('grid.loading'),
  };
}

/** Für die Option "localization" von jqxDateTimeInput: Monats- und Tagesnamen im Kalender. */
export function calendarLocalization(): Record<string, unknown> {
  return {
    calendar: {
      '/': '/',
      ':': ':',
      // Die Listen beginnen bei jQWidgets immer mit Sonntag; Monate brauchen einen 13. leeren Eintrag
      days: { names: list('calendar.days'), namesAbbr: list('calendar.daysShort'), namesShort: list('calendar.daysShort') },
      months: { names: [...list('calendar.months'), ''], namesAbbr: [...list('calendar.monthsShort'), ''] },
      AM: ['AM', 'am', 'AM'],
      PM: ['PM', 'pm', 'PM'],
      eras: [{ name: 'A.D.', start: null, offset: 0 }],
      twoDigitYearMax: 2029,
      patterns: { d: 'dd.MM.yyyy', D: 'dddd, d. MMMM yyyy', M: 'd. MMMM', Y: 'MMMM yyyy' },
    },
  };
}

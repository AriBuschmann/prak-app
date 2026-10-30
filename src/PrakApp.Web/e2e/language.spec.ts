import { test, expect } from '@playwright/test';

const languages = [
  {
    language: 'de',
    locale: 'de-DE',
    titles: { login: 'Anmeldung', customers: 'Kunden', customer: 'Kunde', contract: 'Vertrag' },
    pager: ['Zeilen anzeigen:', 'Gehe zu Seite:', ' von '],
    weekdays: /Mo\s*Di\s*Mi\s*Do\s*Fr\s*Sa\s*So/,
    arrows: { back: 'Zurück', forward: 'Vor' },
    notFound: 'Nicht gefunden',
  },
  {
    language: 'en',
    locale: 'en-GB',
    titles: { login: 'Sign in', customers: 'Customers', customer: 'Customer', contract: 'Contract' },
    pager: ['Show rows:', 'Go to page:', ' of '],
    weekdays: /Mo\s*Tu\s*We\s*Th\s*Fr\s*Sa\s*Su/,
    arrows: { back: 'Back', forward: 'Forward' },
    notFound: 'Not found',
  },
];

for (const expected of languages) {
  test(`Oberfläche komplett in der gewählten Sprache: ${expected.language}`, async ({ page }) => {
    await page.addInitScript((language) => localStorage.setItem('language', language), expected.language);

    await page.goto('/login');
    await expect(page).toHaveTitle(expected.titles.login);
    await page.locator('button[data-user="praktikum"]').click();
    await expect(page).toHaveURL(/\/customers$/);
    await expect(page).toHaveTitle(expected.titles.customers);
    for (const text of expected.pager) {
      await expect(page.locator('.jqx-grid-pager').first()).toContainText(text);
    }

    await page.goto('/customers/40');
    await expect(page).toHaveTitle(expected.titles.customer);

    await page.goto('/contracts/77');
    await expect(page).toHaveTitle(expected.titles.contract);
    await page.locator('.page-header button.primary').click();
    await page.getByTestId('payment-date').locator('.jqx-icon-calendar').click();
    const month = new Date().toLocaleDateString(expected.locale, { month: 'long', timeZone: 'Europe/Berlin' });
    const calendar = page.locator('.jqx-calendar').filter({ visible: true }).first();
    await expect(calendar).toContainText(month);
    await expect(calendar).toContainText(expected.weekdays);
    // Tooltips der Pfeile zum Blättern (vorher "undefined", weil die Texte in calendarLocalization() fehlten)
    await expect(calendar.locator('.jqx-calendar-title-navigation.jqx-icon-arrow-left')).toHaveAttribute('title', expected.arrows.back);
    await expect(calendar.locator('.jqx-calendar-title-navigation.jqx-icon-arrow-right')).toHaveAttribute('title', expected.arrows.forward);

    await page.goto('/customers');
    await page.evaluate(() => {
      history.pushState({}, '', '/customers/999999');
      dispatchEvent(new PopStateEvent('popstate'));
    });
    await expect(page.locator('.error-banner')).toContainText(`404: ${expected.notFound}`);
  });
}

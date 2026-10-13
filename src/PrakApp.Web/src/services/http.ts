/**
 * Fehler eines API-Aufrufs. Bei Validierungsfehlern (HTTP 400) enthält
 * `fieldErrors` die Meldungen pro Feld, z. B. { lastName: ['Nachname ist erforderlich.'] }.
 */
export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
    public readonly fieldErrors: Record<string, string[]> = {},
  ) {
    super(message);
  }
}

async function request<T>(method: string, url: string, body?: unknown): Promise<T> {
  const response = await fetch(url, {
    method,
    credentials: 'same-origin',
    headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  });

  if (!response.ok) {
    let problem: { title?: string; errors?: Record<string, string[]> } = {};
    try {
      problem = await response.json();
    } catch {
      // Antwort ohne JSON-Inhalt
    }
    throw new ApiError(response.status, problem.title ?? response.statusText, normalizeKeys(problem.errors));
  }

  if (response.status === 204) {
    return undefined as T;
  }
  return (await response.json()) as T;
}

// Das Backend liefert Feldnamen teils mit großem Anfangsbuchstaben ("LastName")
function normalizeKeys(errors?: Record<string, string[]>): Record<string, string[]> {
  const result: Record<string, string[]> = {};
  for (const [key, messages] of Object.entries(errors ?? {})) {
    result[key.charAt(0).toLowerCase() + key.slice(1)] = messages;
  }
  return result;
}

export const http = {
  get: <T>(url: string) => request<T>('GET', url),
  post: <T>(url: string, body?: unknown) => request<T>('POST', url, body ?? {}),
  put: <T>(url: string, body: unknown) => request<T>('PUT', url, body),
};

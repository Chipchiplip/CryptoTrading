let accessToken: string | null = null;

export function setAccessToken(token: string | null) {
  accessToken = token;
}

export async function authFetch(input: RequestInfo | URL, init?: RequestInit) {
  const headers = new Headers(init?.headers || {});
  if (accessToken) headers.set('Authorization', `Bearer ${accessToken}`);
  return fetch(input, { ...init, headers, credentials: 'include' });
}



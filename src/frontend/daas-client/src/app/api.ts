export type SchemaField = {
  id?: number;
  fieldName: string;
  fieldType: number;
  schemaId?: string;
};

export type Schema = {
  id: string;
  name: string;
  fields: SchemaField[];
};

export type NewSchema = {
  name: string;
  fields: Pick<SchemaField, 'fieldName' | 'fieldType'>[];
};

export type ApiLink = {
  id: string;
  schemaId: string;
  isActive: boolean;
  createdAt: string;
  expiresAt: string | null;
  defaultRecordCount: number;
};

export type ApiLinkWithUrl = ApiLink & { url: string };

const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '');

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`${apiBaseUrl}${path}`, {
      ...init,
      headers: { ...(init?.body ? { 'Content-Type': 'application/json' } : {}), ...init?.headers },
    });
  } catch {
    throw new Error('Could not reach the API. Check that the API is running and try again.');
  }

  if (!response.ok) {
    if (response.status === 404) throw new Error('The requested schema was not found.');
    const detail = await response.text();
    throw new Error(detail || `Request failed (${response.status}).`);
  }

  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

export const schemaApi = {
  list: () => request<Schema[]>('/api/schema'),
  get: (id: string) => request<Schema>(`/api/schema/${encodeURIComponent(id)}`),
  create: (schema: NewSchema) => request<{ id: string }>('/api/schema', {
    method: 'POST',
    body: JSON.stringify(schema),
  }),
  remove: (id: string) => request<void>(`/api/schema/${encodeURIComponent(id)}`, { method: 'DELETE' }),
  generate: (id: string, count: number) => request<Record<string, unknown>[]>(
    `/api/schema/${encodeURIComponent(id)}/data/${count}`,
  ),
};

export const apiLinkApi = {
  list: (schemaId: string) => request<ApiLink[]>(`/api/schema/${encodeURIComponent(schemaId)}/links`),
  create: (schemaId: string, defaultRecordCount: number, expiresAt: string | null) =>
    request<ApiLinkWithUrl>(`/api/schema/${encodeURIComponent(schemaId)}/links`, {
      method: 'POST', body: JSON.stringify({ defaultRecordCount, expiresAt }),
    }),
  update: (schemaId: string, linkId: string, defaultRecordCount: number, expiresAt: string | null) =>
    request<ApiLink>(`/api/schema/${encodeURIComponent(schemaId)}/links/${encodeURIComponent(linkId)}`, {
      method: 'PUT', body: JSON.stringify({ defaultRecordCount, expiresAt }),
    }),
  revoke: (schemaId: string, linkId: string) =>
    request<void>(`/api/schema/${encodeURIComponent(schemaId)}/links/${encodeURIComponent(linkId)}`, { method: 'DELETE' }),
  rotate: (schemaId: string, linkId: string) =>
    request<ApiLinkWithUrl>(`/api/schema/${encodeURIComponent(schemaId)}/links/${encodeURIComponent(linkId)}/rotate`, { method: 'POST' }),
};

import { useState, type FormEvent } from 'react';
import type { ApiLink } from '../../app/api';
import { ErrorState } from '../../shared/States';

type Props = {
  link?: ApiLink;
  onSave: (count: number, expiresAt: string | null) => Promise<void>;
  busy: boolean;
};

export function LinkSettingsForm({ link, onSave, busy }: Props) {
  const [count, setCount] = useState(String(link?.defaultRecordCount ?? 10));
  const [expiry, setExpiry] = useState(() => {
    if (!link?.expiresAt) return '';
    const date = new Date(link.expiresAt);
    return new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
  });
  const [error, setError] = useState('');
  async function submit(event: FormEvent) {
    event.preventDefault();
    const date = expiry ? new Date(expiry) : null;
    if (date && !(date.getTime() > Date.now())) { setError('Choose an expiry in the future.'); return; }
    setError('');
    try { await onSave(Number(count), date?.toISOString() ?? null); }
    catch (err) { setError(err instanceof Error ? err.message : 'Could not save link settings.'); }
  }
  return <form onSubmit={event => void submit(event)}>
    {error && <ErrorState>{error}</ErrorState>}
    <fieldset className="form-stack" disabled={busy}>
      <label>Records per request<input type="number" required min={1} max={1000} value={count} onChange={event => setCount(event.target.value)} /><small>Choose between 1 and 1,000.</small></label>
      <label>Expiry <span className="optional">(optional)</span><input type="datetime-local" value={expiry} onChange={event => setExpiry(event.target.value)} /><small>Leave blank to keep the link available until revoked.</small></label>
      <button className="button button-primary">{busy ? 'Saving…' : link ? 'Save settings' : 'Create public link'}</button>
    </fieldset>
  </form>;
}

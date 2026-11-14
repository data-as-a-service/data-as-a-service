import { useEffect, useState } from 'react';
import { apiLinkApi, schemaApi, type ApiLink, type ApiLinkWithUrl, type Schema } from '../../app/api';
import { EmptyState, ErrorState, LoadingState } from '../../shared/States';

export function ApiLinksPage() {
  const [schemas, setSchemas] = useState<Schema[]>([]);
  const [schemaId, setSchemaId] = useState('');
  const [links, setLinks] = useState<ApiLink[]>([]);
  const [count, setCount] = useState(10);
  const [expiry, setExpiry] = useState('');
  const [lastUrl, setLastUrl] = useState('');
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    schemaApi.list().then(items => {
      setSchemas(items);
      setSchemaId(items[0]?.id ?? '');
    }).catch(err => setError(err instanceof Error ? err.message : 'Could not load schemas.'))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    setLastUrl('');
    if (!schemaId) { setLinks([]); return; }
    setLoading(true);
    apiLinkApi.list(schemaId).then(setLinks)
      .catch(err => setError(err instanceof Error ? err.message : 'Could not load API links.'))
      .finally(() => setLoading(false));
  }, [schemaId]);

  async function refresh() {
    if (schemaId) setLinks(await apiLinkApi.list(schemaId));
  }

  async function create() {
    setBusy(true); setError(''); setLastUrl('');
    try {
      const created = await apiLinkApi.create(schemaId, count, expiry ? new Date(expiry).toISOString() : null);
      setLastUrl(created.url); await refresh();
    } catch (err) { setError(err instanceof Error ? err.message : 'Could not create API link.'); }
    finally { setBusy(false); }
  }

  async function update(link: ApiLink) {
    const nextCount = Number(window.prompt('Default records per request (1–1000)', String(link.defaultRecordCount)));
    if (!Number.isInteger(nextCount) || nextCount < 1 || nextCount > 1000) return;
    const expiryDate = window.prompt('Expiry date (YYYY-MM-DD; leave blank for no expiry)', link.expiresAt?.slice(0, 10) ?? '');
    if (expiryDate === null) return;
    const nextExpiry = expiryDate.trim() ? new Date(`${expiryDate.trim()}T23:59:59Z`) : null;
    if (nextExpiry && (Number.isNaN(nextExpiry.getTime()) || nextExpiry.getTime() <= Date.now())) {
      setError('Expiry must be a future date in YYYY-MM-DD format.');
      return;
    }
    setBusy(true); setError('');
    try { await apiLinkApi.update(schemaId, link.id, nextCount, nextExpiry?.toISOString() ?? null); await refresh(); }
    catch (err) { setError(err instanceof Error ? err.message : 'Could not update link.'); }
    finally { setBusy(false); }
  }

  async function rotate(link: ApiLink) {
    setBusy(true); setError(''); setLastUrl('');
    try { const result: ApiLinkWithUrl = await apiLinkApi.rotate(schemaId, link.id); setLastUrl(result.url); await refresh(); }
    catch (err) { setError(err instanceof Error ? err.message : 'Could not rotate link.'); }
    finally { setBusy(false); }
  }

  async function revoke(link: ApiLink) {
    if (!window.confirm('Revoke this public link? Existing copies will stop working.')) return;
    setBusy(true); setError('');
    try { await apiLinkApi.revoke(schemaId, link.id); await refresh(); }
    catch (err) { setError(err instanceof Error ? err.message : 'Could not revoke link.'); }
    finally { setBusy(false); }
  }

  async function copy(text: string) {
    try { await navigator.clipboard.writeText(text); }
    catch { setError('Clipboard access was unavailable. Select and copy the URL manually.'); }
  }

  if (loading && schemas.length === 0) return <LoadingState>Loading schemas…</LoadingState>;

  return <>
    <div className="page-heading"><div><p className="eyebrow">SHARE YOUR DATA</p><h1>API links</h1><p className="page-description">Create public GET endpoints backed by your schemas.</p></div><span className="count-pill">{links.length} {links.length === 1 ? 'link' : 'links'}</span></div>
    <div className="link-layout">
      <section className="panel create-panel">
        <div className="panel-heading"><div><h2>Create a public link</h2><p>Anyone with the URL can request generated records.</p></div><span className="step-badge">01</span></div>
        {error && <ErrorState>{error}</ErrorState>}
        {schemas.length === 0 ? <EmptyState>Create a schema before creating an API link.</EmptyState> : <div className="schema-form">
          <label className="input-label" htmlFor="link-schema">Schema</label>
          <select id="link-schema" className="select-input" value={schemaId} onChange={event => setSchemaId(event.target.value)}>{schemas.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select>
          <label className="input-label link-label" htmlFor="link-count">Default records per request</label>
          <input id="link-count" className="text-input" type="number" min={1} max={1000} value={count} onChange={event => setCount(Number(event.target.value))} />
          <label className="input-label link-label" htmlFor="link-expiry">Expires at (optional)</label>
          <input id="link-expiry" className="text-input" type="datetime-local" value={expiry} onChange={event => setExpiry(event.target.value)} />
          <p className="helper-text">Records are random on each request. Maximum: 1,000.</p>
          <button className="button button-primary create-submit" disabled={busy || count < 1 || count > 1000} onClick={() => void create()}>Create API link <span>↗</span></button>
        </div>}
        {lastUrl && <div className="link-created"><strong>Copy this URL now</strong><p>The secret is shown only at creation or rotation.</p><code>{lastUrl}</code><button className="button button-quiet" onClick={() => void copy(lastUrl)}>Copy URL</button></div>}
      </section>
      <section className="panel list-panel">
        <div className="panel-heading"><div><h2>Links for this schema</h2><p>Revoked links can no longer serve data.</p></div></div>
        {loading ? <LoadingState>Loading links…</LoadingState> : links.length === 0 ? <EmptyState>No API links for this schema yet.</EmptyState> : <div className="schema-list">{links.map(link => {
          const expired = Boolean(link.expiresAt && new Date(link.expiresAt).getTime() <= Date.now());
          return <article className="schema-card" key={link.id}>
          <div className="schema-meta"><h3>{!link.isActive ? 'Revoked link' : expired ? 'Expired link' : 'Active public link'}</h3><span>{link.defaultRecordCount} records · {link.expiresAt ? `Expires ${new Date(link.expiresAt).toLocaleString()}` : 'No expiry'} · Created {new Date(link.createdAt).toLocaleDateString()}</span></div>
          <div className="schema-actions">{link.isActive && <><button className="button button-quiet" disabled={busy} onClick={() => void update(link)}>Settings</button><button className="button button-quiet" disabled={busy} onClick={() => void rotate(link)}>Rotate</button><button className="button button-quiet" disabled={busy} onClick={() => void revoke(link)}>Revoke</button></>}</div>
        </article>})}</div>}
      </section>
    </div>
  </>;
}

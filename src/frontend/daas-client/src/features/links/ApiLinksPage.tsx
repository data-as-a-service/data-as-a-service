import { useEffect, useState } from 'react';
import { apiLinkApi, type ApiLink, type ApiLinkWithUrl } from '../../app/api';
import { Dialog } from '../../shared/Dialog';
import { Icon } from '../../shared/Icon';
import { ErrorState, LoadingState } from '../../shared/States';
import { useSchemaChoices } from '../schemas/useSchemaChoices';
import { LinkSettingsForm } from './LinkSettingsForm';

export function ApiLinksPage({ initialSchemaId }: { initialSchemaId: string }) {
  const choices = useSchemaChoices(initialSchemaId);
  return <>
    <header className="page-heading"><div><h1>API links</h1><p>Publish a saved dataset and manage the URLs that serve it.</p></div></header>
    {choices.error ? <ErrorState>{choices.error} <button className="text-action" onClick={choices.retry}>Try again</button></ErrorState> :
      choices.loading ? <LoadingState>Loading schemas…</LoadingState> :
      !choices.schemaId ? <div className="panel blank"><Icon name="links" /><h2>Create a schema to share data</h2><p>Each public link serves records from one schema.</p><a className="button button-primary" href="#schemas">Open schema library</a></div> :
      <><label className="schema-picker">Selected schema<select value={choices.schemaId} onChange={event => choices.setSchemaId(event.target.value)}>
        {choices.schemas.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
        <LinkWorkspace key={choices.schemaId} schemaId={choices.schemaId} /></>}
  </>;
}

function LinkWorkspace({ schemaId }: { schemaId: string }) {
  const [links, setLinks] = useState<ApiLink[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState('');
  const [attempt, setAttempt] = useState(0);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [editing, setEditing] = useState<ApiLink | null>(null);
  const [issued, setIssued] = useState<ApiLinkWithUrl | null>(null);
  const [copied, setCopied] = useState(false);
  const [notice, setNotice] = useState('');

  useEffect(() => {
    let active = true;
    setLoading(true); setLoadError('');
    apiLinkApi.list(schemaId).then(items => { if (active) setLinks(items); })
      .catch(err => { if (active) setLoadError(err instanceof Error ? err.message : 'Could not load links.'); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [schemaId, attempt]);

  function showUrl(link: ApiLinkWithUrl) { setIssued(link); setCopied(false); }
  async function save(count: number, expiry: string | null) {
    setBusy(true); setError(''); setNotice('');
    try {
      if (editing) {
        await apiLinkApi.update(schemaId, editing.id, count, expiry);
        setEditing(null); setNotice('Link settings saved.');
      } else { showUrl(await apiLinkApi.create(schemaId, count, expiry)); }
      setAttempt(value => value + 1);
    } finally { setBusy(false); }
  }

  async function act(link: ApiLink, action: 'rotate' | 'revoke' | 0 | 1) {
    if (action === 'rotate' && !window.confirm('Replace this URL? Existing copies will stop working.')) return;
    if (action === 'revoke' && !window.confirm('Revoke this URL? Existing copies will stop working.')) return;
    setBusy(true); setError(''); setNotice('');
    try {
      if (action === 'rotate') showUrl(await apiLinkApi.rotate(schemaId, link.id));
      else if (action === 'revoke') {
        await apiLinkApi.revoke(schemaId, link.id);
        if (issued?.id === link.id) setIssued(null);
        setNotice('Public link revoked.');
      } else {
        const data = await apiLinkApi.regenerate(schemaId, link.id, action);
        setNotice('Saved ' + data.length + ' new records for link ' + link.id.slice(0, 8) + '.');
      }
      setAttempt(value => value + 1);
    } catch (err) { setError(err instanceof Error ? err.message : 'Could not change the link.'); }
    finally { setBusy(false); }
  }

  async function copy(url: string) {
    try { await navigator.clipboard.writeText(url); setCopied(true); }
    catch { setError('Select the URL and copy it manually. Clipboard access is unavailable.'); }
  }

  return <>
    {error && <ErrorState>{error}</ErrorState>}
    {notice && <p className="notice success-notice" role="status">{notice}</p>}
    {issued && <section className="issued" aria-label="New public URL"><strong role="status">Your public URL is ready</strong>
      <p>Copy it now. Recovering a lost URL replaces it.</p><div><code>{issued.url}</code>
        <button className="button button-quiet" onClick={() => void copy(issued.url)}>{copied ? 'Copied' : 'Copy URL'}</button></div></section>}
    <div className="links-grid">
      <section className="panel link-registry"><div className="panel-heading"><h2>Published links</h2>{!loading && !loadError && <span className="number-tag">{links.length}</span>}</div>
        {loadError ? <ErrorState>{loadError} <button className="text-action" onClick={() => setAttempt(value => value + 1)}>Try again</button></ErrorState> :
          loading ? <LoadingState>Loading links…</LoadingState> : links.length === 0 ?
            <div className="blank"><Icon name="links" /><h3>No links for this schema</h3><p>Create a link to give your frontend a repeatable JSON response.</p></div> :
            links.map(link => {
              const expired = Boolean(link.expiresAt && new Date(link.expiresAt).getTime() <= Date.now());
              const status = !link.isActive ? 'Revoked' : expired ? 'Expired' : 'Active';
              return <article key={link.id} className="link-row">
                <div className="link-identity"><h3>Link <code>{link.id.slice(0, 8)}</code></h3><span className="link-status" data-active={status === 'Active'}>{status}</span></div>
                <p>{link.defaultRecordCount} records · {link.expiresAt ? 'Expires ' + new Date(link.expiresAt).toLocaleString() : 'No expiry'}</p>
                <small>Created {new Date(link.createdAt).toLocaleDateString()}</small>
                {link.isActive && <details><summary>Manage link</summary><div className="link-actions">
                  {!expired && <><button disabled={busy} onClick={() => void act(link, 0)}>Regenerate random data</button><button disabled={busy} onClick={() => void act(link, 1)}>Regenerate with AI</button></>}
                  <button disabled={busy} onClick={() => setEditing(link)}>Settings</button>
                  {!expired && <button disabled={busy} onClick={() => void act(link, 'rotate')}>Rotate URL</button>}
                  <button className="danger" disabled={busy} onClick={() => void act(link, 'revoke')}>Revoke</button>
                </div></details>}
              </article>;
            })}
      </section>
      <section className="panel"><div className="panel-heading"><h2>New public link</h2><Icon name="links" /></div>
        <p className="panel-intro">Anyone with the URL can request this dataset.</p>
        <LinkSettingsForm busy={busy || !!editing} onSave={save} />
      </section>
    </div>
    {editing && <Dialog title="Link settings" onClose={() => { if (!busy) setEditing(null); }}>
      <LinkSettingsForm link={editing} busy={busy} onSave={save} />
    </Dialog>}
  </>;
}

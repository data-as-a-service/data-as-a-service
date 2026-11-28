import { useEffect, useRef, useState } from 'react';
import { schemaApi, type Schema } from '../../app/api';
import { Dialog } from '../../shared/Dialog';
import { Icon } from '../../shared/Icon';
import { ErrorState, LoadingState } from '../../shared/States';
import { CreateSchemaForm } from './CreateSchemaForm';
import { fieldTypeName } from './schemaFieldTypes';

export function SchemaListPage() {
  const [schemas, setSchemas] = useState<Schema[]>([]);
  const [selectedId, setSelectedId] = useState('');
  const [query, setQuery] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [creating, setCreating] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const inspector = useRef<HTMLElement>(null);
  const matches = schemas.filter(schema => schema.name.toLowerCase().includes(query.trim().toLowerCase()));
  const selected = matches.find(schema => schema.id === selectedId) ?? matches[0];

  async function refresh() {
    setLoading(true);
    setError('');
    try { setSchemas(await schemaApi.list()); }
    catch (err) { setError(err instanceof Error ? err.message : 'Could not load schemas.'); }
    finally { setLoading(false); }
  }
  useEffect(() => { void refresh(); }, []);

  async function remove(schema: Schema) {
    if (!window.confirm('Delete ' + schema.name + ' and its public API links? This cannot be undone.')) return;
    setDeleting(true);
    try {
      await schemaApi.remove(schema.id);
      setNotice(schema.name + ' deleted.');
      await refresh();
    } catch (err) { setError(err instanceof Error ? err.message : 'Could not delete schema.'); }
    finally { setDeleting(false); }
  }

  return <>
    <header className="page-heading"><div><h1>Schema library</h1><p>Define the fields your frontend expects.</p></div>
      <button className="button button-primary" onClick={() => setCreating(true)}><Icon name="plus" />New schema</button>
    </header>
    {notice && <p className="notice success-notice" role="status">{notice}</p>}
    {error && <ErrorState>{error} <button className="text-action" onClick={() => void refresh()}>Try again</button></ErrorState>}
    <div className="catalog">
      <section className="panel library-panel" aria-labelledby="library-heading">
        <div className="panel-heading"><h2 id="library-heading">Saved schemas</h2>{!loading && !error && <span className="number-tag">{schemas.length}</span>}</div>
        <label className="search"><Icon name="search" /><input type="search" aria-label="Find a schema" placeholder="Find a schema…" value={query} onChange={event => setQuery(event.target.value)} /></label>
        {loading ? <LoadingState>Loading schemas…</LoadingState> : matches.length ? <ul className="schema-rows">
          {matches.map(schema => <li key={schema.id}><button className="schema-row" aria-pressed={schema.id === selected?.id} onClick={() => {
            setSelectedId(schema.id);
            if (window.matchMedia('(max-width: 710px)').matches) inspector.current?.focus();
          }}>
            <span className="file-icon"><Icon name="schemas" /></span>
            <span><strong>{schema.name}</strong><small>{schema.fields.length} {schema.fields.length === 1 ? 'field' : 'fields'}</small></span>
            <Icon name="arrow" />
          </button></li>)}
        </ul> : <div className="blank"><Icon name="schemas" /><h3>{error ? 'Library unavailable' : query ? 'No matching schemas' : 'Create your first schema'}</h3>
          <p>{error ? 'Retry when the API is available.' : query ? 'Try a different schema name.' : 'Start with a name and a field. Your definition will appear here.'}</p>
          {!error && <button className="text-action" onClick={() => query ? setQuery('') : setCreating(true)}>{query ? 'Clear search' : 'Add a schema'} →</button>}
        </div>}
      </section>
      <section className="inspector" ref={inspector} tabIndex={-1} aria-label="Schema details">
        <header><span><Icon name="generate" />Field inspector</span><span className="format-tag">SCHEMA</span></header>
        {selected && !loading ? <><div className="inspect-title"><h2>{selected.name}</h2><span>{selected.fields.length} fields</span></div>
          <div className="inspect-body"><table><caption className="sr-only">Fields in {selected.name}</caption><thead><tr><th>Field name</th><th>Type</th></tr></thead>
            <tbody>{selected.fields.map((field, i) => <tr key={field.id ?? i}><td>{field.fieldName}</td><td>{fieldTypeName(field.fieldType)}</td></tr>)}</tbody></table>
            {selected.fields.length === 0 && <p>This schema has no fields.</p>}</div>
          <div className="inspect-actions">
            <a className="button button-signal" href={'#generate?schema=' + encodeURIComponent(selected.id)}>Generate records <Icon name="arrow" /></a>
            <a className="button invert" href={'#links?schema=' + encodeURIComponent(selected.id)}>API links</a>
            <button className="delete-link" disabled={deleting} onClick={() => void remove(selected)}>Delete</button>
          </div></> : <div className="inspector-empty"><span aria-hidden="true">{'{ }'}</span><h2>Schema preview</h2><p>Select a saved schema to inspect its field names and types.</p></div>}
      </section>
    </div>
    <section className="next-tools" aria-label="Schema tools">
      <a href="#generate"><Icon name="generate" /><div><strong>Data generator</strong><p>Preview and copy a JSON response.</p></div><Icon name="arrow" /></a>
      <a href="#links"><Icon name="links" /><div><strong>API links</strong><p>Serve a saved dataset from a public URL.</p></div><Icon name="arrow" /></a>
    </section>
    {creating && <Dialog title="New schema" onClose={() => setCreating(false)}>
      <CreateSchemaForm onCreated={async (id, name) => {
        setCreating(false); setSelectedId(id); setQuery(''); setNotice(name + ' created.'); await refresh();
      }} />
    </Dialog>}
  </>;
}

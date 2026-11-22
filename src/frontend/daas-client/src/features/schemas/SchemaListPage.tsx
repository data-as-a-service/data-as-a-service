import { useEffect, useState } from 'react';
import { schemaApi, type Schema } from '../../app/api';
import { EmptyState, ErrorState, LoadingState } from '../../shared/States';
import { CreateSchemaForm } from './CreateSchemaForm';
import { fieldTypeName } from './schemaFieldTypes';

type Props = { onGenerate: (id: string) => void };

export function SchemaListPage({ onGenerate }: Props) {
  const [schemas, setSchemas] = useState<Schema[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [selected, setSelected] = useState<Schema | null>(null);
  const [deleting, setDeleting] = useState('');

  async function refresh() {
    setLoading(true);
    setError('');
    try { setSchemas(await schemaApi.list()); }
    catch (err) { setError(err instanceof Error ? err.message : 'Could not load schemas.'); }
    finally { setLoading(false); }
  }

  useEffect(() => { void refresh(); }, []);

  async function remove(schema: Schema) {
    if (!window.confirm(`Delete “${schema.name}”? This cannot be undone.`)) return;
    setDeleting(schema.id);
    setError('');
    try {
      await schemaApi.remove(schema.id);
      if (selected?.id === schema.id) setSelected(null);
      await refresh();
    } catch (err) { setError(err instanceof Error ? err.message : 'Could not delete schema.'); }
    finally { setDeleting(''); }
  }

  return <>
    <section className="welcome-banner">
      <div className="welcome-copy">
        <span className="banner-kicker"><span className="banner-spark">✦</span> YOUR DATA WORKSPACE</span>
        <h2>Shape data.<br /><em>Build with confidence.</em></h2>
        <p>Design a schema, generate realistic sample records, and share them through a simple API link.</p>
        <div className="banner-actions">
          <button className="button button-light" onClick={() => schemas.length ? onGenerate(schemas[0].id) : document.getElementById('schema-create-panel')?.scrollIntoView({ behavior: 'smooth', block: 'center' })}>{schemas.length ? 'Generate a sample' : 'Create your first schema'} <span>→</span></button>
          <a href="/swagger" target="_blank" rel="noreferrer">Explore API docs <span>↗</span></a>
        </div>
        <div className="workflow-hint"><span>01 <b>Define</b></span><i /> <span>02 <b>Generate</b></span><i /> <span>03 <b>Share</b></span></div>
      </div>
      <div className="banner-visual" aria-hidden="true">
        <div className="visual-glow" />
        <div className="code-window"><div className="code-window-top"><span /><span /><span /><small>customer.json</small></div><pre><span className="json-brace">{'{'}</span>{'\n'}  <span className="json-key">"customer"</span>: <span className="json-brace">{'{'}</span>{'\n'}    <span className="json-key">"name"</span>: <span className="json-value">"Jordan Lee"</span>,{'\n'}    <span className="json-key">"active"</span>: <span className="json-bool">true</span>,{'\n'}    <span className="json-key">"orders"</span>: <span className="json-brace">[</span><span className="json-brace">{'{'}</span>{' … '}<span className="json-brace">{'}'}</span><span className="json-brace">]</span>{'\n'}  <span className="json-brace">{'}'}</span>{'\n'}<span className="json-brace">{'}'}</span></pre><div className="visual-badge"><span>✦</span> Generated preview</div></div>
      </div>
      <div className="banner-orb orb-one" /><div className="banner-orb orb-two" />
    </section>

    <div className="page-heading">
      <div><p className="eyebrow">DATA MODELS</p><h1>Schemas</h1><p className="page-description">Define the fields that shape your generated data.</p></div>
      <span className="count-pill">{schemas.length} {schemas.length === 1 ? 'schema' : 'schemas'}</span>
    </div>

    <div className="overview-strip">
      <div className="overview-item"><span className="overview-icon">▦</span><span><strong>{schemas.length}</strong><small>{schemas.length === 1 ? 'Schema ready' : 'Schemas ready'}</small></span></div>
      <div className="overview-item"><span className="overview-icon violet">≡</span><span><strong>{schemas.reduce((total, schema) => total + schema.fields.length, 0)}</strong><small>Fields defined</small></span></div>
      <div className="overview-item overview-note"><span className="status-mini" /><span><strong>Ready when you are</strong><small>Your schemas shape every generated response.</small></span></div>
    </div>

    <div className="schema-layout">
      <section className="panel create-panel" id="schema-create-panel">
        <div className="panel-heading"><div><h2>Create a schema</h2><p>Start with a name and add the fields you need.</p></div><span className="step-badge">01</span></div>
        <CreateSchemaForm onCreated={refresh} />
      </section>

      <section className="panel list-panel">
        <div className="panel-heading"><div><h2>Your schemas</h2><p>Browse and generate data from saved definitions.</p></div><button className="icon-button" onClick={() => void refresh()} aria-label="Refresh schemas" title="Refresh">↻</button></div>
        {error && <ErrorState>{error}</ErrorState>}
        {loading ? <LoadingState>Loading schemas…</LoadingState> : schemas.length === 0 ? <EmptyState>No schemas yet. Create your first schema to get started.</EmptyState> :
          <div className="schema-list">{schemas.map(schema => <article className="schema-card" key={schema.id}>
            <div className="schema-card-main"><div className="schema-icon">{schema.name.slice(0, 1).toUpperCase() || 'S'}</div><div className="schema-meta"><h3>{schema.name}</h3><span>{schema.fields.length} {schema.fields.length === 1 ? 'field' : 'fields'}</span></div></div>
            <div className="schema-actions"><button className="button button-quiet" onClick={() => setSelected(schema)}>Details</button><button className="button button-primary" onClick={() => onGenerate(schema.id)}>Generate</button><button className="icon-button danger-icon" disabled={deleting === schema.id} onClick={() => void remove(schema)} aria-label={`Delete ${schema.name}`} title="Delete">{deleting === schema.id ? '…' : '×'}</button></div>
          </article>)}</div>}
      </section>
    </div>

    {selected && <div className="modal-backdrop" role="presentation" onMouseDown={event => { if (event.target === event.currentTarget) setSelected(null); }}>
      <section className="modal panel" role="dialog" aria-modal="true" aria-labelledby="schema-details-title">
        <div className="panel-heading"><div><p className="eyebrow">SCHEMA DETAILS</p><h2 id="schema-details-title">{selected.name}</h2></div><button className="icon-button" onClick={() => setSelected(null)} aria-label="Close details">×</button></div>
        <p className="id-label">ID <code>{selected.id}</code></p>
        {selected.fields.length === 0 ? <EmptyState>This schema has no fields.</EmptyState> : <div className="field-table">{selected.fields.map(field => <div className="field-row" key={`${field.id ?? field.fieldName}`}><span>{field.fieldName}</span><span className="type-pill">{fieldTypeName(field.fieldType)}</span></div>)}</div>}
        <div className="modal-actions"><button className="button button-quiet" onClick={() => setSelected(null)}>Close</button><button className="button button-primary" onClick={() => { onGenerate(selected.id); setSelected(null); }}>Generate data</button></div>
      </section>
    </div>}
  </>;
}

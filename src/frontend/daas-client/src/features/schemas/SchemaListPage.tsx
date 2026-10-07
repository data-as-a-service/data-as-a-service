import { useEffect, useState } from 'react';
import { schemaApi, type Schema } from '../../app/api';
import { EmptyState, ErrorState, LoadingState } from '../../shared/States';
import { CreateSchemaForm } from './CreateSchemaForm';

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
    <div className="page-heading">
      <div><p className="eyebrow">DATA MODELS</p><h1>Schemas</h1><p className="page-description">Define the fields that shape your generated data.</p></div>
      <span className="count-pill">{schemas.length} {schemas.length === 1 ? 'schema' : 'schemas'}</span>
    </div>

    <div className="schema-layout">
      <section className="panel create-panel">
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

export function fieldTypeName(value: number) {
  return ['INT', 'FLOAT', 'DOUBLE', 'DECIMAL', 'CHAR', 'STRING', 'BOOLEAN', 'DATE', 'TIME', 'DATETIME', 'GUID', 'UUID', 'BYTE', 'SHORT', 'USHORT', 'LONG', 'LONGLONG', 'ULONG', 'SBYTE', 'BINARY'][value] ?? `TYPE ${value}`;
}

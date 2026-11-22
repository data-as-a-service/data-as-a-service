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
  const [creating, setCreating] = useState(false);

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
    <section className="intro-block">
      <div className="intro-copy"><p className="eyebrow">A SMALL WORKSPACE FOR SAMPLE DATA</p><h1>Make the data<br />your project needs.</h1><p className="intro-description">Define the shape of a response, generate records that fit it, then share a link your frontend can call.</p></div>
      <div className="intro-aside"><span className="intro-index">01 <i /> 03</span><p>Start with a schema.<br />The rest follows from it.</p></div>
    </section>

    <section className="workflow-strip" aria-label="How to use Data as a Service">
      <article className="workflow-step"><span className="workflow-number">01</span><div><h2>Describe</h2><p>Name a schema and add the fields your response needs.</p></div></article>
      <article className="workflow-step"><span className="workflow-number">02</span><div><h2>Generate</h2><p>Choose a schema and make a batch of sample records.</p></div></article>
      <article className="workflow-step"><span className="workflow-number">03</span><div><h2>Share</h2><p>Create a public link that serves its saved dataset.</p></div></article>
    </section>

    <section className="workspace-section" aria-labelledby="schemas-heading">
      <div className="section-heading"><div><p className="eyebrow">YOUR DEFINITIONS</p><h2 id="schemas-heading">Schemas <span className="section-count tabular-nums">{schemas.length.toLocaleString()}</span></h2><p>Each schema describes the fields in your sample response.</p></div><button className="button button-primary" onClick={() => setCreating(true)}>New schema <span aria-hidden="true">+</span></button></div>
      {error && <ErrorState>{error}</ErrorState>}
      {loading ? <div className="workspace-state"><LoadingState>Loading schemas…</LoadingState></div> : schemas.length === 0 ? <div className="workspace-empty"><div className="empty-index">01</div><div><h3>Nothing here yet</h3><p>Create your first schema to define the shape of the data you want to work with.</p><button className="text-action" onClick={() => setCreating(true)}>Create a schema <span aria-hidden="true">→</span></button></div></div> : <div className="schema-table-wrap"><table className="schema-table"><thead><tr><th>NAME</th><th>FIELDS</th><th>USE THIS SCHEMA</th><th aria-label="Actions" /></tr></thead><tbody>{schemas.map(schema => <tr key={schema.id}><td><button className="schema-name-button" onClick={() => setSelected(schema)}>{schema.name}<span>View fields</span></button></td><td className="tabular-nums field-count-cell">{schema.fields.length.toLocaleString()} <span>{schema.fields.length === 1 ? 'field' : 'fields'}</span></td><td><button className="text-action" onClick={() => onGenerate(schema.id)}>Generate records <span aria-hidden="true">→</span></button></td><td><button className="icon-button danger-icon" disabled={deleting === schema.id} onClick={() => void remove(schema)} aria-label={`Delete ${schema.name}`} title="Delete schema">×</button></td></tr>)}</tbody></table></div>}
      <p className="section-footnote">Schemas are data shapes. They don’t contain your generated records.</p>
    </section>

    {creating && <div className="modal-backdrop" role="presentation" onMouseDown={event => { if (event.target === event.currentTarget) setCreating(false); }}>
      <section className="modal panel" role="dialog" aria-modal="true" aria-labelledby="create-schema-title">
        <div className="panel-heading"><div><p className="eyebrow">DEFINE YOUR RESPONSE</p><h2 id="create-schema-title">New schema</h2><p>Add a name and at least one field.</p></div><button className="icon-button" onClick={() => setCreating(false)} aria-label="Close">×</button></div>
        <CreateSchemaForm onCreated={async () => { await refresh(); setCreating(false); }} />
      </section>
    </div>}

    {selected && <div className="modal-backdrop" role="presentation" onMouseDown={event => { if (event.target === event.currentTarget) setSelected(null); }}>
      <section className="modal panel" role="dialog" aria-modal="true" aria-labelledby="schema-details-title">
        <div className="panel-heading"><div><p className="eyebrow">SCHEMA FIELDS</p><h2 id="schema-details-title">{selected.name}</h2><p>{selected.fields.length} {selected.fields.length === 1 ? 'field' : 'fields'} in this definition</p></div><button className="icon-button" onClick={() => setSelected(null)} aria-label="Close details">×</button></div>
        <div className="field-table" role="table" aria-label={`${selected.name} fields`}><div className="field-table-head" role="row"><span role="columnheader">FIELD</span><span role="columnheader">TYPE</span></div>
          {selected.fields.length === 0 ? <EmptyState>This schema has no fields yet.</EmptyState> : selected.fields.map(field => <div className="field-row" role="row" key={`${field.id ?? field.fieldName}`}><span role="cell">{field.fieldName}</span><span className="type-pill" role="cell">{fieldTypeName(field.fieldType)}</span></div>)}
        </div>
        <div className="modal-actions"><button className="button button-quiet" onClick={() => setSelected(null)}>Close</button><button className="button button-primary" onClick={() => { onGenerate(selected.id); setSelected(null); }}>Generate data <span aria-hidden="true">→</span></button></div>
      </section>
    </div>}
  </>;
}

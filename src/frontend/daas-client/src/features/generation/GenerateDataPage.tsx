import { useEffect, useMemo, useState } from 'react';
import { schemaApi, type Schema } from '../../app/api';
import { EmptyState, ErrorState, LoadingState } from '../../shared/States';
import { fieldTypeName } from '../schemas/schemaFieldTypes';

type Props = { initialSchemaId: string; onBrowseSchemas: () => void };

export function GenerateDataPage({ initialSchemaId, onBrowseSchemas }: Props) {
  const [schemas, setSchemas] = useState<Schema[]>([]);
  const [schemaId, setSchemaId] = useState(initialSchemaId);
  const [count, setCount] = useState(10);
  const [records, setRecords] = useState<Record<string, unknown>[] | null>(null);
  const [loadingSchemas, setLoadingSchemas] = useState(true);
  const [generating, setGenerating] = useState(false);
  const [error, setError] = useState('');
  const [copied, setCopied] = useState(false);
  const selected = useMemo(() => schemas.find(schema => schema.id === schemaId), [schemaId, schemas]);

  useEffect(() => {
    schemaApi.list().then(result => {
      setSchemas(result);
      if (!result.some(schema => schema.id === initialSchemaId)) setSchemaId(result[0]?.id ?? '');
    }).catch(err => setError(err instanceof Error ? err.message : 'Could not load schemas.'))
      .finally(() => setLoadingSchemas(false));
  }, [initialSchemaId]);

  async function generate(event: React.FormEvent) {
    event.preventDefault();
    if (!schemaId) return;
    setGenerating(true); setError(''); setRecords(null); setCopied(false);
    try { setRecords(await schemaApi.generate(schemaId, count)); }
    catch (err) { setError(err instanceof Error ? err.message : 'Could not generate data.'); }
    finally { setGenerating(false); }
  }

  async function copy() {
    if (!records) return;
    try { await navigator.clipboard.writeText(JSON.stringify(records, null, 2)); setCopied(true); }
    catch { setError('Clipboard access is unavailable in this browser.'); }
  }

  return <>
    <div className="page-heading"><div><p className="eyebrow">SAMPLE DATA</p><h1>Generate data</h1><p className="page-description">Create sample records from one of your schemas.</p></div><span className="status-pill"><span /> API-backed</span></div>
    {error && <ErrorState>{error}</ErrorState>}
    {loadingSchemas ? <section className="panel"><LoadingState>Loading schemas…</LoadingState></section> : schemas.length === 0 ? <section className="panel"><EmptyState><div><h2>No schemas available</h2><p>Create a schema first, then come back to generate records.</p><button className="button button-primary" onClick={onBrowseSchemas}>Go to schemas</button></div></EmptyState></section> : <div className="generation-layout">
      <section className="panel generation-form-panel"><div className="panel-heading"><div><h2>Configure generation</h2><p>Select a data shape and sample size.</p></div><span className="step-badge">02</span></div>
        <form className="generation-form" onSubmit={event => void generate(event)}>
          <label className="input-label" htmlFor="schema-select">Schema</label>
          <select id="schema-select" className="select-input full-width" value={schemaId} onChange={event => { setSchemaId(event.target.value); setRecords(null); }} required>{schemas.map(schema => <option key={schema.id} value={schema.id}>{schema.name}</option>)}</select>
          {selected && <div className="selected-schema-summary"><strong>{selected.fields.length} fields</strong><span>{selected.fields.map(field => `${field.fieldName} · ${fieldTypeName(field.fieldType)}`).join('  /  ')}</span></div>}
          <label className="input-label" htmlFor="record-count">Number of records</label>
          <div className="count-control"><button type="button" aria-label="Decrease record count" disabled={count <= 1} onClick={() => setCount(value => Math.max(1, value - 1))}>−</button><input id="record-count" type="number" min="1" value={count} onChange={event => setCount(Math.max(1, Number(event.target.value) || 1))} /><button type="button" aria-label="Increase record count" onClick={() => setCount(value => value + 1)}>＋</button></div>
          <p className="helper-text">Enter the number of records to generate.</p>
          <button className="button button-primary create-submit" disabled={generating || !schemaId}>{generating ? 'Generating…' : 'Generate records'} <span aria-hidden="true">→</span></button>
        </form>
      </section>
      <section className="panel results-panel"><div className="panel-heading"><div><h2>Output</h2><p>{records ? `${records.length} records generated` : 'Your generated JSON will appear here.'}</p></div>{records && <button className="button button-quiet" onClick={() => void copy()}>{copied ? 'Copied ✓' : 'Copy JSON'}</button>}</div>
        {generating ? <LoadingState>Generating records…</LoadingState> : records ? <pre className="json-output">{JSON.stringify(records, null, 2)}</pre> : <div className="output-placeholder"><div className="code-mark">{'{ }'}</div><p>Ready when you are</p><span>Choose a schema and generate sample records.</span></div>}
      </section>
    </div>}
  </>;
}

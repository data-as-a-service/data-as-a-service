import { useEffect, useMemo, useState } from 'react';
import { schemaApi, type Schema } from '../../app/api';
import { ErrorState, LoadingState } from '../../shared/States';
import { fieldTypeName } from '../schemas/schemaFieldTypes';

type Props = { initialSchemaId: string; onBrowseSchemas: () => void };
type GenerationMode = 'random' | 'ai';

export function GenerateDataPage({ initialSchemaId, onBrowseSchemas }: Props) {
  const [schemas, setSchemas] = useState<Schema[]>([]);
  const [schemaId, setSchemaId] = useState(initialSchemaId);
  const [count, setCount] = useState(10);
  const [mode, setMode] = useState<GenerationMode>('random');
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
    try { setRecords(await schemaApi.generateWithMode(schemaId, count, mode === 'ai' ? 1 : 0)); }
    catch (err) { setError(err instanceof Error ? err.message : 'Could not generate data.'); }
    finally { setGenerating(false); }
  }

  async function copy() {
    if (!records) return;
    try { await navigator.clipboard.writeText(JSON.stringify(records, null, 2)); setCopied(true); }
    catch { setError('Clipboard access is unavailable in this browser.'); }
  }

  return <>
    <div className="page-heading">
      <div><p className="eyebrow">DATA WORKBENCH</p><h1>Generate data</h1><p className="page-description">Choose a schema and create ready-to-use JSON in seconds.</p></div>
      <span className="status-pill"><span /> {mode === 'ai' ? 'AI with random fallback' : 'Random generator'}</span>
    </div>
    {error && <ErrorState>{error}</ErrorState>}
    {loadingSchemas ? <section className="panel"><LoadingState>Loading schemas…</LoadingState></section> : schemas.length === 0 ? <section className="panel empty-panel"><div className="empty-illustration">{'{ }'}</div><h2>Start with a schema</h2><p>Create a data shape first. Then choose random or AI assisted generation here.</p><button className="button button-primary" onClick={onBrowseSchemas}>Create a schema <span>→</span></button></section> : <div className="generation-layout">
      <section className="panel generation-form-panel">
        <div className="panel-heading"><div><p className="eyebrow">CONFIGURE</p><h2>Your data recipe</h2><p>Select a schema, generation style, and record count.</p></div><span className="step-badge">01</span></div>
        <form className="generation-form" onSubmit={event => void generate(event)}>
          <label className="input-label" htmlFor="schema-select">Schema</label>
          <select id="schema-select" className="select-input full-width" value={schemaId} onChange={event => { setSchemaId(event.target.value); setRecords(null); }} required>{schemas.map(schema => <option key={schema.id} value={schema.id}>{schema.name}</option>)}</select>
          {selected && <div className="selected-schema-summary"><strong>{selected.fields.length} {selected.fields.length === 1 ? 'field' : 'fields'}</strong><span>{selected.fields.map(field => `${field.fieldName} · ${fieldTypeName(field.fieldType)}`).join('  /  ')}</span></div>}

          <span className="input-label mode-label">Generation style</span>
          <div className="mode-options" aria-label="Generation style">
            <button type="button" aria-pressed={mode === 'random'} className={`mode-card ${mode === 'random' ? 'selected' : ''}`} onClick={() => { setMode('random'); setRecords(null); }}>
              <span className="mode-icon">✳</span><span><strong>Random</strong><small>Fast, built-in, no AI service needed.</small></span><span className="radio-dot" />
            </button>
            <button type="button" aria-pressed={mode === 'ai'} className={`mode-card ${mode === 'ai' ? 'selected' : ''}`} onClick={() => { setMode('ai'); setRecords(null); }}>
              <span className="mode-icon ai-icon">✦</span><span><strong>AI assisted</strong><small>More realistic values; falls back to random if unavailable.</small></span><span className="radio-dot" />
            </button>
          </div>
          <p className="helper-text">AI uses the provider configured by the DaaS operator. Prompts and credentials stay on the server.</p>

          <label className="input-label count-label" htmlFor="record-count">Number of records</label>
          <div className="count-control"><button type="button" aria-label="Decrease record count" disabled={count <= 1} onClick={() => setCount(value => Math.max(1, value - 1))}>−</button><input id="record-count" type="number" min="1" max="1000" value={count} onChange={event => setCount(Math.min(1000, Math.max(1, Number(event.target.value) || 1)))} /><button type="button" aria-label="Increase record count" disabled={count >= 1000} onClick={() => setCount(value => Math.min(1000, value + 1))}>＋</button></div>
          <p className="helper-text">Choose from 1 to 1,000 records.</p>
          <button className="button button-primary create-submit" disabled={generating || !schemaId}>{generating ? 'Creating your data…' : mode === 'ai' ? 'Generate with AI' : 'Generate records'} <span aria-hidden="true">→</span></button>
        </form>
      </section>
      <section className="panel results-panel">
        <div className="panel-heading"><div><p className="eyebrow">PREVIEW</p><h2>JSON output</h2><p>{records ? `${records.length} records · ${mode === 'ai' ? 'AI requested, fallback enabled' : 'Random'}` : 'Generated data will appear here.'}</p></div>{records && <button className="button button-quiet" onClick={() => void copy()}>{copied ? 'Copied ✓' : 'Copy JSON'}</button>}</div>
        {generating ? <LoadingState>{mode === 'ai' ? 'Asking the configured AI provider…' : 'Generating sample records…'}</LoadingState> : records ? <><div className="output-toolbar"><span className="output-dot" /> Valid JSON <span className="output-records">{records.length} records</span></div><pre className="json-output">{JSON.stringify(records, null, 2)}</pre></> : <div className="output-placeholder"><div className="code-mark">{'{ }'}</div><p>Your preview is ready to be generated</p><span>Pick a schema and generation style to get started.</span></div>}
      </section>
    </div>}
  </>;
}

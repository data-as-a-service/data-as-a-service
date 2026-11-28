import { useState, type FormEvent } from 'react';
import { schemaApi } from '../../app/api';
import { Icon } from '../../shared/Icon';
import { ErrorState, LoadingState } from '../../shared/States';
import { useSchemaChoices } from '../schemas/useSchemaChoices';
import { fieldTypeName } from '../schemas/schemaFieldTypes';

export function GenerateDataPage({ initialSchemaId }: { initialSchemaId: string }) {
  const choices = useSchemaChoices(initialSchemaId);
  const [count, setCount] = useState('10');
  const [mode, setMode] = useState<0 | 1>(0);
  const [records, setRecords] = useState<Record<string, unknown>[] | null>(null);
  const [generating, setGenerating] = useState(false);
  const [error, setError] = useState('');
  const [copied, setCopied] = useState(false);
  const selected = choices.schemas.find(schema => schema.id === choices.schemaId);

  async function generate(event: FormEvent) {
    event.preventDefault();
    setGenerating(true); setRecords(null); setError(''); setCopied(false);
    try { setRecords(await schemaApi.generateWithMode(choices.schemaId, Number(count), mode)); }
    catch (err) { setError(err instanceof Error ? err.message : 'Could not generate records.'); }
    finally { setGenerating(false); }
  }

  async function copy() {
    try { await navigator.clipboard.writeText(JSON.stringify(records, null, 2)); setCopied(true); }
    catch { setError('Select the JSON and copy it manually. Clipboard access is unavailable.'); }
  }

  return <>
    <header className="page-heading"><div><h1>Data generator</h1><p>Run a schema and inspect the JSON response.</p></div></header>
    {choices.error ? <ErrorState>{choices.error} <button className="text-action" onClick={choices.retry}>Try again</button></ErrorState> :
      choices.loading ? <LoadingState>Loading schemas…</LoadingState> :
      !selected ? <div className="panel blank"><Icon name="schemas" /><h2>A schema comes first</h2><p>Define the fields you want in each record.</p><a className="button button-primary" href="#schemas">Open schema library</a></div> :
      <div className="workbench">
        <section className="panel"><div className="panel-heading"><h2>Sample settings</h2></div>
          <form onSubmit={event => void generate(event)} onChange={() => { setRecords(null); setError(''); setCopied(false); }}>
            <fieldset className="form-stack" disabled={generating}>
              <label>Schema<select value={choices.schemaId} onChange={event => choices.setSchemaId(event.target.value)}>{choices.schemas.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
              <details className="sample-fields"><summary>Inspect {selected.fields.length} fields</summary>
                <dl>{selected.fields.map((field, index) => <div key={field.id ?? index}><dt>{field.fieldName}</dt><dd>{fieldTypeName(field.fieldType)}</dd></div>)}</dl>
              </details>
              <label>Record count<input type="number" required min={1} max={1000} value={count} onChange={event => setCount(event.target.value)} /><small>1 to 1,000 records per sample.</small></label>
              <fieldset className="mode-picker"><legend>Generation mode</legend>
                <label><input type="radio" name="mode" checked={mode === 0} onChange={() => setMode(0)} /><span><strong>Random</strong><small>Generate values by field type.</small></span></label>
                <label><input type="radio" name="mode" checked={mode === 1} onChange={() => setMode(1)} /><span><strong>AI assisted</strong><small>Uses random values if the AI provider is unavailable.</small></span></label>
              </fieldset>
              <button className="button button-primary">{generating ? 'Generating…' : 'Generate records'}<Icon name="arrow" /></button>
            </fieldset>
          </form>
        </section>
        <section className="inspector output" aria-label="JSON response">
          <header><span><Icon name="generate" />Response preview</span><span className="format-tag">JSON</span></header>
          {error && <ErrorState>{error}</ErrorState>}
          {generating ? <LoadingState>Generating records…</LoadingState> : records ?
            <><div className="output-tools"><span role="status">{records.length} records</span><button className="button invert" onClick={() => void copy()}>{copied ? 'Copied' : 'Copy JSON'}</button></div>
              <pre className="json-output" tabIndex={0}>{JSON.stringify(records, null, 2)}</pre></> :
            <div className="inspector-empty"><span aria-hidden="true">[ ]</span><h2>Awaiting a sample</h2><p>Generate records from {selected.name} to preview them here.</p></div>}
        </section>
      </div>}
  </>;
}

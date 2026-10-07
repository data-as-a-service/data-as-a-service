import { useState, type FormEvent } from 'react';
import { schemaApi, type SchemaField } from '../../app/api';
import { ErrorState } from '../../shared/States';

// Only schema INT (0) aligns with a generator mapping (FieldType.Int = 0).
// The stored and generator enums currently diverge for the other values.
const GENERATABLE_TYPES = [{ label: 'Integer', value: 0 }];

type Props = { onCreated: () => Promise<void> };

export function CreateSchemaForm({ onCreated }: Props) {
  const [name, setName] = useState('');
  const [fields, setFields] = useState<Pick<SchemaField, 'fieldName' | 'fieldType'>[]>([{ fieldName: '', fieldType: 0 }]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  function updateField(index: number, change: Partial<Pick<SchemaField, 'fieldName' | 'fieldType'>>) {
    setFields(current => current.map((field, i) => i === index ? { ...field, ...change } : field));
  }

  async function submit(event: FormEvent) {
    event.preventDefault();
    const cleanedName = name.trim();
    const cleanedFields = fields.map(field => ({ ...field, fieldName: field.fieldName.trim() }));
    if (!cleanedName || cleanedFields.some(field => !field.fieldName)) {
      setError('Add a schema name and a name for every field.');
      return;
    }
    setSaving(true); setError(''); setSuccess('');
    try {
      const result = await schemaApi.create({ name: cleanedName, fields: cleanedFields });
      setName(''); setFields([{ fieldName: '', fieldType: 0 }]);
      setSuccess(`Schema created (${result.id}).`);
      await onCreated();
    } catch (err) { setError(err instanceof Error ? err.message : 'Could not create schema.'); }
    finally { setSaving(false); }
  }

  return <form className="schema-form" onSubmit={event => void submit(event)}>
    {error && <ErrorState>{error}</ErrorState>}{success && <div className="notice success-notice" role="status">{success}</div>}
    <label className="input-label" htmlFor="schema-name">Schema name</label>
    <input id="schema-name" className="text-input" value={name} onChange={event => setName(event.target.value)} placeholder="e.g. Customer profile" maxLength={120} required />
    <div className="fields-heading"><span className="input-label">Fields</span><span className="muted">{fields.length} added</span></div>
    <div className="field-editor-list">{fields.map((field, index) => <div className="field-editor" key={index}>
      <input className="text-input" aria-label={`Field ${index + 1} name`} value={field.fieldName} onChange={event => updateField(index, { fieldName: event.target.value })} placeholder="Field name" maxLength={120} required />
      <select className="select-input" aria-label={`Field ${index + 1} type`} value={field.fieldType} onChange={event => updateField(index, { fieldType: Number(event.target.value) })}>{GENERATABLE_TYPES.map((type, typeIndex) => <option key={`${type.label}-${typeIndex}`} value={type.value}>{type.label}</option>)}</select>
      <button type="button" className="icon-button remove-field" aria-label={`Remove field ${index + 1}`} disabled={fields.length === 1} onClick={() => setFields(current => current.filter((_, i) => i !== index))}>×</button>
    </div>)}</div>
    <button type="button" className="add-field" onClick={() => setFields(current => [...current, { fieldName: '', fieldType: 0 }])}><span>＋</span> Add field</button>
    <button className="button button-primary create-submit" disabled={saving}>{saving ? 'Creating…' : 'Create schema'} <span aria-hidden="true">→</span></button>
  </form>;
}

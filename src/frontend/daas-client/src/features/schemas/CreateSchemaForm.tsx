import { useState, type FormEvent } from 'react';
import { schemaApi } from '../../app/api';
import { ErrorState } from '../../shared/States';
import { Icon } from '../../shared/Icon';
import { GENERATABLE_FIELD_TYPES } from './schemaFieldTypes';

export function CreateSchemaForm({ onCreated }: { onCreated: (id: string, name: string) => Promise<void> }) {
  const [name, setName] = useState('');
  const [fields, setFields] = useState([{ fieldName: '', fieldType: 0 }]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  async function submit(event: FormEvent) {
    event.preventDefault();
    const cleaned = fields.map(field => ({ ...field, fieldName: field.fieldName.trim() }));
    if (!name.trim() || cleaned.some(field => !field.fieldName)) {
      setError('Enter a schema name and a name for each field.'); return;
    }
    if (new Set(cleaned.map(field => field.fieldName)).size !== cleaned.length) {
      setError('Give each field a unique name so every value has its own JSON property.'); return;
    }
    setSaving(true); setError('');
    try {
      const result = await schemaApi.create({ name: name.trim(), fields: cleaned });
      await onCreated(result.id, name.trim());
    } catch (err) { setError(err instanceof Error ? err.message : 'Could not save schema.'); }
    finally { setSaving(false); }
  }

  return <form onSubmit={event => void submit(event)}>
    {error && <ErrorState>{error}</ErrorState>}
    <fieldset className="form-stack" disabled={saving}>
      <label>Schema name<input autoFocus value={name} onChange={event => setName(event.target.value)} placeholder="e.g. Customer" maxLength={120} required /></label>
      <div className="fields-heading"><strong>Field definitions</strong><span>{fields.length}</span></div>
      {fields.map((field, index) => <div className="field-editor" key={index}>
        <label>Name<input aria-label={'Field ' + (index + 1) + ' name'} placeholder="e.g. customerId" value={field.fieldName} required maxLength={120}
          onChange={event => setFields(fields.map((item, i) => i === index ? { ...item, fieldName: event.target.value } : item))} /></label>
        <label>Type<select aria-label={'Field ' + (index + 1) + ' type'} value={field.fieldType}
          onChange={event => setFields(fields.map((item, i) => i === index ? { ...item, fieldType: Number(event.target.value) } : item))}>
          {GENERATABLE_FIELD_TYPES.map(type => <option key={type.value} value={type.value}>{type.label}</option>)}</select></label>
        <button type="button" className="icon-button" disabled={fields.length === 1} aria-label={'Remove field ' + (index + 1)}
          onClick={() => setFields(fields.filter((_, i) => i !== index))}><Icon name="close" /></button>
      </div>)}
      <button type="button" className="button button-quiet" onClick={() => setFields([...fields, { fieldName: '', fieldType: 0 }])}><Icon name="plus" />Add field</button>
      <button className="button button-primary">{saving ? 'Saving…' : 'Save schema'}</button>
    </fieldset>
  </form>;
}

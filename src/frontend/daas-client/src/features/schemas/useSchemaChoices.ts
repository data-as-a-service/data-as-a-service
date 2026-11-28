import { useEffect, useState } from 'react';
import { schemaApi, type Schema } from '../../app/api';

export function useSchemaChoices(initialId: string) {
  const [schemas, setSchemas] = useState<Schema[]>([]);
  const [schemaId, setSchemaId] = useState(initialId);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [attempt, setAttempt] = useState(0);
  useEffect(() => {
    let active = true;
    setLoading(true); setError('');
    schemaApi.list().then(items => {
      if (!active) return;
      setSchemas(items);
      setSchemaId(current => items.some(item => item.id === current) ? current : items[0]?.id ?? '');
    }).catch(err => { if (active) setError(err instanceof Error ? err.message : 'Could not load schemas.'); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [attempt]);
  return { schemas, schemaId, setSchemaId, loading, error, retry: () => setAttempt(value => value + 1) };
}

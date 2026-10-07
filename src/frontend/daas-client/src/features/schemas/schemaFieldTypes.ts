export const GENERATABLE_FIELD_TYPES = [
  { label: 'Integer', value: 0 },
  { label: 'Float', value: 1 },
  { label: 'Boolean', value: 2 },
  { label: 'String', value: 3 },
  { label: 'Character', value: 4 },
  { label: 'GUID', value: 5 },
  { label: 'Date', value: 6 },
  { label: 'Double', value: 7 },
] as const;

const fieldTypeNames = new Map<number, string>(
  GENERATABLE_FIELD_TYPES.map(({ label, value }) => [value, label.toUpperCase()]),
);

export function fieldTypeName(value: number) {
  return fieldTypeNames.get(value) ?? `UNSUPPORTED (${value})`;
}

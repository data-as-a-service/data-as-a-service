const paths = {
  schemas: 'M3 6h7l2 2h9v12H3z M3 6V4h8l2 2h6',
  generate: 'm8 7-5 5 5 5 M16 7l5 5-5 5 M14 4l-4 16',
  links: 'm10 14 4-4 M8 16l-1 1a3.5 3.5 0 0 1-5-5l4-4a3.5 3.5 0 0 1 5 0 M16 8l1-1a3.5 3.5 0 1 1 5 5l-4 4a3.5 3.5 0 0 1-5 0',
  search: 'M17 10a7 7 0 1 1-14 0 7 7 0 0 1 14 0z M15 15l6 6',
  arrow: 'M4 12h15 M13 6l6 6-6 6',
  plus: 'M12 5v14 M5 12h14',
  close: 'm6 6 12 12 M18 6 6 18',
  guide: 'M12 6C9 4 5 4 2 5v14c4-1 7-1 10 1 3-2 6-2 10-1V5c-3-1-7-1-10 1z M12 6v14',
} as const;

export function Icon({ name }: { name: keyof typeof paths }) {
  return <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor"
    strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
    <path d={paths[name]} />
  </svg>;
}

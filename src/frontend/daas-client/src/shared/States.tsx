type StateProps = { children: React.ReactNode };

export function LoadingState({ children }: StateProps) {
  return <div className="state" role="status"><span className="spinner" />{children}</div>;
}

export function EmptyState({ children }: StateProps) {
  return <div className="state empty-state">{children}</div>;
}

export function ErrorState({ children }: StateProps) {
  return <div className="notice error-notice" role="alert">{children}</div>;
}

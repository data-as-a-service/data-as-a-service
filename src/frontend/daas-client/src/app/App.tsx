import { useEffect, useState } from 'react';
import { schemaApi } from './api';
import { GenerateDataPage } from '../features/generation/GenerateDataPage';
import { SchemaListPage } from '../features/schemas/SchemaListPage';
import { ApiLinksPage } from '../features/links/ApiLinksPage';

type Page = 'schemas' | 'generate' | 'links';

export function App() {
  const [page, setPage] = useState<Page>('schemas');
  const [schemaId, setSchemaId] = useState('');
  const [apiStatus, setApiStatus] = useState<'checking' | 'connected' | 'unavailable'>('checking');

  useEffect(() => {
    let active = true;
    const checkApi = () => {
      void schemaApi.list().then(
        () => { if (active) setApiStatus('connected'); },
        () => { if (active) setApiStatus('unavailable'); },
      );
    };

    checkApi();
    const interval = window.setInterval(checkApi, 30_000);
    return () => { active = false; window.clearInterval(interval); };
  }, []);

  function goGenerate(id = '') { setSchemaId(id); setPage('generate'); }

  return <div className="app-shell">
    <aside className="sidebar">
      <a className="brand" href="#schemas" onClick={event => { event.preventDefault(); setPage('schemas'); }}><span className="brand-mark">d</span><span>DaaS<span className="brand-sub">STUDIO</span></span></a>
      <div className="nav-label">WORKSPACE</div>
      <nav aria-label="Main navigation">
        <button className={`nav-item ${page === 'schemas' ? 'active' : ''}`} onClick={() => setPage('schemas')}><span className="nav-icon">▦</span>Schemas</button>
        <button className={`nav-item ${page === 'generate' ? 'active' : ''}`} onClick={() => goGenerate()}><span className="nav-icon">✳</span>Generate data</button>
        <button className={`nav-item ${page === 'links' ? 'active' : ''}`} onClick={() => setPage('links')}><span className="nav-icon">↗</span>API links</button>
      </nav>
      <div className="sidebar-bottom"><div className="sidebar-help-mark">?</div><div><strong>Need a hand?</strong><span>Explore the API docs</span></div><a href="/swagger" target="_blank" rel="noreferrer" aria-label="Open API documentation">↗</a></div>
    </aside>
    <main className="main-area">
      <header className="topbar"><div className="breadcrumb">Workspace <span>/</span> <strong>{page === 'schemas' ? 'Schemas' : page === 'links' ? 'API links' : 'Generate data'}</strong></div><div className={`topbar-right api-connection api-connection-${apiStatus}`} role="status" aria-live="polite"><span className="environment-dot" /> {apiStatus === 'checking' ? 'Checking API…' : apiStatus === 'connected' ? 'API connected' : 'API unavailable'}</div></header>
      <div className="content-area">{page === 'schemas' ? <SchemaListPage onGenerate={goGenerate} /> : page === 'links' ? <ApiLinksPage /> : <GenerateDataPage initialSchemaId={schemaId} onBrowseSchemas={() => setPage('schemas')} />}</div>
      <footer className="footer"><span>DAAS CONSOLE</span><span>Powered by your schema definitions</span></footer>
    </main>
  </div>;
}

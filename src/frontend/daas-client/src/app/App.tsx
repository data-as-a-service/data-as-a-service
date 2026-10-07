import { useState } from 'react';
import { GenerateDataPage } from '../features/generation/GenerateDataPage';
import { SchemaListPage } from '../features/schemas/SchemaListPage';

type Page = 'schemas' | 'generate';

export function App() {
  const [page, setPage] = useState<Page>('schemas');
  const [schemaId, setSchemaId] = useState('');

  function goGenerate(id = '') { setSchemaId(id); setPage('generate'); }

  return <div className="app-shell">
    <aside className="sidebar">
      <a className="brand" href="#schemas" onClick={event => { event.preventDefault(); setPage('schemas'); }}><span className="brand-mark">d</span><span>DaaS<span className="brand-sub">STUDIO</span></span></a>
      <div className="nav-label">WORKSPACE</div>
      <nav aria-label="Main navigation">
        <button className={`nav-item ${page === 'schemas' ? 'active' : ''}`} onClick={() => setPage('schemas')}><span className="nav-icon">▦</span>Schemas</button>
        <button className={`nav-item ${page === 'generate' ? 'active' : ''}`} onClick={() => goGenerate()}><span className="nav-icon">✳</span>Generate data</button>
      </nav>
      <div className="sidebar-bottom"><div className="sidebar-help-mark">?</div><div><strong>Need a hand?</strong><span>Explore the API docs</span></div><a href="/swagger" target="_blank" rel="noreferrer" aria-label="Open API documentation">↗</a></div>
    </aside>
    <main className="main-area">
      <header className="topbar"><div className="breadcrumb">Workspace <span>/</span> <strong>{page === 'schemas' ? 'Schemas' : 'Generate data'}</strong></div><div className="topbar-right"><span className="environment-dot" /> Local workspace</div></header>
      <div className="content-area">{page === 'schemas' ? <SchemaListPage onGenerate={goGenerate} /> : <GenerateDataPage initialSchemaId={schemaId} onBrowseSchemas={() => setPage('schemas')} />}</div>
      <footer className="footer"><span>DAAS CONSOLE</span><span>Powered by your schema definitions</span></footer>
    </main>
  </div>;
}

import { useEffect, useState } from 'react';
import { schemaApi } from './api';
import { GenerateDataPage } from '../features/generation/GenerateDataPage';
import { SchemaListPage } from '../features/schemas/SchemaListPage';
import { ApiLinksPage } from '../features/links/ApiLinksPage';

type Page = 'schemas' | 'generate' | 'links';

const navigation: { id: Page; label: string }[] = [
  { id: 'schemas', label: 'Schemas' },
  { id: 'generate', label: 'Generate data' },
  { id: 'links', label: 'API links' },
];

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
    <header className="site-header">
      <a className="brand" href="#schemas" onClick={event => { event.preventDefault(); setPage('schemas'); }} aria-label="Data as a Service home">
        <img className="brand-mark" src="/assets/260086583.png" alt="" />
        <span className="brand-name">data<span>as a service</span></span>
      </a>
      <nav className="top-navigation" aria-label="Main navigation">
        {navigation.map(item => <button key={item.id} className={`nav-item ${page === item.id ? 'active' : ''}`} aria-current={page === item.id ? 'page' : undefined} onClick={() => item.id === 'generate' ? goGenerate() : setPage(item.id)}>{item.label}</button>)}
      </nav>
      <div className={`header-status api-connection-${apiStatus}`} role="status" aria-live="polite"><span className="environment-dot"/><span>{apiStatus === 'checking' ? 'Connecting…' : apiStatus === 'connected' ? 'Connected' : 'API unavailable'}</span></div>
    </header>
    <main className="main-area">
      <div className="content-area" id="main-content">{page === 'schemas' ? <SchemaListPage onGenerate={goGenerate} /> : page === 'links' ? <ApiLinksPage /> : <GenerateDataPage initialSchemaId={schemaId} onBrowseSchemas={() => setPage('schemas')} />}</div>
      <footer className="footer"><span>DATA AS A SERVICE</span><span>Turn a data shape into sample records or a shareable link.</span></footer>
    </main>
  </div>;
}

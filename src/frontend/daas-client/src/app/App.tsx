import { useEffect, useRef, useState } from 'react';
import { schemaApi } from './api';
import { GenerateDataPage } from '../features/generation/GenerateDataPage';
import { SchemaListPage } from '../features/schemas/SchemaListPage';
import { ApiLinksPage } from '../features/links/ApiLinksPage';
import { Icon } from '../shared/Icon';
import { Dialog } from '../shared/Dialog';

const navigation = [
  { id: 'schemas', label: 'Schema library' },
  { id: 'generate', label: 'Data generator' },
  { id: 'links', label: 'API links' },
] as const;

function readRoute() {
  const [section, query] = window.location.hash.slice(1).split('?');
  return {
    page: navigation.find(item => item.id === section) ?? navigation[0],
    schemaId: new URLSearchParams(query).get('schema') ?? '',
  };
}

export function App() {
  const [route, setRoute] = useState(readRoute);
  const [guide, setGuide] = useState(false);
  const main = useRef<HTMLElement>(null);
  const [status, setStatus] = useState('Connecting');

  useEffect(() => {
    const navigate = () => {
      setRoute(readRoute());
      main.current?.focus({ preventScroll: true });
      window.scrollTo(0, 0);
    };
    window.addEventListener('hashchange', navigate);
    return () => window.removeEventListener('hashchange', navigate);
  }, []);

  useEffect(() => {
    document.title = route.page.label + ' · DaaS Studio';
  }, [route.page]);

  useEffect(() => {
    let active = true;
    async function check() {
      try { await schemaApi.list(); if (active) setStatus('API connected'); }
      catch { if (active) setStatus('API unavailable'); }
    }
    void check();
    const timer = window.setInterval(check, 30000);
    return () => { active = false; window.clearInterval(timer); };
  }, []);

  return <div className="studio">
    <button className="skip" onClick={() => main.current?.focus()}>Skip to workspace</button>
    <aside className="sidebar">
      <a className="brand" href="#schemas" aria-label="DaaS Studio home">
        <img src="/assets/260086583.png" alt="" /><span>DaaS <b>Studio</b></span>
      </a>
      <nav aria-label="Workspace">{navigation.map(item =>
        <a key={item.id} href={'#' + item.id} aria-current={item.id === route.page.id ? 'page' : undefined}>
          <Icon name={item.id} />{item.label}
        </a>)}</nav>
      <button className="guide" onClick={() => setGuide(true)}><Icon name="guide" />Workspace guide</button>
    </aside>
    <div className="canvas">
      <header className="context-bar"><span>Workspace <i>/</i> <strong>{route.page.label}</strong></span>
        <span className="connection" role="status" data-state={status}><i />{status}</span>
      </header>
      <main ref={main} tabIndex={-1}>
        {route.page.id === 'schemas' ? <SchemaListPage /> : route.page.id === 'generate'
          ? <GenerateDataPage key={route.schemaId} initialSchemaId={route.schemaId} />
          : <ApiLinksPage key={route.schemaId} initialSchemaId={route.schemaId} />}
      </main>
    </div>
    {guide && <Dialog title="Workspace guide" onClose={() => setGuide(false)}>
      <ol className="guide-list">
        <li><strong>Define a schema</strong><p>Give your response a name and choose a type for each field.</p></li>
        <li><strong>Generate a sample</strong><p>Choose random or AI-assisted values, then copy the JSON into your project.</p></li>
        <li><strong>Share a dataset</strong><p>Create a public API link to serve saved records. Copy the URL when it appears. Rotating it replaces the URL.</p></li>
      </ol>
    </Dialog>}
  </div>;
}

const form = document.querySelector('#create');
const list = document.querySelector('#schemas');
const fields = document.querySelector('#fieldsContainer');
const message = document.querySelector('#message');
const responsePanel = document.querySelector('#response');

async function request(path, options) {
  const response = await fetch(path, options);
  if (!response.ok) throw new Error('The API returned HTTP ' + response.status + '.');
  return response.status === 204 ? null : response.json();
}

function addField() {
  fields.append(document.querySelector('#field').content.cloneNode(true));
}

function action(label, run) {
  const button = document.createElement('button');
  button.textContent = label;
  button.addEventListener('click', async () => {
    button.disabled = true;
    try { await run(); }
    catch (error) { message.textContent = error.message; }
    finally { button.disabled = false; }
  });
  return button;
}

async function loadSchemas() {
  try {
    const schemas = await request('/api/schema');
    list.replaceChildren();
    if (!schemas.length) list.textContent = 'Create your first schema to generate records.';
    for (const schema of schemas) {
      const row = document.createElement('article');
      const name = document.createElement('h3');
      name.textContent = schema.name;
      const summary = document.createElement('p');
      summary.textContent = schema.fields.length + ' fields';
      row.append(name, summary, action('Generate records', async () => {
        const data = await request('/api/schema/' + encodeURIComponent(schema.id) + '/data/10');
        document.querySelector('#generatedData').textContent = JSON.stringify(data, null, 2);
        responsePanel.hidden = false;
        responsePanel.scrollIntoView();
        message.textContent = 'Generated records for ' + schema.name + '.';
      }), action('Delete', async () => {
        if (!window.confirm('Delete ' + schema.name + ' and its API links?')) return;
        await request('/api/schema/' + encodeURIComponent(schema.id), { method: 'DELETE' });
        message.textContent = 'Schema deleted.';
        await loadSchemas();
      }));
      list.append(row);
    }
  } catch (error) {
    list.replaceChildren(document.createTextNode('Could not load schemas. '), action('Retry', loadSchemas));
  }
}

form.addEventListener('submit', async event => {
  event.preventDefault();
  const name = document.querySelector('#schemaName').value.trim();
  const definitions = Array.from(fields.children, row => ({
    fieldName: row.querySelector('input').value.trim(),
    fieldType: Number(row.querySelector('select').value),
  }));
  if (!name || definitions.some(field => !field.fieldName)) { message.textContent = 'Enter a name for the schema and each field.'; return; }
  form.querySelector('fieldset').disabled = true;
  try {
    await request('/api/schema', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ name, fields: definitions }) });
    form.reset(); fields.replaceChildren(); addField();
    message.textContent = name + ' created.';
    await loadSchemas();
  } catch (error) { message.textContent = error.message; }
  finally { form.querySelector('fieldset').disabled = false; }
});
document.querySelector('#addField').addEventListener('click', addField);
addField();
void loadSchemas();

async function loadSchemas() {

    const response =
        await fetch('/api/schema');

    const schemas =
        await response.json();

    console.log(schemas);
}

loadSchemas();
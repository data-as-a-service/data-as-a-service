async function loadSchemas() {

    const response =
        await fetch('/api/schema');

    const schemas =
        await response.json();

    const container =
        document.getElementById("schemas");

    container.innerHTML = "";

    schemas.forEach(schema => {

        container.innerHTML += `
            <div class="card mb-2">
                <div class="card-body">

                    <h5>${schema.name}</h5>

                    <p>${schema.fields.length} fields</p>

                    <button
                        class="btn btn-primary"
                        onclick="generateData('${schema.id}')">

                        Generate Data
                    </button>

                    <button
                        class="btn btn-danger"
                        onclick="deleteSchema('${schema.id}')">

                        Delete
                    </button>

                </div>
            </div>
        `;
    });
}

loadSchemas();
function generateData(id) {
    alert(id);
}

function deleteSchema(id) {
    alert(id);
}
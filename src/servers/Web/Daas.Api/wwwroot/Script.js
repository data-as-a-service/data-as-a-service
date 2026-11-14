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

async function deleteSchema(id) {
    await fetch(
        `/api/schema/${id}`,
        {
            method: 'DELETE'
        });

    loadSchemas();
}

async function generateData(id) {
    const response =
        await fetch(
            `/api/schema/${id}/data/10`
        );

    const data =
        await response.json();

    document.getElementById(
        "generatedData"
    ).textContent =
        JSON.stringify(data, null, 2);
}

function addField() {
    const container =
        document.getElementById(
            "fieldsContainer"
        );

    container.innerHTML += `
        <div class="row mb-2">

            <div class="col">
                <input
                    class="form-control fieldName"
                    placeholder="Field Name">
            </div>

            <div class="col">
                <select
                    class="form-select fieldType">
                    <option value="0">INT</option>
                    <option value="1">Float</option>
                    <option value="2">Boolean</option>
                    <option value="3">String</option>
                    <option value="4">Character</option>
                    <option value="5">GUID</option>
                    <option value="6">Date</option>
                    <option value="7">Double</option>
                </select>
            </div>

        </div>
    `;
}

addField();

async function createSchema() {
    const schemaName =
        document.getElementById(
            "schemaName"
        ).value;

    const fieldNames =
        document.querySelectorAll(
            ".fieldName"
        );

    const fieldTypes =
        document.querySelectorAll(
            ".fieldType"
        );

    const fields = [];

    for (let i = 0; i < fieldNames.length; i++) {
        fields.push({
            fieldName:
                fieldNames[i].value,

            fieldType:
                parseInt(
                    fieldTypes[i].value
                )
        });
    }

    const schema = {
        name: schemaName,
        fields: fields
    };

    await fetch(
        "/api/schema",
        {
            method: "POST",

            headers: {
                "Content-Type":
                    "application/json"
            },

            body:
                JSON.stringify(schema)
        });

    loadSchemas();
}
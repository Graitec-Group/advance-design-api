# Advance Design API 2027.1

The **Advance Design API** is a REST API that lets you automate [Advance Design](https://www.graitec.com/advance-design/) programmatically. You can create, edit and save projects, define materials, sections and elements, apply loads, generate combinations, run analyses and Expert design calculations, and retrieve the results, all without opening the Advance Design UI.

This folder provides everything you need to get started: an OpenAPI schema, documentation, ready-to-run C# samples and a Python client.

---

## What's New in 2027.1

2027.1 adds **20 new endpoints** (22 → 42) and removes or renames nothing, so integrations built against 2027 keep working. The main additions:

- **Update and delete** elements: full CRUD on structural and informational elements.
- **Expert design**: design templates, design properties on combinations, `LaunchDesignCalculation` and `GetDesignResults`.
- **Automatic load combinations** and **model settings** (get / set / reset).
- **User-defined cross-sections**, **structural systems and levels**, **edge release templates**, and **points and poly lines**.
- **Generalized buckling** analysis, with the new `GetResultsByQuery` endpoint.
- **Project, session and service control**: Save / Save As, close without saving, design standards per session, model status, and service version and status.

| Document | Description |
|---|---|
| [`API-2027.1-Overview.md`](API-2027.1-Overview.md) | Short overview of the new capabilities and what they make possible |
| [`API-2027.1-Delta.md`](API-2027.1-Delta.md) | Detailed comparison with 2027: every new and changed endpoint, every extended object, compatibility notes and a testing guide |

> Before you point an existing 2027 client at a 2027.1 server, read the [Compatibility](API-2027.1-Delta.md#compatibility) section. Two behaviours changed: planar contours are validated more strictly, and rigid punctual supports now have an explicit `constraintsType`.

---

## Repository Structure

```
v2027.1/
├── API-2027.1-Overview.md  # What's new in 2027.1, short version
├── API-2027.1-Delta.md     # What's new in 2027.1, detailed comparison with 2027
├── docs/                   # Documentation
│   ├── HELP.md             # Full developer reference (endpoints, data models, enumerations, examples)
│   └── Summary.md          # Condensed capabilities summary
├── Samples/
│   └── Test.API.Client/    # C# .NET 8 sample application
├── Python/
│   └── PythonClient.zip    # Python API client generated with Microsoft Kiota
└── schema OpenAPI/
    └── swagger.json        # OpenAPI 3.0 specification (Swagger)
```

---

## Getting Started

### 1. Explore the Documentation

| Document | Description |
|---|---|
| [`docs/Summary.md`](docs/Summary.md) | Quick overview of all available endpoints and supported types |
| [`docs/HELP.md`](docs/HELP.md) | Comprehensive developer reference: endpoints, request/response models, enumerations, error handling and full C# usage examples |

### 2. Run the C# Samples

The [`Samples/Test.API.Client`](Samples/Test.API.Client) folder contains a **.NET 8 application** with multiple ready-to-run scenarios:

- **Analysis results**: portal frames, slabs, walls, imposed displacements, support reaction verification, nodal displacements
- **Climatic load generation**: wind (EN 1991-1-4), snow (EN 1991-1-3), IBC/ASCE 7-22, NBC Canada
- **Model editing**: updating and deleting elements
- **Generalized buckling** results on an existing model
- **Materials, sections and load cases**: retrieval and user IDs
- **Error handling**: intentional material errors and custom properties on reopen

See the [Samples README](Samples/Test.API.Client/README.md) for how to build and run them. The full list of samples is in `SampleCatalog.cs` and in the application's menu.

### 3. Use the Python Client

[`Python/PythonClient.zip`](Python/PythonClient.zip) contains a Python API client generated with [Microsoft Kiota](https://learn.microsoft.com/en-us/openapi/kiota/overview) from the 2027.1 schema. It covers all 42 endpoints.

The generated code uses relative imports, so use it as a package. Extract the archive into a folder such as `ApiSdk/`, then import it with `from ApiSdk.api_client import ApiClient`. It needs the Kiota Python runtime:

```bash
pip install microsoft-kiota-abstractions microsoft-kiota-http microsoft-kiota-serialization-json microsoft-kiota-serialization-text microsoft-kiota-serialization-form microsoft-kiota-serialization-multipart
```

The schema declares no server URL, so set the base URL of your API server (for example `http://localhost:52000/`) on the request adapter.

### 4. Generate Your Own Client

You are free to use the OpenAPI schema located at [`schema OpenAPI/swagger.json`](schema%20OpenAPI/swagger.json) to generate a client in any language or framework of your choice. Tools such as [Microsoft Kiota](https://learn.microsoft.com/en-us/openapi/kiota/overview), [NSwag](https://github.com/RicoSuter/NSwag), [OpenAPI Generator](https://openapi-generator.tech/) and others can consume this file directly.

> If you generated a client from the 2027 schema, regenerate it from this one. A 2027 client cannot deserialize the new types and enumeration values that 2027.1 responses can carry.

---

## OpenAPI Schema

The full API contract is described in the **OpenAPI 3.0** specification file (`info.version` `1.27.1`):

```
schema OpenAPI/swagger.json
```

Import it into tools like **Swagger UI**, **Postman** or any OpenAPI-compatible client to browse endpoints, inspect request/response schemas and try out calls interactively.

---

## Prerequisites

- **Advance Design 2027.1** installed (the API server ships with the product)
- **.NET 8 SDK** (for the C# samples)
- **Python 3.10+** (for the Python client)

---

## License

Please refer to the GRAITEC Advance Design license terms for usage of this API.

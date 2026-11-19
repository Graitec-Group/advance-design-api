# Advance Design API 1.27.1 — Developer Guide

This guide explains how to drive Advance Design from your own program: create a structural model, calculate
it, and read the results. It applies to API version **1.27.1** (Advance Design 2027.1).

- New to the API? Read [Getting Started](#2-getting-started), then try
  [Example 1](#15-complete-c-usage-examples).
- Looking for one call? Go to the [endpoint reference](#3-api-endpoints-reference).
- Looking for one object or one value? Go to the data models (Sections 6 to 12) or to the
  [enumerations](#13-enumerations-reference).

`Summary.md`, next to this file, lists everything the API can do on a few pages.

---

## Table of Contents

1. [Overview](#1-overview)
2. [Getting Started](#2-getting-started)
3. [API Endpoints Reference](#3-api-endpoints-reference)
   - 3.1 [Project Management](#31-project-management)
   - 3.2 [Model — Materials](#32-model--materials)
   - 3.3 [Model — Sections](#33-model--sections)
   - 3.4 [Model — Elements (geometric)](#34-model--elements-geometric)
   - 3.5 [Model — Informational Elements](#35-model--informational-elements)
   - 3.6 [Model — Combinations](#36-model--combinations)
   - 3.7 [Model — General settings](#37-model--general-settings)
   - 3.8 [Model — Analysis](#38-model--analysis)
   - 3.9 [Model — Actions](#39-model--actions)
   - 3.10 [Model — Query & Retrieval](#310-model--query--retrieval)
   - 3.11 [Service — version, status & liveness](#311-service--version-status--liveness)
4. [Response Envelope Pattern](#4-response-envelope-pattern)
5. [Polymorphism & the `$type` Discriminator](#5-polymorphism--the-type-discriminator)
6. [Data Models — Materials](#6-data-models--materials)
   - 6.5 [Data Models — Sections](#65-data-models--sections)
7. [Data Models — Elements (geometric)](#7-data-models--elements-geometric)
8. [Data Models — Supports](#8-data-models--supports)
9. [Data Models — Loads](#9-data-models--loads)
10. [Data Models — Informational Elements (Load Cases & Families)](#10-data-models--informational-elements-load-cases--families)
    - 10.1 [Load Case Families](#101-load-case-families)
    - 10.2 [Load Cases](#102-load-cases)
    - 10.3 [Combinations](#103-combinations)
    - 10.4 [Structural Systems](#104-structural-systems)
    - 10.5 [Linear Release Templates](#105-linear-release-templates)
    - 10.6 [Expert Design Templates](#106-expert-design-templates)
11. [Data Models — Analysis Results](#11-data-models--analysis-results)
12. [Data Models — Climatic Parameters](#12-data-models--climatic-parameters)
13. [Enumerations Reference](#13-enumerations-reference)
14. [Error Handling & Diagnostics](#14-error-handling--diagnostics)
15. [Complete C# Usage Examples](#15-complete-c-usage-examples)
16. [Change History](#16-change-history)

---

## 1. Overview

The Advance Design API is a local HTTP (REST) service. It runs the Advance Design engine without the user
interface, and any program that can send HTTP requests can use it.

With the API you can:

- create, open, save and close Advance Design projects (`.fto`);
- build the model: materials, cross-sections, linear and planar elements, supports, points and poly lines;
- define loads, load cases and load case families, including climatic (wind, snow) and seismic ones;
- create combinations by hand or generate them automatically;
- organise the model: structural systems (building levels), edge releases, Expert design templates;
- change the general settings of the model (combination generation, steel design, tolerances, work space);
- run the finite-element analysis and the Expert design calculation (concrete, steel, timber, composite);
- read the results: displacements, forces, stresses, support reactions, the mesh, and the steel design
  verifications.

**What is delivered.** These files are in the Advance Design installation folder:

| File | What it is |
|---|---|
| `AD.API.Srv.exe` | The API server. Start it, then send requests to it |
| `AD.API.Client.dll` | The C# client (.NET 8). One method per endpoint, one class per object |
| `PythonClient.zip` | The Python client, with this guide |
| `swagger.json` | The OpenAPI 3.0 description of the API. Use it to generate a client for any other language, and as the complete reference of every property |

**Two kinds of endpoints.** `/api/Model/*` works on the open project. `/api/Service/*` tells you whether the
server is running and licensed.

---

## 2. Getting Started

### 2.1 Start the server

```text
AD.API.Srv.exe [/p <port>] [/w <folder>] [/console] [/h]
```

| Option | Description |
|---|---|
| `/p <port>` | HTTP port. Default: `52000` |
| `/w <folder>` | Working directory of the server. Default: the folder the server was started from |
| `/console` | Shows a console window with the server messages |
| `/h` | Shows the help and exits |

The server listens on `http://localhost:52000` by default. It uses HTTP only and accepts connections from
the local machine only.

**One server = one project.** A server holds one session and one open project, and it runs one request at
a time: requests sent in parallel wait in a queue. To work on several projects at the same time, start
several servers, each with its own port (`/p`) and its own working directory (`/w`).

**Working directory (`/w`).**

- A relative file name given to `NewProject`, `OpenProject` or `SaveProjectAs` is resolved against it.
- The folder must exist and be writable. If it is not, the server does not start (exit code `2`).
- The server writes its log, `Log.log`, in it, and its temporary files in the `Temp` sub-folder.
- Inside quotes, do not end the path with a backslash: write `/w "D:\Jobs\Batch 1"`, not `"D:\Jobs\Batch 1\"`.

**Exit codes.**

| Code | Meaning |
|---|---|
| `0` | Normal exit, after `CloseSession` |
| `2` | The command line was refused (invalid `/w`) |
| `3` | The session could not be closed cleanly (project not saved, or licence not released) |
| `4` | The port is already in use. Start the server on another port |

**Check that it works.**

```text
curl http://localhost:52000/api/Service/GetVersion
"1.27.1"
```

`GetVersion` tells you the server is running. `GetStatus` tells you whether it can work — in particular,
whether it has its licence. See [Section 3.11](#311-service--version-status--liveness).

### 2.2 Connect from C#

Reference `AD.API.Client.dll` and use the `AD_Client` class (namespace `AD.API.Client`).

```csharp
using AD.API.Client;

var http   = new HttpClient();
var client = new AD_Client("http://localhost:52000", http);

ServiceStatus status = client.GetStatus();
if (!status.CanRun)
    throw new InvalidOperationException($"API not ready: {status.Status} - {status.Message}");
```

- **Use `AD_Client`.** It removes the HTTP time limit. A plain `HttpClient` gives up after 100 seconds,
  which is shorter than many analyses.
- **Start the server from your program** with `CSessionManager`. It starts `AD.API.Srv.exe` on the first
  free port from 52000 and returns the address, or an empty string if the server could not start:

  ```csharp
  string serverFolder = @"<folder that contains AD.API.Srv.exe>";
  string url = CSessionManager.StartNewSession(serverFolder);
  if (url == "")
      throw new InvalidOperationException("The API server could not be started.");
  var client = new AD_Client(url, new HttpClient());
  ```
- **Every endpoint has three forms**: `Method(args)` (blocking), `MethodAsync(args)` and
  `MethodAsync(args, cancellationToken)`.
- **Enumeration names in C#.** The client writes the first letter of every enumeration type and value in
  upper case. The value `eLanguageEnglish` of `Language_Code` is `Language_Code.ELanguageEnglish`; `bar`
  is `GeneralBeamType_API.Bar`; the type `eMeshState` is `EMeshState`. This guide lists the values as they
  appear in JSON.

### 2.3 Connect from Python

`PythonClient.zip` contains a client generated with [Kiota](https://github.com/microsoft/kiota).

1. Unzip it into a folder that becomes a Python package, for example `ad_api`.
2. Install the Kiota runtime:

   ```text
   pip install microsoft-kiota-abstractions microsoft-kiota-http microsoft-kiota-serialization-json microsoft-kiota-serialization-text microsoft-kiota-serialization-form microsoft-kiota-serialization-multipart
   ```

```python
import asyncio
import httpx
from kiota_abstractions.authentication.anonymous_authentication_provider import AnonymousAuthenticationProvider
from kiota_abstractions.base_request_configuration import RequestConfiguration
from kiota_http.httpx_request_adapter import HttpxRequestAdapter
from kiota_http.kiota_client_factory import KiotaClientFactory

from ad_api.api_client import ApiClient
from ad_api.api.model.management.new_project.new_project_request_builder import NewProjectRequestBuilder
from ad_api.api.model.elements.create_informational_element.create_informational_element_post_request_body import CreateInformationalElementPostRequestBody
from ad_api.models.environments import Environments
from ad_api.models.load_case_family_dead_loads import LoadCaseFamily_DeadLoads
from ad_api.models.localization_code import Localization_Code
from ad_api.models.log_verbose_level import LogVerboseLevel

async def main():
    # No time limit: an analysis can take longer than the default 100 seconds.
    http = KiotaClientFactory.create_with_default_middleware(httpx.AsyncClient(timeout=None))
    adapter = HttpxRequestAdapter(AnonymousAuthenticationProvider(), http_client=http,
                                  base_url="http://localhost:52000")
    client = ApiClient(adapter)

    print(await client.api.service.get_version.get())                    # 1.27.1

    # Query parameters go in a RequestConfiguration.
    query = NewProjectRequestBuilder.NewProjectRequestBuilderPostQueryParameters(
        filename=r"C:\Projects\test.fto")
    env = Environments(localization=Localization_Code.LOCALIZATION_EUROPE,
                       log_verbosity=LogVerboseLevel.ErrorsAndWarnings)
    created = await client.api.model.management.new_project.post(
        env, RequestConfiguration(query_parameters=query))
    print(created.data, created.details.success)

    # A polymorphic body goes in a wrapper, and "$type" (the "type" property) must be set by hand.
    family = LoadCaseFamily_DeadLoads(type="LoadCaseFamily_DeadLoads", name="G")
    created_family = await client.api.model.elements.create_informational_element.post(
        CreateInformationalElementPostRequestBody(load_case_family_dead_loads=family))
    print(created_family.data.value)

    # Materials and user sections: send the JSON yourself (see the limitation below).
    async with httpx.AsyncClient(base_url="http://localhost:52000", timeout=None) as raw:
        answer = await raw.post("/api/Model/materials/CreateMaterial",
                                json={"$type": "MaterialSteel", "name": "My steel", "e": 2.1e11})
        print(answer.json()["data"]["value"])

    await client.api.model.management.close_project.post()

asyncio.run(main())
```

What differs from the C# examples of this guide:

- **Every call is `async`.** The path of the endpoint becomes a chain of properties:
  `/api/Model/elements/CreateElement` is `client.api.model.elements.create_element.post(...)`.
- **Names are in snake_case**: `GeomPtStart` is `geom_pt_start`. Enumeration values have their first letter
  in upper case, as in C# (`GeneralBeamType_API.Bar`).
- **Set `type` yourself.** For an object of a polymorphic type (materials, sections, elements, load cases,
  settings, queries), set `type` to the class name. The C# client does this for you; the Python client
  does not. Without it the server reads the object as its base class and the call fails or does the wrong
  thing.
- **Request bodies that accept several classes use a wrapper** with one property per class, as
  `CreateInformationalElementPostRequestBody` above. Fill exactly one.

> **Warning:** Known limitation of the Python client. The server needs `"$type"` as the first property of a
> JSON object, and the client writes some properties before it. The call is then answered with HTTP 400.
> This affects:
>
> - materials (`CreateMaterial`) and user sections (`CreateUserSection`), because `name` is written first.
>   Send these bodies as plain JSON, as in the example above;
> - a settings object read with `GetModelSettings` and sent back with `SetModelSettings`: set
>   `settings_category = None` before you send it;
> - a `LoadCaseAnalysisBuckling` inside a `ResultsQueryFEM` (`GetResultsByQuery`): send the query as plain
>   JSON.
>
> Elements, supports, loads, load cases, combinations, systems, templates and queries are not affected.

### 2.4 Connect from another language

Send plain HTTP requests with JSON bodies, or generate a client from `swagger.json` with a tool such as
[NSwag](https://github.com/RicoSuter/NSwag), [Kiota](https://github.com/microsoft/kiota) or
[OpenAPI Generator](https://openapi-generator.tech/).

```text
curl -X POST "http://localhost:52000/api/Model/management/NewProject?filename=C:/Projects/test.fto" ^
     -H "Content-Type: application/json" ^
     -d "{ \"localization\": \"LOCALIZATION_EUROPE\", \"logVerbosity\": \"ErrorsAndWarnings\" }"
```

### 2.5 A typical session

| Step | Call |
|---|---|
| 1. Create or open a project | `NewProject` / `OpenProject` |
| 2. Create materials and sections | `CreateMaterial`, `CreateSection`, `CreateUserSection` |
| 3. Create elements and supports | `CreateElement` |
| 4. Create load case families and load cases | `CreateInformationalElement` |
| 5. Create loads | `CreateElement` |
| 6. Create or generate combinations | `AddCombination` / `GenerateCombinations` |
| 7. Run the analysis | `LaunchAnalysis` |
| 8. Read the results | `GetResults`, `GetResultsByQuery` |
| 9. Run a design calculation and read its results (optional) | `LaunchDesignCalculation`, `GetDesignResults` |
| 10. Save and close | `SaveProject`, `CloseProject`, or `CloseSession` to stop the server |

```csharp
// 1. Create a project (Eurocode settings)
var env = new Environments
{
    Localization = Localization_Code.LOCALIZATION_EUROPE,
    LogVerbosity = LogVerboseLevel.ErrorsAndWarnings
};
BooleanApiResponse project = client.NewProject(@"C:\Projects\MyModel.fto", env);
if (!project.Details.Success)
    throw new Exception(string.Join("; ", project.Details.Diagnostics.Select(d => d.Message)));

// 2. A material and a section
long steelId   = client.CreateMaterial(new Material { Name = "S235" }).Data.Value;   // catalog material
long sectionId = client.CreateSection("HEA200").Data.Value;

// 3. A 5 m beam along X
var beam = new ElementLinear
{
    GeomPtStart     = new Pt3D { X = 0, Y = 0, Z = 0 },
    GeomPtEnd       = new Pt3D { X = 5, Y = 0, Z = 0 },
    Material        = new EID { Value = steelId },
    Section         = new EID { Value = sectionId },
    GeneralBeamType = GeneralBeamType_API.BeamWStandardBending
};
long beamId = client.CreateElement(beam).Data.Value;

// ... supports, load cases, loads, analysis and results: see Example 1 in Section 15

// 10. Save and close
client.CloseProject();
```

### 2.6 Conventions

- **Units are SI**: lengths in m, forces in N, moments in N·m, stresses and moduli in Pa, densities in
  kg/m³, angles in rad, times in s. A few properties use another unit; their description says so.
  A steel Young's modulus is `2.1e11`, not `210000`.
- **Identifiers.** A create call returns the identifier of the new object (`EID`, a 64-bit integer in
  `Value`). Use it to reference the object in later calls (`Material = new EID { Value = id }`). `UserID`
  is something else: the number shown in the Advance Design interface.
- **Property names.** This guide writes them as in C# (`GeomPtStart`). In JSON they start with a lower-case
  letter (`geomPtStart`); in Python they are in snake_case (`geom_pt_start`).
- **Enumeration values travel as names**, never as numbers: `"generalBeamType": "beamWStandardBending"`.
- **`NaN`, `Infinity` and `-Infinity`** are accepted and returned as JSON names where a number is expected.
- **Optional means "leave as it is".** For most optional properties, a value that is not sent (`null`)
  takes the Advance Design default when an object is created, and keeps the current value when it is
  updated.
- **Polymorphic objects need `$type`** — see [Section 5](#5-polymorphism--the-type-discriminator).
- **Errors come in two ways** — in the response envelope, or as an HTTP error for a malformed request. See
  [Section 4](#4-response-envelope-pattern) and [Section 14](#14-error-handling--diagnostics).

---

## 3. API Endpoints Reference

Every endpoint below is a method of the generated client with the same name (`client.NewProject(...)`).
All `/api/Model/*` endpoints return an envelope with the result in `Data` and the messages in `Details` —
see [Section 4](#4-response-envelope-pattern).

> **Note:** A numeric or enumeration **query** parameter that is left out is not reported as an error: it
> takes the default of its type (`0`, or the first value of the enumeration). Always send the query
> parameters an endpoint lists.

### 3.1 Project Management

A server works on **one project at a time**. Opening a project also opens the *session*: the language,
the country settings and the design standards the project is calculated with. They are chosen once, by the
call that opens the project, and stay in force until the project is closed.

| Endpoint | Method | What it does |
|---|---|---|
| `/api/Model/management/NewProject` | `POST` | Creates a project and opens it |
| `/api/Model/management/OpenProject` | `POST` | Opens an existing project |
| `/api/Model/management/GetEnvironment` | `GET` | Returns the language, localization and design standards in force |
| `/api/Model/management/SaveProject` | `POST` | Saves the open project |
| `/api/Model/management/SaveProjectAs` | `POST` | Saves the open project under a new name and continues on the new file |
| `/api/Model/management/CloseProject` | `POST` | Saves the project, then closes it |
| `/api/Model/management/CloseProjectWithoutSave` | `POST` | Closes the project and discards the unsaved changes |
| `/api/Model/management/CloseSession` | `POST` | Saves and closes the project, then shuts the server down |
| `/api/Model/management/CloseSessionWithoutSave` | `POST` | Shuts the server down without saving the project |

---

#### `POST /api/Model/management/NewProject`

Creates a new project file and opens it.

| Parameter | In | Type | Required | Description |
|---|---|---|---|---|
| `filename` | query | `string` | Yes | Path of the `.fto` file to create. Absolute, or relative to the [working directory](#21-start-the-server) of the server. Forward slashes are accepted |
| *(body)* | body | [`Environments`](#311-the-environment-of-a-session) | Yes | Language, localization, log verbosity and design standards of the session. Send at least an empty object (`new Environments()`) |

**Returns:** `BooleanApiResponse` — `Data` is `true` when the project was created.

```csharp
var env = new Environments
{
    Language     = Language_Code.ELanguageEnglish,
    Localization = Localization_Code.LOCALIZATION_EUROPE,
    LogVerbosity = LogVerboseLevel.ErrorsAndWarnings
};
BooleanApiResponse r = client.NewProject(@"C:\Projects\test.fto", env);
```

Rules:

- The folder that is to contain the file must already exist: `PROJECT_FOLDER_NOT_FOUND`. The server does
  not create it.
- The name must end in `.fto`: `PROJECT_INVALID_EXTENSION`.
- The name must be a valid Windows file name and must have a name before the extension (`.fto` alone is
  refused): `PROJECT_INVALID_FILENAME`.
- A project must not already be open: `PROJECT_ENVIRONMENT_REFUSED`, and nothing is created. Close the
  open project first.
- A missing body is refused: `PROJECT_INVALID_ENV`.
- A new project starts with the same default project settings as a new project of the desktop
  application.

---

#### `POST /api/Model/management/OpenProject`

Opens an existing project file.

| Parameter | In | Type | Required | Description |
|---|---|---|---|---|
| `filename` | query | `string` | Yes | Path of the `.fto` file to open. Absolute, or relative to the working directory of the server |
| *(body)* | body | [`Environments`](#311-the-environment-of-a-session) | Yes | Same as for `NewProject` |

**Returns:** `BooleanApiResponse` — `Data` is `true` when the project was opened.

```csharp
BooleanApiResponse r = client.OpenProject(@"C:\Projects\existing.fto", new Environments
{
    Localization = Localization_Code.LOCALIZATION_EUROPE
});
```

Rules:

- The file must exist: `PROJECT_FILE_NOT_FOUND` (the message gives the full path that was tried).
- The file name rules of `NewProject` apply: `PROJECT_INVALID_EXTENSION`, `PROJECT_INVALID_FILENAME`.
- A project must not already be open: `PROJECT_ENVIRONMENT_REFUSED`.

> **Warning:** A project file does not store its localization or its design standards. A reopened project
> runs with the `Environments` of the `OpenProject` call, not with the one it was created with. Send the
> same `Environments` you used to create it, and check the result with `GetEnvironment`.

---

#### `GET /api/Model/management/GetEnvironment`

Returns the environment in force: language, localization, log verbosity, and all the design standards and
national annexes. No parameters.

**Returns:** `EnvironmentsApiResponse` — `Data` is an [`Environments`](#311-the-environment-of-a-session).

```csharp
EnvironmentsApiResponse env = client.GetEnvironment();

Console.WriteLine(env.Data.Localization);              // LOCALIZATION_FRANCE
Console.WriteLine(env.Data.DesignCodes.Steel);         // Eurocode
Console.WriteLine(env.Data.DesignCodes.SteelAnnex);    // France
```

- The answer is what the session really uses, not a copy of what you sent. Use it to see which standards a
  localization selects, or to confirm an override.
- It fails with `ENVIRONMENT_NO_SESSION` when no project is open.
- A standard that this API has no name for (a 2nd generation Eurocode, CM66, BS 8110 — possible in a
  project configured in the desktop application) is returned as `null` for that property, with a warning.

---

#### `POST /api/Model/management/SaveProject`

Saves the open project to its own file. No parameters.

**Returns:** `BooleanApiResponse` — `Data` is `true` when the project was saved.

```csharp
client.SaveProject();
```

| Code | Meaning |
|---|---|
| `PROJECT_SAVED` | Information (returned only with `LogVerbosity = AllDetails`). The project was saved |
| `PROJECT_NOT_OPEN` | No project is open |
| `PROJECT_SAVE_FAILED` | The project could not be saved; the other messages say why. The project stays open |
| `PROJECT_SAVE_EXCEPTION` | Internal error while saving |

---

#### `POST /api/Model/management/SaveProjectAs`

Saves the open project to a new `.fto` file — the same as **File > Save As** in Advance Design. The whole
project is copied: the model, the analysis model, the results and the project folders. From this call on,
the session works on the new file (`SaveProject` and `CloseProject` write to it). The original file keeps
the state of its last save.

| Parameter | In | Type | Required | Description |
|---|---|---|---|---|
| `filename` | query | `string` | Yes | Name of the new `.fto` file. Absolute, or relative to the working directory of the server. Its folder must already exist |

**Returns:** `BooleanApiResponse` — `Data` is `true` when the project was saved. The information message
`PROJECT_SAVED_AS` is added when `LogVerbosity` is `AllDetails`.

```csharp
BooleanApiResponse r = client.SaveProjectAs(@"C:\Projects\copy.fto");
```

The call is refused, before anything is written, with one of these codes:

| Code | When |
|---|---|
| `PROJECT_NOT_OPEN` | No project is open |
| `PROJECT_INVALID_FILENAME` | Invalid file name, or a name that contains the extension twice |
| `PROJECT_INVALID_EXTENSION` | The name does not end in `.fto` |
| `PROJECT_FOLDER_NOT_FOUND` | The folder that is to contain the file does not exist |
| `PROJECT_TARGET_EXISTS` | The file, or a folder with the same name without `.fto`, already exists |
| `PROJECT_TARGET_INVALID` | The new project folder would be inside the open project's folder, or the reverse |

- Spaces around the name and before the extension are removed.
- The full path must be at least 28 characters shorter than the Windows path limit (260 characters).

If the save itself fails, `Data` is `false` with `PROJECT_SAVE_AS_FAILED` (or `PROJECT_SAVE_AS_EXCEPTION`
for an internal error). Read the message — it says which project the session is on:

| Message starts with | What happened | What to do |
|---|---|---|
| "The session's project is still ..." | Nothing was written (for example the path is too long or the folder is not writable). The open project is unchanged | Fix the cause and try again |
| "The session's project is now ..." | The copy started and did not finish. The session is on the new, possibly incomplete, project | Do **not** call `SaveProject` or `CloseProject`. Call `CloseProjectWithoutSave`, check both folders, and open the original again |

---

#### `POST /api/Model/management/CloseProject`

Saves the open project, then closes it. The server keeps running. No parameters.

**Returns:** `BooleanApiResponse`

```csharp
client.CloseProject();
```

- `Data` is `false` with `PROJECT_CLOSE_FAILED` when the project could not be saved or closed cleanly. The
  project is closed anyway, and the changes since the last save may be lost. The other messages say why.

---

#### `POST /api/Model/management/CloseProjectWithoutSave`

Closes the open project **without saving it**. Everything changed since the last save is discarded. The
server keeps running. No parameters.

**Returns:** `BooleanApiResponse`

```csharp
client.CloseProjectWithoutSave();
```

- "The last save" is not only your last `SaveProject` / `SaveProjectAs`. These operations also save the
  project: `LaunchAnalysis`, `LaunchDesignCalculation`, `GenerateCombinations`, `SetModelSettings`,
  `ResetModelSettings`, and deleting a structural system.
- `Data` is `false` with `PROJECT_CLOSE_FAILED` when the project could not be closed cleanly.

---

#### `POST /api/Model/management/CloseSession`

Saves and closes the open project, releases the licence and **shuts the server process down**. Use it at
the end of a run. No parameters; calling `CloseProject` first is not needed.

**Returns:** `BooleanApiResponse` — the call returns at once, with `Data = true`.

```csharp
client.CloseSession();   // saves the open project, then the server exits
```

- The shutdown starts about 5 seconds after the call. An operation that is still running finishes first.
- After the call, every other `/api/Model/*` request is refused with **HTTP 503** and the code
  `SERVICE_STOPPING`. `GetStatus` answers `Stopping` until the process has exited. Do not send further
  requests to this server.
- Calling it again changes nothing. The **first** close request decides whether the project is saved.
- With `LogVerbosity = AllDetails`, the information message `SESSION_CLOSING` says what the close in
  progress does: the project is saved, or closed without saving.
- The process exits with code `0` when the close succeeded and `3` when it failed (see the server log).
- Stopping the server with Ctrl+C closes the session the same way.

---

#### `POST /api/Model/management/CloseSessionWithoutSave`

Same as `CloseSession`, but the open project is closed **without saving**: everything changed since its
last save is discarded. No parameters.

**Returns:** `BooleanApiResponse`

```csharp
client.CloseSessionWithoutSave();   // discards the unsaved changes, then the server exits
```

- If `CloseSession` was already called, this call does not change it: the project is saved.

---

##### 3.1.1 The environment of a session

`Environments` is the body of `NewProject` and `OpenProject`.

| Property | Type | Description |
|---|---|---|
| `Language` | `Language_Code?` | Language of the messages and catalogues. `null` keeps the language the server already runs in |
| `Localization` | `Localization_Code?` | Country configuration. Selects all eight design standards and their national annexes at once |
| `LogVerbosity` | `LogVerboseLevel` | Which messages come back in `Details.Diagnostics`: `Errors`, `ErrorsAndWarnings` (default) or `AllDetails` |
| `DesignCodes` | `DesignCodes?` | Design standards and national annexes to set individually, on top of what `Localization` selected. `null` keeps them all |

```csharp
// A French model, with Eurocode 3 verified to the German national annex.
var env = new Environments
{
    Localization = Localization_Code.LOCALIZATION_FRANCE,
    LogVerbosity = LogVerboseLevel.ErrorsAndWarnings,
    DesignCodes  = new DesignCodes
    {
        Steel      = SteelDesignCode.Eurocode,
        SteelAnnex = SteelEurocodeAnnex.Germany
    }
};
client.NewProject(@"C:\Projects\mixed.fto", env);
```

```json
{
  "localization": "LOCALIZATION_FRANCE",
  "logVerbosity": "ErrorsAndWarnings",
  "designCodes": { "steel": "eurocode", "steelAnnex": "germany" }
}
```

**The four rules to remember**

1. **The environment is set once per project.** It is applied by the `NewProject` / `OpenProject` call and
   lasts until the project is closed. To change it, close the project and open it again with another
   `Environments`.
2. **Always send `Localization`.** When it is `null`, the server keeps the standards of its own Advance
   Design installation, so the same request gives a Eurocode model on one machine and a North American
   model on another.
3. **The language is fixed when the server starts working.** The first project settles it. A later request
   that sends a different `Language` is refused — restart the server to change the language. A different
   language that is only implied by `Localization` is ignored with a warning; the standards of that
   localization are still applied.
4. **Nothing is written to the machine.** The settings of a session do not change the Advance Design
   configuration of the computer.

`LogVerbosity` is the one setting that belongs to the server and not to the project: the last value sent
stays in force for all following requests. Warnings are only returned with `ErrorsAndWarnings` or
`AllDetails`.

> **Warning:** In C#, set `LogVerbosity` explicitly. An `Environments` object created without it sends
> `Errors`, and the warnings this guide mentions are then not returned.

###### The eight standards and their annexes — `DesignCodes`

Each family has a standard and a national annex. Every property is optional: `null` keeps what
`Localization` selected.

| Family | Standard property · type | Annex property · type | The annex is accepted with |
|---|---|---|---|
| Combinations | `Combinations` · `eCombinationsStandard` | `CombinationsAnnex` · `eCombinationsEurocodeAnnex` | `Eurocode` |
| Seismic | `Seismic` · `SeismicDesignCode` | `SeismicAnnex` · `SeismicEurocodeAnnex` | `en1998_1` |
| Climatic (snow and wind) | `Climatic` · `ClimaticDesignCode` | `ClimaticAnnex` · `ClimaticEurocodeAnnex` | `eurocode` (any annex); `pr_en13031_1` (`general` or `france`); `romania` (`romania`) |
| Reinforced concrete | `Concrete` · `ConcreteDesignCode` | `ConcreteAnnex` · `ConcreteEurocodeAnnex` | `eurocode` (any annex except `italy`); `ntc2008` / `ntc2018` (`italy`) |
| Steel | `Steel` · `SteelDesignCode` | `SteelAnnex` · `SteelEurocodeAnnex` | `eurocode` (any annex except `italy`) |
| Timber | `Timber` · `TimberDesignCode` | `TimberAnnex` · `TimberEurocodeAnnex` | `eurocode` (any annex except `italy`) |
| Masonry | `Masonry` · `MasonryDesignCode` | `MasonryAnnex` · `MasonryEurocodeAnnex` | `eurocode` |
| Composite | `Composite` · `CompositeDesignCode` | `CompositeAnnex` · `CompositeEurocodeAnnex` | `eurocode` |

One more property, `AiscMethod` (`eAiscDesignMethod`: `AllowableStrengthDesign` or
`LoadAndResistanceFactorDesign`), chooses ASD or LRFD for the AISC 360 verifications. It is accepted when
`Combinations` is `US` / `US2018` or `Steel` is `aisc` / `aisc_2016`, and refused otherwise. When it is
`null` and `Localization` is sent, the method is ASD.

The values of all these enumerations are listed in [Section 13](#design-standards-and-national-annexes).

Rules:

- **All or nothing.** If anything in the request is not valid, nothing is applied: the call fails with
  `PROJECT_ENVIRONMENT_REFUSED` and one message per problem.
- **An annex sent for a standard that has none is refused**, not ignored. Example: `Concrete = aci` with
  any `ConcreteAnnex`.
- **Send the annex together with the standard.** If you change only the standard, the annex selected by
  the localization stays, and the pair may not be valid. Example: `LOCALIZATION_ITALIA` selects NTC 2018
  with the Italian annex; asking only for `Concrete = eurocode` leaves `italy`, which Eurocode 2 does not
  accept.
- **Two standards set their own annex:** `Concrete = ntc2008` / `ntc2018` always uses `italy`, and
  `Climatic = romania` always uses `romania`.
- **Steel and composite are independent.** A pair the desktop application would not allow (for example
  `Steel = aisc_2016` with `Composite = eurocode`) is applied with a warning; the composite design may then
  be unable to run.
- **`can_csa_s16_2024` is refused as an input.** Send `can_csa_s16_2019`, which calculates the same way.
- **The greenhouse standards** (`Combinations = Greenhouse_EN13031_1`, `Climatic = pr_en13031_1`) need a
  licence with the greenhouse option.
- **Send enumeration values by name, never by number.**
- **No 2nd generation Eurocode can be selected** through the API.
- The standards cannot be changed later through the model settings: `CombinationsGenerationSettings.Standard`
  / `Annex` and `SteelCalculationSettings.DesignCode` are read-only there.

---

### 3.2 Model — Materials

A material is added to the open project by name and properties. The calls return the internal ID of the material;
elements refer to a material by that ID.

| Endpoint | Method | What it does |
|---|---|---|
| `/api/Model/materials/CreateMaterial` | `POST` | Creates a material in the project, or returns the one that already has that name |
| `/api/Model/materials/GetListMaterials` | `GET` | Returns the IDs of all materials of the project |
| `/api/Model/materials/GetMaterials` | `POST` | Returns the materials with the given IDs |

---

#### `POST /api/Model/materials/CreateMaterial`

Creates a material in the current project.

| Parameter | In | Type | Required | Description |
|---|---|---|---|---|
| *(body)* | body | [`Material`](#6-data-models--materials) (polymorphic) | Yes | The material. Send one of the types below, with its `$type` |

| `$type` | Material |
|---|---|
| `MaterialSteel` | Steel |
| `MaterialReinforcedConcrete` | Reinforced concrete |
| `MaterialWood` | Timber (Eurocode) |
| `MaterialWoodNorthAmerica` | Timber (North America) |
| `MaterialRigid` | Rigid material |
| `MaterialOther` | User-defined material |
| `Material` | A name only. Use it to get a standard material of the Advance Design catalogs by its name (for example `S235` or `C25/30`). If the name is not a known material, a steel material with that name is created |

**Returns:** `EIDApiResponse` — `Data.Value` is the ID of the material.

```csharp
// Values are SI: E and SigmaE in Pa, Ro in kg/m³.
var steel = new MaterialSteel
{
    Name   = "S355_Custom",
    E      = 2.1e11,
    Ro     = 7850,
    Nu     = 0.3,
    SigmaE = 3.55e8
};
EIDApiResponse r = client.CreateMaterial(steel);
long materialId = r.Data.Value;
```

The same request as raw JSON:

```json
{
  "$type": "MaterialSteel",
  "name": "S355_Custom",
  "behaviorType": "isotropic_behavior",
  "e": 2.1e11,
  "ro": 7850,
  "nu": 0.3,
  "damping": 0.05,
  "alpha": 1.2e-5,
  "sigmaE": 3.55e8
}
```

Rules:

- Always send the `$type`. A missing body is refused: `MATERIAL_NULL_INPUT`.
- A property you leave out takes its default. The defaults, and the unit of every property, are in
  [Section 6](#6-data-models--materials). All values are SI: Young's modulus and strengths in Pa.
- If creation fails, `Data.Value` is `0` and `Details.Diagnostics` gives the reason. Always test `Data.Value`.
  `MATERIAL_CREATION_FAILED` reports an internal error.

> **Warning:** The name decides whether a material is created. If the project already has a material with
> that name (case is ignored), or the name is a standard material of the Advance Design catalogs (for example
> `S235` or `C25/30`), that material is returned and the properties you sent are ignored. The existing
> material is never updated. For a custom material, use a name that is not a catalog name, and read the result
> back with `GetMaterials`.

---

#### `GET /api/Model/materials/GetListMaterials`

Returns the IDs of all materials of the project. No parameters.

**Returns:** `Int64ListApiResponse` — `Data` is the list of material IDs.

```csharp
Int64ListApiResponse matIds = client.GetListMaterials();
foreach (long id in matIds.Data)
    Console.WriteLine($"Material ID: {id}");
```

---

#### `POST /api/Model/materials/GetMaterials`

Returns the materials with the given IDs.

| Parameter | In | Type | Required | Description |
|---|---|---|---|---|
| *(body)* | body | `long[]` | No | The material IDs |

**Returns:** `MaterialListApiResponse` — `Data` is a list of `Material` objects. Each one comes back as its
concrete type (`MaterialSteel`, `MaterialReinforcedConcrete`, ...): cast it or pattern-match on it.

```csharp
MaterialListApiResponse mats = client.GetMaterials(new List<long> { 1, 2, 3 });
foreach (var mat in mats.Data)
{
    if (mat is MaterialSteel steel)
        Console.WriteLine($"{steel.Name}: E = {steel.E / 1e9} GPa");   // E is in Pa
}
```

Rules:

- An empty body returns an empty list with the warning `MATERIALS_EMPTY_INPUT`.
- An ID that is `0` or does not exist is skipped, without a message. The result can be shorter than the
  request.
- A material of a kind that the API has no class for comes back as a plain `Material` with only its `Name`.

---

### 3.3 Model — Sections

A section is added to the open project by name (a catalog or parametric section) or by its sectional
characteristics (a user section). The calls return the internal ID of the section.

| Endpoint | Method | What it does |
|---|---|---|
| `/api/Model/sections/CreateSection` | `POST` | Adds a section to the project by its name |
| `/api/Model/sections/CreateUserSection` | `POST` | Creates or updates a user section: the caller supplies its characteristics and, optionally, its outline |
| `/api/Model/sections/GetListSections` | `GET` | Returns the IDs of all sections of the project |
| `/api/Model/sections/GetSections` | `POST` | Returns the sections with the given IDs |

---

#### `POST /api/Model/sections/CreateSection`

Adds a section to the project by its name. The section is taken from the Advance Design section catalogs, or
built from its parametric name.

| Parameter | In | Type | Required | Description |
|---|---|---|---|---|
| `sectionName` | query | `string` | Yes | Name of the section, for example `IPE300`, `HEA200` or `R20*30` |

**Returns:** `EIDApiResponse` — `Data.Value` is the ID of the section.

```csharp
EIDApiResponse sec = client.CreateSection("IPE300");
long sectionId = sec.Data.Value;
```

Rules:

- If the project already has a section with that name (case is ignored), its ID is returned and nothing is
  added.
- If no section has that name, `Data.Value` is `0`. `Details.Success` can still be `true`, so always test
  `Data.Value`.
- `SECTION_CREATION_FAILED` reports an internal error.
- To define a section by its characteristics, use `CreateUserSection`.

---

#### `POST /api/Model/sections/CreateUserSection`

Creates a user section: a section whose characteristics you supply, instead of reading them from a catalog or
computing them from dimensions. It is the *User* tab of the Advance Design section manager. The section is
created in the project only; the user section catalog database is never changed.

| Parameter | In | Type | Required | Description |
|---|---|---|---|---|
| *(body)* | body | [`UserSection`](#65-data-models--sections) | Yes | Must carry `"$type": "UserSection"`. A plain `Section` is refused |

**Returns:** `EIDApiResponse` — `Data.Value` is the ID of the section, or `0` when the call was refused.

```csharp
// A 200 x 300 mm solid rectangle, outline included. All values are SI.
var section = new UserSection
{
    Name = "MyUserSection",
    Area = 0.06,                                    // m²
    Iy = 4.5e-4, Iz = 2.0e-4, It = 4.7e-4,          // m⁴
    Iw = 0,                                         // m⁶; 0 for a solid section (reported as a warning)
    WelyInf = 3.0e-3, WelySup = 3.0e-3,             // m³
    WelzInf = 2.0e-3, WelzSup = 2.0e-3,
    Wply = 4.5e-3, Wplz = 3.0e-3, Wt = 2.8e-3,
    Sy = 0.04, Sz = 0.04, Avy = 0.04, Avz = 0.04,   // m²
    ProfileType = UserSectionProfileType_API.Rectangular,
    RollingType = SectionRollingType_API.HotRolled,
    Shape = new UserSectionShape                    // one rectangle, centred on the centre of gravity
    {
        Contours =
        [
            new UserSectionContour
            {
                Points =
                [
                    new() { Y = -0.1, Z =  0.15 }, new() { Y = 0.1, Z =  0.15 },
                    new() { Y =  0.1, Z = -0.15 }, new() { Y = -0.1, Z = -0.15 }
                ]
            }
        ]
    }
};

EIDApiResponse resp = client.CreateUserSection(section);
if (resp.Data.Value == 0)
    foreach (var d in resp.Details.Diagnostics)
        Console.WriteLine($"{d.Severity}: {d.Message}");
```

The same request as raw JSON:

```json
{
  "$type": "UserSection",
  "name": "MyUserSection",
  "type": "st_USER",
  "area": 0.06,
  "iy": 4.5e-4,
  "iz": 2.0e-4,
  "iyz": 0,
  "it": 4.7e-4,
  "iw": 0,
  "welyInf": 3.0e-3,
  "welySup": 3.0e-3,
  "welzInf": 2.0e-3,
  "welzSup": 2.0e-3,
  "wply": 4.5e-3,
  "wplz": 3.0e-3,
  "wt": 2.8e-3,
  "sy": 0.04,
  "sz": 0.04,
  "avy": 0.04,
  "avz": 0.04,
  "profileType": "Rectangular",
  "rollingType": "HotRolled",
  "shape": {
    "contours": [
      {
        "type": "Polygon",
        "isHole": false,
        "points": [
          { "y": -0.100, "z":  0.150 },
          { "y":  0.100, "z":  0.150 },
          { "y":  0.100, "z": -0.150 },
          { "y": -0.100, "z": -0.150 }
        ]
      }
    ]
  }
}
```

Rules:

- `Name` is required. It must not be empty once spaces at both ends are removed.
- The characteristics, their units and the validation (what is refused, what is accepted with a warning) are
  in [Section 6.5](#65-data-models--sections). A refused call writes nothing to the project: `Data.Value` is `0`
  with `USER_SECTION_CREATION_FAILED`, and the error messages say which value was rejected.
- A missing body is refused: `USER_SECTION_NULL_INPUT`.
- The name is compared with the sections already in the project, case ignored:
  - A user section with that name is updated in place and its ID is returned, with a warning. Every
    characteristic, `ProfileType` and `RollingType` take the values you send, and a value you leave out
    becomes its default. `Shape` is replaced when you send one and is kept when it is `null`.
  - A section that is not a user section (a catalog section) is not overwritten: the call is refused.
- Warnings are returned only when the session runs with `ErrorsAndWarnings` or `AllDetails` verbosity (see
  [Section 3.1.1](#311-the-environment-of-a-session)).
- `Type`, `FamilyCode` and `CatalogName` are outputs. Send `"type": "st_USER"`; a different `Type` is replaced
  with a warning.

---

#### `GET /api/Model/sections/GetListSections`

Returns the IDs of all sections of the project. No parameters.

**Returns:** `Int64ListApiResponse` — `Data` is the list of section IDs.

```csharp
Int64ListApiResponse secIds = client.GetListSections();
```

---

#### `POST /api/Model/sections/GetSections`

Returns the sections with the given IDs.

| Parameter | In | Type | Required | Description |
|---|---|---|---|---|
| *(body)* | body | `long[]` | No | The section IDs |

**Returns:** `SectionListApiResponse` — `Data` is a list of [`Section`](#65-data-models--sections). A user
section comes back as a `UserSection`, with its characteristics and its outline. Any other section comes back
as a `Section` with `Name`, `Type`, `FamilyCode` and `CatalogName`. Cast or pattern-match on the result.

```csharp
SectionListApiResponse secs = client.GetSections(new List<long> { 1, 2 });
foreach (var s in secs.Data)
{
    Console.WriteLine($"{s.Name} (type: {s.Type})");
    if (s is UserSection us)
        Console.WriteLine($"  A = {us.Area} m², Iy = {us.Iy} m⁴");
}
```

Rules:

- An empty body returns an empty list with the warning `SECTIONS_EMPTY_INPUT`.
- An ID that is `0` or does not exist is skipped, without a message.
- An outline that you read back can be an approximation. Read the limits in
  [Section 6.5](#65-data-models--sections) before you send it again.

---

### 3.4 Model — Elements (geometric)

Geometric elements (structural elements, supports, loads, load areas, masses, points, poly lines) are
created, updated and deleted with the calls below. Their properties are in
[Section 7](#7-data-models--elements-geometric), [Section 8](#8-data-models--supports) and
[Section 9](#9-data-models--loads).

| Endpoint | Method | What it does |
|---|---|---|
| `/api/Model/elements/CreateElement` | `POST` | Creates an element, a support or a load |
| `/api/Model/elements/UpdateElement` | `POST` | Changes an existing element, support or load |
| `/api/Model/elements/DeleteElements` | `POST` | Deletes elements and other model objects |
| `/api/Model/elements/AssignDesignTemplateToElements` | `POST` | Assigns a design template to several elements |

---

#### `POST /api/Model/elements/CreateElement`

Creates one element and returns its EID.

| Parameter | In | Type | Required | Description |
|---|---|---|---|---|
| *(body)* | body | `ElementBase` *(polymorphic)* | Yes | The element. `"$type"` selects the type — see [Section 5](#5-polymorphism--the-type-discriminator) |

**Returns:** `EIDApiResponse` — `Data.Value` is the EID of the new element.

The 23 types that can be created: `ElementLinear`, `ElementPlanar`, `ElementSinglePile`, `ElementLoadArea`,
`ElementMass`, `ElementPoint`, `ElementPolyLine` ([Section 7](#7-data-models--elements-geometric)); the 12
support types `ElementRigid…Support`, `ElementElastic…Support`, `ElementTC…Support` and
`ElementAdvanced…Support`, each as `Punctual`, `Linear` or `Planar` ([Section 8](#8-data-models--supports));
`ElementLoadPunctual`, `ElementLoadLinear`, `ElementLoadPlanar` and `ElementImposedDisplacement`
([Section 9](#9-data-models--loads)). `ElementBase` and the three `FamilySupport…` classes are base classes
and cannot be created.

```csharp
// A 6 m beam along X. sectionId and materialId come from CreateSection / CreateMaterial.
var beam = new ElementLinear
{
    GeomPtStart       = new Pt3D { X = 0, Y = 0, Z = 0 },
    GeomPtEnd         = new Pt3D { X = 6, Y = 0, Z = 0 },
    Section           = new EID { Value = sectionId },
    Material          = new EID { Value = materialId },
    LinearElementType = LinearElementFEMType_API.ELinearElementFEMTypeGeneral,
    GeneralBeamType   = GeneralBeamType_API.BeamWStandardBending
};
EIDApiResponse r = client.CreateElement(beam);
if (r.Details.Success && r.Data.Value != 0)
    Console.WriteLine($"Beam created, EID {r.Data.Value}");
```

The same beam as a raw JSON body:

```json
{
  "$type": "ElementLinear",
  "geomPtStart": { "x": 0, "y": 0, "z": 0 },
  "geomPtEnd":   { "x": 6, "y": 0, "z": 0 },
  "section":  { "value": 12 },
  "material": { "value": 5 },
  "linearElementType": "eLinearElementFEMTypeGeneral",
  "generalBeamType": "beamWStandardBending"
}
```

A rigid (fixed) punctual support, as a raw JSON body:

```json
{
  "$type": "ElementRigidPunctualSupport",
  "geomPt": { "x": 0, "y": 0, "z": 0 },
  "material": { "value": 5 },
  "constraintsType": "fix"
}
```

Rules:

- Create what the element refers to first: the material ([Section 3.2](#32-model--materials)), the section
  ([Section 3.3](#33-model--sections)), the load case ([Section 3.5](#35-model--informational-elements)) and the
  structural systems. Use the EIDs from their responses. An EID that does not exist is not always reported,
  so an element can be created with the wrong or a missing reference.
- Test the result with `Details.Success` and `Data.Value != 0`. A failed creation returns `Data.Value = 0`
  and an `ENGINE_ERROR` message that gives the reason. The `ELEMENT_CREATED` message is returned even when
  nothing was created, so it is not a success test.
- Coordinates are global and in metres; all values are SI. A planar contour that is not valid is refused and
  the element is not created ([Section 7](#planar-contour-validity)). `SystemIDs` selects the structural
  system ([Section 7](#system-membership--systemids)).

---

#### `POST /api/Model/elements/UpdateElement`

Changes the properties of an existing element.

| Parameter | In | Type | Required | Description |
|---|---|---|---|---|
| `elementId` | query | `long` | Yes | EID of the element to update |
| *(body)* | body | `ElementBase` *(polymorphic)* | Yes | The new state of the element. `"$type"` must be the type of the element with that EID |

**Returns:** `BooleanApiResponse` — `Data` is `true` when the update was applied.

```csharp
// Read, change, send back.
ElementBaseListApiResponse got = client.GetElementsObject(new List<long> { beamId });
var beam = (ElementLinear)got.Data.First();

beam.SectionOrientationAngle = Math.PI / 2;      // rad
BooleanApiResponse r = client.UpdateElement(beamId, beam);
```

##### Properties that are not sent

The update applies what the body contains. Always read the element with `GetElementsObject`, change the
properties you need and send the whole object back. Otherwise a property you left out can be overwritten.

| Property you leave out | Result |
|---|---|
| A nullable value (`bool?`, `double?`, `int?`) | Unchanged |
| A sub-object (`HaunchStart`, `MeshProperties`, `Footing`, ...) | Unchanged |
| A reference (`Material`, `Section`, `LoadCase`): omitted or `0` | Unchanged |
| `UserName`: omitted or empty | Unchanged |
| `SystemIDs`: omitted or empty | Unchanged |
| `SelectedReleaseName`, `LinearReleases`: omitted, empty or `null` | Unchanged |
| `DesignTemplateId` | `null` keeps the template, `0` detaches it |
| `Openings` of a planar element | `null` keeps them, an empty list removes all of them |
| A value that is not nullable (number, `bool`, enumeration, point) | **Overwritten** with the value you send. A property you omit is sent as its default |

Examples of the last row: an omitted `GeomPtStart` moves the start of a beam to the origin, an omitted
`GeneralBeamType` turns the beam into `sbeam`, an omitted `Fz` of a load becomes `0`.

> **Warning:** A sub-object that you send is applied completely. The properties you leave out inside it take
> their default, they do not keep the current value. The generated C# classes also fill in defaults:
> `new ElementLinear()` already carries `SectionOrientationAngle = 0`, `ElementActive = true`, `AutoMesh = true`
> and `ExtendIntoWall = true`, and an update with it resets those four properties.

Rules:

- The type of the body selects the update. All 23 types listed under `CreateElement` can be updated. The
  base classes cannot.
- A load or an imposed displacement is moved to another load case by sending another `LoadCase`.
- A refused contour (planar element, planar support, load area, planar load) or poly line point list changes
  nothing at all, and `Data` is `false`.
- A refused single property (for example an invalid material EID) is skipped: an `ENGINE_ERROR` message names
  it, the other properties are still applied and `Data` can still be `true`. Check `Details.HasErrors`, not
  only `Data`.
- An invalid `SystemIDs` entry returns `Data = false`; changing the contour of a planar element deletes its
  openings and can reset the edge releases — see [Section 7](#7-data-models--elements-geometric).
- A missing body is refused: `ELEMENT_NULL_INPUT`. A failed update reports `Data = false` and an `ENGINE_ERROR`
  message with the reason.

---

#### `POST /api/Model/elements/DeleteElements`

Deletes several elements in one call.

| Parameter | In | Type | Required | Description |
|---|---|---|---|---|
| *(body)* | body | `long[]` | Yes | EIDs to delete. An empty list is refused: `ELEMENT_DELETE_EMPTY_INPUT` |

**Returns:** `BooleanApiResponse` — `Data` is `true` only when every EID was deleted.

```csharp
BooleanApiResponse r = client.DeleteElements(new List<long> { beamId, slabId, supportId });
```

What can be deleted: all 23 geometric types (see `CreateElement`), load cases and load case families,
combinations, structural systems, linear release templates and design templates. Materials and sections
cannot be deleted with this call.

Rules:

- Every EID is handled on its own. An EID that cannot be deleted does not stop the others: `Data` is `false`
  and an `ENGINE_ERROR` message names the EID.
- An EID is refused when it is `0`, when it does not exist, when it is a material or a section, or when it is
  a load case or load case family that is locked.
- These cannot be deleted: the root system `Structure 0`, the three built-in linear release templates (Free,
  Pinned, Fixed) and the built-in default design template.
- Deleting a load case family also deletes its load cases and their loads. Combinations that use a deleted
  load case are updated — see [Section 10](#10-data-models--informational-elements-load-cases--families).
- Deleting a combination also removes it from every combination that contains it. A combination left empty is
  deleted.
- Deleting a structural system deletes it with all its sub-systems and saves the project. The elements inside
  are not deleted. An element that would be left in no system goes back to `Structure 0`, with a warning.
- Deleting a linear release template sets every planar element edge that used it to the `Fixed` template,
  with a warning.
- Deleting a design template detaches the linear elements that used it. They keep their current expert
  values, with a warning — see [Section 10.6](#106-expert-design-templates).
- There is no undo.

---

#### `POST /api/Model/elements/AssignDesignTemplateToElements`

Assigns one [Expert design template](#106-expert-design-templates) to several elements at once, or detaches
them from the template they carry. Only the design template of each element changes. This is the difference
from `UpdateElement`, which needs the whole element.

| Parameter | In | Type | Required | Description |
|---|---|---|---|---|
| `designTemplateId` | query | `long` | Yes | EID of the template to assign, or `0` to detach the elements from their template (they return to the built-in default template, "None") |
| *(body)* | body | `long[]` | Yes | EIDs of the elements. An empty list is refused: `DESIGN_TEMPLATE_ASSIGN_EMPTY_INPUT` |

**Returns:** `BooleanApiResponse` — `Data` is `true` only when every element was assigned.

```csharp
client.AssignDesignTemplateToElements(templateId, new List<long> { beam1, beam2, beam3 });
client.AssignDesignTemplateToElements(0, new List<long> { beam1, beam2, beam3 });   // detach
```

A template is applied to an element only when all three conditions are true:

- it is a template for the same kind of element (linear elements);
- it is of the same material family as the element. A steel element modelled as a composite beam belongs to
  the composite family, so a steel template cannot be assigned to it;
- it holds the properties of the design code the model uses at the time of the call.

Rules:

- A mixed selection is a normal partial success. An element that breaks a rule is reported in
  `Details.Diagnostics` and is left unchanged. The other elements are assigned and `Data` is `false`.
- A `designTemplateId` that is not a template of the model changes nothing.
- An EID that appears twice is processed once.
- Detaching does not reset the expert values of the element.

---

### 3.5 Model — Informational Elements

Informational elements are the model objects that are not geometric: load cases and load case families,
combinations, structural systems, linear release templates and Expert design templates. Their properties are
in [Section 10](#10-data-models--informational-elements-load-cases--families). They are read with
`GetInformationalElementsObject` ([Section 3.10](#310-model--query--retrieval)) and deleted with
`DeleteElements` ([Section 3.4](#34-model--elements-geometric)).

| Endpoint | Method | What it does |
|---|---|---|
| `/api/Model/elements/CreateInformationalElement` | `POST` | Creates a load case family, a load case, a combination, a structural system, a release template or a design template |
| `/api/Model/elements/UpdateInformationalElement` | `POST` | Changes one of these objects |

---

#### `POST /api/Model/elements/CreateInformationalElement`

Creates one informational element and returns its EID.

| Parameter | In | Type | Required | Description |
|---|---|---|---|---|
| *(body)* | body | `InformationalElementBase` *(polymorphic)* | Yes | The element. `"$type"` selects the type |

**Returns:** `EIDApiResponse` — `Data.Value` is the EID of the new element.

Types: `LoadCaseFamily_*` ([Section 10.1](#101-load-case-families)), `LoadCase_*`
([Section 10.2](#102-load-cases)), `Combination` ([Section 10.3](#103-combinations)), `StructuralSystem`
([Section 10.4](#104-structural-systems)), `LinearReleaseTemplate` ([Section 10.5](#105-linear-release-templates))
and the Expert design templates `EC3SteelLinearDesignTemplate`, `AISCSteelLinearDesignTemplate` and
`CSASteelLinearDesignTemplate`, one per steel design code family ([Section 10.6](#106-expert-design-templates)).

Rules:

- `LoadCaseFamily_AnalysisBuckling` and `LoadCase_Buckling` cannot be created: reads return them, this call
  does not accept them.
- The abstract base types (`InformationalElementBase`, `LoadCase`, `LoadCaseFamily`, `DesignTemplate` and
  its abstract subclasses) cannot be created. A design template must match the steel design code of the model.
- Test the result with `Details.Success` and `Data.Value != 0`. A failed creation returns `Data.Value = 0`
  and an `ENGINE_ERROR` message with the reason. `INFORMATIONAL_ELEMENT_CREATED` is returned even when nothing
  was created.

---

#### `POST /api/Model/elements/UpdateInformationalElement`

Changes an existing informational element.

| Parameter | In | Type | Required | Description |
|---|---|---|---|---|
| `elementId` | query | `long` | Yes | EID of the element to update |
| *(body)* | body | `InformationalElementBase` *(polymorphic)* | Yes | The new state. `"$type"` must be the type of the element with that EID |

**Returns:** `BooleanApiResponse` — `Data` is `true` when the update was applied.

Read the element with `GetInformationalElementsObject`, change it, then call
`client.UpdateInformationalElement(elementId, element)`.

Rules:

- The update works like [`UpdateElement`](#properties-that-are-not-sent): read the element, change it, send
  the whole object back. The rules of each class are in [Section 10](#10-data-models--informational-elements-load-cases--families).
- A missing body is refused: `INFORMATIONAL_ELEMENT_NULL_INPUT`.
- A failed update reports `Data = false` and an `ENGINE_ERROR` message with the reason. Check
  `Details.HasErrors` as well as `Data`.

---

### 3.6 Model — Combinations

A combination multiplies load cases by coefficients. There are two ways to get one into the project:

- Create it yourself with `AddCombination`.
- Let Advance Design generate the whole set with `GenerateCombinations`, the way the **Combinations** dialog
  does.

Both kinds are ordinary combinations. Read them back with `GetInformationalElementsObject` and change them with
`UpdateInformationalElement` ([Section 3.5](#35-model--informational-elements)). The objects are described in
[Section 10.3](#103-combinations).

| Endpoint | Method | What it does |
|---|---|---|
| `/api/Model/combinations/AddCombination` | `POST` | Creates one combination from load cases and coefficients |
| `/api/Model/combinations/GenerateCombinations` | `POST` | Generates the combination set from the load cases and the model settings |

The options that drive the generation are model settings: they are read and written with `GetModelSettings` /
`SetModelSettings` / `ResetModelSettings` ([Section 3.7](#37-model--general-settings)) and are described in
[3.6.3](#363-settings-of-the-automatic-generation).

> **Note:** Warnings only reach `Details.Diagnostics` when the session was opened with `LogVerbosity =
> ErrorsAndWarnings` or `AllDetails` ([Section 3.1.1](#311-the-environment-of-a-session)). Several rules below
> are reported as warnings.

---

#### `POST /api/Model/combinations/AddCombination`

Creates a combination in the open project.

| Parameter | In | Type | Required | Description |
|---|---|---|---|---|
| *(body)* | body | [`Combination`](#103-combinations) | Yes | The combination |

**Returns:** `EIDApiResponse` — `Data` is the EID of the new combination.

`ECombinationType` (type `eCombinationType`) is one of `eComboProjectSituationEluStrgeo`, `eComboProjectSituationEluEqu`,
`eComboProjectSituationElsCharacteristic`, `eComboProjectSituationElsFrecvent`,
`eComboProjectSituationElsQuasiPermanent`, `eComboProjectSituationELUA`, `eComboProjectSituationElsA` or
`eComboProjectSituationNone`.

```csharp
var combo = new Combination
{
    ECombinationType = ECombinationType.EComboProjectSituationEluStrgeo,
    ListCasesCoeffs  = new List<EIDDoublePair>
    {
        new EIDDoublePair { Key = new EID { Value = deadLoadId }, Value = 1.35 },
        new EIDDoublePair { Key = new EID { Value = liveLoadId }, Value = 1.5  }
    },
    // The design calculations that verify this combination - see 3.6.1.
    DesignCalculations = new List<ECombinationDesignCalculation> { ECombinationDesignCalculation.SteelProfiles }
};
EIDApiResponse r = client.AddCombination(combo);
```

```json
{
  "$type": "Combination",
  "eCombinationType": "eComboProjectSituationEluStrgeo",
  "listCasesCoeffs": [
    { "key": { "value": 101 }, "value": 1.35 },
    { "key": { "value": 102 }, "value": 1.5 }
  ],
  "designCalculations": [ "SteelProfiles" ]
}
```

Rules:

- `ECombinationType` is required and must not be `eComboProjectSituationNone`.
- `ListCasesCoeffs` needs at least one pair. Each key is the EID of an existing load case. A coefficient of `0`
  is refused.
- `IdComb` (the "No." of the combination) and `UserName` are read-only. Advance Design assigns them; do not
  send them.
- A refused combination returns `Data.Value = 0`, `Details.Success = false` and an `ENGINE_ERROR` message that says
  why. `COMBINATION_CREATED` can still appear in the same response, so test `Data.Value` or `Details.HasErrors`, not
  the code.
- Without `DesignCalculations` the combination takes part in no design calculation, and a design calculation
  refuses to run when no combination names it. See [3.6.1](#361-design-expert-calculations-of-a-combination).

Codes: `COMBINATION_NULL_INPUT` (no body), `COMBINATION_CREATED`, `COMBINATION_CREATION_FAILED` (internal error).

---

##### 3.6.1 Design (Expert) calculations of a combination

A design engine verifies only the combinations that name its calculation. `Combination.DesignCalculations` is the
list of those calculations (`SteelProfiles`, `SteelDeflections`, `ConcreteDesign`, ...; the full list is in
[10.3](#103-combinations)). The coefficients each engine applies are in four optional groups on the combination:
`ConcreteDesign`, `SteelDesign`, `TimberDesign` and `CompositeDesign`.

Every property of every group is optional. A property left `null` keeps the value already stored, or takes the
default of the Advance Design combinations dialog (the partial safety factors come from the model and the
design settings; the defaults are listed in [10.3](#103-combinations)).

**What is written, per design calculation**

| Listed in `DesignCalculations` | Group supplied | Result |
|---|---|---|
| yes | yes | The supplied properties are written. Properties left `null` keep the stored value, or take the default if nothing is stored |
| yes | no | Defaults are written if the combination does not carry the values yet. A warning is reported. Values already stored are not changed |
| no | yes | The values are stored. A warning says that no design calculation will use them |
| no | no | Nothing is written |

**`DesignCalculations` when you create and when you update**

| Value | `AddCombination` | `UpdateInformationalElement` |
|---|---|---|
| absent / `null` | No design calculation | The stored selection is left unchanged |
| `[]` (empty list) | No design calculation | The combination is taken out of every design calculation |
| `[...]` | Exactly these | Exactly these; the previous selection is replaced |

Rules:

- An update still has to send `ListCasesCoeffs` (at least one pair) and a combination type other than
  `eComboProjectSituationNone`.
- A calculation listed twice is applied once.
- A combination generated by `GenerateCombinations` already lists its design calculations
  ([3.6.2](#362-automatic-generation-of-combinations)). A combination created by hand does not.

> **Warning:** Set `DesignCalculations` before `LaunchAnalysis` ([Section 3.8](#38-model--analysis)). The design
> engines read the analysis model, which copies the selection when it is built. Changing `DesignCalculations` on
> an analysed model has no effect on a design calculation until `LaunchAnalysis` runs again.

```csharp
// A combination checked for steel strength and steel deflection.
// Explicit factors on the deflection side, model defaults on the strength side.
var combo = new Combination
{
    ECombinationType = ECombinationType.EComboProjectSituationElsCharacteristic,
    ListCasesCoeffs  = new List<EIDDoublePair>
    {
        new EIDDoublePair { Key = new EID { Value = deadLoadId }, Value = 1.0 },
        new EIDDoublePair { Key = new EID { Value = liveLoadId }, Value = 1.0 }
    },
    DesignCalculations = new List<ECombinationDesignCalculation>
    {
        ECombinationDesignCalculation.SteelProfiles,
        ECombinationDesignCalculation.SteelDeflections
    },
    SteelDesign = new SteelCombinationDesign
    {
        Deflections = new CombinationDeflectionVerification
        {
            Criterion    = EDeflectionCriterion.FirstCriterion,
            SafetyFactor = 1.0
        }
        // Profiles left null: created with the model defaults, and a warning is reported.
    }
};
client.AddCombination(combo);
```

---

##### 3.6.2 Automatic generation of combinations

#### `POST /api/Model/combinations/GenerateCombinations`

Generates the combination set from the load cases and writes it into the project. It runs the same generation as
the **Combinations** dialog of Advance Design (the *Simplified Comb.* and *Detailed Comb.* buttons).

The standard is not a parameter. Everything is read from the model:

- The permanent actions use the coefficients of their own load cases.
- Every variable action (live, snow, wind, temperature) uses the γ and ψ of the generation settings of the model.
  The same settings say which project situations are generated.

The request only chooses the work-flow. Write the settings first, with `SetModelSettings`
([3.6.3](#363-settings-of-the-automatic-generation)).

| Parameter | In | Type | Required | Description |
|---|---|---|---|---|
| *(body)* | body | [`CombinationsGenerationRequest`](#103-combinations) | Yes | The work-flow to run |

**Returns:** `CombinationsGenerationResultApiResponse` — `Data` is a [`CombinationsGenerationResult`](#103-combinations).

```csharp
CombinationsGenerationResultApiResponse r = client.GenerateCombinations(new CombinationsGenerationRequest
{
    Mode = ECombinationsGenerationMode.Simplified
});

Console.WriteLine($"{r.Data.CreatedCombinations.Count} combination(s), No. {r.Data.FirstIdentifier} to {r.Data.LastIdentifier}");

// The generated combinations are ordinary combinations: read them back like any other.
var objects = client.GetInformationalElementsObject(r.Data.CreatedCombinations.Select(e => e.Value).ToList());
```

```json
{ "mode": "Simplified" }
```

Rules:

- Generation is additive. Existing combinations are kept and the new ones are numbered after them, so a second
  call on the same model doubles the set.
- The generated combinations already list their design calculations (`DesignCalculations`): ULS combinations
  for the strength checks, SLS combinations for the deflection checks. A combination created by hand needs
  `DesignCalculations` ([3.6.1](#361-design-expert-calculations-of-a-combination)).
- The generation does not write the design coefficients of those calculations. Before a steel design, read
  each generated combination and send it back with `UpdateInformationalElement`: the default coefficients
  are then written (the second row of the table in 3.6.1). Do it before `LaunchAnalysis`. If you skip it,
  the steel design can leave the combinations out: `LaunchDesignCalculation` then reports *"Following load
  combinations were not taken into account for steel calculation"* and the work ratios are 0.
  [Example 5](#example-5--steel-design-combinations-analysis-design-results) shows the sequence.
- Leave `UpdateLoadCaseCodes` unset (it means `true`). The generation matches load cases on their normative code,
  and a load case created through the API carries a neutral one: with `false`, nothing is generated on such a
  model. Send `false` only for a project file whose codes were set on purpose.
- `DetailedMatrix` uses the concomitance rules stored in the model. The API cannot edit them, so use this mode on a
  model opened from a project file whose author set them up. `ApplyDefaultConcomitanceRules = true` resets them
  to the defaults first.
- The number of combinations is limited by the maximum set in the model (an option of the desktop
  application). A larger set is cut to the limit and `LimitExceeded` is `true`; the combinations beyond the limit do not exist.
- Nothing generated is not an error: `Success = true`, an empty `CreatedCombinations` and the warning
  `COMBINATIONS_GENERATION_EMPTY`. The reason is in `Details.Diagnostics`.
- The combination standard is a setting of the session. It is chosen once, by the call that opens the project
  (`Environments.DesignCodes.Combinations`), and otherwise follows `Localization`
  ([Section 3.1.1](#311-the-environment-of-a-session)). The same model generates different sets under different
  localizations.
- The call also saves the project (see `CloseProjectWithoutSave` in [Section 3.1](#31-project-management)).

> **Warning:** Leave `CombinationsCoefficients` unset on the load cases and on their families
> ([Section 10.2](#102-load-cases)). When it is `null` (the default of the generated clients), Advance Design
> fills it from the type of the load case: dead 1.10 / 1.35; live 1.50 with ψ 0.7 / 0.5 / 0.3; snow 1.50 with
> 0.5 / 0.2 / 0 (French annex, up to 1000 m); wind 1.50 with 0.6 / 0.2 / 0. A block you send replaces all of
> these values, including the ones you did not mean to change. Assign it only to override a coefficient on
> purpose, and then send the whole block.

Codes: `COMBINATIONS_GENERATION_NULL_INPUT` (no body), `COMBINATIONS_GENERATED`, `COMBINATIONS_GENERATION_EMPTY`
(warning), `COMBINATIONS_GENERATION_FAILED` (internal error). Engine messages come as `ENGINE_WARNING` /
`ENGINE_ERROR`.

---

##### 3.6.3 Settings of the automatic generation

The generation options are the ones of the *Combinations options* dialog: which project situations are generated,
and the γ / ψ coefficients of each action family. They belong to the model, not to a combination. Write them once,
before `GenerateCombinations`.

They are one group of the general model settings, category `CombinationsGeneration`, and have no endpoint of
their own. Use the three calls of [Section 3.7](#37-model--general-settings):

| Call | With | Returns |
|---|---|---|
| `GetModelSettings` | `category = CombinationsGeneration` | `ModelSettingsBaseApiResponse` — one of the shapes below |
| `SetModelSettings` | one of the shapes below | `BooleanApiResponse` |
| `ResetModelSettings` | `category = CombinationsGeneration` | `BooleanApiResponse` |

`ResetModelSettings` is the *Load defaults* button: it restores the values that the active standard and annex
prescribe.

**One shape per standard.** Eurocode, US, Canada and EN 13031-1 (greenhouses) have different options, so the
settings are polymorphic on `$type` ([Section 5](#5-polymorphism--the-type-discriminator)). The shape to use is
the one of the standard of the session; `CombinationsGenerationSettings.Standard` reports it. The `$type` on the
wire is always one of the shapes below.

| Shape | Standards it covers |
|---|---|
| `EurocodeCombinationsGenerationSettings` | `Eurocode` (any national annex), `EurocodeItalia`, `NTC2018` |
| `USCombinationsGenerationSettings` | `US`, `US2018` |
| `CanadaCombinationsGenerationSettings` | `Canada`, `CanadaNBC2015`, `CanadaCNB2020` |
| `GreenhouseCombinationsGenerationSettings` | `Greenhouse_EN13031_1` |
| `Eurocode2ndGenCombinationsGenerationSettings` | EN 1990:2023 (see the last rule) |

The properties of each shape are in [Section 10.3](#103-combinations).

```csharp
// Read the settings of the model and the standard they belong to.
ModelSettingsBaseApiResponse got = client.GetModelSettings(EModelSettingsCategory.CombinationsGeneration);
if (got.Data is EurocodeCombinationsGenerationSettings current)
    Console.WriteLine($"{current.Standard} / {current.Annex}");

// Change two things. Everything that is not sent keeps its value.
client.SetModelSettings(new EurocodeCombinationsGenerationSettings
{
    ProjectSituations = new EurocodeProjectSituations { SlsFrequent = true },
    LiveLoads         = new EurocodeLiveLoadSettings   { Category = EExploitationCategory.CategoryC }
});
```

Rules:

- Every property is optional and what you do not send keeps its value: a group changes only the properties it
  carries. A `Get`, edit, `Set` round trip is therefore safe.
- `Standard` and `Annex` are read-only here. A value sent for them is ignored. They are set when the project is
  opened ([Section 3.1.1](#311-the-environment-of-a-session)).
- A category dictates its coefficients. Where the standard tabulates ψ (or γ) against a category, the category
  decides and the coefficient is read-only, as in the dialog. Setting `LiveLoads.Category` recomputes ψ0 / ψ1 / ψ2
  from the table of the annex. A coefficient sent with it is ignored and reported as a warning, not refused, so
  that sending back what a `Get` returned never fails. Each family has one *user defined* category that unlocks
  its coefficients; it is the only way to impose your own.
- The live-load category list depends on the annex: thirteen categories under the French annex (A, B, C, D, E1,
  E2a, E2b, E2c, F, G, H, K, K1), eight under the Czech annex (A to H), ten everywhere else (A to J).
  `LiveLoads.Category` may only name a category that means the same under the active annex (A to D under the
  French annex, A to H under the Czech annex); any other value is refused. Use `LiveLoads.CategoryIndex` for the
  rest. It wins when both are sent, and `Get` always fills it.
- `AvailableCategoryCount` and `UserDefinedCategory` (read-only) give the size of the list of the annex: 13, 8
  or 10 for live loads; 3 for snow; 1 for wind and temperature.
- At least one project situation and at least one variable-action family as a leading action must stay on, as in
  the dialog. A request that would leave none is refused (`MODEL_SETTINGS_WRITE_REFUSED`).
- A field that the active standard or annex does not offer is stored, with a warning that the generation does not
  read it. This covers `UlsGeotechnical`, `Equation` and `SlsCharacteristicIncludesPermanent`
  ([10.3](#103-combinations) says where each is offered).
- 2nd generation Eurocode (EN 1990:2023): `Eurocode2ndGenCombinationsGenerationSettings` is published, but the
  standard is not released. An ordinary installation refuses a model configured for it (`GetModelSettings`,
  `SetModelSettings`, `ResetModelSettings`, `GenerateCombinations`). Its properties are not described here.

---

### 3.7 Model — General settings

General settings are the options of the model as a whole, not of one element, load case or combination. They
are what the option dialogs of the desktop application edit: the combination generation options, the steel
*Calculation settings*, the model tolerances, the structure work space. Three endpoints serve every group.
The group is chosen by an `eModelSettingsCategory` value on the two calls that read and reset, and by the
`$type` of the body on the call that writes ([Section 5](#5-polymorphism--the-type-discriminator)).

| Endpoint | Method | What it does |
|---|---|---|
| `/api/Model/model/GetModelSettings` | `GET` | Reads one group of settings |
| `/api/Model/model/SetModelSettings` | `POST` | Writes one group of settings |
| `/api/Model/model/ResetModelSettings` | `POST` | Puts one group back to its defaults |

| `eModelSettingsCategory` | Class to send (`$type`) | What it holds | Described in |
|---|---|---|---|
| `CombinationsGeneration` | the class for the model's combination standard | Options of the automatic combination generation | [3.6.3](#363-settings-of-the-automatic-generation) |
| `SteelEc3Calculation` | `Ec3SteelCalculationSettings` | Steel *Calculation settings* of a Eurocode 3 model | [3.7.1](#371-steel-eurocode-3-calculation-settings) |
| `SteelAiscCalculation` | `AiscSteelCalculationSettings` | Steel *Calculation settings* of an AISC or CAN/CSA model | [3.7.2](#372-steel-aisc--can-csa-calculation-settings) |
| `Tolerances` | `ToleranceSettings` | Geometric, mesh and composite tolerances | [3.7.3](#373-model-tolerances) |
| `Structure` | `StructureSettings` | Work space, bending stiffness, reference temperature | [3.7.4](#374-structure-work-space) |

---

#### `GET /api/Model/model/GetModelSettings`

Reads one group of settings.

Parameter: `category` (query, `eModelSettingsCategory`, required).

**Returns:** `ModelSettingsBaseApiResponse` — `Data` is the settings object of the group, with every property
filled. Its `$type` is the concrete class, so cast it (`(ToleranceSettings)r.Data`).

```csharp
var r = client.GetModelSettings(EModelSettingsCategory.Tolerances);
var tol = (ToleranceSettings)r.Data;
Console.WriteLine(tol.MeshTolerance);
```

- `SettingsCategory` in the answer says which group answered. It is read-only.
- `category = None` is what an omitted query parameter becomes. It is refused.
- `MODEL_SETTINGS_UNAVAILABLE`: the category is `None` or unknown, or the group does not exist for the model's
  steel design code. The engine messages say which.
- `MODEL_SETTINGS_READ_FAILED`: internal error.

---

#### `POST /api/Model/model/SetModelSettings`

Writes one group of settings. The `$type` of the body chooses the group; there is no `category` parameter.

Parameter: the body, a `ModelSettingsBase` (required): an instance of the class of the group (table above).

**Returns:** `BooleanApiResponse` — `Data` is `true` when the settings were written.

```csharp
client.SetModelSettings(new ToleranceSettings { MeshTolerance = 0.002 });
```

Rules:

- Every property is optional. `null` keeps the current value, so a request that carries one property changes
  that property only. A Get, change one value, Set round trip is safe.
- A value that the model derives from another one cannot be imposed. It is ignored and reported as a warning,
  not refused, so handing back what a Get returned never fails. Each group lists what it derives.
- In the groups below, one invalid value (out of range, `NaN` or `Infinity`) refuses the whole request, names
  the property and writes nothing.
- `SettingsCategory` is read-only. A value sent in a Set is ignored.
- `ModelSettingsBase` itself carries no settings and is refused.
- Warnings are only returned when the session runs with `LogVerbosity = ErrorsAndWarnings` or `AllDetails`
  ([3.1.1](#311-the-environment-of-a-session)).

| Code | Meaning |
|---|---|
| `MODEL_SETTINGS_NULL_INPUT` | The body is missing |
| `MODEL_SETTINGS_WRITE_REFUSED` | Nothing was written. The engine messages say why: a `$type` that names no group, a group that does not exist for the steel code, a value out of range |
| `MODEL_SETTINGS_WRITE_FAILED` | Internal error while writing |

---

#### `POST /api/Model/model/ResetModelSettings`

Puts one group back to the values that the model's own standard and national annex prescribe (the *Load
defaults* button of the dialog).

Parameter: `category` (query, `eModelSettingsCategory`, required).

**Returns:** `BooleanApiResponse` — `Data` is `true` when the defaults were written.

- `category = None` or an unknown category is refused: `MODEL_SETTINGS_RESET_REFUSED`, as is a group that does
  not exist for the steel code. `MODEL_SETTINGS_RESET_FAILED` is an internal error.

---

#### 3.7.1 Steel (Eurocode 3) calculation settings

Category `SteelEc3Calculation`, class `Ec3SteelCalculationSettings`. These are the general options of the
Expert steel design calculation, as the *Calculation settings* dialog edits them. They belong to the model.
What one member contributes on its own (its buckling lengths, its lateral-torsional buckling data, its
deflection limits) lives on the element or on its [design template](#106-expert-design-templates).

- The group exists for the steel codes `eurocode`, `ntc2008` and `ntc2018`. Under any other steel code all
  three calls are refused and the message names the code. A North-American model is told to use
  [`SteelAiscCalculation`](#372-steel-aisc--can-csa-calculation-settings).
- The steel code is a session setting: `Environments.DesignCodes.Steel`, or the one the localization selects
  ([3.1.1](#311-the-environment-of-a-session)). `DesignCode` reports it and is read-only here.
- 2nd generation Eurocode 3 (EN 1993-1-1:2023) is not released. The `SecondGeneration` sub-objects
  (`Ec3G2VerificationOptions`, `Ec3G2SafetyFactorOptions`) are published but ignored with a warning on an
  ordinary installation.

> **Warning:** Call `SetModelSettings` before `LaunchAnalysis` ([Section 3.8](#38-model--analysis)). The
> design engine runs on a copy of the model that is built during the analysis. Settings written afterwards
> are not seen by the design calculation until that copy is rebuilt, and no message tells you so.

The object has seven optional sub-objects, one per tab of the dialog: `Verification`
(`Ec3SteelVerificationSettings`), `SafetyFactors` (`Ec3SteelSafetyFactorSettings`), `Optimization`
(`SteelOptimizationSettings`), `ProfileSorting` (`SteelProfileSortingSettings`), `Buckling`
(`SteelBucklingSettings`), `AngleVerification` (`SteelAngleVerificationSettings`) and `CalculationSequence`
(`SteelCalculationSequenceSettings`). The last five are shared with
[3.7.2](#372-steel-aisc--can-csa-calculation-settings). A `null` sub-object keeps the whole tab.
Inside a sub-object every property is optional too (`bool?`, `double?`, `int?` or a nullable enumeration; the
enumerations are in [Section 13](#13-enumerations-reference)). Units are SI, with two differences from the
dialog: the fire time is in seconds and the profile limits are in metres.

##### `Verification`

| Property | Unit | Default | Meaning |
|---|---|---|---|
| `CrossSectionStrength`, `ElementStability` | | `true` | Verify the strength of the cross-sections / the stability of the elements |
| `CalculationType` | | `eTypeDeCalculBiaxialBending` | Which internal forces the verification combines |
| `FireVerification` | | `false` | Perform the fire verification |
| `FireResistanceTime` | s | `0` | Fire resistance duration, 0 to 1,000,000. 30 minutes is `1800`. Read only while `FireVerification` is on |
| `GlobalSwayImperfections`, `LocalBowImperfections` | | `false` | Account for the imperfection |
| `CrownImperfections` | | | Crown imperfection of EN 13031-1. Offered only on installations configured for greenhouses; stored otherwise but not read |
| `SecondOrderAnalysis` | | `false` | 2nd order analysis of the static combinations |
| `ColumnsBucklingLength`, `BeamsBucklingLength` | | `imposed` | Where the buckling length of columns / beams comes from (`imposed` = the element properties) |
| `ImperfectionGeneration` | | `eImperfectionGenerationForGoverningComb` | Which combinations the imperfections are generated for |
| `PlateBucklingMethod` | | `eMethodWebBucklingEffectiveCrossSections` | How a slender web is verified |
| `SecondGeneration` | | | 2nd generation additions (ignored, see above) |

- The three imperfections gate other options. `SecondOrderAnalysis`, `ColumnsBucklingLength` and
  `ImperfectionGeneration` are read only while any of the three imperfections is on. `BeamsBucklingLength` is
  read only while `LocalBowImperfections` is on. A value sent while its gate is off is stored and reported as
  a warning.
- A request that turns the last imperfection off also clears `SecondOrderAnalysis`, as the dialog does.
- `eMethodWebBucklingReducedStresses` is accepted everywhere. Under the Czech and Slovak national annexes it
  is reported as a warning.
- `eImperfectionGenerationUndefined` and `eMethodWebBucklingUndefined` mean "no option selected". A Get
  returns them for a model created under another code. Sending back the value the model already holds does
  nothing. Moving a model to one of them is refused.

##### `SafetyFactors`

| Property | Default | Meaning |
|---|---|---|
| `Option` | `eSafetyFactorsAuto` | `eSafetyFactorsAuto`: the code and its national annex decide. `eSafetyFactorsImposed`: your values are used |
| `GammaM0` | 1.00 | Resistance of cross-sections |
| `GammaM1` | 1.00 | Resistance to instability |
| `GammaM2` | 1.25 | Resistance of the net section at bolt holes (angle verification) |
| `GammaMfi` | 1.00 | Accidental fire situation |
| `SecondGeneration` | | 2nd generation addition (ignored, see above) |

- Each gamma accepts 0 to 100.
- The gamma factors are ignored with a warning unless `Option` is `eSafetyFactorsImposed`. Send the option
  and the values in the same request.
- The defaults above are those of a plain Eurocode model. The German annex gives `GammaM1` 1.10 and
  `GammaMfi` 1.15. The Italian annex and the NTC standards give `GammaM0` and `GammaM1` 1.05. A Get shows
  the values in force.

##### `Optimization`

Controls when the shape calculation may replace a section. It is read only by a run that calculates shapes
(`CalculationSequence.CalculateShapes`). Percentages are integers from 0 to 100.

| Property | Unit | Default | Meaning |
|---|---|---|---|
| `Mode` | | `eOptimizationParElements` | What changes together when a section is replaced: each element, same section, same design template, same name, same structural system |
| `OptimizeWhenStrengthRatioAbove` + `StrengthRatioUpperLimit` | % | `true`, 100 | Replace the section when the strength / stability work ratio exceeds the limit |
| `OptimizeWhenStrengthRatioBelow` + `StrengthRatioLowerLimit` | % | `false`, 50 | Replace the section when the work ratio falls below the limit (over-designed section) |
| `OptimizeWhenDeflectionRatioAbove` + `DeflectionRatioUpperLimit` | % | `true` (Eurocode), 100 | Replace the section when the deflection ratio exceeds the limit |
| `SearchRange` + `MaximumIterationCount` | | `eProfileOptimizationSearchRangeAuto`, 8 | How far the search may go. The count (1 to 100) is read only with `eProfileOptimizationSearchRangeAutoWithMaxLimit` |

- An upper work-ratio limit at or below the lower one is refused while both criteria are on.
- `eOptimizationNonExistant` is refused.

##### `ProfileSorting`

The order in which candidate sections are tried, and the size band they come from. The limits are in metres
(0 to 1,000,000), each read only while its switch is on. A minimum is not checked against its maximum.

| Property | Unit | Default | Meaning |
|---|---|---|---|
| `SortCriterion` | | `eTriCriteriaEnveloppe` (Eurocode) | Which work ratio decides that a candidate is acceptable |
| `OrderingCriterion` | | `eProfileOptimizationOrderedByWeight` | The order the candidates are tried in |
| `LimitMinimumHeight` + `MinimumHeight`, `LimitMaximumHeight` + `MaximumHeight` | m | `false`, 0.10 / 1.00 | Smallest and largest section height |
| `LimitMinimumWidth` + `MinimumWidth`, `LimitMaximumWidth` + `MaximumWidth` | m | `false`, 0.10 / 1.00 | Smallest and largest section width |

##### `Buckling`

`Method` is the buckling length method. Default `eMethodBucklingN1_N2`.

- Under the Eurocode family only `eMethodBucklingKa_Kb` and `eMethodBucklingN1_N2` are accepted. Any other
  method is refused.
- Sending back the method the model already holds is always accepted. This includes `eMethodBucklingNone`
  ("no method selected"), which a Get can return for a model converted from another code. Moving a model to it
  is refused.

##### `AngleVerification`

Bolt geometry for the net area of angle sections. The values are multiples of the bolt hole diameter d0, not
lengths.

| Property | Default | Meaning |
|---|---|---|
| `EdgeDistanceFactorE1`, `EdgeDistanceFactorE2` | 2.0 | e1 and e2, longitudinal and transversal edge distance. 1.2 to 100 |
| `BoltSpacingFactorP1` | 3.0 | p1, spacing between bolts. 2.2 to 100 |

##### `CalculationSequence`

Which steps a steel design run performs. A step that is off gives empty result families in `GetDesignResults`.

| Property | Default | Meaning |
|---|---|---|
| `Verify`, `CalculateBuckling`, `VerifyDeflections`, `CalculateShapes` | `true` | Verify the elements; compute the buckling lengths; verify the deflections; run the shape (section optimisation) calculation |
| `ChainedOptimization` | `false` | Repeat the sequence until it settles |
| `CompleteCalculationCount` | 1 | Maximum number of complete calculations, 1 to 20. Read only while `ChainedOptimization` is on |

- `ChainedOptimization` together with `GlobalSwayImperfections` is stored and reported as a warning, not
  refused. The imperfection forces are not regenerated between chained iterations, so the two do not belong
  together. The warning exists only under a Eurocode steel code.

##### Rules for the whole group

- Send only the sub-object you change. Echoing a whole Get re-sends read-only values and collects their
  warnings.
- A value whose control is greyed out in the dialog is stored, not refused, and a warning says that nothing
  reads it. This applies to the fire time, the optimisation limits, the iteration ceiling, the profile limits
  and the complete-calculation count. Send the value together with its switch to avoid the warning.
- `ResetModelSettings` restores what a new model with this steel code and national annex carries. A German
  project comes back with `GammaM1` = 1.10, not 1.00.

```csharp
// Read what the design engine will run with.
var r = client.GetModelSettings(EModelSettingsCategory.SteelEc3Calculation);
var steel = (Ec3SteelCalculationSettings)r.Data;
Console.WriteLine(steel.DesignCode);                 // Eurocode, Ntc2008 or Ntc2018

// Impose gamma factors. Only this tab is changed.
client.SetModelSettings(new Ec3SteelCalculationSettings
{
    SafetyFactors = new Ec3SteelSafetyFactorSettings
    {
        Option  = ESteelSafetyFactorsOption.ESafetyFactorsImposed,
        GammaM0 = 1.00,
        GammaM1 = 1.10
    }
});

client.LaunchAnalysis();
```

The same call as raw JSON (`$type` first, camelCase properties, enumerations by name):

```json
{
  "$type": "Ec3SteelCalculationSettings",
  "safetyFactors": { "option": "eSafetyFactorsImposed", "gammaM0": 1.0, "gammaM1": 1.1 },
  "calculationSequence": { "verify": true, "calculateBuckling": false }
}
```

---

#### 3.7.2 Steel (AISC / CAN-CSA) calculation settings

Category `SteelAiscCalculation`, class `AiscSteelCalculationSettings`. The same *Calculation settings* dialog,
for a model designed to a North-American steel code.

- One class covers six steel codes: `aisc`, `aisc_2016`, `can_csa_s16_2009`, `can_csa_s16_2014`,
  `can_csa_s16_2019` and `can_csa_s16_2024`. They are treated identically. `DesignCode` reports which one the
  model has and is read-only here.
- The steel code is a session setting ([3.1.1](#311-the-environment-of-a-session)). When `Localization`
  selects it: `US` gives `aisc_2016`, `CANADA` gives `can_csa_s16_2019`, `ASIA` gives `aisc`. `Environments`
  does not accept `can_csa_s16_2024` as an input.
- Under any other steel code all three calls are refused. A Eurocode model is told to use
  [`SteelEc3Calculation`](#371-steel-eurocode-3-calculation-settings).
- Call it before `LaunchAnalysis` (see the warning in [3.7.1](#371-steel-eurocode-3-calculation-settings)).

The object has six optional sub-objects: `Verification` (`AiscSteelVerificationSettings`, *Verifications*)
and `Optimization`, `ProfileSorting`, `Buckling`, `AngleVerification`, `CalculationSequence`. The last five
are the classes of [3.7.1](#371-steel-eurocode-3-calculation-settings), with the same properties, ranges,
units (profile limits in metres), defaults and warnings, except for `Buckling` (below). A `null` sub-object
keeps the whole tab.

- `Verification` has three properties only: `CrossSectionStrength` and `ElementStability` (default `true`)
  and `CalculationType` (default `eTypeDeCalculBiaxialBending`).
- `Buckling.Method` accepts `eMethodBucklingGa_Gb` only, which is also its default. Any other method is
  refused. Sending back the method the model already holds is always accepted.
- `ChainedOptimization` raises no sway warning here.

Not in this group, because they do not exist for North-American codes: `SafetyFactors` (the verification
uses the resistance factor phi for LRFD or the safety factor Omega for ASD given by the code, not an editable
gamma), the fire verification and its time, the imperfections, `SecondOrderAnalysis`, the buckling-length
options, `ImperfectionGeneration` (EN 1993-1-1 options), `PlateBucklingMethod` (EN 1993-1-5) and
`SecondGeneration`. The model still holds those values. A write to this group never changes them, and `ResetModelSettings` leaves
them alone. The lateral-torsional-buckling factors Kt and Kz of this family are not exposed either. The
settings apply to the whole model whether or not an element has an
[expert design template](#106-expert-design-templates).

```csharp
var r = client.GetModelSettings(EModelSettingsCategory.SteelAiscCalculation);
var steel = (AiscSteelCalculationSettings)r.Data;
Console.WriteLine(steel.DesignCode);                 // Aisc_2016, Can_csa_s16_2019, ...
Console.WriteLine(steel.Buckling.Method);            // EMethodBucklingGa_Gb

// Change the verification tab only.
client.SetModelSettings(new AiscSteelCalculationSettings
{
    Verification = new AiscSteelVerificationSettings
    {
        CalculationType = ESteelCalculationType.ETypeDeCalculBiaxialBending
    }
});
```

---

#### 3.7.3 Model tolerances

Category `Tolerances`, class `ToleranceSettings`. The geometric tolerances of the model: the values of the
*Advanced CAD properties* (*General* page), the *Mesh options* and the *Composite calculation settings*
(*Verification* page). They do not depend on any design code.

The five properties are optional `double?`. Distances are in metres and angles in radians.

| Property | Unit | Range | Default | Meaning |
|---|---|---|---|---|
| `CadDistanceTolerance` | m | 1e-12 to 0.05 | 1e-8 | Two points closer than this are the same point (*Minimum distance between 2 points*). Applies to the whole server process |
| `VerticalAngularTolerance` | rad | greater than 0.01, up to pi/6 (0.5235988) | pi/60 (3 deg, 0.0523599) | A linear element whose axis is within this angle of the global Z axis is vertical (a column) rather than a beam (*Tolerance for vertical elements*). Applies to the whole server process |
| `ClimaticRoofAngularTolerance` | rad | 0 to 5 deg (0.0872665) | pi/60 (3 deg) | Angular tolerance for the coplanarity of roof planes in the climatic (wind) load generation |
| `MeshTolerance` | m | 1e-12 to 0.05 | 1e-3 | Distance tolerance of the mesh, the finite element solve and the verifications (*Mesh tolerance*) |
| `ParallelComponentsTolerance` | rad | 0 to 50 deg (0.8726646) | 10 deg (0.1745329) | Angle under which a beam or a floor deck is parallel to a composite beam (*Tolerance for parallel components*) |

- The ranges are those of the desktop dialogs, except the lower bound of `VerticalAngularTolerance`. It is
  exclusive, although the dialog accepts 0.01, because the desktop application replaces a stored value of 0.01
  or less by 3 deg when it opens the model.
- `CadDistanceTolerance` and `VerticalAngularTolerance` apply to every geometric operation of the server
  process: when two points are the same point, and when an element is vertical, which decides its default
  local axes. The session takes them from the model when `NewProject` or `OpenProject` opens it, and
  `SetModelSettings` and `ResetModelSettings` change them at once. The server holds one project at a time.
- Call it before you create the geometry the tolerances should govern, and before `LaunchAnalysis`.
- `ResetModelSettings` restores the defaults of a new model.

```csharp
client.SetModelSettings(new ToleranceSettings
{
    CadDistanceTolerance     = 1e-6,                 // m
    VerticalAngularTolerance = 5.0 * Math.PI / 180.0 // rad (5 deg)
});
```

---

#### 3.7.4 Structure (work space)

Category `Structure`, class `StructureSettings`. The work space of the structure (*Plane / Grid / 3D* of the
*Structure* dialog), with its bending stiffness and its reference temperature. The work space decides which
degrees of freedom the finite element model has. It does not depend on any design code.

| Property | Unit | Default | Meaning |
|---|---|---|---|
| `Workspace` (`eStructureWorkspace?`) | | `spaceWorkspace` (`planeWorkspace` without the 3D module) | The work space |
| `BendingStiffStructure` (`bool?`) | | `true` | *Bending stiff / Truss*: `true` for a structure rigid in bending (frames), `false` for a truss |
| `ReferenceTemperature` (`double?`) | degC | 0 | A thermal load acts with its uniform temperature minus this one. Any finite value |

| `eStructureWorkspace` | Model | Degrees of freedom |
|---|---|---|
| `planeWorkspace` | 2D, in the global XZ plane | TX, TZ, RY |
| `gridWorkspace` | 2D, in the global XY plane, loaded along Z | TZ, RX, RY |
| `spaceWorkspace` | 3D | all six |

- `spaceWorkspace` needs the 3D module of the licence and is refused without it.
- A Set does what the dialog does on OK. The default view follows the work space (front view for a plane
  structure, top view for a grid, perspective view for a structure that becomes 3D; one that already is 3D
  keeps its view). For a plane or grid structure the imposed-displacements template is reset to the degrees of
  freedom of the work space. A refused request leaves the model as it was.
- Set the work space right after `NewProject`, before you create any element. A planar model built in 3D has
  no stiffness out of its plane (a node reached by bars only has none along Y), and `LaunchAnalysis` fails.
- `ResetModelSettings` restores the defaults of a new model.

```csharp
// A planar truss in the XZ plane: make the model a plane structure before creating it.
client.NewProject(projectFile, environments);
client.SetModelSettings(new StructureSettings { Workspace = EStructureWorkspace.PlaneWorkspace });

// ... create the elements, supports and loads, then
client.LaunchAnalysis();
```

---

### 3.8 Model — Analysis

These endpoints run the calculation of the model and read what it produced. The order is `LaunchAnalysis`, then
optionally `LaunchDesignCalculation`, then the read endpoints. `GetAnalysisModelStatus` tells you where the model
stands. A project must be open: `PROJECT_NOT_OPEN`.

| Endpoint | Method | What it does |
|---|---|---|
| `/api/Model/analysis/LaunchAnalysis` | `POST` | Meshes the model and runs the finite-element calculation |
| `/api/Model/analysis/LaunchDesignCalculation` | `POST` | Runs the design calculation of one material family |
| `/api/Model/analysis/GetAnalysisModelStatus` | `GET` | Says how far the model has been calculated |
| `/api/Model/analysis/GetResults` | `POST` | Returns finite-element results of one load case or combination |
| `/api/Model/analysis/GetResultsByQuery` | `POST` | Same, from a query object. Needed for buckling modes |
| `/api/Model/analysis/GetDesignResults` | `POST` | Returns design results of steel linear elements |
| `/api/Model/analysis/GetMeshConnectivity` | `POST` | Returns the mesh cells of elements, as node numbers |
| `/api/Model/analysis/GetMeshNodesPosition` | `POST` | Returns the coordinates of all mesh nodes |

---

#### `POST /api/Model/analysis/LaunchAnalysis`

Checks the model, meshes it and runs the finite-element calculation of the whole model. No parameters.

**Returns:** `BooleanApiResponse` — `Data` is `true` when the finite-element calculation succeeded.

```csharp
BooleanApiResponse r = client.LaunchAnalysis();
if (!r.Data)
    Console.WriteLine(string.Join("\n", r.Details.Diagnostics.Select(d => d.Message)));
```

- It fails with `ANALYSIS_FAILED` when the model is not valid, or the mesh, the model check or the solver fails. The
  `ENGINE_ERROR` entries say which. A model that is not valid is not calculated.
- The call saves the project. See [Section 3.1](#31-project-management). It runs no design.
- Run it again after every change to the model. Nothing marks the results as out of date — see
  `GetAnalysisModelStatus`.
- A model with construction stages is calculated as a whole; the stages are not analysed through the API.
- To design later, the combinations must already list the design calculation when you call `LaunchAnalysis`. See
  `LaunchDesignCalculation`.

---

#### `POST /api/Model/analysis/LaunchDesignCalculation`

Runs the design calculation (the *Expert* calculation) of one material family. The design engine verifies its code
criteria against the finite-element results.

| Parameter | In | Type | Required | Description |
|---|---|---|---|---|
| `designType` | query | `DesignCalculationType` | Yes | Material family to design |
| *(body)* | body | `long[]` | No | EIDs of the elements to calculate. Empty or omitted: every element of the model |

`designType` is `Concrete`, `Steel`, `Timber` or `Composite` (steel section and concrete slab). Piles have no entry:
their design cannot be launched through the API.

**Returns:** `BooleanApiResponse` — `Data` is `true` when the design engine reports that it produced design results.

```csharp
// Whole model, steel design (after LaunchAnalysis).
BooleanApiResponse design = client.LaunchDesignCalculation(DesignCalculationType.Steel, new List<long>());
if (!design.Data || design.Details.HasErrors)
    foreach (DiagnosticEntry d in design.Details.Diagnostics)
        Console.WriteLine($"{d.Severity} {d.Code}: {d.Message}");

// Only two elements: pass new List<long> { beamEid, columnEid } instead.
```

Rules:

- `LaunchAnalysis` must have run first. A model that was never calculated is refused. The API never runs the
  analysis for you.
- At least one combination must list the design calculation ([Section 3.6.1](#361-design-expert-calculations-of-a-combination)).
  The engine only verifies the combinations that list it. If none does, the whole run is refused (the steel engine
  says *"No case selected for the Steelwork Design calculation!"*). A combination created without
  `DesignCalculations` takes part in no design. See also
  [Section 3.6.2](#362-automatic-generation-of-combinations).
- Set that selection before `LaunchAnalysis`. The analysis keeps its own copy. A later change has no effect until the
  analysis runs again.
- The element list uses the same EIDs as everywhere else. A duplicate is calculated once. An id with no counterpart
  in the analysis is skipped with a warning. If none of the listed elements exists, nothing is calculated and
  `Data` is `false`.
- The element list is ignored for `Composite`: a warning says so and every composite element is calculated.
- The design code and the kind of each element are not parameters. The model's design codes decide them.
- Refused when the licence has no module for the family (`Composite` needs both the concrete and the steel module;
  see [Section 3.11](#311-service--version-status--liveness)), and when a model with construction stages has a
  conflicting setting: the chained optimization (steel, composite), the imperfections (steel) or the deflection
  calculation (concrete). The message names it. A model configured for a 2nd generation Eurocode is refused where
  those codes are not enabled.
- Everything the engine reports comes back in `Details.Diagnostics`. Engine warnings are sent on the error channel, so
  a successful design can return `Data = true` together with `Details.HasErrors = true` and
  `Details.Success = false`. Read the messages. `LogVerbosity = AllDetails` adds an information summary.
- The finite-element results stay readable and unchanged. If the model changed since the last analysis, a warning
  says so and the design uses the existing results. Call `LaunchAnalysis` again for an up-to-date design.
- `DESIGN_CALCULATION_FAILED` means the engine produced no results. The earlier messages say why.
- The call saves the project.

---

#### `GET /api/Model/analysis/GetAnalysisModelStatus`

Reads how far the model has been calculated: analysis model, mesh, finite-element analysis, design families and
construction stages. No parameters.

**Returns:** `AnalysisModelStatusApiResponse` — `Data` is an [`AnalysisModelStatus`](#analysismodelstatus).

```csharp
AnalysisModelStatusApiResponse s = client.GetAnalysisModelStatus();
if (s.Data.OverallState != EAnalysisModelState.Calculated)
{
    client.LaunchAnalysis();
    s = client.GetAnalysisModelStatus();
}

if (s.Data.MeshState == EMeshState.Mesh_incomplete)
    Console.WriteLine("Warning: parts of the model have no finite elements.");

bool steelDone = s.Data.Design.Single(d => d.Family == DesignCalculationType.Steel).IsCalculated;
```

Rules:

- The call only reads and is cheap: call it as often as you like. It creates, opens and calculates nothing.
- With no project open the call is refused: `PROJECT_NOT_OPEN`. `ANALYSIS_MODEL_STATUS_UNAVAILABLE` means that the
  status of an open project could not be read: the earlier message says why. A project with no analysis model is a
  successful answer with `OverallState = NotCreated`.
- If the analysis model of the project is not open in this session, the call does not open it. It reports
  `OverallState = Created`, leaves the other values at their defaults and adds a warning. `OpenProject` and
  `LaunchAnalysis` normally leave the analysis model open.
- `FemCalculatedCasesCount` counts the load cases, combinations and envelopes that carry results. `GetResults`
  returns nothing for a case that carries none.

> **Warning:** `FemIsUpToDate` and `OverallState = OutOfDate` do not tell you whether your changes invalidated the
> results. Only the desktop application sets `FemIsUpToDate` back to `false`. In an API session it stays `true`
> after a successful `LaunchAnalysis`, whatever you create, update or delete, and `OverallState` is never
> `OutOfDate`. `false` can be trusted (a project saved out of date by the desktop application). `true` only means
> that nothing reported a change. If you changed the model, call `LaunchAnalysis` again.

> **Warning:** `DesignExpertStatus.IsCalculated` is a stored flag, not proof that design results exist. A project
> saved by the desktop application can report a family as calculated without any result. A model calculated only
> through the API is not affected. When you must be sure, read the results.

---

#### `POST /api/Model/analysis/GetResults`

Reads the finite-element results of one load case or combination, for one result type and a list of elements.

| Parameter | In | Type | Required | Description |
|---|---|---|---|---|
| `eResType` | query | `ResultType` | Yes | What to read |
| `IDAnalysisCase` | query | `long` | Yes | EID of the load case or combination |
| *(body)* | body | `long[]` | Yes | EIDs of the elements to read |

`ResultType` is `displacement`, `forces` (internal forces of linear elements and planar elements, reactions of supports), `stresses`,
`resultantforces` (resultant of supports, torsors of walls) or `generalizedBucklingRes` (use `GetResultsByQuery`).
Send it by name. What each type fills for each kind of element is in [Section 11](#fem-results).

**Returns:** `ResBaseListApiResponse` — `Data` is a polymorphic list of [`ResBase`](#fem-results).

```csharp
ResBaseListApiResponse res = client.GetResults(ResultType.Forces, loadCaseId, new List<long> { beamEid });

foreach (ResElementLinear beam in res.Data.OfType<ResElementLinear>())
    foreach (ResNode node in beam.ResNodes)
        Console.WriteLine($"node {node.Id.Value}: N = {node.ResForces.Fx} N, My = {node.ResForces.My} N·m");
```

Rules:

- Send both query parameters. One that is left out takes the default of its type: `displacement`, and case `0`,
  which gives an empty answer.
- The case is a load case or a combination. A combination is read only if it existed at the last `LaunchAnalysis`.
- Elements are linear elements, planar elements and supports. Elements of any other kind are ignored.
- One call reads one result type for one case. Results are not enveloped.
- An empty element list returns an empty result and the warning `RESULTS_EMPTY_INPUT`.
- An empty list also comes back, with no message, when the case is unknown, was not calculated, or was created after
  the last analysis. Check `GetAnalysisModelStatus.FemCalculatedCasesCount`.
- `generalizedBucklingRes` always returns nothing here. Use `GetResultsByQuery`.

> **Warning:** `ResBase.Id` of a finite-element result is the identifier of the element in the analysis model. It
> is not always the EID you sent. Elements without results are left out, so the position in the list does not
> identify an element either. To know which element a result belongs to, read one element per call, or compare the
> node numbers of the result with the cells from `GetMeshConnectivity`.

---

#### `POST /api/Model/analysis/GetResultsByQuery`

Reads finite-element results from one query object instead of query parameters. It returns the same results as
`GetResults`. The only difference is that the load case can carry more than its EID. Use it for buckling modes.

The body is a [`ResultsQuery`](#the-queries): send a `ResultsQueryFEM` (`EResType`, `ListElementsIDs`,
`AnalysisCaseInfo`). **Returns:** `ResBaseListApiResponse`, as for `GetResults`.

Rules:

- Only `$type = ResultsQueryFEM` is served. Any other query, the design queries included, is refused:
  `RESULTS_QUERY_INVALID`. Design results are read with `GetDesignResults`.
- `AnalysisCaseInfo` is a `LoadCaseInfo` (the EID only, as in `GetResults`) or a `LoadCaseAnalysisBuckling` (the EID
  and a buckling `Mode`). All the rules of `GetResults` apply, including the empty answers and the `ResBase.Id`
  warning.
- `ResultType.generalizedBucklingRes` returns one [`ResGeneralizedBuckling`](#fem-results) with the critical load
  factor `LambdaFactor` of the mode. It needs a `LoadCaseAnalysisBuckling` with `Mode` 0 or more. `Mode` is
  0-based. The default `-1` returns nothing. The number of modes of a buckling case is `NoModes` of its
  `LoadCase_Buckling`.
- The result is model-wide, not per element: the element list is ignored. It must still contain at least one id,
  otherwise nothing is read (`RESULTS_EMPTY_INPUT`).
- With a `LoadCaseAnalysisBuckling`, the other result types are read for that buckling mode. Nothing is returned when
  the buckling case was not calculated or the mode does not exist.

```csharp
ResBaseListApiResponse res = client.GetResultsByQuery(new ResultsQueryFEM
{
    EResType = ResultType.GeneralizedBucklingRes,
    ListElementsIDs = new List<long> { beamEid },        // required, but not used for this result type
    AnalysisCaseInfo = new LoadCaseAnalysisBuckling { Id = new EID { Value = bucklingCaseId }, Mode = 0 }
});

ResGeneralizedBuckling lambda = res.Data.OfType<ResGeneralizedBuckling>().FirstOrDefault();
Console.WriteLine(lambda is null ? "no result" : $"lambda = {lambda.LambdaFactor}");
```

```json
{
  "$type": "ResultsQueryFEM",
  "eResType": "generalizedBucklingRes",
  "listElementsIDs": [ 4103 ],
  "analysisCaseInfo": { "$type": "LoadCaseAnalysisBuckling", "id": { "value": 4120 }, "mode": 0 }
}
```

---

#### `POST /api/Model/analysis/GetDesignResults`

Reads the results of a design calculation. It is the read side of `LaunchDesignCalculation`.

The body is a [`ResultsQueryDesign`](#the-queries): send a `ResultsQueryDesignSteelLinear` (`ListElementsIDs`,
`EResType`, `EResultSet`). **Returns:** `ResBaseListApiResponse`. `Data` holds one object per element and result
family, in one of three object types depending on how the family is stored. See
[Section 11](#design-results).

##### What is available

- Design results can be read for **steel** and for **linear elements** only.
- Steel codes served: Eurocode 3 (the Italian NTC 2008 and NTC 2018 are verified as Eurocode 3), AISC 360
  (`aisc`, `aisc_2016`) and CSA S16 (`can_csa_s16_2009`, `can_csa_s16_2014`, `can_csa_s16_2019`, `can_csa_s16_2024`).
- The code is not a parameter. The model's steel code decides, and the `$type` of each payload says which one it is.
  A model on any other steel code (for example CM66) is refused with an error that names the code.
- Concrete, timber and composite designs can be launched, but their results cannot be read through the API.
- Planar elements are refused (*"is not a linear element"*). There is no query by super element.

`EResultSet` selects the engine whose results are read:

| `EResultSet` | Engine | Families returned for `EResType = All` |
|---|---|---|
| `Standard` | Standard steel verification | The *Standard* rows below |
| `ColdFormed` | Cold-formed members: EN 1993-1-3 (Eurocode 3), AISI S100 / CSA S136 (AISC / CSA) | The *ColdFormed* rows |
| `AdvancedStability` | Non-linear FEM ("Advanced stability"), Eurocode 3 only | `AdvancedStabilityResistance` |

The `ColdFormed*` types and `AdvancedStabilityResistance` always read their own engine, whatever you send in
`EResultSet`. `ProfileOptimisation` is the one type where `EResultSet` matters: send `ColdFormed` to read the
optimisation of cold-formed members (Eurocode 3 only).

Each payload is a polymorphic object. The table gives its `$type` per code and how it is stored: *cell* is one
payload per mesh cell (`ResDesignElementLinearSteel`), *element* is one per element
(`ResDesignElementLinearSteelPerElement`), *combination* is one per combination
(`ResDesignElementLinearSteelPerCase`).

| `EResType` | Set | Stored | Eurocode 3 | AISC 360 / CSA S16 |
|---|---|---|---|---|
| `CrossSectionResistance` | Standard | cell | `ResEc3Resistance` | `ResAiscResistance` (also holds the member stability) |
| `Stability` | Standard | cell | `ResEc3Stability` | Not available. See `CrossSectionResistance` |
| `Deflection1`, `Deflection2` | Standard | cell | `ResEc3Deflection` | `ResAiscDeflection` |
| `BucklingLengths` | Standard | cell | `ResEc3BucklingLengths` | `ResAiscBucklingLengths` |
| `LateralTorsionalBucklingLengths` | Standard | cell | `ResEc3LateralTorsionalBucklingLengths` | `ResAiscLateralTorsionalBucklingLengths` |
| `CrossSectionClass` | Standard | cell | `ResEc3CrossSectionClass` | `ResAiscCrossSectionClass` |
| `MaxWorkRatio` | Standard | cell | `ResEc3MaxWorkRatio` | `ResAiscMaxWorkRatio` |
| `CrossSectionInfo` | Standard | cell | Not available | `ResAiscSectionInfo` |
| `FireResistance` | Standard | cell | `ResEc3FireResistance` | Not available |
| `FireCrossSectionClass` | Standard | cell | `ResEc3FireCrossSectionClass` | Not available |
| `FireTemperature` | Standard | cell | `ResEc3FireTemperature` | Not available |
| `WebShearBuckling` | Standard | cell | `ResEc3WebShearBuckling` | Not available |
| `ProfileOptimisation` | Standard | element | `ResSteelProfileOptimisation` | `ResSteelProfileOptimisation` |
| `HaunchSectionProperties` | Standard | element | `ResEc3HaunchSectionProperties` | Not available |
| `BucklingLengthCoefficients` | Standard | element | `ResEc3BucklingLengthCoefficients` | Not available |
| `BucklingLengthNodeRestraints` | Standard | element | `ResEc3BucklingLengthNodeRestraints` | Not available |
| `GlobalSwayImperfectionForces` | Standard | combination | `ResEc3GlobalSwayImperfectionForces` | Not available |
| `LocalBowImperfectionForces` | Standard | combination | `ResEc3LocalBowImperfectionForces` | Not available |
| `ColdFormedResistance` | ColdFormed | cell | `ResEc3ColdFormedResistance` | `ResAisiColdFormedResistance` |
| `ColdFormedDeflection1`, `ColdFormedDeflection2` | ColdFormed | cell | `ResEc3Deflection` | `ResAiscDeflection` |
| `ColdFormedSectionInfo` | ColdFormed | element | `ResSteelColdFormedSectionInfo` | `ResSteelColdFormedSectionInfo` |
| `ProfileOptimisation` (cold-formed) | ColdFormed | element | `ResSteelProfileOptimisation` | Not available |
| `AdvancedStabilityResistance` | AdvancedStability | cell | `ResEc3AdvancedStabilityResistance` | Not available |

Rules for the table:

- *Not available* means the design engine does not produce that family under that code. The call returns no result
  and a diagnostic that names the code. It is not a statement about your model.
- AISC 360 and CSA S16 fold member stability into the strength checks. Read it from `CrossSectionResistance`.
- `ColdFormedSectionInfo` exists for cold-formed sections only. `HaunchSectionProperties` exists for haunched
  elements only. An element without a result of a per-element family is left out of the answer.
- The two imperfection families exist only when the imperfections were generated: the switches in the steel
  calculation settings ([Section 3.7.1](#371-steel-eurocode-3-calculation-settings)) and in the design template of
  the element must be on. Otherwise no element is returned. This is not an error.

Rules for the call:

- Run `LaunchAnalysis`, then `LaunchDesignCalculation` for `Steel`. Without a successful steel design the call is
  refused. `Data` is empty and `Details.Success` is `false`.
- The element list is required. An empty list does not mean the whole model. It returns an empty result and the
  warning `DESIGN_RESULTS_EMPTY_INPUT`.
- The ids are the EIDs used everywhere else. `ResBase.Id` of each result is the EID you sent. `UserID` is the
  number shown in the Advance Design interface, a different numbering. A duplicate id is answered once.
- An unknown id, an element that is not linear and an element with no user identifier are each reported in
  `Details.Diagnostics` and skipped. The others are still returned. Each skip is an error, so `Details.Success` is
  `false` and `Details.HasErrors` is `true` although `Data` holds the other results. Read `Data` anyway.
- A result type asked for by name that produced nothing is an error. `All` does not report it.
- Any query type other than `ResultsQueryDesignSteelLinear` is refused: `DESIGN_RESULTS_QUERY_INVALID`.
- Results are per mesh cell. No envelope is computed: take your own maximum.
- Values are in SI units (N, N·m, m, m², Pa). Work ratios are dimensionless, not multiplied by 100. Above 1, a
  check is not satisfied.
- A value can be `Infinity` or negative, and both are genuine. A fully restrained member has an infinite buckling
  length or critical moment. The server writes the JSON literals `Infinity` and `-Infinity`.
- Test `IsCalculated` (or `Exists`, `IsIgnored`) before you read a check. See [Section 11](#design-results).

```csharp
ResBaseListApiResponse res = client.GetDesignResults(new ResultsQueryDesignSteelLinear
{
    EResType = SteelLinearDesignResultType.CrossSectionResistance,
    EResultSet = SteelDesignResultSet.Standard,
    ListElementsIDs = new List<long> { beamEid, columnEid }
});

foreach (ResDesignElementLinearSteel element in res.Data.OfType<ResDesignElementLinearSteel>())
    foreach (ResDesignMesh mesh in element.ResMeshes)
        switch (mesh.Data)
        {
            case ResEc3Resistance ec3:
                foreach (ResEc3ResistanceCheck check in ec3.Checks.Where(x => x.IsCalculated))
                    Console.WriteLine($"{element.UserID}/{mesh.MeshIndex} {check.Check}: " +
                                      $"{check.Action} / {check.Limit} = {check.WorkRatio:F2}");
                break;
            case ResAiscResistance aisc:      // AISC 360 / CSA S16: Strength instead of Limit
                foreach (ResAiscResistanceCheck check in aisc.Checks.Where(x => x.IsCalculated))
                    Console.WriteLine($"{element.UserID}/{mesh.MeshIndex} {check.Check}: {check.WorkRatio:F2}");
                break;
        }
```

```json
{
  "$type": "ResultsQueryDesignSteelLinear",
  "listElementsIDs": [ 12, 13 ],
  "eResType": "CrossSectionResistance",
  "eResultSet": "Standard"
}
```

---

#### `POST /api/Model/analysis/GetMeshConnectivity`

Returns the mesh cells of elements, as node numbers. It needs a mesh: run `LaunchAnalysis` first.

The body is a `long[]` of element EIDs. **Returns:** `MeshElementListApiResponse` — one `MeshElement` per element.
`Id` is the EID you sent. `Connectivity` is an `Int32Matrix`.

- `Connectivity` has one column per mesh cell and one row per node of the largest cell. A cell with fewer nodes is
  padded with `-1`. A support cell has one node.
- The data is column-major: the node in row `r` of column `c` is at index `r + c * Rows` of the data.
- A node number is a 0-based index into the list returned by `GetMeshNodesPosition`. It is also the `Id` of the
  `ResNode` and `ResPlanarNode` objects of the results.
- An element that has no counterpart in the analysis is left out of the answer. An element without mesh cells comes
  back with an empty matrix. An empty element list returns an empty result and the warning `MESH_EMPTY_INPUT`.

```csharp
Pt3DListApiResponse nodes = client.GetMeshNodesPosition();
MeshElementListApiResponse mesh = client.GetMeshConnectivity(new List<long> { beamEid });

Pt3D[] xyz = nodes.Data.ToArray();
foreach (MeshElement e in mesh.Data)
{
    int[] data = e.Connectivity._data.ToArray();
    for (int cell = 0; cell < e.Connectivity.Cols; cell++)
        for (int row = 0; row < e.Connectivity.Rows; row++)
        {
            int node = data[row + cell * e.Connectivity.Rows];
            if (node >= 0)
                Console.WriteLine($"cell {cell}: node {node} at ({xyz[node].X}, {xyz[node].Y}, {xyz[node].Z}) m");
        }
}
```

---

#### `POST /api/Model/analysis/GetMeshNodesPosition`

Returns the coordinates of every node of the mesh of the model. No parameters. It needs a mesh: run `LaunchAnalysis`
first. Without a mesh the list is empty.

**Returns:** `Pt3DListApiResponse` — `Data` is a list of `Pt3D` (`X`, `Y`, `Z`, in m). The position in the list is the
node number used by `GetMeshConnectivity` and by the results.

---

### 3.9 Model — Actions

| Endpoint | Method | What it does |
|---|---|---|
| `/api/Model/actions/ProcessAction` | `POST` | Runs an action on the model: the automatic generation of wind and snow loads |
| `/api/Model/elements/ExportAdditionalEnums` | `POST` | Technical endpoint that publishes two enumerations |

---

#### `POST /api/Model/actions/ProcessAction`

Runs an action on the open project. The only action today is the automatic generation of the climatic loads.

| Parameter | In | Type | Required | Description |
|---|---|---|---|---|
| `ActionToProcess` | query | `AD_API_ActionType` | Yes | The action to run. The only value is `ClimaticAutoGeneration`: it generates the wind and snow loads of all existing climatic load case families |
| *(body)* | body | `ActionArgumentsBase[]` | No | Arguments of the action. `ClimaticAutoGeneration` takes none: send an empty list or no body |

**Returns:** `BooleanApiResponse` — `Data` is `true` when the generation succeeded.

```csharp
BooleanApiResponse r = client.ProcessAction(AD_API_ActionType.ClimaticAutoGeneration, new List<ActionArgumentsBase>());
```

Rules:

- The climatic load case families must exist, with their parameters — see
  [Section 10.1](#101-load-case-families) and [Section 12](#12-data-models--climatic-parameters).
- When the model contains at least one load area (`ElementLoadArea`), the 3D generation runs. Without a load
  area, the 2D generation runs.
- The messages are `ProcessAction_COMPLETED` (information) and `ProcessAction_FAILED` (error). The engine
  messages that explain a failure come with them. `ProcessAction_EXCEPTION` is returned for an internal
  error.

---

#### `POST /api/Model/elements/ExportAdditionalEnums`

A utility that does nothing to the model. It exists so that two enumerations that no model class uses appear
in `swagger.json` and in the generated clients. You never need to call it.

| Parameter | In | Type | Required | Description |
|---|---|---|---|---|
| *(body)* | body | `AuxiliaryEnums` | No | `WindDirectionsNo` (`Wind_Directions_type`) and `CasesTypes` (`eLoadCaseType`) |

**Returns:** `BooleanApiResponse` — `Data` is always `true` (`ENUMS_EXPORTED`). The call works without an open
project.

`Wind_Directions_type` has the values `DIRECTION_X_PLUS`, `DIRECTION_X_MINUS`, `DIRECTION_Y_PLUS`,
`DIRECTION_Y_MINUS` and `GRCG_WIND_DIRECTIONS_NO` (the number of directions, 4). The four directions give the
order of the entries of `LoadAreaClimaticProperties.ExposurePerDirection`: X+, X−, Y+, Y−. The values of
`eLoadCaseType` (kinds of load case and analysis) are in [Section 13](#13-enumerations-reference).

---

### 3.10 Model — Query & Retrieval

Reading elements takes two steps. First ask for the EIDs with a query (`GetElementsID`). Then read the
objects for those EIDs (`GetElementsObject` or `GetInformationalElementsObject`).

Materials and sections are listed and read with their own calls: `GetListMaterials` and `GetMaterials`
([Section 3.2](#32-model--materials)), `GetListSections` and `GetSections`
([Section 3.3](#33-model--sections)).

| Endpoint | Method | What it does |
|---|---|---|
| `/api/Model/elements/GetElementsID` | `POST` | Finds objects by type and returns their EIDs |
| `/api/Model/elements/GetElementsObject` | `POST` | Returns elements, supports and loads by EID |
| `/api/Model/elements/GetInformationalElementsObject` | `POST` | Returns load cases, combinations, systems and templates by EID |

---

#### `POST /api/Model/elements/GetElementsID`

Returns the EIDs of the objects that match the queries in the body.

| Parameter | In | Type | Required | Description |
|---|---|---|---|---|
| *(body)* | body | `QueryBase[]` *(polymorphic)* | Yes | One or more query objects. Each one needs its `"$type"` |

**Returns:** `Int64ListApiResponse` — `Data` is the list of EIDs.

##### The query objects

| `$type` | Properties | Returns the EIDs of |
|---|---|---|
| `QueryElementsModel` | `ElementType` (`ElementTypeEnum`) | All geometric elements of that type |
| `QueryElementsLoads` | `ElementType`, `LoadCaseId` (`EID`) | The loads of the load case `LoadCaseId`. `ElementType` is `ElementLoadPunctual`, `ElementLoadLinear`, `ElementLoadPlanar` or `ElementImposedDisplacement`. A missing `LoadCaseId` or `0` means all load cases |
| `QueryInfoModel` | `InformationalElementType` (`InformationalElementTypeEnum`) | All informational elements of that type |
| `QueryInfoLoadCase` | `InformationalElementType`, `CaseFamilyId` (`EID`) | The load cases of the load case family `CaseFamilyId`. `InformationalElementType` is a load case type (`LoadCase`, `LoadCase_Wind`, ...) |

`QueryBase` is the common base class. It has no property except `"$type"`.

- `ElementType` takes the name of the element class (`ElementLinear`, `ElementPoint`, ...) — see
  `ElementTypeEnum` in [Section 13](#13-enumerations-reference).
- For an `ElementType` that is not a load, `QueryElementsLoads` works like `QueryElementsModel`.
- `QueryInfoLoadCase` filters load cases. With a load case family type in `InformationalElementType`,
  `CaseFamilyId` is ignored.

The informational types that can be queried:

| `InformationalElementType` | Returns |
|---|---|
| `LoadCaseFamily` | All load case families |
| `LoadCaseFamily_DeadLoads`, `_LiveLoads`, `_Snow`, `_Wind`, `_Seismic`, `_Thermic`, `_PushOver`, `_Other`, `_Accidental`, `_DynamicTemporal`, `_AnalysisBuckling` | The families of one kind, whatever the design code. `LoadCaseFamily_Wind` returns the wind families of every code |
| `LoadCase` | All load cases |
| `LoadCase_DeadLoads`, `_LiveLoads`, `_Snow`, `_Wind`, `_Seismic`, `_Thermic`, `_PushOver`, `_Other`, `_Accidental`, `_DynamicTemporal`, `_Buckling` | The load cases of one kind, whatever the design code |
| `Combination`, `StructuralSystem` | All combinations; all structural systems, including the root system `Structure 0` (design groups are not included) |
| `LinearReleaseTemplate` | All linear release templates, in the order of the pool: the three built-in ones (Free, Pinned, Fixed) first, then yours. Use it to read the localized names of the built-in templates |
| `DesignTemplate` | All Expert design templates, whatever their material family and element kind. The built-in default template ("None") is not in the list |

The code-specific types (`LoadCaseFamily_Wind_1991_1_4`, `LoadCaseFamily_WindIBC`, `LoadCaseFamily_WindCBN`,
`LoadCaseFamily_Snow_1991_1_3`, `LoadCaseFamily_SnowIBC`, `LoadCaseFamily_SnowCBN`,
`LoadCaseFamily_SeismicEN_1998_1`, `LoadCaseFamily_SeismicIBC`, `LoadCaseFamily_SeismicCBN` and the matching
`LoadCase_*` types) are not queryable: the query returns an empty list without a message. Use the kind types
of the table instead.

##### Example

```csharp
// All linear elements.
var query = new List<QueryBase>
{
    new QueryElementsModel { ElementType = ElementTypeEnum.ElementLinear }
};
Int64ListApiResponse ids = client.GetElementsID(query);
```

Several queries in one request, as a raw JSON body:

```json
[
  { "$type": "QueryElementsModel", "elementType": "ElementLinear" },
  { "$type": "QueryElementsLoads", "elementType": "ElementLoadLinear", "loadCaseId": { "value": 42 } },
  { "$type": "QueryInfoModel", "informationalElementType": "StructuralSystem" },
  { "$type": "QueryInfoLoadCase", "informationalElementType": "LoadCase_Wind", "caseFamilyId": { "value": 40 } }
]
```

Rules:

- The answer is the union of all queries, grouped by query in the order you sent them. An EID that matches
  several queries appears once.
- A query that matches nothing is not an error: the list is empty. An empty request returns an empty list with
  the warning `ELEMENTS_ID_EMPTY_INPUT`.
- A query without `"$type"` matches nothing: `ELEMENTS_QUERY_MISSING_DISCRIMINATOR`.
- A query for the base type (`ElementBase` or `InfoObjBase`) matches nothing:
  `ELEMENTS_QUERY_TYPE_NOT_QUERYABLE`.

---

#### `POST /api/Model/elements/GetElementsObject`

Returns the geometric elements for a list of EIDs, with all their properties.

| Parameter | In | Type | Required | Description |
|---|---|---|---|---|
| *(body)* | body | `long[]` | Yes | EIDs of geometric elements |

**Returns:** `ElementBaseListApiResponse` — `Data` holds one object of the concrete class for each element.

```csharp
ElementBaseListApiResponse elements = client.GetElementsObject(ids.Data.ToList());
foreach (var el in elements.Data)
{
    if (el is ElementLinear beam)
        Console.WriteLine($"Beam from {beam.GeomPtStart.X} to {beam.GeomPtEnd.X}");
}
```

Rules:

- The objects come back in the order of the EIDs. They carry no EID, only `UserID`.
- An EID that is not a geometric element (a load case, a combination, a material, an unknown EID) is skipped
  with the warning `ELEMENTS_OBJECT_PARTIAL`. The result is then shorter than the request and the positions no
  longer match the EIDs. Read such an EID on its own, or with `GetInformationalElementsObject`.
- An empty request returns an empty list with the warning `ELEMENTS_OBJECT_EMPTY_INPUT`.
- The result is complete: read an object, change it and send it to `UpdateElement`
  ([Section 3.4](#34-model--elements-geometric)).

---

#### `POST /api/Model/elements/GetInformationalElementsObject`

Returns the informational elements for a list of EIDs: load cases, load case families, combinations,
structural systems, linear release templates and Expert design templates.

| Parameter | In | Type | Required | Description |
|---|---|---|---|---|
| *(body)* | body | `long[]` | Yes | EIDs of informational elements |

**Returns:** `InformationalElementBaseListApiResponse` — `Data` holds one object of the concrete class for
each element.

```csharp
InformationalElementBaseListApiResponse systems =
    client.GetInformationalElementsObject(systemIds.Data.ToList());   // systemIds: from GetElementsID
```

Rules:

- The same rules as `GetElementsObject` apply. An EID that is not an informational element is skipped with
  the warning `INFORMATIONAL_ELEMENTS_PARTIAL`. An empty request gives `INFORMATIONAL_ELEMENTS_EMPTY_INPUT`.

---

### 3.11 Service — version, status & liveness

| Endpoint | Method | What it does |
|---|---|---|
| `/api/Service/GetVersion` | `GET` | Returns the API version. An answer proves that the service is up |
| `/api/Service/GetStatus` | `GET` | Tells whether the service can run model operations, and what its licence allows |

These are the only endpoints that are not about a model.

- No project, model or session is needed, and the calls do not create one.
- They are not queued behind a running operation. They answer even while a long analysis runs.
- They return a bare value, not the [response envelope](#4-response-envelope-pattern): no `Data`, no
  `Details`, no diagnostics.
- They always answer HTTP 200 while the service is running. The result is in the body.

---

#### `GET /api/Service/GetVersion`

Returns the version of the Advance Design API service. No parameters.

**Returns:** a plain JSON string.

```csharp
string version = client.GetVersion();    // "1.27.1"
```

```text
curl http://localhost:52000/api/Service/GetVersion
"1.27.1"
```

- The value is the version of the API contract. It is the same string as `info.version` in `swagger.json`.
  It is not the build number of `AD.API.Srv.exe`.
- Use the call as a ping. Any HTTP 200 means that the service is up.

---

#### `GET /api/Service/GetStatus`

Tells whether the service can run model operations, and if not, why. Today it reports whether the service
holds its licence, what kind of licence it is, and which design calculations the licence allows. No
parameters.

**Returns:** a bare [`ServiceStatus`](#servicestatus) object.

```csharp
ServiceStatus status = await client.GetStatusAsync();
if (!status.CanRun)                       // decide on CanRun, not on the Status text
{
    Console.WriteLine($"{status.Status}: {status.Message}");
    return;
}
LicenceStatus licence = status.Licence;   // may be null: "not known yet"
Console.WriteLine(licence?.IsPermanent == true ? "permanent licence"
                                              : $"{licence?.DaysToExpiry} day(s) to expiry");
```

```json
{
  "schemaVersion": 1,
  "serviceVersion": "1.27.1",
  "status": "Ok",
  "canRun": true,
  "message": null,
  "licence": {
    "checkedAt": "2026-10-12T08:15:02+00:00",
    "isAvailable": true,
    "errorMessage": null,
    "protectionType": "Commercial",
    "isPermanent": false,
    "daysToExpiry": 245,
    "designCalculations": [
      { "type": "Concrete",  "isAvailable": true },
      { "type": "Steel",     "isAvailable": true },
      { "type": "Timber",    "isAvailable": false },
      { "type": "Composite", "isAvailable": true }
    ]
  }
}
```

The licence kind, the days to expiry and the available families in this example are only an illustration.
They depend on the licence.

##### The `Status` values

`Status` is a string, not an enumeration, so that new values can be added. Decide with `CanRun`.

| `Status` | `CanRun` | Meaning | What to do |
|---|---|---|---|
| `Ok` | `true` | The service holds everything it needs to run model operations. `Message` is `null` | Go on |
| `Starting` | `false` | The service is still creating its engine session and checking out its licence. `Licence` is `null` | Ask again shortly |
| `Licence_Unavailable` | `false` | The service runs, but does not hold its mandatory licence. Model operations cannot be relied upon. `Message` and `Licence.ErrorMessage` say why | Fix the licence, then restart the server. A missing licence is not checked out again |
| `Initialization_Failed` | `false` | The engine session could not be created. The server log has the details | A later model request retries the creation and may recover it. If not, read the server log and restart the server |
| `Stopping` | `false` | `CloseSession` was called and the process is about to exit. If the close failed, `Message` reads "The session close failed: ... See the server log." | Send no more model operations |

- A service without its mandatory licence still starts and still accepts model operations. `GetStatus` is
  where the missing licence shows (`Licence_Unavailable`). No licence dialog is shown.
- Treat any other `Status` value as "not Ok". Decide with `CanRun`, which keeps its meaning whatever the
  `Status`.
- `Initialization_Failed` counts only while there is no session. Once a request creates the session, the
  status comes from the licence again.

##### After `CloseSession`

From the moment `CloseSession` is requested until the process exits, `GetStatus` answers `Stopping`. It never
answers `Licence_Unavailable`, although the licence is released on the way out.

- `Licence` is the last snapshot taken. It shows the licence still held, until the session is closed and
  the licence released. After that, only `CheckedAt` and `ErrorMessage` are set.
- Every other `/api/Model/*` request is refused with HTTP 503 and the code `SERVICE_STOPPING`. See
  [`CloseSession`](#31-project-management).

##### Timing

- At startup the server creates its engine session, and checks its licence out, before it handles any model
  request. While that runs, `GetStatus` waits up to 30 s, then answers `Starting`.
- After that, `Licence` is a snapshot, not a live read. `Licence.CheckedAt` says when it was taken. The server
  refreshes it when it is idle, at most every 30 s. The call that triggers a refresh usually returns the
  previous snapshot, and the next call returns the new one.
- Polling `GetStatus` more often than every 30 s returns the same snapshot. It does not poll the licence
  server.

##### Forward compatibility

- `Status` and `CanRun` are the only two values that are guaranteed never to break a client.
- `SchemaVersion` rises when a section or a status value is added. Do not fail on a higher value.
- A section that is `null` (for example `Licence`) means "not known yet" or "does not apply". It never
  means "failed".
- Ignore properties you do not know.
- The enumerations inside the licence section (`DesignCalculationType`, `LicenceProtectionType`) are not
  covered by this promise. A new member there is a change of the API contract.

##### Privacy

No serial number, licence server name or licence file path is ever returned. `ErrorMessage` holds only the
part of the licence message that a user can act on. The technical details go to the server log.

---

##### `ServiceStatus`

| Property | Type | Required | Default | Description |
|---|---|---|---|---|
| `SchemaVersion` | `int` | Yes | `1` | Version of the shape of this object. Rises when a section or a status value is added |
| `ServiceVersion` | `string` | Yes | | Version of the API service. The same string as `GetVersion` |
| `Status` | `string` | Yes | `"Starting"` | The verdict: `Ok`, `Starting`, `Licence_Unavailable`, `Initialization_Failed` or `Stopping` |
| `CanRun` | `bool` | Yes | `false` | `true` when model operations can be sent to the service now. The value to decide on |
| `Message` | `string?` | No | `null` | What to tell a user when `Status` is not `Ok`. `null` when it is `Ok` |
| `Licence` | [`LicenceStatus?`](#licencestatus) | No | `null` | The licence the service holds. `null` while it is not known yet (`Starting`) or when no licence state could be recorded |

##### `LicenceStatus`

The licence section of `GetStatus`. It is a snapshot: `CheckedAt` says when it was taken. `IsAvailable`
is the one value that decides whether the server can work. The other properties describe the licence the
server holds. They keep their defaults when it holds none, also after the licence was released during
shutdown.

| Property | Type | Required | Default | Description |
|---|---|---|---|---|
| `CheckedAt` | `DateTimeOffset` | Yes | | When the licence was read for this snapshot |
| `IsAvailable` | `bool` | Yes | `false` | `true` when the server holds its mandatory licence: the base module and the user seat, checked out together at startup. They are not checked out again, so a network licence whose lease is lost later is not detected |
| `ErrorMessage` | `string?` | No | `null` | Why the mandatory licence is not held, in words a user can act on. Also set after the licence was released at shutdown, and, with `IsAvailable` `true`, when the licence details could not be read. Its language depends on the licence system |
| `ProtectionType` | [`LicenceProtectionType`](#licenceprotectiontype) | Yes | `NoLicence` | The kind of licence the server runs under |
| `IsPermanent` | `bool` | Yes | `false` | `true` when the licence of the base module never expires. `DaysToExpiry` is `null` then |
| `DaysToExpiry` | `int?` | No | `null` | Days left before the licence of the base module expires, counted when the snapshot was taken. `null` when the licence is permanent, when none is held, or when the number is unknown |
| `DesignCalculations` | [`DesignCalculationLicence[]`](#designcalculationlicence) | Yes | | One entry per design family, in the order of `DesignCalculationType`: `Concrete`, `Steel`, `Timber`, `Composite` |

##### `DesignCalculationLicence`

Whether the licence of one design family is available. The values of `DesignCalculationType` are listed in
[Section 13](#designcalculationtype).

| Property | Type | Required | Default | Description |
|---|---|---|---|---|
| `Type` | `DesignCalculationType` | Yes | `Concrete` | The design family. The same enumeration `LaunchDesignCalculation` takes |
| `IsAvailable` | `bool` | Yes | `false` | `true` when the licence modules that the family needs could be checked out when the licence was read (`LicenceStatus.CheckedAt`). `Composite` needs both the concrete and the steelwork modules. Always `false` when the mandatory licence is not held |

- `IsAvailable` `true` means that the family was not refused for licence reasons at `CheckedAt`. It is no
  promise for a later call: `LaunchDesignCalculation` checks the licence again and has other conditions of
  its own.

---

## 4. Response Envelope Pattern

Every `/api/Model/*` call returns the same two-part object, the *envelope*:

```json
{
  "data": { "value": 12 },
  "details": {
    "success": true,
    "hasWarnings": false,
    "hasErrors": false,
    "diagnostics": []
  }
}
```

- `Data` is the result of the call. Its type depends on the endpoint.
- `Details` says how the call went.

The two `/api/Service/*` endpoints are the exception: `GetVersion` returns a string and `GetStatus` returns
a `ServiceStatus` object, without an envelope.

The envelope classes are named after the type of `Data`:

| Envelope | `Data` | Returned by |
|---|---|---|
| `BooleanApiResponse` | `bool` | Updates, deletes, settings, calculations, project management |
| `EIDApiResponse` | `EID` (the identifier of the new object) | The create calls |
| `Int64ListApiResponse` | List of identifiers | `GetElementsID`, `GetListMaterials`, `GetListSections` |
| `ElementBaseListApiResponse` | List of elements | `GetElementsObject` |
| `InformationalElementBaseListApiResponse` | List of informational elements | `GetInformationalElementsObject` |
| `MaterialListApiResponse`, `SectionListApiResponse` | List of materials / sections | `GetMaterials`, `GetSections` |
| `ResBaseListApiResponse` | List of results | `GetResults`, `GetResultsByQuery`, `GetDesignResults` |
| `MeshElementListApiResponse`, `pt3DListApiResponse` | Mesh elements / node positions | `GetMeshConnectivity`, `GetMeshNodesPosition` |
| `AnalysisModelStatusApiResponse` | `AnalysisModelStatus` | `GetAnalysisModelStatus` |
| `EnvironmentsApiResponse` | `Environments` | `GetEnvironment` |
| `ModelSettingsBaseApiResponse` | One group of model settings | `GetModelSettings` |
| `CombinationsGenerationResultApiResponse` | `CombinationsGenerationResult` | `GenerateCombinations` |

### `ApiResponseDetails`

| Property | Type | Description |
|---|---|---|
| `Success` | `bool` | `true` when the call produced no error |
| `HasErrors` | `bool` | `true` when the call produced at least one error |
| `HasWarnings` | `bool` | `true` when the call produced at least one warning |
| `Diagnostics` | `DiagnosticEntry[]` | The messages of the call |

### `DiagnosticEntry`

| Property | Type | Description |
|---|---|---|
| `Severity` | `DiagnosticSeverity` | `Information`, `Warning`, `Error` or `Critical` |
| `Code` | `string` | Stable code to test in a program, for example `PROJECT_NOT_OPEN` — see [Section 14](#14-error-handling--diagnostics) |
| `Message` | `string` | Text for a person |
| `Source` | `string` | Area that produced the message: `Management`, `Elements`, `Analysis`, ... |
| `Timestamp` | date and time (UTC) | When the message was recorded |

### Checking results

```csharp
EIDApiResponse r = client.CreateMaterial(new Material { Name = "S275" });

if (!r.Details.Success)
{
    foreach (DiagnosticEntry d in r.Details.Diagnostics)
        Console.Error.WriteLine($"[{d.Severity}] {d.Code}: {d.Message}");
    return;
}

long id = r.Data.Value;
```

Rules:

- **Check `Success` first.** When it is `false`, do not use `Data`.
- **For a `BooleanApiResponse`, check `Data` too.** An update or a delete that could not be applied returns
  `Data = false`. `Success` can still be `true` in that case, so test both.
- **`LogVerbosity` decides which messages you receive** (see
  [Section 3.1.1](#311-the-environment-of-a-session)): `Errors` returns errors only, `ErrorsAndWarnings`
  adds the warnings, `AllDetails` adds the information messages. It only filters `Diagnostics`; `Success`,
  `HasErrors` and `HasWarnings` always reflect every message.
- **A call that needs a project and finds none** fails with `PROJECT_NOT_OPEN`. This applies to every
  `/api/Model/*` endpoint except the project management ones and `ExportAdditionalEnums`.

---

## 5. Polymorphism & the `$type` Discriminator

Several request bodies accept more than one class. `CreateMaterial` takes a `Material`, which can be a
`MaterialSteel`, a `MaterialReinforcedConcrete`, and so on. The JSON property `"$type"` tells the server
which class the object is. Its value is the class name.

| Base type | Used by | Classes |
|---|---|---|
| `Material` | `CreateMaterial`, `GetMaterials` | [Section 6](#6-data-models--materials) |
| `Section` | `CreateUserSection`, `GetSections` | [Section 6.5](#65-data-models--sections) |
| `ElementBase` | `CreateElement`, `UpdateElement`, `GetElementsObject` | Sections [7](#7-data-models--elements-geometric), [8](#8-data-models--supports), [9](#9-data-models--loads) |
| `InformationalElementBase` | `CreateInformationalElement`, `UpdateInformationalElement`, `GetInformationalElementsObject` | [Section 10](#10-data-models--informational-elements-load-cases--families) |
| `ModelSettingsBase` | `GetModelSettings`, `SetModelSettings` | [Section 3.7](#37-model--general-settings) |
| `QueryBase` | `GetElementsID` | [Section 3.10](#310-model--query--retrieval) |
| `ResultsQuery` | `GetResultsByQuery`, `GetDesignResults` | [Section 11](#11-data-models--analysis-results) |
| `ResBase`, `ResDesignData` | The results | [Section 11](#11-data-models--analysis-results) |

**In C#** you do nothing: create the object of the right class and the client writes `"$type"`.

```csharp
var steel = new MaterialSteel { Name = "My steel", E = 2.1e11, SigmaE = 3.55e8 };
EIDApiResponse r = client.CreateMaterial(steel);   // the client writes "$type": "MaterialSteel"
```

When you read objects, test the class of what comes back:

```csharp
foreach (ElementBase e in client.GetElementsObject(ids).Data)
{
    if (e is ElementLinear beam)
        Console.WriteLine($"Beam from {beam.GeomPtStart.X} to {beam.GeomPtEnd.X}");
}
```

**In Python** set the `type` property yourself — see [Section 2.3](#23-connect-from-python).

**In raw JSON** follow these rules:

- Write `"$type"` as the **first** property of the object.
- Its value is the exact class name. An unknown name is refused with HTTP 400.
- Without `"$type"` the server reads the object as the base class. The properties of the class you meant
  are lost.
- Enumeration values are names, in the spelling of [Section 13](#13-enumerations-reference).

### Raw JSON examples

A steel material:

```json
{
  "$type": "MaterialSteel",
  "name": "My steel",
  "e": 2.1e11,
  "ro": 7850,
  "nu": 0.3,
  "sigmaE": 3.55e8
}
```

A linear element (a beam). `section` and `material` are the identifiers returned by `CreateSection` and
`CreateMaterial`:

```json
{
  "$type": "ElementLinear",
  "geomPtStart": { "x": 0, "y": 0, "z": 0 },
  "geomPtEnd":   { "x": 5, "y": 0, "z": 0 },
  "section":  { "value": 1 },
  "material": { "value": 2 },
  "linearElementType": "eLinearElementFEMTypeGeneral",
  "generalBeamType": "beamWStandardBending"
}
```

---

## 6. Data Models — Materials

All material classes derive from `Material`. The `$type` property selects the class (see
[Section 5](#5-polymorphism--the-type-discriminator)). To create or read materials, see
[Section 3.2](#32-model--materials).

- All values are SI: Pa for moduli and strengths, kg/m³ for density, kg/m² for surface mass, 1/°C for thermal
  expansion.
- A property that you leave out takes the default in the tables below.
- For steel, reinforced concrete, rigid and other materials, `G` = `0` means that the shear modulus is
  calculated from `E` and `Nu`. The timber materials have a default `G`.
- `BehaviorType` is a `MaterialBehaviourType`: `isotropic_behavior` (default) or `orthotropic_behavior`.

### `Material` (base)

| Property | Type | Description |
|---|---|---|
| `Name` | `string` | Material name. Required. It decides whether a new material is created, see [`CreateMaterial`](#post-apimodelmaterialscreatematerial) |

### `MaterialSteel`

| Property | Type | Unit | Default | Description |
|---|---|---|---|---|
| `BehaviorType` | `MaterialBehaviourType` | — | `isotropic_behavior` | Behaviour type |
| `E` | `double` | Pa | `210000000000` | Young's modulus |
| `Ro` | `double` | kg/m³ | `7850` | Density |
| `Nu` | `double` | — | `0.3` | Poisson's ratio |
| `Damping` | `double` | — | `0.05` | Damping, as a fraction of the critical damping |
| `Alpha` | `double` | 1/°C | `0.000012` | Thermal expansion coefficient |
| `SigmaE` | `double` | Pa | `235000000` | Yield (elastic limit) stress |
| `Color` | `double` | — | `1.2566370614359172` | Display colour, an angle (the default is 72° in radians) |
| `Diffusion` | `double` | rad | `1.2566370614359172` | Diffusion angle |
| `G` | `double` | Pa | `0` | Shear modulus. `0`: calculated from `E` and `Nu` |

### `MaterialReinforcedConcrete`

| Property | Type | Unit | Default | Description |
|---|---|---|---|---|
| `BehaviorType` | `MaterialBehaviourType` | — | `isotropic_behavior` | Behaviour type |
| `Ms` | `double` | kg/m² | `0` | Surface mass |
| `Mu` | `double` | kg/m | `0` | Linear mass. Accepted but not stored: it always reads back as `0` |
| `E` | `double` | Pa | `30000000000` | Young's modulus |
| `Nu` | `double` | — | `0.2` | Poisson's ratio |
| `Damping` | `double` | — | `0.05` | Damping, as a fraction of the critical damping |
| `Alpha` | `double` | 1/°C | `0.00001` | Thermal expansion coefficient |
| `Ro` | `double` | kg/m³ | `2500` | Density |
| `Fck` | `double` | Pa | `25000000` | Characteristic cylinder compressive strength |
| `Fcu` | `double` | Pa | `30000000` | Ultimate (cube) compressive strength |
| `Fyk` | `double` | Pa | `500000000` | Characteristic yield strength of the reinforcement |
| `Fykl` | `double` | Pa | `500000000` | Characteristic yield strength of the longitudinal reinforcement |
| `Fykt` | `double` | Pa | `500000000` | Characteristic yield strength of the transverse reinforcement |
| `Es` | `double` | Pa | `200000000000` | Young's modulus of the reinforcement |
| `EiEv` | `double` | — | `3` | Ratio Ei/Ev (modular ratio for creep) |
| `Color` | `double` | — | `1.2566370614359172` | Display colour, an angle (the default is 72° in radians) |
| `Diffusion` | `double` | rad | `1.2566370614359172` | Diffusion angle |
| `G` | `double` | Pa | `0` | Shear modulus. `0`: calculated from `E` and `Nu` |

### `MaterialRigid` and `MaterialOther`

Both have the properties of `MaterialReinforcedConcrete`, with these differences:

- There is no `Es`.
- `Ro` defaults to `0`.
- The other defaults are the same.

`Mu` is accepted but not stored, as for concrete.

### `MaterialWood` and `MaterialWoodNorthAmerica`

| Property | Type | Unit | Default | Description |
|---|---|---|---|---|
| `BehaviorType` | `MaterialBehaviourType` | — | `isotropic_behavior` | Behaviour type |
| `Ro` | `double` | kg/m³ | `380` | Density |
| `E` | `double` | Pa | `11000000000` | Mean modulus of elasticity along the grain |
| `E0_05` | `double` | Pa | `7400000000` | 5th-percentile modulus of elasticity (E0.05) |
| `Nu` | `double` | — | `0.3` | Poisson's ratio |
| `Alpha` | `double` | 1/°C | `0.000005` | Thermal expansion coefficient |
| `SigmaE` | `double` | Pa | `24000000` | Elastic limit stress |
| `Damping` | `double` | — | `0.05` | Damping, as a fraction of the critical damping |
| `Color` | `double` | — | `1.2566370614359172` | Display colour, an angle (the default is 72° in radians) |
| `G` | `double` | Pa | `690000000` | Mean shear modulus |
| `G0_05` | `double` | Pa | `460000000` | 5th-percentile shear modulus (G0.05) |
| `GammaM` | `double` | — | `1.3` | Partial factor for the material properties (γM) |
| `Fmk` | `double` | Pa | `24000000` | Characteristic bending strength |
| `Ft0k` | `double` | Pa | `14000000` | Characteristic tensile strength parallel to the grain |
| `Fc0k` | `double` | Pa | `21000000` | Characteristic compressive strength parallel to the grain |
| `Fvk` | `double` | Pa | `2500000` | Characteristic shear strength |
| `Ft90k` | `double` | Pa | `400000` | Characteristic tensile strength perpendicular to the grain |
| `Fc90k` | `double` | Pa | `2500000` | Characteristic compressive strength perpendicular to the grain |
| `BurningRatioCoeff` | `double` | mm/min | `0.7` | Charring rate, used in the fire design |

`MaterialWood` has five more properties. The first four are used for cross-laminated timber (CLT).

| Property | Type | Unit | Default | Description |
|---|---|---|---|---|
| `GrMean` | `double` | kg/m³ | `0` | Mean density (Gr,mean) |
| `Frk` | `double` | Pa | `0` | Characteristic rolling shear strength |
| `Fvxyk` | `double` | Pa | `0` | Characteristic planar shear strength |
| `Ftork` | `double` | Pa | `0` | Characteristic torsional shear strength |
| `E90` | `double` | Pa | `0` | Modulus of elasticity perpendicular to the grain |

---

### 6.5 Data Models — Sections

`Section` is polymorphic. Its `$type` is `"Section"` for a catalog or parametric section and `"UserSection"`
for a section whose characteristics the caller supplies. To create or read sections, see
[Section 3.3](#33-model--sections).

#### `Section` (base)

| Property | Type | Description |
|---|---|---|
| `Name` | `string` | Section name. Required |
| `Type` | `SectionType_API` | Shape type of the section. Required. For the values see [Section 13](#13-enumerations-reference) |
| `FamilyCode` | `string?` | Family code. `"USECT"` for a user section |
| `CatalogName` | `string?` | Catalog the section comes from. Empty for a user section |

#### `UserSection`

Derives from `Section`. A user section has no dimensions. The analysis and the design checks use only the
characteristics below.

- All values are SI.
- Advance Design never computes the characteristics from the outline and never compares them with it. Supply
  all of them.
- `Type` is always `st_USER`, `FamilyCode` is `"USECT"` and `CatalogName` is empty. They are outputs; send
  `"type": "st_USER"`.

| Property | Type | Unit | Default | Description |
|---|---|---|---|---|
| `Area` | `double` | m² | `0` | Sectional area A. Must be greater than 0 |
| `Iy` | `double` | m⁴ | `0` | Moment of inertia about the local y axis (strong axis). Must be greater than 0 |
| `Iz` | `double` | m⁴ | `0` | Moment of inertia about the local z axis (weak axis). Must be greater than 0 |
| `Iyz` | `double` | m⁴ | `0` | Product of inertia. `0` for a section with an axis of symmetry. Signed: a negative value is valid |
| `It` | `double` | m⁴ | `0` | Torsional constant. Must be greater than 0 |
| `Iw` | `double` | m⁶ | `0` | Warping constant. Can be `0` for solid sections and closed tubes. Needed for lateral torsional buckling |
| `WelyInf` | `double` | m³ | `0` | Elastic modulus about y, lower fibre |
| `WelySup` | `double` | m³ | `0` | Elastic modulus about y, upper fibre |
| `WelzInf` | `double` | m³ | `0` | Elastic modulus about z, lower fibre |
| `WelzSup` | `double` | m³ | `0` | Elastic modulus about z, upper fibre |
| `Wply` | `double` | m³ | `0` | Plastic modulus about y |
| `Wplz` | `double` | m³ | `0` | Plastic modulus about z |
| `Wt` | `double` | m³ | `0` | Torsional modulus. Used for the torsional shear stress |
| `Sy` | `double` | m² | `0` | Shear area for a shear force along y (European notation). Cannot exceed `Area` |
| `Sz` | `double` | m² | `0` | Shear area for a shear force along z (European notation). Cannot exceed `Area` |
| `Avy` | `double` | m² | `0` | Shear area for a shear force along y (US / Canada notation). Cannot exceed `Area` |
| `Avz` | `double` | m² | `0` | Shear area for a shear force along z (US / Canada notation). Cannot exceed `Area` |
| `ProfileType` | `UserSectionProfileType_API` | — | `Unknown` | The standard shape the section behaves like. The steel design checks need it. For the values see [Section 13](#13-enumerations-reference) |
| `RollingType` | `SectionRollingType_API` | — | `Welded` | How the section was made. Selects the buckling curve |
| `Shape` | `UserSectionShape?` | — | `null` | The outline. `null` keeps the default outline |

- *Lower* and *upper* fibre are the fibres at the lowest and at the highest z. Z is always positive upwards in
  the API.
- `ProfileType` left at `Unknown`: the steel design engines skip the checks that depend on the shape.
- `RollingType` is one of `Unknown`, `Rolled`, `HotRolled`, `ColdRolled`, `Welded`, `ThickWelded`,
  `RolledColdFormed`, `BendColdFormed`. `Welded` is what Advance Design gives a user section.
- The default outline is a 50 x 50 mm square centred on the origin.

##### Validation of the characteristics

`CreateUserSection` refuses the call, and writes nothing, in these cases:

- A value is not a finite number. `"NaN"`, `"Infinity"` and `"-Infinity"` are valid JSON for this service, so
  they can reach the endpoint.
- A value is negative. `Iyz` is the only one that can be negative.
- `Area`, `Iy`, `Iz` or `It` is `0` or less.
- `Iy·Iz − Iyz²` is `0` or less.
- `ProfileType` or `RollingType` is not a value of its enumeration (for example an integer that names nothing).

It accepts the section and reports a warning in these cases:

- `Area`, `Iy`, `Iz` or `It` is above 0 but not above `1e-12`. The values are probably not in SI.
- `Iw`, `WelyInf`, `WelySup`, `WelzInf`, `WelzSup`, `Wply`, `Wplz`, `Wt`, `Sy`, `Sz`, `Avy` or `Avz` is `0`.
  The checks that need the value are then not available.
- `ProfileType` is `Unknown`.
- `Wply` is smaller than `WelyInf` or `WelySup`, or `Wplz` is smaller than `WelzInf` or `WelzSup`.
- A shear area (`Sy`, `Sz`, `Avy`, `Avz`) is larger than `Area`.
- `Type` is neither `st_USER` nor `st_UNKNOWN`. It is replaced with `st_USER`.

Warnings are returned only when the session runs with `ErrorsAndWarnings` or `AllDetails` verbosity (see
[Section 3.1.1](#311-the-environment-of-a-session)).

#### `UserSectionShape` — the outline

The outline is the geometry the section is drawn with. It is a list of contours, so a section of several
separate parts, or with holes, needs nothing special. It changes no characteristic.

| Property | Type | Description |
|---|---|---|
| `Contours` | `UserSectionContour[]` | The contours of the outline. At least one must add material. Stored and drawn in the order given |

##### How to define an outline

- **Points are 2D, in metres.** Each point is a `(Y, Z)` pair in the plane of the cross-section: y is
  horizontal, z is vertical and positive upwards. There is no third coordinate.
- **The origin is the centre of gravity.** Advance Design does not compute the centre of gravity of a user
  section. It takes it as `(0, 0)` and maps the member offsets from there.
- **Which way up matters.** For a section that is not symmetric, the `Inf` moduli belong to the fibres at the
  lowest z and y, and the `Sup` moduli to the fibres at the highest. The moduli must agree with the outline.
- **A contour closes itself.** List the vertices of a polygon once, in order around the contour. Do not repeat
  the first point at the end. The order is stored as given; Advance Design writes its own outlines clockwise.
- **Several contours describe several parts.** One material contour per part: a double angle is two contours,
  a double channel is two, a battened box of four angles is four. The parts do not have to touch and are not
  merged.
- **A hole removes material.** Set `IsHole` on a contour that lies inside a material contour to describe a
  tube, a box or a cell. Holes and material contours can be mixed in any order. Each can be a polygon or a
  circle: a circular tube is one circle and one circular hole.

> **Warning:** Nothing recentres an outline that arrives through the API. An outline that is not centred on
> `(0, 0)` is stored and drawn where you put it, and the members that use the section look shifted from their
> own axis.

#### `UserSectionContour`

| Property | Type | Default | Description |
|---|---|---|---|
| `Type` | `UserSectionContourType_API` | `Polygon` | `Polygon` (a list of vertices) or `Circle` (a centre and a radius) |
| `IsHole` | `bool` | `false` | `false` adds material, `true` removes it |
| `Points` | `UserSectionContourPoint[]` | — | A polygon: its vertices, at least 3. A circle: exactly one point, its centre |
| `Radius` | `double` | `0` | Radius of a circle, in m. Must be greater than 0. Ignored on a polygon |

#### `UserSectionContourPoint`

| Property | Type | Unit | Default | Description |
|---|---|---|---|---|
| `Y` | `double` | m | `0` | Coordinate along the local y axis |
| `Z` | `double` | m | `0` | Coordinate along the local z axis, positive upwards |
| `FilletRadius` | `double` | m | `0` | Radius of the fillet that rounds the corner at this vertex. `0` keeps the corner sharp. Polygons only |

##### Validation of the outline

`CreateUserSection` refuses the call, and writes nothing, in these cases:

- The outline has no contour. To keep the default outline, leave `Shape` as `null`.
- The outline has only holes.
- The outline has more than 500 contours, or a contour has more than 500 points.
- A contour is `null`, or has no `Points` list.
- A polygon has fewer than 3 points.
- A circle has not exactly one point, or its `Radius` is not greater than 0.
- Two consecutive points of a polygon are identical. A polygon that repeats its first point at the end falls
  under this rule.
- A `FilletRadius` is negative.
- A `FilletRadius` is too large for its corner. The tangent length of the fillet, `radius / tan(angle / 2)`,
  where `angle` is the interior angle at the vertex, must be shorter than both adjacent edges.
- Any coordinate, `FilletRadius` or circle `Radius` is not a finite number.

It accepts the outline and reports a warning when a fillet sits on a vertex that forms no corner (the vertex
lies on a straight edge between its neighbours). The fillet has no effect.

##### Update and read-back

- Sending a `Shape` for an existing user section replaces its outline entirely.
- Leaving `Shape` as `null` keeps the outline it has. It does not reset it to the default square.
- A user section edited in the Advance Design cross-section editor can carry thin-walled contours in addition
  to its drawn outline (they feed the profile segmentation of the Advanced Stability solver).
  `UserSectionShape` cannot express them. A read returns only the drawn contours. Sending an outline for such a
  section drops the other contours, with a warning.
- The editor also draws arcs, segments and polylines whose points carry a camber. `UserSectionShape` has no
  equivalent. A read returns these contours as polygons, with a warning that names the section. This is exact
  for a straight polyline (the default square is one) and an approximation for the others. Do not send such an
  outline back unchanged.

##### Example — a rectangular tube with a hole

A 200 x 100 mm rectangular tube with a 10 mm wall: one rectangle of material and one rectangular hole, both
centred on the origin.

```csharp
var tube = new UserSectionShape
{
    Contours =
    [
        new UserSectionContour   // the outside
        {
            Points =
            [
                new UserSectionContourPoint { Y = -0.050, Z =  0.100 },
                new UserSectionContourPoint { Y =  0.050, Z =  0.100 },
                new UserSectionContourPoint { Y =  0.050, Z = -0.100 },
                new UserSectionContourPoint { Y = -0.050, Z = -0.100 }
            ]
        },
        new UserSectionContour   // the cell, 10 mm inside it
        {
            IsHole = true,
            Points =
            [
                new UserSectionContourPoint { Y = -0.040, Z =  0.090 },
                new UserSectionContourPoint { Y =  0.040, Z =  0.090 },
                new UserSectionContourPoint { Y =  0.040, Z = -0.090 },
                new UserSectionContourPoint { Y = -0.040, Z = -0.090 }
            ]
        }
    ]
};
```

---

## 7. Data Models — Elements (geometric)

All geometric elements derive from `ElementBase`. Every type in Sections 7 to 9 is sent and received with its
`"$type"` ([Section 5](#5-polymorphism--the-type-discriminator)). Lengths are in metres and angles in
radians, unless a property says otherwise. How a call treats properties that are not sent is explained in
[Section 3.4](#properties-that-are-not-sent). `EID` is an object with one property, `Value`. The tables list
the properties that carry a rule; the complete list of each class, with all defaults, is in `swagger.json`.

### `ElementBase` (base)

| Property | Type | Description |
|---|---|---|
| `UserID` | `int` | The number shown for the element in Advance Design. Read-only: ignored on create and update. Not the EID |
| `UserName` | `string` | The name of the element. Written only when it is not empty |

### System membership — `SystemIDs`

Every structural type carries a `SystemIDs` list (`ICollection<EID>`): the
[structural systems](#104-structural-systems) (containers, building levels) the element belongs to. An
element can belong to several systems.

Types with `SystemIDs`: `ElementLinear`, `ElementPlanar`, `ElementLoadArea`, `ElementMass`,
`ElementSinglePile`, `ElementPoint`, `ElementPolyLine` and the 12 support types. Loads and imposed
displacements have no system membership.

| Call | Behaviour |
|---|---|
| Create | The element is created in the first listed system and added to the others. An empty or missing list puts it in the root system `Structure 0`. If one listed EID is not a system, the element is not created |
| Read | The list holds the current systems of the element |
| Update | A non-empty list sets the membership to exactly these systems: the element is added to the new ones and removed from the others. An empty list leaves the membership unchanged. If one listed EID is not a system, the membership stays unchanged and the update returns `false`. The other changes of the same call stay applied |

An element always belongs to at least one system. That is why an empty list means "unchanged" and not
"clear". When an element would be left in no system (its systems were deleted, or the requested membership
could not be applied), it goes back to `Structure 0` and a warning is returned.

### `ElementLinear`

A beam, column, truss bar, tie, strut or cable.

| Property | Type | Unit | Default | Description |
|---|---|---|---|---|
| `GeomPtStart`, `GeomPtEnd` | `Pt3D` | m | | Start and end point, global axes. Required |
| `Section` | `EID` | | | An existing section. Required. For a variable beam it is the section at the start |
| `SectionEnd` | `EID` | | | Section at the end of a variable beam — see below |
| `Material` | `EID` | | | An existing material. Required |
| `LinearElementType` | `LinearElementFEMType_API` | | `eLinearElementFEMTypeGeneral` | Or `eLinearElementFEMTypeCompositeBeam` |
| `GeneralBeamType` | `GeneralBeamType_API` | | `sbeam` | Kind of member — see below |
| `SectionOrientationAngle` | `double?` | rad | `0` | Rotation of the section about the element axis |
| `SectionOrientationPointId` | `int?` | | `null` | `UserID` of an `ElementPoint` the section faces — see below |
| `SectionExcentration` | `SectionOffsetStyle` | | `null` | Offset of the section from the axis |
| `RelaxationTotale`, `RelaxationElastique` | `ConnectionTotale`, `ConnectionElastique` | | `null` | Releases at the ends |
| `HaunchStart`, `HaunchEnd` | `HaunchProperties` | | `null` | Haunch at the start and at the end |
| `LoadAreaLoadTransferProperties` | `LinearLoadAreaLoadTransfer` | | `null` | How load areas load this element |
| `SystemIDs` | `ICollection<EID>` | | | See [System membership](#system-membership--systemids) |
| `DesignTemplateId` | `EID?` | | `null` | [Expert design template](#106-expert-design-templates) of the element |

Also: `ElementActive` (`bool?`, `true`), `AutoMesh` (`bool?`, `true`), `ExtendIntoWall` (`bool?`, `true`),
`DetailedMeshProperties` (`LinearElementMeshStyle`: `NumberOfElements`, `SizeOfElements` in m,
`SpacingOption`, `SpacingFactor`), `InertiaProperties` (`LinearElementInnertia`, the cracked-inertia
coefficients: `LinearConcreteInertiaType` `0` = automatic and `1` = coefficients set by hand, then one
coefficient each for Iy, Iz, torsion and axial stiffness, all `1` by default, and `ProductOfInertiaIyz0`),
`InitialConstraintProperties` (`InitialConstraint`: `InitialConstraintLoadCase`, and the stresses
`InitialConstraintSxx`, `ConstraintSxy`, `ConstraintSxz` in **MPa**) and `Clipping` (`ClippingProperties`:
`ClippingState` and, for the start and the end, the plane type and value in m for the XY and XZ planes).

`GeneralBeamType_API` values: `bar`, `beamWStandardBending`, `sbeam` (beam with shear deformation),
`variablebeam`, `tie` (tension only), `strut` (compression only), `cable`, `rigid`, and for
`eLinearElementFEMTypeCompositeBeam`: `CompositeBeamSimpleBeam`, `CompositeBeamSbeam`,
`CompositeBeamVariable`.

Rules:

- **Variable beams** (`variablebeam` or `CompositeBeamVariable`): `Section` is the section at the start and
  `SectionEnd` the section at the end. `SectionEnd` is required, and both sections must be of the same
  section type. If not, the request is refused: on create no element is created, and on update the end
  section stays unchanged.
- **Other beam types:** the section is the same over the whole length. A `SectionEnd` that differs from
  `Section` is ignored with a warning.
- `DesignTemplateId`: `null` leaves the assignment unchanged and `0` detaches the element from its
  template. The template must fit the element — see
  [`AssignDesignTemplateToElements`](#post-apimodelelementsassigndesigntemplatetoelements). Changing the
  material of an element drops its design template. Assign it again after a material change.
- A sub-object that is `null` is not written on create and not changed on update.

> **Warning:** On create, send `ElementActive` and `AutoMesh` together, or neither. `AutoMesh` is read only
> when `ElementActive` is sent. `ElementActive` without `AutoMesh` makes the creation fail, and the element
> can be left half-built in the model. The generated C# classes send both by default.

#### Orienting a section towards a point

`SectionOrientationPointId` is the `UserID` of an `ElementPoint` (the number shown in the interface), not its
EID. When it is set, the local axes are rebuilt so that the section faces that point, and
`SectionOrientationAngle` no longer drives the orientation.

| Value | Behaviour |
|---|---|
| `0` | No orientation point. `SectionOrientationAngle` orients the section |
| `UserID` of an existing point | The section faces that point |
| `UserID` that matches no point | Rejected. The value is not stored |
| `null` | Unchanged. The property has no default, so an update that omits it keeps the current value |

The element follows the point: moving the point re-orients the element. Deleting the point resets
`SectionOrientationPointId` to `0`, and the element falls back to `SectionOrientationAngle`.

#### Releases at the ends

`ConnectionTotale` (`RelaxationTotale`) has `StartBoundaryConnection` and `EndBoundaryConnection`. Each is a
`ConnectionStyle` with six `bool` properties, all `false` by default: `RelaxationTx`, `RelaxationTy`,
`RelaxationTz`, `RelaxationRx`, `RelaxationRy`, `RelaxationRz`. `true` releases that degree of freedom at
that end.

`ConnectionElastique` (`RelaxationElastique`) has `StartConnectionElastique` and `EndConnectionElastique`.
Each holds six lists of function points (`ValueX`, `ValueFX`), one per degree of freedom
(`ElastiqueRelaxationTxValues` ... `ElastiqueRelaxationRzValues`). A list set to `null` keeps the engine
defaults. On update, a list is applied only when it is not empty. An empty list does not clear a function.

#### `HaunchProperties` (`HaunchStart`, `HaunchEnd`)

`HaunchStart` and `HaunchEnd` decide the end of the element. You set `HaunchPosition` (the side of the
section the haunch deepens: `no_haunch` (default), `haut_et_bas`, `haut`, `bas`), the length
(`LengthType`, `LengthRatio` of the element length, `LengthValue` in m), the height (`HeightType`,
`HeightRatio` of the section depth, `HeightValue` in m) and the haunch section (`HaunchSectionType`:
`identical`, `suivant`, `precedent` or `imposee`; `HaunchSection` is the section used with `imposee`).

- `LengthType` is `ratio` (default), `valeur_axe_local` or `valeur_projete`. `HeightType` is `ratio` (default)
  or `valeur_axe_local`. The value `valeur` and the other height values are not read by the analysis.
- `HaunchPosition` is not a position along the element.
- On update, `HaunchSection` is applied only when it is not `0`.

#### `SectionOffsetStyle` (`SectionExcentration`)

`Option` (default `center_alignment`) sets how the section is offset from the element axis: `center_alignment`
(no offset); `topleft`, `gauche_centre`, `gauche_bas`, `centre_haut`, `centre_bas`, `droite_haut`,
`droite_centre`, `droite_bas` (an edge of the section is placed on the axis, the offset is computed from the
section and the `Delta` values are ignored); `autre` (the offset is given by `DeltaY1`, `DeltaZ1` at the start
and `DeltaY2`, `DeltaZ2` at the end, in m). `ConsideredInFEM` (`bool`, default `false`): the analysis takes the
offset into account only when it is `true`.

#### `LinearLoadAreaLoadTransfer` (`LoadAreaLoadTransferProperties`)

How loads from wind walls, scaffolding, snow and climatic surfaces are transferred to the element. You set
`IsWindWallSupport` (default `true`), `IsScaffoldingSupport`, `ClimaticLE2D` (the element takes part in the 2D
climatic distribution), `LoadSpanFront` and `LoadSpanBehind` (m, `2.5`), `ContinuityCoeff` (`1` = full
continuity), `OpeningType` with `OpeningsValue` (m² or %, according to the type), `SnowGuards2D` and
`ClimaticType`.

`EffectiveWindArea` (`double?`, m²) is the ASCE 7-22 effective wind area of the 2D climatic wind walls
generated from this element. `null` keeps the model default (`0.929` m², 10 ft²) on create and the current
value on update. A value that is not greater than `0` is ignored with a warning.

### `ElementPlanar`

A slab, wall, plate or shell.

| Property | Type | Unit | Default | Description |
|---|---|---|---|---|
| `GeomPtsList` | `ICollection<Pt3D>` | m | | Corner points, global axes, in order. Required, at least 3 points — see [Planar contour validity](#planar-contour-validity) |
| `Material` | `EID` | | | An existing material. `0` or missing: the element is not created |
| `ElementType` | `PlanarElementType` | | `shell` | `membrane`, `plate`, `shell`, `deformation_plane`, `steeldeck`, `layeredShell`, `slabonsteeldeck` |
| `Eccentricity` | `double` | m | `0` | Offset of the element. `EccentricityCOnsideredAlsoForFEM` (`bool`, `false`; capital O in the schema) makes the analysis use it |
| `ThicknessIn1stVertex` | `double` | m | `0.2` | Thickness at the first point. `SlopeX` and `SlopeY` (`0`) give a variable thickness as the tangent of the angle in the local x and y directions |
| `SupportingElement` | `bool` | | `true` | The element supports load areas |
| `MeshProperties` | `PlanarElementMeshProperties` | | `null` | Mesh settings — see below |
| `Openings` | `ICollection<ICollection<Pt3D>>` | m | `null` | Holes. Each opening is a closed polygon in global coordinates. Only openings fully inside the element are accepted. Openings on the edge are rejected |
| `SelectedReleaseName`, `LinearReleases` | `string?`, `ICollection<EID>` | | `null` | [Linear release templates](#105-linear-release-templates) of the edges — see [Edge releases](#edge-releases) |
| `SystemIDs` | `ICollection<EID>` | | | See [System membership](#system-membership--systemids) |

Rules for `Openings`:

- On update, `null` keeps the openings, `[]` removes all of them, and a list replaces all of them.
- A change of `GeomPtsList` deletes all existing openings. They come back only if you send `Openings` in the
  same update.

`PlanarElementMeshProperties` (`MeshProperties`): `AutomaticMesh` (`true`), `MeshType` (`complete`,
`triangulation`, `none`), `MeshDensity` (`global`, `simplified`, `detailed`, `element_size`), and the values
that go with a density: `MeshElementSize` (m) with `element_size`, `MeshNoInXDirInLocal` and
`MeshNoInYDirInLocal` with `simplified`, `DetailedMeshStylePerEdge` with `detailed`. That list has one
`DetailedMeshOnSegment` for each edge, as many as the points of the contour. A `DetailedMeshOnSegment` has
`NumberOfElements`, `SizeOfElements` (m), `SpacingOption` and `SpacingFactor`.

#### Edge releases

The edges of a planar element are connected to the rest of the structure through
[linear release templates](#105-linear-release-templates) from the model pool. The element only refers to the
templates, so editing a template changes every edge that uses it. Two properties set the assignment:

| Property | Type | Granularity | Name in Advance Design |
|---|---|---|---|
| `SelectedReleaseName` | `string?` | One template for all edges | *Releases / Side* |
| `LinearReleases` | `ICollection<EID>` | One template per edge | *Releases / Definition* |

The edge order is the order of the points: `LinearReleases[i]` is the edge from `GeomPtsList[i]` to
`GeomPtsList[i+1]`. The last entry closes the contour back to `GeomPtsList[0]`. The list has exactly as many
entries as the contour has points. Openings never add edges.

| Call | `SelectedReleaseName` | `LinearReleases` |
|---|---|---|
| Create | Optional. When omitted, every edge gets the built-in `Fixed` template. A name that matches no template falls back to `Fixed` with a warning, and the element is still created | Optional. The number of entries must equal the number of edges and every EID must be a template. Otherwise the whole list is rejected, an error is returned and no edge changes |
| Read | The template name when all edges share one, the localized word for "Other" when they differ, empty when the releases were never set | The complete list of the edges. This is the reliable per-edge state |
| Update | `null` or empty keeps the current assignment. A name that matches no template is ignored with a warning | The same validation as on create. An invalid list leaves every edge unchanged |

- `SelectedReleaseName` wins. When both properties are sent, in a create or an update, the name is applied to
  all edges and a warning is returned. Send `LinearReleases` alone for a per-edge assignment.
- The names of the three built-in templates are localized. `SelectedReleaseName` also accepts the keywords
  `free`, `pinned` and `fixed` (not case-sensitive). For any other template, list the pool with
  `QueryInfoModel` and `LinearReleaseTemplate` and read the name from it.

> **Warning:** An update that changes the number of points of `GeomPtsList` resets every edge release to
> `Fixed` and returns a warning. The positions in the list no longer match the same sides, so the old
> releases cannot be kept. A change that keeps the same number of points leaves the releases in place. To
> keep or set a specific assignment, send `SelectedReleaseName` or `LinearReleases` in the same update. An
> explicit request wins over the reset.

### Planar contour validity

The point list `GeomPtsList` of every object built on a planar contour (planar elements, planar supports,
load areas, planar loads) must be a valid planar polygon:

- at least 3 points;
- no two coincident points;
- no three consecutive collinear points. A point in the middle of a straight side makes an edge of zero
  length, which the model does not accept;
- all points in one plane;
- no self-intersection.

A contour that breaks one of these rules is refused. On create, the element is not created. On update,
nothing is modified: the element keeps its geometry, its releases and its name. The error message names the
reason, so correct the points before you try again.

### `ElementSinglePile`

| Property | Type | Unit | Default | Description |
|---|---|---|---|---|
| `GeomPt` | `Pt3D` | m | `(0, 0, 0)` | Position of the pile head. Required |
| `Material`, `CrossSection` | `EID` | | | An existing material and section. Required |
| `TotalLength` | `double` | m | `10` | Length of the pile. Required |
| `Bearing` | `PileBearing` | | `null` | `CompressionBearingCapacity` (N), `TensionBearingCapacity` (N), `LateralBearingCapacity` (N/m) |
| `RestraintsOptions` | `PileRestraintsOptions` | | `null` | `Tx` ... `Rz`, each `ePileSupportRestraintTypeFree`, `...Elastic` or `...Fixed` (default) |
| `RestraintsDiagrams` | `AdvancedRestraintsDiagramDefinitions` | | `null` | Used only when `RestraintsOptions` is also sent — see [Advanced supports](#advanced-supports) |

Also: `ElementActive` (`bool?`, `true`) and `SystemIDs`.

### `ElementLoadArea`

A load area (tributary area). It distributes climatic or mechanical loads onto the structural elements.

| Property | Type | Description |
|---|---|---|
| `GeomPtsList` | `ICollection<Pt3D>` | Corner points, global axes. Required — see [Planar contour validity](#planar-contour-validity) |
| `LoadTransferProperties` | `LoadAreaLoadTransfer` | `LoadTransferMethodType` (`eLoadTransferMethodFailureLines`, `eLoadTransferMethodFemTransfer`, `eLoadTransferMethodAuto` by default) and `LoadTransferSpanDirectionType` (`eFloorDeckLoadSpanDirectionX`, `...Y`, `...XY` by default). Failure lines suit a four-sided area with a uniform load; for other cases use the FEM transfer |
| `MechanicalProperties` | `LoadAreaMechanicalProperties` | `RigidDiafragm` and `SelfWeightAuto` (both `false`), computed from `Material` (an existing material, required when either is `true`) and `Thickness` (m, `0.2`) |
| `ClimaticProperties` | `LoadAreaClimaticProperties` | Wind and snow settings — see below |
| `SystemIDs` | `ICollection<EID>` | See [System membership](#system-membership--systemids) |

`LoadAreaClimaticProperties` key properties: `ClimaticType` (`Load_Area_Type`, default
`CG_LOADAREA_BUILDING`), `AvailableForSnow` and `AvailableForWind` (both `true`), `OpeningType` with
`OpeningValue` (m² or percent from 0 to 100, according to the type), `ExposurePerDirection` (empty, or 4
entries in the order X+, X−, Y+, Y−). The other properties (gutter, parapet, snow guards, free-standing wall)
are in `swagger.json`.

`EffectiveWindArea` (`double?`, m²) is the ASCE 7-22 effective wind area of the components carried by the
load area. `null` keeps the model default (`0.929` m², 10 ft²) on create and the current value on update. A
value that is not greater than `0` is refused. Only the ASCE 7-22 climatic code reads it.

### `ElementPoint`

The *Point* object of Advance Design. A point has no stiffness and no load. It forces a finite element node
at its position, other elements snap onto it, and under Eurocode it is the pivot of the direction of the
steel imperfection forces. Query points with `ElementType = ElementPoint`.

| Property | Type | Unit | Default | Description |
|---|---|---|---|---|
| `GeomPt` | `Pt3D` | m | `(0, 0, 0)` | Position, global axes. Required |
| `MeshActive` | `bool?` | | `true` | `true`: the mesh generator puts a node at the point. `false`: the analysis model ignores the point |
| `Density` | `double` | m | `0` | Target size of the finite elements around the point. `0` = the global mesh settings apply. Used only when `MeshActive` is `true` |
| `MagnetismStatus` | `bool` | | `false` | Elements whose end lies within `MagnetismTolerance` of the point are snapped onto it |
| `MagnetismTolerance` | `double?` | m | `0.001` | Radius of the magnetism. Used only when `MagnetismStatus` is `true` |
| `ImperfectionForceDirectionPivot` | `bool` | | `false` | Pivot for the direction of the steel imperfection forces (read when the design template sets `ForceDirectionPivotOnSuperElement` to `use_points`). Stored for every model, effective only under Eurocode |

`MeshActive` and `MagnetismTolerance` are nullable on purpose. `null` means "keep the Advance Design default"
on create and "keep the current value" on update. Both are always filled when you read the point. Also:
`SystemIDs`.

### `ElementPolyLine`

The *Polyline* object of Advance Design (shown as *Line* in the model tree). A chain of straight segments
with no stiffness, no material, no section and no load. It is reference geometry: it forces nodes along its
segments when `MeshActive` is set, and other elements snap onto it. Query poly lines with
`ElementType = ElementPolyLine`.

| Property | Type | Unit | Description |
|---|---|---|---|
| `GeomPtsList` | `ICollection<Pt3D>` | m | The vertices in drawing order, global axes. Required. N points give N-1 segments |
| `MeshProperties` | `PolyLineMeshProperties` | | Mesh definition. `null` leaves the whole mesh definition alone |

`PolyLineMeshProperties`: `MeshActive` (`bool?`, default `false`) — `true`: the mesh generator puts nodes
along the segments; `false`: pure reference geometry, ignored by the analysis; `null` keeps the default on
create and the current value on update. `DetailedMeshStylePerEdge` (`DetailedMeshOnSegment` list) — one entry
per segment: entry `i` is the segment from `GeomPtsList[i]` to `GeomPtsList[i+1]`, so N-1 entries. Used only
when `MeshActive` is `true`. `null` leaves the segments unchanged. Entries beyond the number of segments are
ignored. Segments the list does not cover, and `null` entries, keep their current values. A list of the
wrong length returns a warning, not an error.

A poly line follows the rules of the interactive *Polyline* command. It is not a planar contour:

| Point list | Result |
|---|---|
| Fewer than 2 points | Refused. There is no segment |
| A point that coincides with the one before it | Dropped, not refused. A warning reports the dropped points |
| Every point coincides with the previous one | Refused. No segment is left |
| Aligned points, any number | Accepted. They make collinear segments |
| Not planar, or self-intersecting | Accepted |
| Any number of points | Accepted. There is no maximum |

- A refused list is rejected before anything is written. On create, no element is created. On update,
  nothing changes.
- After the duplicate points are dropped, the geometry you read back and `DetailedMeshStylePerEdge` refer to
  the segments that remain.
- A poly line is not closed by itself. To close it, repeat the first point as the last point. You read it
  back the same way: N distinct vertices come back as N+1 points and N segments.
- An update that changes the number of segments first resizes `DetailedMeshStylePerEdge`: existing segments
  keep their definition at the same index, new segments get the defaults and removed segments are dropped.
  The list sent in the same update is applied after that.

### `ElementMass`

A lumped mass at a point: `GeomPt` (`Pt3D`, m, required), `Mx`, `My`, `Mz` (kg) and `Ix`, `Iy`, `Iz` (kg·m²,
about the global axes), all `0` by default, and `SystemIDs`.

---

## 8. Data Models — Supports

A support has two parts: the **family** (`FamilySupportPunctual`, `FamilySupportLinear`,
`FamilySupportPlanar`) gives the geometry and the common properties, and the **behaviour** gives the
mechanical behaviour. You create, update and read the 12 behaviour types. The family classes are base
classes and cannot be created. All supports carry `SystemIDs` — see
[System membership](#system-membership--systemids).

The types are `ElementRigid…Support`, `ElementElastic…Support`, `ElementTC…Support` (tension or compression
only) and `ElementAdvanced…Support` (non-linear per degree of freedom), each with `Punctual`, `Linear` or
`Planar` in place of the dots, for example `ElementRigidPunctualSupport`.

### Families

| Family | Geometry (global axes, m) | Other properties |
|---|---|---|
| `FamilySupportPunctual` | `GeomPt` (`Pt3D`, required) | `Material` (`EID`, required), `Footing`, `SuppElement`, `Punching` |
| `FamilySupportLinear` | `GeomPtStart`, `GeomPtEnd` (`Pt3D`, required) | `Material` (`EID`, required), `Footing`, `SuppElement` |
| `FamilySupportPlanar` | `GeomPtsList` (required) — see [Planar contour validity](#planar-contour-validity) | |

All families also have `ElementActive` (`bool?`, `true`) and `SystemIDs`.

- `Footing` (`FootingDimensions`, or `FootingDimensionsLinear` for a linear support) is the footing under the
  support. `FootingEnable` is `false` by default: the dimensions are ignored until it is `true`, and it must be
  `true` to create or keep a footing. The dimensions are in m.
- `SuppElement` (`SupportingElementDimensions`, or `...Linear`) is the supported element: its type
  (`FootingTemplate`, default `TypeAuto`, values in [Section 13](#13-enumerations-reference)) and its width
  `a`, length `b` (punctual only) and height `h` in m.
- `Punching` (`PunchingProperties`, also used by `ElementLoadPunctual`): `Position` (`Auto`, `Edge`, `Angle`,
  `Inside`, `Imposed`, `None`), `Beta` (used with `Imposed`), `ControlPerimeter` (`0` = u1 is calculated,
  `1` = you impose it) and `U1` (m).

The full list of these objects is in `swagger.json`.

### Rigid supports

| Property | Type | Default | Description |
|---|---|---|---|
| `ConstraintsType` | `RigidSupportConstraint` | `fix` | `fix`: all six degrees of freedom locked. `SupportHinge`: `Tx`, `Ty`, `Tz` locked. `other`: the degrees of freedom of `Restraints` |
| `Restraints` | `DegreeOfFreedomRestraints` | `null` | `Tx`, `Ty`, `Tz`, `Rx`, `Ry`, `Rz`, all `false` by default. `true` locks the degree of freedom. Used with `other` |

### Elastic supports and tension/compression supports

The elastic types and the tension/compression types (`ElementTC…Support`) have the same properties. The TC
types add `TcBehavior` (`TractionOrCompressionBehaviour`: `compression` (default) or `traction`).

| Property | Type | Description |
|---|---|---|
| `Stiffness` | `SupportElasticStiffness` | Spring stiffness: `Ktx`, `Kty`, `Ktz` (translation) and `Krx`, `Kry`, `Krz` (rotation), all `0` by default |
| `DampingRatio` | `SeismicSupportDampingRatio` | Damping per degree of freedom (`Tx` ... `Rz`, no unit, default `0`) |
| `VerticalStiffness` | `SupportVerticalStiffness` | Vertical stiffness from soil layers (`VerticalStiffnessCalcul`: `0` automatic, `1` manual; `Layers`: list of `SoilLayer`) |

The unit of the stiffness depends on the support that carries it. Linear and planar stiffnesses are given
per metre of support length and per m² of support area:

| Support | Translation | Rotation |
|---|---|---|
| Punctual | N/m | N·m/rad |
| Linear (per metre of length) | N/m² | N·m/rad/m |
| Planar (per m² of area) | N/m³ | N·m/rad/m² |

Example: a linear support with `Ktz = 1e6` under a load of 10 kN/m settles 0.01 m. A planar support with
`Ktz = 1e6` under a load of 10 kPa settles 0.01 m.

A `SoilLayer` holds a `Soil`, a thickness (m) and the subgrade reaction coefficients (N/m³). Unlike the rest
of the API, the soil uses kPa (cohesion, elastic modulus) and degrees (internal angle, friction angle between
layers). The full list is in `swagger.json`.

### Advanced supports

| Property | Type | Description |
|---|---|---|
| `RestraintsOptions` | `AdvancedRestraintsOptions` | `Tx` ... `Rz`, each an `AdvancedRestraintType`, default `eFixed` |
| `RestraintsDiagrams` | `AdvancedRestraintsDiagramDefinitions` | Used only when `RestraintsOptions` is also sent |

`AdvancedRestraintType` values: `eFree`, `eFixed`, `eElastic` (linear spring), `eNLActiveForCompression`,
`eNLActiveForTension`, `eNLGap` (limit in distance), `eNLHardening` (limit in force), `eNLDiagram` (piecewise
diagram that you define).

`AdvancedRestraintsDiagramDefinitions` has one optional list of `DiagramPoint` per degree of freedom
(`LockDescriptionTX` ... `LockDescriptionRZ`). A `DiagramPoint` has `DVal` (displacement in m or rotation in
rad) and `KVal` (stiffness in N/m or N·m/rad).

---

## 9. Data Models — Loads

Loads belong to a load case ([Section 10.2](#102-load-cases)). Create the load case first and use its EID in
`LoadCase`. Loads and imposed displacements have no `SystemIDs`. Values are in newtons and metres.

Rules for the three loads:

- At least one of `Fx`, `Fy`, `Fz`, `Mx`, `My`, `Mz` must not be `0`. `Moment` is optional when one of
  `Fx`, `Fy`, `Fz` is not `0`.
- `CoordinateSystemType` selects the axes of the components: `global_or_user` (default) or `local_frame`.
  Linear and planar loads also accept `projected_frame`.
- `UserComment` is a free comment (at most 250 characters). `null` leaves it unchanged on update.
- On update, a different `LoadCase` moves the load to that load case.

| Type | Geometry (m) | Force `Fx`, `Fy`, `Fz` | `Moment` (`MomentComponents`) | Variation |
|---|---|---|---|---|
| `ElementLoadPunctual` | `GeomPt` | N | `Mx`, `My`, `Mz` in N·m | none |
| `ElementLoadLinear` | `GeomPtStart`, `GeomPtEnd` | N/m | `Mx`, `My`, `Mz` in N·m | `LinearVariation`: `Coefficient1`, `Coefficient2` |
| `ElementLoadPlanar` | `GeomPtsList` — see [Planar contour validity](#planar-contour-validity) | N/m² | `Mx`, `My`, `Mz` in N·m | `PlanarVariation`: `Coefficient1`, `Coefficient2`, `Coefficient3` |

All geometry points are in global axes and required. `LoadCase` (`EID`) is required. Forces and moments
default to `0`, variation coefficients to `1`. A coefficient is the factor of the load at the first, second
(and third) point. `ElementLoadPunctual` also has `ImpactingSurface` (`PunchingShape`: `Rectangular` (default),
`Square` or `Circular`; `PunchingA` `0.2` m and `PunchingB` `0.3` m) and `Punching` (see
[Section 8](#families)) for the punching check.

### `ElementImposedDisplacement`

An imposed displacement or rotation at a node: `GeomPt` (`Pt3D`, m, required), `LoadCase` (`EID`, required),
`Dx`, `Dy`, `Dz` (m) and `Rx`, `Ry`, `Rz` (rad, about the global axes), all `0` by default.

---

## 10. Data Models — Informational Elements (Load Cases & Families)

An informational element is an object of the model that has no geometry. There are six kinds. All of them are
created with `CreateInformationalElement` (see [Section 3.5](#35-model--informational-elements)), changed with
`UpdateInformationalElement`, and read back with `GetInformationalElementsObject`. To list the identifiers of one
kind, send a `QueryInfoModel` to `GetElementsID` ([Section 3.10](#310-model--query--retrieval)).

| Kind | Classes | Section |
|---|---|---|
| Load case families | `LoadCaseFamily_*` | [10.1](#101-load-case-families) |
| Load cases | `LoadCase_*` | [10.2](#102-load-cases) |
| Load combinations | `Combination` | [10.3](#103-combinations) |
| Structural systems | `StructuralSystem` | [10.4](#104-structural-systems) |
| Linear release templates | `LinearReleaseTemplate` | [10.5](#105-linear-release-templates) |
| Expert design templates | `DesignTemplate` and its subclasses | [10.6](#106-expert-design-templates) |

Every one of them derives from `InformationalElementBase`. The body of the calls above is polymorphic: the
`"$type"` property selects the class ([Section 5](#5-polymorphism--the-type-discriminator)).

#### `InformationalElementBase`

| Property | Type | Description |
|---|---|---|
| `$type` | `string` | Required. The class name, for example `LoadCaseFamily_DeadLoads` |
| `UserName` | `string?` | When it is not empty, it replaces `Name` on create and update. Read-only on a `Combination` |
| `UserID` | `int` | The number Advance Design shows for the element. Returned on read; not written |

Rules common to all informational elements:

- `CreateInformationalElement` returns the new `EID` in `Data.Value`. Keep it: load cases, combinations and
  assignments refer to it. When the creation fails, `Data.Value` is `0` and `Details` holds the engine error. Test
  both `Details.Success` and `Data.Value`.
- A **parameter block** is a nested object of an element, for example `BasePressure` or `Implantation`. A block
  that is `null` is not touched: it keeps the model default on create and the current value on update. A block
  that you send is applied **whole**: every property of the block is written, including those you left at their
  default.
- `UpdateInformationalElement(elementId, body)` takes a body of the same class as the element.
- Enumeration values are sent by name. A value the model cannot hold is not refused: it is applied as the value
  named in its description (for example `XY` as `X` in the seismic direction). The redirects are listed in
  [Section 12](#12-data-models--climatic-parameters).

### 10.1 Load Case Families

A load case family groups load cases of the same nature (all the dead loads, all the wind cases of one
standard). `LoadCaseFamily` is the base class; `LoadCaseFamily` and `LoadCase` themselves cannot be created.
There are 20 families, each with a matching load case class:

| Family (`$type`) | Load case (`$type`) | Used for |
|---|---|---|
| `LoadCaseFamily_DeadLoads` | `LoadCase_DeadLoads` | Permanent actions: self weight through a gravity field |
| `LoadCaseFamily_LiveLoads` | `LoadCase_LiveLoads` | Variable (imposed) loads, with a category |
| `LoadCaseFamily_Accidental` | `LoadCase_Accidental` | Accidental actions |
| `LoadCaseFamily_Thermic` | `LoadCase_Thermic` | Temperature loads |
| `LoadCaseFamily_Other` | `LoadCase_Other` | Any other user-defined action |
| `LoadCaseFamily_DynamicTemporal` | `LoadCase_DynamicTemporal` | Time-history (temporal) dynamic analysis |
| `LoadCaseFamily_PushOver` | `LoadCase_PushOver` | Push-over analysis |
| `LoadCaseFamily_AnalysisBuckling` | `LoadCase_Buckling` | Buckling analysis. Read-only: returned by `GetInformationalElementsObject`, cannot be created |
| `LoadCaseFamily_Wind` | `LoadCase_Wind` | Generic wind: no parameter blocks, the model defaults of the session standard |
| `LoadCaseFamily_Wind_1991_1_4` | `LoadCase_Wind_1991_1_4` | Wind, EN 1991-1-4 ([12.1](#121-wind-en-1991-1-4)) |
| `LoadCaseFamily_WindCBN` | `LoadCase_WindCBN` | Wind, Canadian code ([12.2](#122-wind-cbn)) |
| `LoadCaseFamily_WindIBC` | `LoadCase_WindIBC` | Wind, IBC / ASCE 7-22 ([12.3](#123-wind-ibc--asce-7-22)) |
| `LoadCaseFamily_Snow` | `LoadCase_Snow` | Generic snow: no parameter blocks, the model defaults of the session standard |
| `LoadCaseFamily_Snow_1991_1_3` | `LoadCase_Snow_1991_1_3` | Snow, EN 1991-1-3 / NF EC1 ([12.4](#124-snow-en-1991-1-3-nf-ec1)) |
| `LoadCaseFamily_SnowCBN` | `LoadCase_SnowCBN` | Snow, Canadian code ([12.5](#125-snow-cbn)) |
| `LoadCaseFamily_SnowIBC` | `LoadCase_SnowIBC` | Snow, IBC / ASCE 7-22 ([12.6](#126-snow-ibc--asce-7-22)) |
| `LoadCaseFamily_Seismic` | `LoadCase_Seismic` | Generic seismic: the response spectra are given as curves ([12.7](#127-seismic-en-1998-1)) |
| `LoadCaseFamily_SeismicEN_1998_1` | `LoadCase_SeismicEN_1998_1` | Seismic, EN 1998-1 ([12.7](#127-seismic-en-1998-1)) |
| `LoadCaseFamily_SeismicCBN` | `LoadCase_SeismicCBN` | Seismic, Canadian code ([12.8](#128-seismic-cbn)) |
| `LoadCaseFamily_SeismicIBC` | `LoadCase_SeismicIBC` | Seismic, IBC ([12.9](#129-seismic-ibc)) |

#### How a family and its load cases relate

A load case belongs to one family. The link is the `LoadCaseFamilyID` property of the load case, the `EID` of the
family. So: create the family first, keep its `EID`, then create the load cases with `LoadCaseFamilyID` set to it.

- `LoadCaseFamilyID` is required. The family and the load case must be of the same kind (for example
  `LoadCaseFamily_WindIBC` with `LoadCase_WindIBC`).
- An update never changes `LoadCaseFamilyID`: a load case cannot be moved to another family.

> **Warning:** A `LoadCaseFamilyID` that does not identify a load case family is not refused. Advance Design
> creates a new family with default values and puts the load case in it. The call succeeds and the model has an
> extra family. Always send the `EID` that `CreateInformationalElement` returned for the family.

> **Warning:** The wind, snow and seismic standard of a family is the one of the session (`DesignCodes.Climatic`
> and `DesignCodes.Seismic`, [Section 3.1.1](#311-the-environment-of-a-session)), not the one named by `$type`.
> `$type` only decides which parameter blocks are read from your request. Use the family type that matches the
> session standard ([table in Section 12](#the-family-type-follows-the-session-standard)); otherwise the values
> you send may not apply to the family that is created.

Wind and snow loads are generated from the parameters of the family:

1. Create the wind or snow family and fill its parameter blocks ([Section 12](#12-data-models--climatic-parameters)).
2. Create its load cases (one per wind direction or snow situation) with `EffectParameters.IsAutomatic = true`.
3. Call `ProcessAction` with `AD_API_ActionType.ClimaticAutoGeneration` ([Section 3.9](#39-model--actions)). It
   generates the loads of all the wind and snow families of the model and takes no arguments: send an empty list.

#### `LoadCaseFamily` (base class)

| Property | Type | Default | Description |
|---|---|---|---|
| `Name` | `string` | `""` | Required. Name of the family |
| `CombinationsCoefficients` | `LoadCaseFamilyCombinationsCoefficients?` | `null` | Gamma and psi coefficients of the family. Leave it unset |

> **Warning:** Leave `CombinationsCoefficients` unset (`null`) on a family and on its load cases. Advance Design
> then fills the coefficients from the type of the family. A block that you send **replaces every coefficient** of
> the family or load case, including the ones you did not mean to change, because every property of the block
> has a value (most default to `1`). Send the block only to override a coefficient on purpose, and then send the
> complete block. The coefficients are described in [Section 10.3](#103-combinations).

#### Families with their own properties

- `Accidental`, `Other`, `Thermic`, `DynamicTemporal` and `AnalysisBuckling` families have only the base
  properties. The wind, snow and seismic families are in [Section 12](#12-data-models--climatic-parameters).
- `LoadCaseFamily_LiveLoads.LiveLoadCategory` (`int?`, default `0`) is the category index of the live load.
- `LoadCaseFamily_DeadLoads.DeadLoadsCoefficients` (`LoadCaseFamilyDeadLoadsCoefficients?`) holds the partial
  safety factors of the self weight, dimensionless: `GammaFavSTRGEO` (1.0), `GammaDefavSTRGEO` (1.35),
  `GammaFavEQU` (0.9), `GammaDefavEQU` (1.1). `null` keeps the engine defaults of the standard and annex.
- `LoadCaseFamily_PushOver.LoadType1` and `LoadType2` (`PushOverLoadDistribution?`) configure the two load types:
  `State`, `Control`, `PointOfApplication`, `Distribution` and `Directions` (`XPlus`, `YPlus`, `XMinus`,
  `YMinus`). The full list is in `swagger.json`.

#### Example: a dead-load family and its load case

```csharp
// 1. The family
long familyId = client.CreateInformationalElement(
    new LoadCaseFamily_DeadLoads { Name = "Permanent actions" }).Data.Value;

// 2. The load case, linked to the family. Z = -1: the self weight acts downwards.
EIDApiResponse selfWeight = client.CreateInformationalElement(new LoadCase_DeadLoads
{
    Name             = "Self weight",
    LoadCaseFamilyID = new EID { Value = familyId },
    Field            = new GravityFieldDefinition { X = 0, Y = 0, Z = -1 }
});
```

The family body is `{ "$type": "LoadCaseFamily_DeadLoads", "name": "Permanent actions" }`. The load case body
(`1234` is the `Value` returned for the family):

```json
{
  "$type": "LoadCase_DeadLoads",
  "name": "Self weight",
  "loadCaseFamilyID": { "value": 1234 },
  "field": { "x": 0, "y": 0, "z": -1 }
}
```

### 10.2 Load Cases

A load case is one loading of a family. `LoadCase` is the base class; create one of the concrete classes of the
table in [Section 10.1](#101-load-case-families), after its family.

#### `LoadCase` (base class)

| Property | Type | Default | Description |
|---|---|---|---|
| `Name` | `string` | `""` | Required. Name of the load case |
| `LoadCaseFamilyID` | `EID` | — | Required. The `EID` of its family ([rules](#how-a-family-and-its-load-cases-relate)) |
| `CombinationsCoefficients` | `LoadCaseCombinationsCoefficients?` | `null` | Gamma and psi coefficients. Leave it unset: see the warning in [10.1](#101-load-case-families) and [Section 10.3](#103-combinations) |

Wind, snow and seismic load cases have their own properties, described with their family in
[Section 12](#12-data-models--climatic-parameters). `LoadCase_Accidental` and `LoadCase_Other` have only the base
properties.

#### `LoadCase_DeadLoads`

- `Field` (`GravityFieldDefinition?`) is the direction of the self weight: three **dimensionless** coefficients
  `X`, `Y`, `Z` that multiply the gravity vector. The default is (0, 0, -1): `-1` along Z is the full self
  weight, downwards. Do not send `-9.81`. `null` keeps the default.
- `DeadLoadsCoefficients` (`LoadCaseDeadLoadsCoefficients?`) holds the partial safety factors of the load case:
  `GammaFavSTRGEO` (1.0), `GammaDefavSTRGEO` (1.35), `GammaFavEQU` (1.0), `GammaDefavEQU` (1.1),
  `GravityCategory` (0 = standard), `CoeffMax`, `CoeffMed`, `CoeffMin` (1.0, for the American and other
  combination standards), and `IsWater` and `ReductionFactor` (0.85), which belong to the 2nd generation
  Eurocodes. `null` keeps the engine defaults. The `Coeff2ndGen...` properties of the block also belong to the
  2nd generation Eurocodes, which are not released; the full list is in `swagger.json`.

#### Other load case types

- **`LoadCase_LiveLoads`**: `LiveLoadCategory` (`int?`, default `0`), as on the family.
- **`LoadCase_Thermic`**: `Field` (`TemperatureFieldDefinition?`: `DilatationX` in °C, `GradientY` and
  `GradientZ` in °C/m, all `0` by default), `ThermicCoefficients` (`ThermicCategory`: `0` = standard T, `1` =
  thermal dead load Td; and `CoeffMax`, `CoeffMed`, `CoeffMin`) and `SystemSelection` (`Selection`: `0` = all
  systems, `1` = the systems named in `List`, comma-separated, `2` = none). `null` keeps the defaults; a
  `null` `SystemSelection` applies the load case to all systems.
- **`LoadCase_DynamicTemporal`**: time-history loading. `VariationProperties.KindOfVariation` chooses the
  variation (`0` non-harmonic, `1` harmonic, `2` accelerogram, with `AccelerogramDirection`
  `0` = X, `1` = Y, `2` = Z and `AccelerogramScaleFactor`). `TimeIntervalProperties` gives `Begin`, `End` and
  `Interval` in s. `Function` (`Name`, `Points` as `ValueX` / `ValueFX`) and `Harmoniques` (`HIntensite`,
  `HPulsation` in rad/s, `HPhase` in rad) define the time function. `null` keeps the engine defaults. The full
  list is in `swagger.json`.
- **`LoadCase_PushOver`**: push-over loading, in six blocks: `BaseProperties`, `DirectionConfig` (`Direction`
  `0` = X+, `1` = Y+, `2` = X-, `3` = Y-), `PositionConfig` (eccentricity), `LoadPattern` (`LoadDistribution`
  `0` = concentrated, `1` = uniform, `2` = triangular, `3` = code, `4` = mode shape), `Incrementation` and
  `TargetDisplacement`. Not SI: in `Incrementation`, `ImposedForceValue` and `ImposedFirstStepValue` are in
  **kN**; percentages are in %. `TargetDisplacement.MaxDisplacement` is in m. `null` keeps the defaults. The full
  list is in `swagger.json`.
- **`LoadCase_Buckling`**: read-only, like its family. It returns `InnerLoadCaseId` (the analysis), `NoModes`
  (default 1), `MaxLambda`, `Tolerance` (1e-6) and `MaxIterations` (100).

---

### 10.3 Combinations

How to call: [Section 3.6](#36-model--combinations).

#### `Combination`

Body of `AddCombination`; returned by `GetInformationalElementsObject`. It derives from `InformationalElementBase`
(`$type` = `Combination`).

| Property | Type | Required | Description |
|---|---|---|---|
| `IdComb` | `int` | — | Number ("No.") of the combination. Read-only |
| `UserName` | `string` | — | Name of the combination. Read-only: Advance Design generates it |
| `ECombinationType` | `eCombinationType` | Yes | Project situation of the combination (below) |
| `ListCasesCoeffs` | `ICollection<EIDDoublePair>` | Yes | Load cases with their coefficients. `Key` is the EID of the load case, `Value` the coefficient. At least one pair; `0` is refused |
| `DesignCalculations` | `ICollection<eCombinationDesignCalculation>` | — | The design (Expert) calculations that verify this combination. See [3.6.1](#361-design-expert-calculations-of-a-combination) for the create / update rules |
| `ConcreteDesign` | `ConcreteCombinationDesign` | — | Coefficients of the reinforced concrete engine |
| `SteelDesign` | `SteelCombinationDesign` | — | Coefficients of the steelwork engine |
| `TimberDesign` | `TimberCombinationDesign` | — | Coefficients of the timber engine |
| `CompositeDesign` | `CompositeCombinationDesign` | — | Coefficients of the composite beam engine |

`eCombinationType` values: `eComboProjectSituationEluStrgeo` (ULS STR/GEO), `eComboProjectSituationEluEqu`
(ULS EQU), `eComboProjectSituationElsCharacteristic`, `eComboProjectSituationElsFrecvent` (SLS frequent),
`eComboProjectSituationElsQuasiPermanent`, `eComboProjectSituationELUA` (ULS accidental),
`eComboProjectSituationElsA` (SLS accidental) and `eComboProjectSituationNone` (refused when you send it).

`eCombinationDesignCalculation` values, and the group that holds the coefficients of each:

| Value | Group |
|---|---|
| `ConcreteDesign` | `ConcreteDesign` |
| `SteelProfiles` | `SteelDesign.Profiles` |
| `SteelDeflections` | `SteelDesign.Deflections` |
| `SteelConnections` | `SteelDesign.Connections` |
| `SteelFire` (EC3 engine only) | `SteelDesign.Fire` |
| `TimberProfiles` | `TimberDesign.Profiles` |
| `TimberDeflections` | `TimberDesign.Deflections` |
| `TimberFire` | `TimberDesign.Fire` |
| `PreCompositeProfiles` | `CompositeDesign.PreCompositeProfiles` |
| `PostCompositeProfiles` | `CompositeDesign.PostCompositeProfiles` |
| `PreCompositeDeflections` | `CompositeDesign.PreCompositeDeflections` |
| `PostCompositeDeflections` | `CompositeDesign.PostCompositeDeflections` |

A group is used only when its design calculation is listed in `DesignCalculations`. Every property of every group
is optional: a property left `null` keeps the stored value, or takes the default given below.

#### `ConcreteCombinationDesign`

| Property | Type | Description |
|---|---|---|
| `Type` | `eConcreteCombinationType?` | Limit state: `Uls`, `UlsAccidental`, `Sls` or `None` (not checked). Default: from the project situation — ULS gives `Uls`, SLS gives `Sls`, accidental gives `UlsAccidental` |
| `Duration` | `eConcreteLoadDuration?` | Load duration: `ShortTerm`, `MediumTerm`, `LongTerm` or `None`. Default: `ShortTerm` for an accidental combination, `LongTerm` otherwise |

#### `SteelCombinationDesign`

| Property | Type | Used by |
|---|---|---|
| `Profiles` | `CombinationProfileVerification` | `SteelProfiles` |
| `Deflections` | `CombinationDeflectionVerification` | `SteelDeflections` |
| `Connections` | `CombinationConnectionVerification` | `SteelConnections` |
| `Fire` | `CombinationFireVerification` | `SteelFire` |

#### `TimberCombinationDesign`

| Property | Type | Used by |
|---|---|---|
| `Deflections` | `CombinationTimberDeflectionVerification` | `TimberDeflections` |
| `Profiles` | `CombinationTimberVerification` | `TimberProfiles` |
| `Fire` | `CombinationTimberVerification` | `TimberFire` |

The three groups are written independently. When you read a combination, a timber group is returned only if its
design calculation is listed in `DesignCalculations`.

#### `CompositeCombinationDesign`

A composite beam is verified twice: at the construction stage, when the steel section acts alone
("pre-composite"), and at the final stage, when the composite section acts ("post-composite").

| Property | Type | Used by |
|---|---|---|
| `PreCompositeProfiles` | `CombinationProfileVerification` | `PreCompositeProfiles` |
| `PostCompositeProfiles` | `CombinationProfileVerification` | `PostCompositeProfiles` |
| `PreCompositeDeflections` | `CombinationDeflectionVerification` | `PreCompositeDeflections` |
| `PostCompositeDeflections` | `CombinationDeflectionVerification` | `PostCompositeDeflections` |

#### Verification groups

Steel and composite share the profile and deflection groups.

`CombinationProfileVerification` — strength and stability of a member:

| Property | Type | Description |
|---|---|---|
| `State` | `eDesignVerificationState?` | `Active` (default) or `Inactive`. An inactive verification is skipped for this combination |
| `ResistanceSafetyFactor` | `double?` | γM0. Default: γM0 of the steel design settings of the model, or of the composite settings |
| `StabilitySafetyFactor` | `double?` | γM1. Default: γM1 of the steel design settings of the model, or of the composite settings |

`CombinationDeflectionVerification` — serviceability deflection:

| Property | Type | Description |
|---|---|---|
| `Criterion` | `eDeflectionCriterion?` | `FirstCriterion` (default), `SecondCriterion`, `BothCriteria` or `None`. `BothCriteria` is available with Eurocode 3 only; with any other steel design code it is read as "no criterion" |
| `SafetyFactor` | `double?` | Default: the deflection safety factor of the steel design settings of the model, or γM2 of the composite settings |

`CombinationConnectionVerification` — steel connections:

| Property | Type | Description |
|---|---|---|
| `State` | `eDesignVerificationState?` | `Active` (default) or `Inactive` |
| `SafetyFactor` | `double?` | Default: the connection safety factor of the steel design settings of the model |

`CombinationFireVerification` — steel fire resistance:

| Property | Type | Description |
|---|---|---|
| `State` | `eDesignVerificationState?` | `Active` (default) or `Inactive` |
| `SafetyFactor` | `double?` | γMfi. Default: the fire safety factor of the steel design settings of the model |
| `LoadLevel` | `double?` | Loading level of the fire situation. Default `1.0` |

`CombinationTimberVerification` (strength, fire) and `CombinationTimberDeflectionVerification` (deflection):

| Property | Type | Description |
|---|---|---|
| `Type` | `eTimberCombinationType?` | Limit state: `Uls` (default), `Sls` or `UlsAccidental` |
| `Duration` | `eTimberLoadDuration?` | Load-duration class, which drives kmod and kdef: `Permanent` (default), `LongTerm`, `MediumTerm`, `ShortTerm` or `Instantaneous` |
| `Criterion` | `eTimberDeflectionCriterion?` | Deflection group only: `FirstCriterion` (default), `SecondCriterion` or `None` |

---

#### `CombinationsGenerationRequest`

Body of `GenerateCombinations` ([3.6.2](#362-automatic-generation-of-combinations)). It chooses the work-flow; the
standard and its options come from the model.

| Property | Type | Required | Description |
|---|---|---|---|
| `Mode` | `eCombinationsGenerationMode` | — | `Simplified` (default) or `DetailedMatrix` (below) |
| `UpdateLoadCaseCodes` | `bool?` | — | Refreshes the normative code of every load case from the combination template before generating. Unset means `true`. Leave it unset for a model built through the API; with `false` nothing is generated on such a model. Send `false` only for a project file whose codes were set on purpose |
| `ApplyDefaultConcomitanceRules` | `bool` | — | `DetailedMatrix` only. `true` resets the concomitance matrix to the default rules before generating. Default `false`: the rules stored in the model are used |

| `Mode` | Work-flow | Desktop button |
|---|---|---|
| `Simplified` | Template based: the combination template of the standard of the session, with the coefficients of the load cases and the generation settings of the model | *Simplified Comb.* |
| `DetailedMatrix` | Built from the concomitance rules stored in the model | *Detailed Comb.* |

#### `CombinationsGenerationResult`

| Property | Type | Description |
|---|---|---|
| `CreatedCombinations` | `ICollection<EID>` | EIDs of the created combinations, in generation order. Read them with `GetInformationalElementsObject`. Empty when nothing was generated; the reason is in `Details.Diagnostics` |
| `FirstIdentifier` | `int` | Number ("No.") of the first generated combination; `0` when nothing was generated. Same value as `Combination.IdComb` |
| `LastIdentifier` | `int` | Number of the last generated combination; `0` when nothing was generated |
| `LimitExceeded` | `bool` | `true` when the generation produced more combinations than the maximum set in the model. The set was cut to the limit: `CreatedCombinations` holds what was kept, the others do not exist |

---

#### `CombinationsGenerationSettings` and its shapes

The generation settings are the group `CombinationsGeneration` of the general model settings
([3.6.3](#363-settings-of-the-automatic-generation), [Section 3.7](#37-model--general-settings)). The group has five
shapes, one per kind of standard. They derive from `CombinationsGenerationSettings`, which derives from
`ModelSettingsBase`; `$type` is the name of the shape. Every property is optional (`null` means "leave unchanged").

`CombinationsGenerationSettings` — in every shape:

| Property | Type | Description |
|---|---|---|
| `Standard` | `eCombinationsStandard` | The combination standard of the session. Read-only. Values: `Eurocode`, `US`, `Canada`, `EurocodeItalia`, `CanadaNBC2015`, `NTC2018`, `Greenhouse_EN13031_1`, `US2018`, `CanadaCNB2020` |
| `Annex` | `eCombinationsEurocodeAnnex?` | National annex of the Eurocode standard. Read-only. `null` for standards without an annex (US, Canada, Italian). Values: `General`, `France`, `UK`, `Germany`, `Czech`, `Romania`, `Slovakia`, `Poland`, `Spain`, `Portugal` |

##### `EurocodeCombinationsGenerationSettings`

Standards `Eurocode` (any annex), `EurocodeItalia` and `NTC2018`.

| Property | Type | Description |
|---|---|---|
| `ProjectSituations` | `EurocodeProjectSituations` | Which project situations are generated |
| `PermanentActions` | `EurocodePermanentActionSettings` | Permanent actions |
| `LiveLoads` | `EurocodeLiveLoadSettings` | Live loads (exploitation) |
| `SnowLoads` | `EurocodeSnowLoadSettings` | Snow |
| `WindLoads` | `EurocodeWindLoadSettings` | Wind |
| `TemperatureLoads` | `EurocodeTemperatureLoadSettings` | Temperature |

`EurocodeProjectSituations` — at least one of the six switches must stay on:

| Property | Type | Description |
|---|---|---|
| `UlsEquilibrium` | `bool?` | ULS EQU. Off by default |
| `UlsStructuralGeotechnical` | `bool?` | ULS STR/GEO. On by default |
| `UlsGeotechnical` | `bool?` | ULS GEO as a situation of its own. Italian standards only |
| `SlsCharacteristic` | `bool?` | SLS characteristic. On by default |
| `SlsFrequent` | `bool?` | SLS frequent. Off by default |
| `SlsQuasiPermanent` | `bool?` | SLS quasi-permanent. On by default |
| `SlsSituation` | `eSlsCombinationSituation?` | Situation the generated SLS combinations are labelled with: `Characteristic`, `Frequent` or `QuasiPermanent`. It does not choose which ones are generated; the three switches above do |
| `SlsCharacteristicIncludesPermanent` | `bool?` | Include the permanent actions in the SLS characteristic combinations. UK annex only |
| `Equation` | `eEurocodeCombinationEquation?` | ULS equation: `Equation_6_10` or `Equation_6_10a_6_10b`. General, Poland, Czech and Slovakia annexes only |

`EurocodePermanentActionSettings` — only the unfavourable γ values are settings; the favourable ones come from the
standard.

| Property | Type | Description |
|---|---|---|
| `Situation` | `ePermanentLoadSituation?` | `Unfavorable` (default), `Favorable`, `FavorableAndUnfavorable` or `UserDefined`. Any value but `UserDefined` makes the three γ read-only and recomputed from the standard |
| `GammaEqu` | `double?` | γG for EQU. Writable only with `UserDefined` |
| `GammaStrGeo` | `double?` | γG for STR/GEO. Writable only with `UserDefined` |
| `GammaGeo` | `double?` | γG for GEO. Writable only with `UserDefined`; read by the generation under the Italian standards only |

`EurocodeLiveLoadSettings`, `EurocodeSnowLoadSettings`, `EurocodeWindLoadSettings` and
`EurocodeTemperatureLoadSettings` share these properties (`EurocodeVariableActionSettings`):

| Property | Type | Description |
|---|---|---|
| `GenerateAsLeadingAction` | `bool?` | Generate combinations in which this family is the leading variable action. At least one of the four families must stay on |
| `GammaEqu`, `GammaStrGeo`, `GammaGeo` | `double?` | γQ for EQU, STR/GEO and GEO. Writable only for the user defined category. GEO is read under the Italian standards only |
| `Psi0`, `Psi1`, `Psi2` | `double?` | Combination, frequent and quasi-permanent values. Writable only for the user defined category |
| `AvailableCategoryCount` | `int?` | Read-only. Number of categories of this family under the active standard and annex |
| `UserDefinedCategory` | `int?` | Read-only. Index of the *user defined* category; equal to `AvailableCategoryCount` |

In a raw JSON body each of these four objects needs its own `"$type"` (for example `"$type": "EurocodeLiveLoadSettings"`),
not only the top-level object. Each family adds the property that holds its category. The category fixes ψ0 / ψ1 / ψ2 unless it is *user defined*.

| Class | Category property | Values |
|---|---|---|
| `EurocodeLiveLoadSettings` | `Category` (`eExploitationCategory?`) | `CategoryA` to `CategoryJ`, `UserDefined`. Under the French and Czech annexes only the leading categories that keep their meaning are accepted (see [3.6.3](#363-settings-of-the-automatic-generation)) |
| `EurocodeLiveLoadSettings` | `CategoryIndex` (`int?`) | The category as the index the model stores, from `0` to `UserDefinedCategory`. Wins over `Category`. Always filled on read |
| `EurocodeSnowLoadSettings` | `Category` (`SnowLoadCategory_API?`) | `eSnowZoneNord`, `eSnowZoneOver1000`, `eSnowZoneUnder1000`, `eSnowZoneUserDefined`. Under the Slovakian annex the three zones read as below 1000 m, 1000 to 1500 m and above 1500 m |
| `EurocodeWindLoadSettings` | `Category` (`eWindLoadCategory?`) | `Standard` or `UserDefined` |
| `EurocodeTemperatureLoadSettings` | `Category` (`eThermalLoadCategory?`) | `Standard` or `UserDefined` |

##### `USCombinationsGenerationSettings`

Standards `US` and `US2018` (ASCE 7 combinations).

| Property | Type | Description |
|---|---|---|
| `GenerateUls` | `bool?` | Generate the strength (ULS) combinations. On by default |
| `GenerateSls` | `bool?` | Generate the serviceability (SLS) combinations. On by default |
| `LiveLoadFactorF1` | `eUSLiveLoadFactorF1?` | f1 on the live load in the accidental (fire) combinations: `PublicAssembly` (1.0) or `Others` (0.5) |
| `LiveLoadFactorF1Value` | `double?` | Read-only. The f1 in use |
| `SnowLoadFactorF2` | `eUSSnowLoadFactorF2?` | f2 on the snow load in the accidental combinations: `RoofNotShedding` (0.7) or `RoofOthers` (0.2) |
| `SnowLoadFactorF2Value` | `double?` | Read-only. The f2 in use |
| `AiscDesignMethod` | `eAiscDesignMethod?` | Read-only. `AllowableStrengthDesign` or `LoadAndResistanceFactorDesign`. Chosen with `Environments.DesignCodes.AiscMethod` |

##### `CanadaCombinationsGenerationSettings`

Standards `Canada`, `CanadaNBC2015` and `CanadaCNB2020`.

| Property | Type | Description |
|---|---|---|
| `GenerateSteelShortTermDeflections` | `bool?` | Generate the additional short-term deflection combinations for steel. On by default |
| `ImportanceCategory` | `eCanadaImportanceCategory?` | `Low`, `Normal`, `High` or `PostDisaster`. Fixes the snow and wind importance factors Is and Iw of the generated combinations |

##### `GreenhouseCombinationsGenerationSettings`

Standard `Greenhouse_EN13031_1` (PR EN 13031-1). No localization selects it: ask for it with
`Environments.DesignCodes.Combinations = Greenhouse_EN13031_1`, which only an installation with the greenhouse
option accepts. On any other installation these settings can be written and read, but no generation uses them.

| Property | Type | Description |
|---|---|---|
| `GenerateUls` | `bool?` | Generate the ULS combinations. On by default |
| `GenerateSls` | `bool?` | Generate the SLS combinations. On by default |
| `ConsequenceClass` | `eGreenhouseConsequenceClass?` | `C0`, `C1`, `C2` or `Other`. Any value but `Other` fixes the two factors below (K / accidental K: C0 0.80 / 0.60, C1 0.90 / 0.80, C2 1.00 / 1.00) |
| `ConsequenceFactorK` | `double?` | Consequence factor K of the ULS combinations (seismic excepted). Writable only with `Other` |
| `AccidentalConsequenceFactorK` | `double?` | Consequence factor K of the accidental combinations. Writable only with `Other` |

##### `Eurocode2ndGenCombinationsGenerationSettings`

Settings of the 2nd generation Eurocode (EN 1990:2023). The class is published, but the standard is not released and
an ordinary installation refuses it ([3.6.3](#363-settings-of-the-automatic-generation)). Its properties are not
described here.

---

### 10.4 Structural Systems

A **system** groups structural elements. It can also represent a building level (storey). The model is a tree of
systems. Its root is the system displayed as **"Structure 0"**, which exists in every model.

A system is an informational element with `"$type": "StructuralSystem"`. You manage it with the generic calls:

| Operation | Call |
|---|---|
| Create | `CreateInformationalElement` ([Section 3.5](#35-model--informational-elements)) |
| Update | `UpdateInformationalElement` ([Section 3.5](#35-model--informational-elements)) |
| Read | `GetInformationalElementsObject` ([Section 3.10](#310-model--query--retrieval)) |
| List | `GetElementsID` with `QueryInfoModel { InformationalElementType = StructuralSystem }` ([Section 3.10](#310-model--query--retrieval)) |
| Delete | `DeleteElements` ([Section 3.4](#34-model--elements-geometric)) |

The elements that belong to a system carry its EID in their `SystemIDs` property — see
[Section 7](#7-data-models--elements-geometric).

#### `StructuralSystem`

| Property | Type | Unit | Default | Description |
|---|---|---|---|---|
| `UserName` | `string` | | | System name. Inherited from `InformationalElementBase` |
| `UserID` | `int` | | | System number shown in the interface. Assigned by Advance Design, read-only. The root system has `0` |
| `ParentID` | `EID` | | `0` | System this one is nested under. `0` = directly under the root. **Used on create only** |
| `BehaviourCoefficientX/Y/Z` | `double` | | `1` | Seismic behaviour coefficients. Must be greater than `0` |
| `ImposedDirectionX/Y/Z` | `bool` | | `false` | The behaviour coefficient of that direction is imposed |
| `IsLevel` | `bool` | | `false` | The system is a building level. `LevelTop`, `LevelBottom` and `LevelNumber` apply only then |
| `LevelTop` | `double` | m | `0` | Top elevation of the level |
| `LevelBottom` | `double` | m | `0` | Bottom elevation of the level |
| `LevelNumber` | `int` | | `1` | Level number |
| `Color` | `int` | | `0` | Display colour as a packed RGB integer |
| `IsWallGroup` | `bool` | | `false` | The system is a wall group |
| `SubSystemIDs` | `ICollection<EID>` | | | **Read-only.** The direct sub-systems, filled when you read the system. Ignored on create and update |

Rules:

**Create**

- Set `UserName`. Set `ParentID` to build a sub-system; an id that does not exist or is not a system is refused.
- A new system takes the values you send, defaults included. It does not inherit the colour, the level settings
  or the behaviour coefficients of its parent.
- To build a hierarchy, create the parent first and pass its EID as `ParentID`. `SubSystemIDs` cannot build it.

**Create and update**

- A request is refused when a behaviour coefficient is not greater than `0`, or when `IsLevel` is `true` and
  `LevelTop` is lower than `LevelBottom`. The model stays unchanged.

**Update**

- Every property in the table is compared with the model and the differences are written. A property left at
  its default (`Color = 0`, `LevelNumber = 1`, ...) is therefore a change. Read the system, change what you
  need and send it back.
- `ParentID` and `UserID` are ignored. A system cannot be moved. An empty `UserName` keeps the current name.
- Changing the behaviour coefficients of a system changes those of its sub-systems.
- **Root system ("Structure 0")**: only the behaviour coefficients and `IsWallGroup` can change. Any other
  difference (name, level settings, colour, imposed directions) is refused. Every other system, a top-level
  one included, can be edited freely.

**Delete**

- The root system cannot be deleted.
- Deleting a system deletes its whole sub-tree of sub-systems. The elements inside are **not** deleted. They
  lose that membership, and the ones left without any system move to the root system, with a warning.
- Deleting a system also saves the project — see [Section 3.1](#31-project-management).

**Read and list**

- `ParentID` is `0` for a system directly under the root.
- The list holds the root, every sub-system at any depth, and the empty systems. Design groups are not listed.

```csharp
// Level "Floor 1" under the root, with a sub-system "Zone A"
EIDApiResponse level = client.CreateInformationalElement(new StructuralSystem
{
    UserName    = "Floor 1",
    IsLevel     = true,
    LevelTop    = 6.0,
    LevelBottom = 3.0,
    LevelNumber = 2
});

EIDApiResponse zone = client.CreateInformationalElement(new StructuralSystem
{
    UserName = "Zone A",
    ParentID = level.Data
});

// Every system of the model
Int64ListApiResponse ids = client.GetElementsID(new List<QueryBase>
{
    new QueryInfoModel { InformationalElementType = InformationalElementTypeEnum.StructuralSystem }
});
```

```json
{
  "$type": "StructuralSystem",
  "userName": "Zone A",
  "parentID": { "value": 12 },
  "behaviourCoefficientX": 1.5,
  "behaviourCoefficientY": 1.5,
  "behaviourCoefficientZ": 1.0
}
```

---

### 10.5 Linear Release Templates

A **linear release template** describes how an edge of a planar element is connected to the rest of the structure.
All templates live in one pool for the whole model. The edges of any number of planar elements refer to them
through `ElementPlanar.SelectedReleaseName` and `ElementPlanar.LinearReleases` (see
[Section 7](#7-data-models--elements-geometric)). Editing a template therefore changes every edge that uses it.

A template is an informational element with `"$type": "LinearReleaseTemplate"`. You manage it with
`CreateInformationalElement`, `UpdateInformationalElement`, `GetInformationalElementsObject`, `DeleteElements`
and `GetElementsID` with `QueryInfoModel { InformationalElementType = LinearReleaseTemplate }`, as listed in
[Section 10.4](#104-structural-systems). The type is available for the descriptive model only.

The name of a template is `LinearReleaseName`. `InformationalElementBase.UserName` is not used.

#### `LinearReleaseTemplate`

| Property | Type | Unit | Default | Description |
|---|---|---|---|---|
| `LinearReleaseName` | `string` | | | **Required** on create and on update. Unique in the model, at most 50 characters. `SelectedReleaseName` of a planar element refers to it |
| `LinearReleaseType` | `LinearReleaseType` | | | **Read-only.** `default_release` for the three built-in templates, `user_defined_release` for every other one |
| `Tx` | `bool` | | `false` | Translation along the local x axis of the edge. `true` = released, `false` = fixed |
| `StiffnessTx` | `double` | N/m per m of edge (N/m²) | `0` | Elastic stiffness of the `Tx` release. `0` = perfectly free |
| `TxNonlinearityType` | `LinearReleaseNonlinearityType` | | `none` | Non-linear behaviour of the `Tx` release |
| `Ty`, `StiffnessTy`, `TyNonlinearityType` | | | | Same, local y axis |
| `Tz`, `StiffnessTz`, `TzNonlinearityType` | | | | Same, local z axis |
| `Rx` | `bool` | | `false` | Rotation about the axis of the edge. `true` = released, `false` = fixed |
| `StiffnessRx` | `double` | N·m/rad per m of edge | `0` | Elastic stiffness of the `Rx` release. `0` = perfectly free |
| `RxNonlinearityType` | `LinearReleaseNonlinearityType` | | `none` | Non-linear behaviour of the `Rx` release |

`LinearReleaseNonlinearityType` is `none`, `compression_only` or `tension_only`.

A linear release has four degrees of freedom: `Tx`, `Ty`, `Tz` and `Rx`. There is no `Ry` or `Rz`.

> **Warning:** The stiffness values are in SI units, like the rest of the API: send `1.5e7` for
> 15 000 kN/m per metre of edge. The description of these properties in `swagger.json` says kN/m/m and
> kNm/rad/m; that is only the unit the desktop dialog displays. A wrong factor of 1000 gives a wrong model
> without any message.

#### Built-in templates

Every model has three built-in templates, in this order at the head of the pool:

| # | Template | `Tx` | `Ty` | `Tz` | `Rx` | Use |
|---|---|---|---|---|---|---|
| 1 | "Free" | released | released | released | released | Edge fully disconnected |
| 2 | "Pinned" | fixed | fixed | fixed | released | Hinged edge |
| 3 | "Fixed" | fixed | fixed | fixed | fixed | Default of a new planar element |

- The names are localized. Do not hard-code them. List the pool and recognise the built-in templates by
  `LinearReleaseType = default_release` and by their released degrees of freedom.
- The built-in templates cannot be modified or deleted.

Rules:

**Create**

- `LinearReleaseName` must not exist yet. A duplicate name fails; it is never turned into an update.
- A template you create is always `user_defined_release`.

**Update**

- The body replaces all the release settings of the template. Read the template first and send back what you keep.
- `LinearReleaseName` is required. A name that another template already uses is refused. A built-in template is
  refused.
- Renaming a template also changes `SelectedReleaseName` on every planar element that used the old name.

**Delete**

- Deleting a template that is in use succeeds. Only the edges that used it fall back to the built-in "Fixed"
  template; the other edges keep their own template. A warning gives the number of elements affected, and
  `SelectedReleaseName` of those elements is set again from their edges.
- Deleting a built-in template is refused. In a batch that mixes a built-in template with user templates, the
  built-in one is refused, the others are deleted, and the call returns `false`.

**List**

- The pool is returned in order: "Free", "Pinned", "Fixed", then the user templates. Identify a template by its
  EID or by its name, never by its position: deleting a template moves the ones after it.

```csharp
// List the pool and find the "Pinned" built-in template by its degrees of freedom
var poolIds = client.GetElementsID(new List<QueryBase>
{
    new QueryInfoModel { InformationalElementType = InformationalElementTypeEnum.LinearReleaseTemplate }
}).Data;

var pool = client.GetInformationalElementsObject(poolIds.ToList())
                 .Data.OfType<LinearReleaseTemplate>().ToList();
var pinned = pool.First(r => r.LinearReleaseType == LinearReleaseType.Default_release
                             && !r.Tx && !r.Ty && !r.Tz && r.Rx);

// A user template: hinged edge, vertical elastic spring that works in compression only (1.5e7 N/m per m)
EIDApiResponse created = client.CreateInformationalElement(new LinearReleaseTemplate
{
    LinearReleaseName  = "Elastic bearing",
    Tz                 = true,
    StiffnessTz        = 1.5e7,
    TzNonlinearityType = LinearReleaseNonlinearityType.Compression_only,
    Rx                 = true
});
```

```json
{
  "$type": "LinearReleaseTemplate",
  "linearReleaseName": "Elastic bearing",
  "tz": true,
  "stiffnessTz": 15000000,
  "tzNonlinearityType": "compression_only",
  "rx": true
}
```

To apply a template to the edges of a planar element, see `SelectedReleaseName` and `LinearReleases` in
[Section 7](#7-data-models--elements-geometric).

---

### 10.6 Expert Design Templates

A **design template** is a named set of design (Expert) properties, defined once for the model. It is the
"Design template description" dialog of Advance Design. Structural elements refer to a template. Assigning a
template, or editing it afterwards, copies its properties onto the design properties of every element that
refers to it.

The API exposes the templates of **steel linear elements** (beams, columns, bars).

#### How a template reaches an element

- Set `ElementLinear.DesignTemplateId` when you create or update the element
  ([Section 7](#7-data-models--elements-geometric)).
- Or call `AssignDesignTemplateToElements` to assign one template to many elements
  ([Section 3.4](#34-model--elements-geometric)).

The assignment rules are listed [below](#assignment).

A template is an informational element. You manage it with `CreateInformationalElement`,
`UpdateInformationalElement`, `GetInformationalElementsObject` and `DeleteElements`, as listed in
[Section 10.4](#104-structural-systems). To list the templates, call `GetElementsID` with
`QueryInfoModel { InformationalElementType = DesignTemplate }`.

#### Which `$type` to send

The steel design code is a setting of the whole model, not of a template. A new template always carries the
property set of the code the model is configured with. Send the class that matches that code:

| Steel design code of the model | `$type` to send | Type of `SectionClass` |
|---|---|---|
| `eurocode`, `ntc2008`, `ntc2018` | `EC3SteelLinearDesignTemplate` | `SteelSectionClass` |
| `aisc`, `aisc_2016` | `AISCSteelLinearDesignTemplate` | `SteelSectionClassAISC` |
| `can_csa_s16_2009`, `can_csa_s16_2014`, `can_csa_s16_2019`, `can_csa_s16_2024` | `CSASteelLinearDesignTemplate` | `SteelSectionClass` |

- A class that does not match the code of the model is refused, on create and on update. The message names the
  class the model accepts.
- `DesignTemplate`, `LinearDesignTemplate`, `SteelLinearDesignTemplate` and `NorthAmericanSteelLinearDesignTemplate`
  are base classes. They cannot be created or updated.
- The 2nd generation Eurocodes are not released; a model configured with one is refused.

#### Header properties

| Property | Type | Description |
|---|---|---|
| `UserName` | `string` | Template name. **Required** on create. At most 50 characters after the spaces at both ends are removed. Unique among the steel linear templates. The name of the built-in default template ("None") is refused. On update, an empty `UserName` keeps the current name |
| `MaterialType` | `DesignTemplateMaterialType` | **Read-only.** `steel` |
| `ElementType` | `DesignTemplateElementType` | **Read-only.** `linear` |
| `AssignmentNumber` | `int` | **Read-only.** Number of elements that refer to the template |
| `DesignCode` | `SteelDesignCode` | **Read-only.** Steel design code of the model, i.e. the code the property set belongs to |

#### Eurocode template: `EC3SteelLinearDesignTemplate`

Flat properties:

| Property | Type | Unit | Default | Description |
|---|---|---|---|---|
| `SectionClass` | `SteelSectionClass` | | `auto_class` | Imposed cross-section class (`class_1` to `class_4`) or `auto_class` |
| `ManufacturingQuality` | `SteelManufacturingQuality` | | `class_c` | Manufacturing quality class (`class_a`, `class_b`, `class_c`) |
| `ExecutionClassType` | `SteelExecutionClassType` | | `global_execution_class` | Take the execution class from the model settings, or impose one (`imposed_execution_class`) |
| `ExecutionClassValue` | `SteelExecutionClass` | | `exc2` | Imposed execution class. Used only when `ExecutionClassType` is `imposed_execution_class` |
| `FamilyOfElement` | `SteelFamilyOfElement` | | `family_e` | Family of the element, used with the consequence class to find the execution class |
| `ImposedValues` | `bool` | | `false` | Impose the class 4 effective characteristics below instead of computing them. Meaningful only when `SectionClass` is `class_4` |
| `Sxeff` | `double` | m² | `0` | Effective area |
| `ENy`, `ENz` | `double` | m | `0` | Shift of the centroid of the effective area along y, z |
| `Weff1y`, `Weff2y`, `Weff1z`, `Weff2z` | `double` | m³ | `0` | Effective section moduli |

Property groups. Each group is an optional sub-object.

| Property | Type | What it holds |
|---|---|---|
| `Deflection` | `SteelLinearDeflection` | Deflection checks of the element and of its super-element: the limits 1/n (`AllowableDeflection1`, `AllowableDeflection2`), the reference length, the verification location |
| `Buckling` | `SteelLinearBuckling` | `BucklingLength`: `Lfy`, `Lfz`, the mu and length modes and values per axis, the fire-situation lengths, `YNodesType` and `ZNodesType`, `Cmy0`/`Cmz0`, the buckling curves, the torsional buckling length, `TorsionalBucklingVerification` |
| `LateralTorsionalBuckling` | `EC3SteelLinearLateralTorsionalBuckling` | `UpperFlangeLength`, `LowerFlangeLength`, the buckling curve, and for each flange the restraints, k and kw, C1, C2, C3, the load application point and type, the f and fM coefficients, `CurveMethod`, `McrMode`/`McrValue`, `Mcr0Mode`/`Mcr0Value` |
| `GlobalSwayImperfections` | `SteelGlobalSwayImperfections` | `ColumnImperfections` (`PhiY`, `PhiZ`, `Phi0Y`, `Phi0Z`, `HValueY`, `MValueY`, the bow flags, ...) and `BeamImperfections`. `E0Ly` and `E0Lz` are stored but have no effect |
| `ColdFormedDesign` | `EC3SteelColdFormedDesign` | `IgnoreFyMz`, `ExactClass4Calculation`, `BucklingResistance`, `DesignInMainAxesUuVv` |
| `FireVerification` | `EC3SteelFireVerification` | Fire resistance (minutes), exposed faces, emissivity, and `FireProtection` |
| `AngleVerification` | `SteelAngleVerification` | Net-area verification of angle sections: legs, bolts, bolt holes, net areas, effective slenderness ratio |
| `WebBuckling` | `EC3SteelWebBuckling` | Transverse stiffeners (sides, `TransverseStiffenersLocation` list) and longitudinal stiffeners |
| `NonLinearFEM` | `SteelNonLinearFEM` | 2nd-order analysis: `RunNonLinearFEM`, iterations, annex C.2, and `Parameters` with the lists `PunctualSupports`, `DistributedSprings` and `InitialDeformation.Segments`, and the loads offset |

#### North-American templates: `AISCSteelLinearDesignTemplate` and `CSASteelLinearDesignTemplate`

Compared with the Eurocode template, these have no manufacturing quality, no execution class, no family of
element, no `GlobalSwayImperfections`, no `FireVerification` and no `WebBuckling`.

Flat properties:

| Property | On | Type | Description |
|---|---|---|---|
| `SectionClass` | AISC | `SteelSectionClassAISC` | `auto_class`, `compact`, `noncompact` or `slender_element` |
| `SectionClass` | CAN/CSA | `SteelSectionClass` | `auto_class` or `class_1` to `class_4` |
| `ImposedValues`, `Sxeff`, `ENy`, `ENz`, `Weff1y`, `Weff2y`, `Weff1z`, `Weff2z` | CAN/CSA only | | Class 4 effective characteristics, as on the Eurocode template (same types, units and defaults). AISC has none |

Property groups, the same on both templates. Each group is an optional sub-object.

| Property | Type | What it holds |
|---|---|---|
| `NonLinearFEM` | `SteelNonLinearFEM` | As on the Eurocode template |
| `Deflection` | `SteelLinearDeflection` | As on the Eurocode template |
| `Buckling` | `SteelLinearBuckling` | As on the Eurocode template. `YNodesType`/`ZNodesType` start as `braced` |
| `LateralTorsionalBuckling` | `NorthAmericanSteelLinearLateralTorsionalBuckling` | `ElementVerification` and, for each flange, the restraints, `LengthMode`/`LengthValue` and the Cb factor (`CbMode`, `CbValue`). There is no buckling curve and no C1/C2/C3 |
| `SteelSpecificProperties` | `NorthAmericanSteelSpecificProperties` | HSS design thickness (`HssDesignThicknessChoice`, `HssDesignThicknessValue`, `HssDesignAxialStressRelieved`), `CheckForTorsionAndShear`, `StiffenersSpacing` (m), `StiffenersTensionField`, Kt factor (`KtFactorCheck`, `KtFactorValue`), `ConnectorsSpacing` (m) |
| `ColdFormedDesign` | `NorthAmericanSteelColdFormedDesign` | AISI S100 options: `ExactCFDCalculation`, overlap and overhang at the support, `KPhiE`, `BearingLength`, and the web-crippling conditions of `ExteriorSupport` and `InteriorSupport` |
| `AngleVerification` | `SteelAngleVerification` | As on the Eurocode template |

The cold-formed group is stored and used by the design under both codes. The desktop property list shows it for
AISC only. Values sent for a CAN/CSA template do reach the design, but do not appear in the desktop list.

The full property list of every group is in `swagger.json`.

#### Rules

##### Refused

A refused create or update writes nothing. The call fails with `INFORMATIONAL_ELEMENT_CREATION_FAILED` or
`INFORMATIONAL_ELEMENT_UPDATE_FAILED`, and an `ENGINE_ERROR` message says why.

- The `$type` is not the one of the model's steel design code, or is a base class (see
  [above](#which-type-to-send)).
- `UserName` is empty (on create), longer than 50 characters, already used, or the name of the built-in default
  template.
- `Buckling.BucklingLength.TorsionalBucklingVerification` is `false` on any template except
  `CSASteelLinearDesignTemplate`. It is a CAN/CSA-only property; leave it `true` elsewhere.
- Eurocode template: `ColdFormedDesign.DesignInMainAxesUuVv` is `true`. Leave it `false`. Set
  `ProductOfInertiaIyz0` on the linear element instead ([Section 7](#7-data-models--elements-geometric)).
- Eurocode template: a flange whose `LengthMode` is `imposed` has `LengthValue <= 0`.
- Eurocode template: `RestraintDefinitionValues` is longer than 50 characters.
- Eurocode template: the `RelativeValue` of a transverse stiffener is not strictly between 0 and 1.
- `NonLinearFEM`: two punctual supports have the same `Abscissa`, or two distributed springs have the same
  `AbscissaStart` and `AbscissaEnd`.
- AISC template: `SteelSpecificProperties.StiffenersSpacing` is not `0`, or
  `SteelSpecificProperties.CheckForTorsionAndShear` is `true`. These two are CAN/CSA-only properties.

On the North-American templates the API does not check `LengthValue` of a flange whose `LengthMode` is `imposed`.
Send a value greater than `0`.

##### Read-only / computed on read

What you read back can differ from what you sent:

- `MaterialType`, `ElementType`, `AssignmentNumber` and `DesignCode` are ignored on write.
- `Buckling.BucklingLength.Lfy` follows `LfyMode`. After an update, `Lfy` equals `LfyValue` when `LfyMode` is
  `imposed`, `lateral_torsional_buckling_length_upper_flange` or `lateral_torsional_buckling_length_lower_flange`,
  and `0` otherwise. `Lfz` follows `LfzMode` the same way, but only `imposed` carries a length.
- You can set `Lfy` in two ways. Send `Lfy` alone with `LfyMode` left at its default: the mode becomes `imposed`.
  Or send `LfyMode` and `LfyValue`. If you send both and they disagree, the mode wins. The same applies to `Lfz`.
- `UpperFlangeLength` and `LowerFlangeLength` hold the same quantity as `LengthValue` of the matching flange
  parameters, and are derived from it. Send either the flange length alone (the flange `LengthMode` becomes
  `imposed`), or the flange `LengthMode` and `LengthValue` with the flange length left at `0`.
  On the Eurocode template the flange length is `LengthValue` when the mode is `imposed`, and `0` otherwise. On the
  North-American templates it is `LengthValue` whatever the mode (a length for `imposed`, a coefficient for the
  computed modes).
- An update that sends `LateralTorsionalBucklingLength` without the flange-parameters sub-object keeps the stored
  flange lengths.
- `UpperFlangeRestraintSummary` and `LowerFlangeRestraintSummary` are output only and localized. A value you send
  is ignored. Read the restraints themselves and do not parse the summary.
- Eurocode template: `GlobalSwayImperfections.ColumnImperfections.PhiY` and `PhiZ` are computed.
  `phi = alphaH * alphaM / Phi0`, with `alphaH = clamp(2 / sqrt(h), 2/3, 1)` and
  `alphaM = sqrt(0.5 * (1 + 1/m))`, where `Phi0` is `Phi0Y` or `Phi0Z`, `h` is `HValueY` or `HValueZ`, and `m` is
  `MValueY` or `MValueZ`. The value you send is kept only when `Phi0`, `h` or `m` is not greater than `0`; even
  then, changing `GlobalBowImperfectionsY` or `GlobalBowImperfectionsZ` sets the `Phi` of that direction to `0`,
  as ticking the box does in the property list.
- Eurocode template: `GlobalSwayImperfections.E0Ly` and `E0Lz` are stored and returned, but have no effect on
  the design.
- Eurocode template: `ColdFormedDesign.BucklingResistance = true` switches the buckling verification on. To do
  that directly, set `Buckling.ElementVerification`.
- Transverse stiffeners come back sorted by `RelativeValue`. Their `AbscissaValue` is always `0`, because a
  template has no geometry: `RelativeValue` is the only position.
- `NonLinearFEM.RunNonLinearFEM = true` sets `Buckling.ElementVerification` and
  `LateralTorsionalBuckling.ElementVerification` to `false`.
- North-American templates: `SteelSpecificProperties.HssDesignThicknessValue` is output only. It follows
  `HssDesignThicknessChoice` and the code: `reduced` gives 0.93 under AISC and 0.90 under CAN/CSA, `nominal` gives
  1.00. Set the choice and read the value.

##### Partial updates

- A group set to `null` is skipped and keeps what the template holds. This applies to create and to update.
  The same holds for the nested sub-objects, for example `Deflection.SuperElementDeflections`.
- A group you do send is written as a whole. Its properties that you leave out take their default values.
- A list you send replaces the stored list. It is not merged: sending two entries where three existed leaves two.
  A list set to `null` is left unchanged; an empty list clears it. The lists are
  `NonLinearFEM.Parameters.PunctualSupports`, `NonLinearFEM.Parameters.DistributedSprings`,
  `NonLinearFEM.Parameters.InitialDeformation.Segments` and
  `WebBuckling.TransverseStiffeners.TransverseStiffenersLocation`.
- `Buckling.BucklingLength.YNodesType` and `ZNodesType` are the only nullable properties of that group. `null`
  keeps the stored value. A new template starts with `unbraced` under the Eurocode and `braced` under AISC and
  CAN/CSA. Both always read back with a value, so a read-then-update keeps them.
- To change one property, read the template, change it and send the template back. Read-only values in the body
  are ignored.

##### Assignment

A template is applied to an element only when all three conditions hold:

1. The template is a template of the same kind of element (a linear template on a linear element).
2. The template is of the same material family as the element. A steel element modelled as a composite beam
   (`LinearElementType = eLinearElementFEMTypeCompositeBeam`) belongs to the composite family: a steel template
   cannot be assigned to it.
3. The template carries the property set of the design code the model has at the moment of the assignment.

| `DesignTemplateId` | Effect |
|---|---|
| `null` | The assignment does not change |
| `0` | The element is detached to the built-in default template ("None") |
| an EID | That template is assigned and its properties are copied onto the element |

- A refused assignment leaves the element's template as it was and is reported as an error in
  `Details.Diagnostics`. When it comes from `CreateElement` or `UpdateElement`, the element is still created or
  updated.
- `AssignDesignTemplateToElements` applies each element on its own. Elements that break a rule are reported one by
  one and are not changed; the others are assigned, and the call returns `false`. An EID that is not a template
  changes no element.
- Detaching is not a reset. The element keeps its current design property values.
- Changing the material of an element removes its design template, and changing the material back does not
  restore it. Assign the template again after any material change.
- `AssignmentNumber` counts the elements that refer to the template.
- Editing a template updates every element that refers to it.

##### List and delete

- The list holds the real templates of the model. The built-in default template ("None") is not in it. Identify a
  template by its EID or its name, never by its position.
- The built-in default template cannot be modified or deleted.
- Deleting a template that is in use succeeds. The elements that referred to it return to the default template
  and keep their current values. A warning gives the number of elements.

#### Examples

```csharp
// 1. Create an Eurocode template. Only the groups you set are written.
EIDApiResponse created = client.CreateInformationalElement(new EC3SteelLinearDesignTemplate
{
    UserName     = "Purlins - EC3",
    SectionClass = SteelSectionClass.Class_2,
    Deflection   = new SteelLinearDeflection
    {
        ElementVerification = true,
        Deflections = new SteelDeflection { AllowableDeflection1 = 250, AllowableDeflection2 = 300 }
    },
    FireVerification = new EC3SteelFireVerification { FireResistance = 60 }
});
long templateId = created.Data.Value;

// 2. Assign it to several elements
client.AssignDesignTemplateToElements(templateId, new List<long> { beam1, beam2, beam3 });

// 3. Edit it: read, change, send back. The three beams follow.
var stored = (EC3SteelLinearDesignTemplate)client
    .GetInformationalElementsObject(new List<long> { templateId }).Data.First();
stored.Deflection.Deflections.AllowableDeflection1 = 300;
client.UpdateInformationalElement(templateId, stored);

// 4. Detach one beam. It keeps its current design values.
client.AssignDesignTemplateToElements(0, new List<long> { beam2 });
```

```csharp
// A North-American template. The model must have an AISC steel design code, otherwise the call is refused.
EIDApiResponse aisc = client.CreateInformationalElement(new AISCSteelLinearDesignTemplate
{
    UserName     = "HSS columns - AISC",
    SectionClass = SteelSectionClassAISC.Compact,
    SteelSpecificProperties = new NorthAmericanSteelSpecificProperties
    {
        HssDesignThicknessChoice = SteelHssDesignThickness.Reduced,   // read 0.93 in HssDesignThicknessValue
        KtFactorCheck            = true,
        KtFactorValue            = 0.85
    },
    LateralTorsionalBuckling = new NorthAmericanSteelLinearLateralTorsionalBuckling
    {
        ElementVerification = true,
        LateralTorsionalBucklingLength = new NorthAmericanSteelLateralTorsionalBucklingLength
        {
            UpperFlangeParameters = new NorthAmericanSteelLateralTorsionalBucklingParameters
            {
                StartRestraint = SteelLtbEndRestraint.Pinned,
                EndRestraint   = SteelLtbEndRestraint.Pinned,
                LengthMode     = SteelBucklingLengthMode.Imposed,
                LengthValue    = 3.0,                                  // m
                CbMode         = SteelLtbFactorCbMode.Imposed_cb,
                CbValue        = 1.14
            }
        }
    }
});
```

```json
{
  "$type": "EC3SteelLinearDesignTemplate",
  "userName": "Purlins - EC3",
  "sectionClass": "class_2",
  "deflection": {
    "elementVerification": true,
    "deflections": { "allowableDeflection1": 250, "allowableDeflection2": 300 }
  },
  "fireVerification": { "fireResistance": 60 }
}
```

---

## 11. Data Models — Analysis Results

This section describes the objects of [Section 3.8](#38-model--analysis): the model status, the queries, the
finite-element (FEM) results and the design results. Results are polymorphic objects derived from `ResBase`. The
`$type` of each object tells you which class it is.

**Units.** All values are in SI units: m, rad, N, N·m, Pa. The Advance Design tables and reports show kN, kN·m, cm and
percentages. The API does not convert them.

### AnalysisModelStatus

Returned by `GetAnalysisModelStatus`. The limits of its flags are explained under
[that endpoint](#get-apimodelanalysisgetanalysismodelstatus).

| Property | Type | Default | Description |
|---|---|---|---|
| `HasAnalysisModel` | `bool` | `false` | The project holds an analysis model. A project created through the API has none until `LaunchAnalysis` runs |
| `MeshState` | `eMeshState` | `mesh_no` | `mesh_no` (no mesh), `mesh_available` (complete) or `mesh_incomplete` |
| `FemIsCalculated` | `bool` | `false` | The finite-element analysis ran and produced results. `LaunchDesignCalculation` needs it |
| `FemIsUpToDate` | `bool` | `false` | The analysis model still matches the results. Not reliable in an API session |
| `FemCalculatedCasesCount` | `int` | `0` | Number of load cases, combinations and envelopes that carry results |
| `Design` | `DesignExpertStatus[]` | | One entry per `DesignCalculationType`, always all four, in that order |
| `HasConstructionStages` | `bool` | `false` | The model defines construction stages |
| `AllStagesCalculated` | `bool?` | `null` | Every stage is calculated. `null` when the model has no stage or the analysis model could not be read |
| `OverallState` | `eAnalysisModelState` | `NotCreated` | One-value summary. See below |

`DesignExpertStatus`:

| Property | Type | Default | Description |
|---|---|---|---|
| `Family` | `DesignCalculationType` | `Concrete` | The family of this entry |
| `IsCalculated` | `bool` | `false` | The design engine of this family produced results. A stored flag, not proof that results exist |
| `SettingsChangedSinceCalculation` | `bool?` | `null` | The calculation settings changed after the last design. `null` for `Concrete`. Set by the desktop application only: `true` can be trusted, `false` means nothing was reported |

`mesh_incomplete` means the mesher could not cover part of the model. The calculation still runs on it, so results can
exist while parts of the structure have no finite elements. Treat it as a warning.

`OverallState` takes the first rule that matches: `NotCreated` if `HasAnalysisModel` is `false`; `OutOfDate` if
`FemIsCalculated` and not `FemIsUpToDate`; `Calculated` if `FemIsCalculated`; `Meshed` if `MeshState` is not
`mesh_no`; otherwise `Created`.

- The design families are not part of the rule. Right after `LaunchAnalysis` the state is `Calculated` with every
  family not calculated. Read `Design`.
- `mesh_incomplete` with `Calculated` is a valid state. Read `MeshState` when it matters.
- With `NotCreated`, every other value is at its default and means nothing.

### The queries

`ResultsQuery` is the polymorphic base of the request bodies of `GetResultsByQuery` and `GetDesignResults`. It has no
properties. Its `$type` is `ResultsQueryFEM`, `ResultsQueryDesign` or `ResultsQueryDesignSteelLinear`.

`ResultsQueryFEM` (body of `GetResultsByQuery`):

| Property | Type | Description |
|---|---|---|
| `EResType` | `ResultType` | `displacement`, `forces`, `stresses`, `resultantforces` or `generalizedBucklingRes` |
| `ListElementsIDs` | `long[]` | EIDs of the elements to read. Must not be empty, even for `generalizedBucklingRes` |
| `AnalysisCaseInfo` | `LoadCaseInfo` | The load case or combination. A `LoadCaseInfo`, or a `LoadCaseAnalysisBuckling` for a buckling mode |

`LoadCaseInfo` (`"$type": "LoadCaseInfo"`) has `Id` (`EID`), the EID of the load case or combination.
`LoadCaseAnalysisBuckling` (`"$type": "LoadCaseAnalysisBuckling"`) adds `Mode` (`int`, 0-based, default `-1`).

`ResultsQueryDesign` is the base of the body of `GetDesignResults`. Send its derived type `ResultsQueryDesignSteelLinear`
(`"$type": "ResultsQueryDesignSteelLinear"`), the only one served:

| Property | Type | Default | Description |
|---|---|---|---|
| `ListElementsIDs` | `long[]` | | EIDs of the linear elements. Required: an empty list does not mean the whole model |
| `EResType` | `SteelLinearDesignResultType` | `All` | The result family. `All` returns every family available under the model's steel code for the chosen result set |
| `EResultSet` | `SteelDesignResultSet` | `Standard` | `Standard`, `ColdFormed` or `AdvancedStability` |

The result types and result sets are in the table under [`GetDesignResults`](#post-apimodelanalysisgetdesignresults).
The enumeration values are in [Section 13](#13-enumerations-reference).

### FEM results

Returned by `GetResults` and `GetResultsByQuery`: a list of `ResBase`.

| `$type` | Returned for |
|---|---|
| `ResElementLinear` | Linear elements |
| `ResElementPlanar` | Planar elements (slabs, walls) |
| `ResPunctualSupport`, `ResLinearSupport`, `ResPlanarSupport` | Supports |
| `ResNode` | `displacement` only: every mesh node of the requested elements, punctual supports excepted, listed once, in the global axes |
| `ResGeneralizedBuckling` | `generalizedBucklingRes`: `LambdaFactor`, the critical load factor of the mode (no unit). Model-wide |

`ResPlanarNode` is the node result inside a planar element result.

`ResBase.Id` is the identifier of the element in the analysis model. It is not always the EID you sent. See the
warning under [`GetResults`](#post-apimodelanalysisgetresults). The design results are different: their `Id` is the EID
you sent.

#### What each result type fills

| `ResultType` | Linear element | Planar element | Punctual support | Linear support | Planar support |
|---|---|---|---|---|---|
| `displacement` | Nodes: `Dx` `Dy` `Dz` `D` `Rx` `Ry` `Rz` `R`. Diagrams: `Dx` `Dy` `Dz` `D` | Nodes: `Dx` `Dy` `Dz` `D` | `ResNode`: all eight | Nodes: all eight | Nodes: all eight |
| `forces` | Nodes and diagrams: `Fx` to `Mz` | Nodes: `ResPlanarForces` | `ResNode.ResForces` | Nodes: `ResForces` | Nodes: `ResForces` |
| `stresses` | Nodes and diagrams: `LinearElement` | Nodes: `ResPlanarStresses` | Nothing | Nodes: `Support` | Nodes: `Support` |
| `resultantforces` | Nothing | `ResTorsors` (walls only) | Nothing | `Torsors*` | `Torsors*` |

- A property that the result type does not fill reads `0`, never `null`. Do not read it as a calculated zero. For
  example, planar elements have no rotations and punctual supports have no stresses.
- `ResNode.Id` and `ResPlanarNode.Id` are mesh node numbers: 0-based indexes into the list of
  `GetMeshNodesPosition`. The `ResNode` of a punctual support has `Id = 0` under `forces`.
- The results of a linear element (nodes and diagrams) are in the local axes of the linear element by default: they follow the frame that
  the post-processing is configured with. The `ResNode` objects of `displacement` and the support results are in the
  global axes.

#### Shape of the FEM results

- `ResNode` (also the node of a linear element or a support) has `ResDisplacements`, `ResForces` and `ResStresses`. `ResStresses`
  has two groups, `LinearElement` (linear elements) and `Support` (supports). Only the one that fits the element is filled.
- `ResElementLinear` has `ResNodes` (one per mesh node) and `ResDiagrams`: `ResDisplacements` (`Dx`, `Dy`, `Dz`, `D`),
  `ResForces` (`Fx` to `Mz`) and `ResStresses` (`SxxMin` to `Sv`). Each is a list of `ResAbscissaValue` (`Abscissa`
  in m, and `Value`). On the nodes of a linear element, `R` is the norm of the three rotations. The diagrams have translations
  only.
- `ResElementPlanar` has `ResNodes` (`ResPlanarNode`: `ResDisplacements`, `ResForces` as `ResPlanarForces`,
  `ResStresses` as `ResPlanarStresses`) and `ResTorsors`.
- `ResPunctualSupport` has `ResNode`. `ResLinearSupport` and `ResPlanarSupport` have `ResNodes` (one per mesh node of
  the support) and `TorsorsFx`, `TorsorsFy`, `TorsorsFz`, `TorsorsMx`, `TorsorsMy`, `TorsorsMz`, the resultant of the
  whole support (`resultantforces` only).
- `ResPlanarStresses` names a value `<Component><Face>`. Components: `Sxx`, `Syy`, `Sxy`, `Sxz`, `Syz`, `Szz`, `Sv`
  (von Mises), `S1`, `S2` (principal), `Alpha` (principal angle), `_S1`, `_S2` (supplemental principal). Faces: `Top`,
  `Inf` (inferior), `Mid` (mid-plane). Example: `SxxTop`, `AlphaInf`.
- `ResPlanarTorsors` holds the resultants of a wall along its cuts, in the local axes of each cut. A wall has exactly
  two entries: `ResTorsors[0]` is the bottom cut (`*_BottomTop`) and the left cut (`*_LeftRight`); `ResTorsors[1]` is
  the top and the right cut. Only walls are answered: other planar elements are left out of the `resultantforces` answer.
  `N_*` is the normal force, `Txy_*` the in-plane shear, `Tyz_*` the out-of-plane shear, `Mz_*` the in-plane bending
  moment, `Mf_*` the out-of-plane bending moment, `TW_Length_*` and `TW_Offset_*` the length and the offset of the
  cut. `MX_GROUP`, `MY_GROUP`, `NZ_GROUP`, `TX_GROUP`, `TY_GROUP` and the same with `_ETAGE` are not filled. Ignore
  them.

#### Units of the FEM results

| Class | Properties | Unit |
|---|---|---|
| `ResDisplacements` | `Dx`, `Dy`, `Dz`, `D` | m |
| `ResDisplacements` | `Rx`, `Ry`, `Rz`, `R` | rad |
| `ResForces` (also nodal reactions of supports) | `Fx`, `Fy`, `Fz` | N |
| `ResForces` | `Mx`, `My`, `Mz` | N·m |
| `LinearElementStresses` | `SxxMin`, `SxxMax`, `SxyMin`, `SxyMax`, `SxzMin`, `SxzMax`, `Sv` | Pa |
| `SupportStresses` of a **linear** support | `Sx`, `Sy`, `Sz`, `S` | N/m (distributed reaction per metre of support, not a pressure) |
| `SupportStresses` of a **planar** support | `Sx`, `Sy`, `Sz`, `S` | Pa (pressure) |
| `ResPlanarForces` | `Fxx` `Fyy` `Fxy` `Fxz` `Fyz` `Fzz`, `F1`, `F2` | N/m |
| `ResPlanarForces` | `Mxx` `Myy` `Mxy` `Mxz` `Myz` `Mzz`, `M1`, `M2` | N·m/m |
| `ResPlanarForces` | `AlphaF`, `AlphaM` | rad |
| `ResPlanarStresses` | every component except `Alpha*` | Pa |
| `ResPlanarStresses` | `AlphaTop`, `AlphaInf`, `AlphaMid` | rad |
| Support torsors, `ResPlanarTorsors` `N_*`, `Txy_*`, `Tyz_*` | `TorsorsFx` to `TorsorsFz` | N |
| Support torsors, `ResPlanarTorsors` `Mz_*`, `Mf_*` | `TorsorsMx` to `TorsorsMz` | N·m |
| `ResPlanarTorsors` | `TW_Length_*`, `TW_Offset_*` | m |
| `ResAbscissaValue` | `Abscissa` | m |

### Design results

Returned by `GetDesignResults`. They have the same root, `ResBase`, but are separate objects: one per element and
result family. Their `Id` is the EID you sent and `UserID` is the number shown in the Advance Design interface.

| Object | Stored | Carries |
|---|---|---|
| `ResDesignElementLinearSteel` | Per mesh cell | `ResMeshes`: one `ResDesignMesh` (`MeshIndex`, `Data`) per cell |
| `ResDesignElementLinearSteelPerElement` | Per element | `Data`: one payload |
| `ResDesignElementLinearSteelPerCase` | Per combination | `ResCases`: one `ResDesignCase` (`CaseNumber`, `Data`) per combination |

All three have `Id`, `UserID`, `DesignFamily` (`Steel`) and `ResultType` (the family of the object). The table under
[`GetDesignResults`](#post-apimodelanalysisgetdesignresults) says which object each family arrives in. Switch on the
`$type` of the object, then on the `$type` of the payload.

- `MeshIndex` is the 0-based index of the cell within the element. It restarts at 0 on every element. A cell without
  a result is left out of `ResMeshes`: use `MeshIndex`, not the position in the list.
- `ResDesignCase.CaseNumber` is the number of the combination as shown in the Advance Design interface (`-1` when
  none). `ResCases` is in ascending order.
- An element with no result for a per-element or per-combination family is left out of the answer.
- Each per-cell payload carries its own `MeshPoint`, from 0 to 4: the point inside the cell where the extreme
  occurred (the cell is sampled at its start, quarter points, mid-point and end). Every check has its own.
- The `CaseNumber` of a check or a location is the most unfavourable combination, as the engine stores it. It is not
  an EID and not the user's combination number. Each check of a cell keeps its own.

#### Reading the checks

1. **`IsCalculated`, not `Verification`.** Every resistance and fire check has both. `Verification` is a bit mask of
   the code equations that apply to the section. It is filled in whether or not the check ran. `IsCalculated` says
   that the check ran. When it is `false`, `Action`, `Limit` and `WorkRatio` are zero and mean nothing.
2. **A work ratio of 0 does not mean "verified, not stressed".** Look at the flag first: `IsCalculated` for a
   resistance or fire check, `Exists` for a deflection location or a cold-formed clause, `IsIgnored` for a
   cold-formed lateral-torsional buckling check that the code allows to skip.
3. **`ResEc3CrossSectionClass.SectionClass` is the class setting, not the class.** It is `Auto` unless `IsImposed`
   is `true`. The classes that the engine found are in `ClassPerLoadingPattern`, one per loading pattern. The class a
   verification used is at `SectionClassDetails.ClassDetails.SectionClass` of that check.
4. **`Lfy` is the buckling length in the y direction, which governs buckling about z-z.** This follows Advance
   Design: its shape sheet pairs "Buckling on Y-Y" with `Lfz`. `SlendernessLfy` goes with `Lambda_bar_z`.
5. **The bow imperfection `e0/L` is a ratio.** `BowImperfectionRatioY` and `Z` hold the ratio itself: a column that
   Advance Design prints as L/200 comes back as `0.005`. For a beam the ratio does not include the factor k.
6. **Buckling curves** use `SteelBucklingCurve` (`auto_curve`, `curve_a0`, `curve_a`, `curve_b`, `curve_c`,
   `curve_d`), also for the lateral-torsional curve. `auto_curve` there means the general case of clause 6.3.2.2,
   where alpha,LT is computed. Advance Design prints it as "Curve -".
7. **`Deflection1` and `Deflection2`** are the first and the second allowable-deflection criterion.
8. **A deflection is reported as a denominator.** `AllowableCoefficient` and `EffectiveCoefficient` are the `x` of
   `L/x`: L/200 is `200`. A larger `EffectiveCoefficient` is a smaller deflection.

#### Payloads

The full property list of every payload is in `swagger.json`. Work ratios are dimensionless. All other values are SI.

| Payload `$type` | Holds |
|---|---|
| `ResEc3Resistance` | The 12 cross-section resistance checks, with the reduced moment resistances |
| `ResEc3Stability` | Flexural, torsional and lateral-torsional buckling: reduction factors, slenderness, critical forces and moments, interaction factors and both interaction equations |
| `ResEc3FireResistance` | The 6 fire checks, with the fire duration (minutes) and the number of exposed faces |
| `ResEc3Deflection` | The deflections of the local y and z directions at every location, and the envelope |
| `ResEc3WebShearBuckling` | Up to 3 EN 1993-1-5 checks (shear resistance, interaction, reduced stress). Each is `null` when it was not performed |
| `ResEc3BucklingLengths` | `Lfy`, `Lfz` (m), their ratios to the super-element length, and the slendernesses |
| `ResEc3LateralTorsionalBucklingLengths` | `Ldi`, `Lds` (m), ratios, slendernesses, and the EN 1993-1-6 shell data of class 4 tubes |
| `ResEc3CrossSectionClass`, `ResEc3FireCrossSectionClass` | The classification per loading pattern, with the per-plate classes and the effective properties |
| `ResEc3FireTemperature` | Steel temperature and critical temperature (°C), and the work ratio |
| `ResEc3AdvancedStabilityResistance` | Normal, von Mises and shear stress checks of the non-linear engine, the critical load factor, `Ncr`, `Mcr`. A non-empty `FailedAnalysisIds` means the result is incomplete |
| `ResEc3MaxWorkRatio` | The governing work ratio and the maxima per family |
| `ResEc3ColdFormedResistance` | One block per EN 1993-1-3 clause (6.1.2 to 6.3). Each reports `UsedEquation`, the formula that was applied |
| `ResEc3HaunchSectionProperties` | The section properties of the haunch at each end, or `null` where there is none |
| `ResEc3BucklingLengthCoefficients` | The end-restraint coefficients behind the automatic buckling lengths. `null` for a direction whose length was imposed |
| `ResEc3BucklingLengthNodeRestraints` | Per direction, the members that frame into each end of the element, grouped as superior, inferior and horizontal, and the stiffness sum of each group |
| `ResEc3GlobalSwayImperfectionForces` | Per combination: `Ved` and the equivalent forces `PhiYVed`, `PhiZVed` (N) |
| `ResEc3LocalBowImperfectionForces` | Per combination: punctual (N) and linear (N/m) forces per direction, the length, the ratio `e0/L`, and `IsColumn` |
| `ResSteelProfileOptimisation` | The current section and the proposed section, with the strength and deflection work ratios of both |
| `ResSteelColdFormedSectionInfo` | The start and end sections of a cold-formed element: gross and effective area, inertias, moduli, shear centre, `Fy`, `Fu`, `Fya` (Pa) |
| `ResAiscResistance` | The 7 strength checks and their per-chapter detail, including the buckling quantities |
| `ResAiscDeflection` | One set of locations. It is already the envelope of the two directions, so no direction is reported |
| `ResAiscBucklingLengths` | `Lfy`, `Lfz` (m) and the slenderness ratios KL/r |
| `ResAiscLateralTorsionalBucklingLengths` | `Lbi`, `Lbs` (unbraced lengths, m), ratios and slendernesses |
| `ResAiscCrossSectionClass` | The classification for four force patterns. There are no effective properties |
| `ResAiscMaxWorkRatio` | The governing work ratio and the maxima per family |
| `ResAiscSectionInfo` | The gross section of one cell: dimensions, areas, moduli, inertias, radii of gyration, shear centre |
| `ResAisiColdFormedResistance` | The AISI S100 / CSA S136 chapters D2, E, F, G2, G5 and H1 to H4 |

Every `ResAisc*` and `ResAisi*` payload derives from `ResNorthAmericaSteelLinearDesignData`. Its `DesignCode`
(`Aisc360`, `Aisc360_2016`, `CsaS16_2009`, `CsaS16_2014`, `CsaS16_2019`, `CsaS16_2024`) says which code and edition
produced the result.

- `ResSteelProfileOptimisation` under Eurocode 3 records the current section whether or not an optimisation was
  requested. Under AISC 360 and CSA S16 the record is empty when none was requested: `InitialStartSection` is `null`
  then. In the cold-formed set the proposal equals the current section, so it is not a suggestion.
- `ResSteelColdFormedSection.SectionClass` is always `null` under AISC 360 and CSA S16.
- The payloads named `ResEc3G2*`, and `ResEc3G2CrossSectionInfo`, belong to the 2nd generation of Eurocode 3
  (EN 1993-1-1:2023). They are published but not released: a model configured for that code is refused on an ordinary
  installation, so nothing produces them.

What to read first in the most used payloads:

- **`ResEc3Resistance`**: `Checks`, 12 entries of `ResEc3ResistanceCheck`. Test `IsCalculated`, then read `WorkRatio`
  (above 1: not satisfied), `Action` and `Limit` (N, N·m, or Pa for `ElasticStress`), `Check` (which of the 12) and
  `CaseNumber`.
- **`ResEc3Stability`**: `WorkRatio1` and `WorkRatio2` (interaction equations 6.61 and 6.62, with their N, My and Mz
  terms), `CaseNumber`, `MeshPoint`, the reduction factors `Chi_y`, `Chi_z`, `Chi_LT`, and the buckling curves. Test
  `VerificationBuckling` and `VerificationLateralTorsionalBuckling`: `0` means no check was performed.
- **`ResEc3Deflection`** (`DeflectionY`, `DeflectionZ`) and **`ResAiscDeflection`** (the same members, directly):
  `SuperElementEnd`, `SuperElementSpan`, `SegmentSpan`, `SegmentEnd` and `Envelope`, each a location result. Test
  `Exists`, then read `DeflectionValue` (m), `WorkRatio` and `CaseNumber`. `AllowableCoefficient` is the allowable
  deflection as the `x` of `L/x`.
- **`ResEc3MaxWorkRatio`** and **`ResAiscMaxWorkRatio`**: `WorkRatios`, entries of `Component` and `WorkRatio` (not
  multiplied by 100). A family that was not checked is absent, not `0`. Eurocode 3 answers `Overall`,
  `CrossSectionResistance`, `Stability`, `Deflections` and `FireResistance`. AISC 360 and CSA S16 answer `Overall`,
  `CrossSectionResistance` and `Deflections`.
- **`ResAiscResistance`**: `Checks`, 7 entries. Test `IsCalculated`, then read `WorkRatio`, `Action`, `Strength` (N or
  N·m) and `CaseNumber`. `IsCompression` tells whether `AxialDetail` is meaningful. The detail objects hold the
  quantities behind each check (KL/r, Fe, Fcr, Lp, Lr, Lb, Cb).

> **Warning:** The method of an AISC 360 model is ASD unless you chose LRFD in the environment of the project
> ([Section 3.1.1](#311-the-environment-of-a-session)). `ResAiscResistance.DesignMethod` says which (`null` under CSA
> S16, which has only the limit-states format). The `Strength` of an ASD result is Rn/Omega, not phi·Rn. Do not
> compare it with an LRFD value.

---

## 12. Data Models — Climatic Parameters

This section describes the wind, snow and seismic families and their load cases: the parameter blocks of each
family class and the properties of each load case class. The families, their link to the load cases, and the
rules for all families are in [Section 10.1](#101-load-case-families) and [Section 10.2](#102-load-cases). Units
are SI unless a table states another one.

- **Send only the blocks you need.** A block that is `null` keeps the model default. A block that you send is
  applied whole, with the defaults of its properties
  ([Section 10](#10-data-models--informational-elements-load-cases--families)).
- **The tables list the properties a caller normally sets.** Every other property is described in `swagger.json`.
  The values of every enumeration are in [Section 13](#13-enumerations-reference).

#### The family type follows the session standard

The wind, snow and seismic standard of a model is chosen in the session
([Section 3.1.1](#311-the-environment-of-a-session)). The family that Advance Design creates follows that
standard. Choose the family type from this table:

| Session standard | Wind and snow | Seismic |
|---|---|---|
| `Climatic` = `eurocode` (any annex), `romania`, `ntc2008`, `ntc2018`, `pr_en13031_1` | `LoadCaseFamily_Wind_1991_1_4`, `LoadCaseFamily_Snow_1991_1_3` | — |
| `Climatic` = `cbnsw2010`, `cbnsw2015`, `cbnsw2020` | `LoadCaseFamily_WindCBN`, `LoadCaseFamily_SnowCBN` | — |
| `Climatic` = `ibc2015`, `ibc2018`, `asce_7_22` | `LoadCaseFamily_WindIBC`, `LoadCaseFamily_SnowIBC` | — |
| `Climatic` = `france` (NV 65 / N 84) | `LoadCaseFamily_Wind`, `LoadCaseFamily_Snow` (generic, no parameter blocks) | — |
| `Seismic` = `en1998_1`, `ntc2008`, `ntc2018` | — | `LoadCaseFamily_SeismicEN_1998_1` |
| `Seismic` = `cnb2005`, `cnb2015`, `cnb2020` | — | `LoadCaseFamily_SeismicCBN` |
| `Seismic` = `ibc2012`, `ibc2018` | — | `LoadCaseFamily_SeismicIBC` |
| `Seismic` = `ps92`, `ps92_2010`, `p100`, `p100_1_2013`, `rps2000`, `rpa99_2003` | — | `LoadCaseFamily_Seismic` (generic) |

A standard with no dedicated type is returned by `GetInformationalElementsObject` as the generic type. The 2nd
generation Eurocodes are not released, so no family type is offered for them.

#### Generating the loads

Wind and snow loads are generated by `ProcessAction` with `ClimaticAutoGeneration`
([Section 3.9](#39-model--actions)), after the families and their load cases exist. When the model has no wind
wall elements, the generation works on the 2D model and uses the `Building2D` parameters of the families.

#### Values that are applied as another value

An enumeration value that the model cannot hold is not refused; it is applied as the value shown here.

- `InternalPressure_Wind_EN1991_1_4_API.WIND_PRESSURE_COEFF` is applied as `WIND_SURPRESSION`.
- `PitchType_Wind_EN1991_1_4_API.PITCH_TYPE_MONO` is applied as `PITCH_TYPE_POSITIVE_SLOPE`, and `PITCH_TYPE_DUO`
  as `PITCH_TYPE_NEGATIVE_SLOPE`. A read returns the new names.
- `ComputingProbabilityCoeff_Wind_EN1991_1_4_API.eWindComputingProbabilityCoeffByAdjustmentFactor` is applied as
  `eWindComputingProbabilityCoeffImposed`.
- `GreenhouseClass_SnowNF_EC1_API.GREENHOUSE_CLASS_C` is applied as `GREENHOUSE_CLASS_B`.
- `SurfaceMaterialCm_SnowNF_EC1_API.GREENHOUSE_MATERIAL_CM_0_8` is applied as `GREENHOUSE_MATERIAL_CM_1_0`.
- `ExposureCoefficient_SnowNF_EC1_API.GRCG_SNOWNF_NE_1991_1_3_COEXP_NORMAL_1` is applied as
  `GRCG_SNOWNF_NE_1991_1_3_NORMAL_TOPO_1`.
- The seismic direction `XY` is applied as `X` (`DirectionChoice_API` and `DirectionChoiceEN1998_API`): a seismic
  load case takes one direction, `X`, `Y` or `Z`.

### 12.1 Wind EN 1991-1-4

`LoadCaseFamily_Wind_1991_1_4` has six blocks, all optional. `BasePressure`
(`BasePressureParameters_Wind_EN1991_1_4`: wind speed, terrain, dynamic factors) is described below. The others
are described in `swagger.json`:

- `AutoGeneration`: switches of the generation (`WindWallStatus`, `PressureCoeff`, `SplitWindWalls`,
  `LoadGeneration`, `AutoCalculationCpeCpi`, `UseCTICMCodeUpdates`), `ImposedCpeType` and `PitchType`.
- `SiteCoefficients` (orography, phi), `ResponseCoefficients` (turbulence intensity, ce(z); `PeakVelocityPressureQpZ`
  is in Pa) and `Building2D` (building length and portal position, in m, for the 2D generation).
- `GreenhouseParameters`: greenhouses (EN 13031-1). This needs a licence with the greenhouse option
  ([Section 3.1.1](#311-the-environment-of-a-session)).

#### `BasePressureParameters_Wind_EN1991_1_4`

| Property | Type | Unit | Default | Description |
|---|---|---|---|---|
| `Speed` | `double` | m/s | 0 | Basic wind speed |
| `Pressure` | `double` | Pa | 0 | Wind pressure. Overrides `Speed` when set |
| `CDir` | `double` | — | 1.0 | Direction coefficient |
| `CSeas` | `double` | — | 1.0 | Season coefficient |
| `HeightOfStructureBase` | `double` | m | 0 | Height of the structure base above the ground |
| `PlacementType` | `PlacementType_Wind_EN1991_1_4_API` | — | `TERRAIN_CATEGORY_II` | Terrain category, `TERRAIN_CATEGORY_0` to `TERRAIN_CATEGORY_IV` |
| `RigLength` | `double` | m | 1.0 | Roughness length z0 |
| `Kr` | `double` | — | 1.0 | Terrain factor |
| `CsCdCalcMode` | `CsCdCalcMode_Wind_EN1991_1_4_API` | — | `CS_CD_AUTO` | `CS_CD_AUTO`, `CS_CD_IMPOSED`, or `CS_CD_NO` (no structural factor) |
| `CsCdVal` | `double` | — | 1.0 | cs·cd value |
| `Delta` | `double` | — | 0.1 | Logarithmic decrement |
| `N` | `double` | Hz | 0 | Fundamental frequency |
| `NAuto` | `bool` | — | `true` | Calculate N automatically |
| `CorrelationCoefficientType` | `CorrelationCoefficientType_Wind_EN1991_1_4_API` | — | `CORREL_AUTO` | `CORREL_AUTO`, `CORREL_IMPOSED`, or `CORREL_NO` (no correlation coefficient) |

Also: `HeightOfStructureBaseMaxForAllComputations`, `CsCdValMin`, `PhiScaffolding`, `PhiScaffoldingAuto`,
`CorrelationCoefficientKdc`, `CalculOfTurbulenceFactorKL`, `TurbulenceFactorKL`.

#### `LoadCase_Wind_1991_1_4`

`EffectParameters` (`WindEffectParameters_EN1991_1_4`) sets the load case. The Canadian and IBC wind load cases
use `EffectParametersWindCBN` and `EffectParametersWindIBC`, which have the same properties.

| Property | Type | Default | Description |
|---|---|---|---|
| `WindDirection` | `WindDirection_Wind_EN1991_1_4_API` | `WIND_X_PLUS` | `WIND_X_PLUS`, `WIND_X_MINUS`, `WIND_Y_PLUS`, `WIND_Y_MINUS` |
| `InternalPressure` | `InternalPressure_Wind_EN1991_1_4_API` | `WIND_SURPRESSION` | `WIND_SURPRESSION` or `WIND_DEPRESSION` |
| `IsAutomatic` | `bool` | `false` | The loads of this load case are generated automatically |
| `TypeOfLoadingsCase` | `LoadingsCaseType_Wind` | `NORMAL_TYPE_WIND` | `NORMAL_TYPE_WIND` or `ACCIDENTAL_TYPE_WIND` |

Also: `IsUpdated` (internal flag, leave it `false`), `IsPartialMass`, `PartialMassCoefficient`.

#### Example: a wind family, its load cases, and the generation

```csharp
// 1. The family
var wind = new LoadCaseFamily_Wind_1991_1_4
{
    Name = "Wind EN 1991-1-4",
    BasePressure = new BasePressureParameters_Wind_EN1991_1_4
    {
        Speed         = 28,                                              // m/s
        PlacementType = PlacementType_Wind_EN1991_1_4_API.TERRAIN_CATEGORY_II,
        CsCdCalcMode  = CsCdCalcMode_Wind_EN1991_1_4_API.CS_CD_AUTO
    },
    AutoGeneration = new AutoGenerationParameters_Wind_EN1991_1_4
    {
        WindWallStatus        = true,
        LoadGeneration        = true,
        AutoCalculationCpeCpi = true
    }
};
long windId = client.CreateInformationalElement(wind).Data.Value;

// 2. One load case per wind direction (the four directions of the enumeration)
foreach (var direction in Enum.GetValues<WindDirection_Wind_EN1991_1_4_API>())
{
    client.CreateInformationalElement(new LoadCase_Wind_1991_1_4
    {
        Name             = $"W {direction}",
        LoadCaseFamilyID = new EID { Value = windId },
        EffectParameters = new WindEffectParameters_EN1991_1_4
        {
            WindDirection      = direction,
            InternalPressure   = InternalPressure_Wind_EN1991_1_4_API.WIND_SURPRESSION,
            IsAutomatic        = true,
            TypeOfLoadingsCase = LoadingsCaseType_Wind.NORMAL_TYPE_WIND
        }
    });
}

// 3. Generate the loads of all the wind and snow families
client.ProcessAction(AD_API_ActionType.ClimaticAutoGeneration, new List<ActionArgumentsBase>());
```

The body of the family:

```json
{
  "$type": "LoadCaseFamily_Wind_1991_1_4",
  "name": "Wind EN 1991-1-4",
  "basePressure": { "speed": 28, "placementType": "TERRAIN_CATEGORY_II", "csCdCalcMode": "CS_CD_AUTO" },
  "autoGeneration": { "windWallStatus": true, "loadGeneration": true, "autoCalculationCpeCpi": true }
}
```

### 12.2 Wind CBN

`LoadCaseFamily_WindCBN` (Canadian code) has three blocks, all optional: `BasePressure`, `AutoGeneration`
(`WindWallStatus`, `PressureCoeff`, `SplitWindWalls`, `LoadGeneration`) and `Building2D` (building length and
portal position, as in [12.1](#121-wind-en-1991-1-4)). `LoadCase_WindCBN.EffectParameters` is described in
[12.1](#121-wind-en-1991-1-4).

#### `BasePressureParametersWindCBN`

| Property | Type | Unit | Default | Description |
|---|---|---|---|---|
| `ZoneHash` | `string` | — | `" "` | Identifier of the location zone |
| `Pressure` | `double` | Pa | 420 | Reference pressure q |
| `ImportanceFactorIE` | `ImportanceFactorIE_SnowCBN_API` | — | `SNOWCBNSn2010_IE_NORMAL` | Importance factor (this enumeration is shared with the Canadian snow family) |
| `CeMode` | `CeMode_WindCBN_API` | — | `WindCBNWd2010_CE_C_MANUAL` | How Ce is obtained |
| `Ce`, `Cei`, `Ct` | `double` | — | 1.0 | Exposure factors (external, internal) and topographic factor |
| `Cg`, `Cgi` | `double` | — | 2.5, 2.0 | Gust effect factors, external and internal |
| `HeightOfStructureBase` | `double` | m | 0 | Height of the structure base above the ground |

Also: `ExternalPressureArticleUsedForLowBuildings`.

### 12.3 Wind IBC / ASCE 7-22

`LoadCaseFamily_WindIBC` has three blocks, all optional: `BasePressure`, `AutoGeneration` (same four switches as
[12.2](#122-wind-cbn)) and `Building2D`. `LoadCase_WindIBC.EffectParameters` is described in
[12.1](#121-wind-en-1991-1-4).

#### `BasePressureWindIBC`

| Property | Type | Unit | Default | Description |
|---|---|---|---|---|
| `V` | `double` | m/s | 67 | Basic wind speed |
| `ExposureCategory` | `ExposureCategory_WindIBC_API` | — | `WINDIBCWd2015_EXPOSURE_B` | Exposure `B`, `C` or `D` (`WINDIBCWd2015_EXPOSURE_B`, `_C`, `_D`) |
| `RiskCategory` | `RiskCategory_WindIBC_API` | — | `CLIMATIC_IBC2015_RISK_I` | `CLIMATIC_IBC2015_RISK_I` to `CLIMATIC_IBC2015_RISK_IV` |
| `Kzt`, `Ke`, `Ri` | `double` | — | 1.0 | Topographic factor, ground elevation factor, reduction factor |
| `Kd`, `DGust` | `double` | — | 0.85 | Directionality factor, gust effect factor |
| `WindDesign` | `int` | — | 0 | Wind design method: `0` = Chapter 28 envelope, low-rise building; `1` = scaffolding. Send the integer: there is no enumeration |
| `TorsionalLoadCases` | `bool` | — | `false` | Generate the torsional load cases. `HeightOfStructureBase` (m, default 0) is also available |

### 12.4 Snow EN 1991-1-3 (NF EC1)

`LoadCaseFamily_Snow_1991_1_3` has four properties, all optional: `EurocodeParameters` (main table below),
`ProjectSituation` (`SnowExpFall`, `SnowExpAcc`: exceptional fall and accumulation, `false` by default),
`GreenhouseParameters` (`ExpCoeffCe`, `GreenhouseClass`, `SurfaceMaterialCm`; EN 13031-1) and `SnowLoadCategory`.

#### `EurocodeGeneralParametersSnowNF_EC1`

| Property | Type | Unit | Default | Description |
|---|---|---|---|---|
| `PressureEuGen` | `double` | Pa | 450 | Characteristic snow load on the ground sk |
| `ExceptCoefEuGen` | `double` | Pa | 0 | Exceptional snow load Sad |
| `CoefficientExpEuGen` | `ExposureCoefficient_SnowNF_EC1_API` | — | `GRCG_SNOWNF_NE_1991_1_3_UNDER_WIND_08` | Exposure coefficient Ce (see below) |
| `CoefficientTermicEuGen` | `double` | — | 1.0 | Thermal coefficient Ct |
| `AltitudeEuGen` | `double` | m | 0 | Altitude of the site |
| `PeriodN` | `int` | years | 50 | Return period N |
| `PressureEuGenSn` | `double` | Pa | 450 | Snow load for N years Sn |
| `ExceptCoefEuGenSAdn` | `double` | Pa | 0 | Exceptional snow load for N years SAdn |

Also: `VariationCoeffV`, `AdjustFactorOption`, `AdjustFactor`, `ComputingPresure_n_years`.

`CoefficientExpEuGen`: `GRCG_SNOWNF_NE_1991_1_3_UNDER_WIND_08` is Ce = 0.8 (windswept),
`GRCG_SNOWNF_NE_1991_1_3_NORMAL_TOPO_1` is 1.0, `GRCG_SNOWNF_NE_1991_1_3_OVER_WIND_12` is 1.2 (sheltered; the
name is historical).

> **Warning:** `GRCG_SNOWNF_EC1_COEXP_OTHER` gives `Ce = 0` under EN 1991-1-3: only the greenhouse standards
> (EN 13031) read `GreenhouseParameters.ExpCoeffCe`. Do not use it for an ordinary snow family. When you update a
> family with this value, send `GreenhouseParameters.ExpCoeffCe` in the same request.

`SnowLoadCategory` (`SnowLoadCategory_API?`, default `eSnowZoneUnder1000`) is a property of the snow families
(EN, CBN and IBC) and of their load cases. It selects the combination coefficients psi of the snow load:
`eSnowZoneNord` (Scandinavia), `eSnowZoneOver1000` (above 1000 m), `eSnowZoneUnder1000` (at or below 1000 m) or
`eSnowZoneUserDefined`.

#### `LoadCase_Snow_1991_1_3`

`EffectParameters` (`SnowEffectNF_EC1Parameters`) has `TypeOfLoadingsCase` (`NORMAL_SNOW`, `DRIFTED_SNOW` or
`ACCIDENTAL_SNOW`; default `NORMAL_SNOW`), `IsAutomatic` (the loads are generated automatically) and `IsUpdated`
(internal flag, leave it `false`). `SnowLoadCategory` sets the situation of this load case.

### 12.5 Snow CBN

`LoadCaseFamily_SnowCBN` (Canadian code) has `Implantation`, `Building2D` (building length and portal position)
and `SnowLoadCategory` (default `eSnowZoneUnder1000`, see [12.4](#124-snow-en-1991-1-3-nf-ec1)), all optional.

#### `ImplantationSnowCBN`

| Property | Type | Unit | Default | Description |
|---|---|---|---|---|
| `Ss50` | `double` | Pa | 2600 | Ground snow load Ss, 1-in-50 years |
| `Sr50` | `double` | Pa | 400 | Associated rain load Sr, 1-in-50 years |
| `Cw` | `WindExposureCw_SnowCBN_API` | — | `SNOWCBNSn2010_CW_1` | Wind exposure coefficient: 1.0 (`SNOWCBNSn2010_CW_1`), 0.75 (`SNOWCBNSn2010_CW_075`) or 0.5 (`SNOWCBNSn2010_CW_05`) |
| `ImportanceFactorIE` | `ImportanceFactorIE_SnowCBN_API` | — | `SNOWCBNSn2010_IE_NORMAL` | Importance factor |

Also: `Zone`, `ZoneHash` (location zone).

`LoadCase_SnowCBN.EffectParameters` (`EffectParametersSnowCBN`) has the properties of
`SnowEffectNF_EC1Parameters` ([12.4](#124-snow-en-1991-1-3-nf-ec1)).

### 12.6 Snow IBC / ASCE 7-22

`LoadCaseFamily_SnowIBC` has `Implantation`, `Building2D` (building length and portal position) and
`SnowLoadCategory` (default `null`, see [12.4](#124-snow-en-1991-1-3-nf-ec1)), all optional.

#### `ImplantationSnowASCE722`

| Property | Type | Unit | Default | Description |
|---|---|---|---|---|
| `Altitude` | `double` | m | 0 | Altitude of the site |
| `Pg` | `double` | Pa | 2395 | Ground snow load pg |
| `W2` | `double` | — | 0.5 | Winter wind parameter W2, from 0 to 1 |
| `TerrainCategory` | `TerrainCategory_SnowASCE722_API` | — | `SNOWIBCSn2015_TERRAIN_B` | Terrain category (`SNOWIBCSn2015_TERRAIN_B`, `..._C`, `..._D`, above the tree line, Alaska without trees) |
| `Exposure` | `Exposure_SnowASCE722_API` | — | `SNOWIBCSn2015_EXPOSURE_PARTIALLY` | Roof exposure: fully, partially or sheltered |
| `Ct` | `ThermalFactorCt_SnowASCE722_API` | — | `SNOWIBCSn2015_CT_10` | Thermal factor: 1.0 (`SNOWIBCSn2015_CT_10`), 1.1, 1.2, 1.3 or 0.85 (`SNOWIBCSn2015_CT_085`) |
| `RiskCategory` | `RiskCategory_SnowASCE722_API` | — | `CLIMATIC_IBC2015_RISK_I` | `CLIMATIC_IBC2015_RISK_I` to `CLIMATIC_IBC2015_RISK_IV` |

Also: `SnowZone` (`GRCG_ASCE_ZONE_0_OTHER` is the only value). The full value names of these enumerations
are in [Section 13](#13-enumerations-reference).

`LoadCase_SnowIBC.EffectParameters` (`SnowEffectASCE722Parameters`) has the properties of
`SnowEffectNF_EC1Parameters` ([12.4](#124-snow-en-1991-1-3-nf-ec1)).

### 12.7 Seismic EN 1998-1

`LoadCaseFamily_SeismicEN_1998_1` has these properties, all optional: `Implantation`, `Structure`, `Method` and
`ResponseSpectrumDefinitions` (below).

#### `ImplantationSeismicEN1998`

| Property | Type | Unit | Default | Description |
|---|---|---|---|---|
| `SpectrumType` | `SpectrumType_API` | — | `SPECTRE_DESIGN_1` | `SPECTRE_ELASTIC_1`, `SPECTRE_DESIGN_1`, `SPECTRE_ELASTIC_2`, `SPECTRE_DESIGN_2` |
| `AgRg` | `double` | g | 0.08 | Design ground acceleration ag·γI, as a fraction of g (not m/s²) |
| `SoilClass` | `SoilClassEN1998_API` | — | `C` | Ground type `A` to `E` |
| `S` | `double` | — | 0 | Soil factor S |
| `TbImposedValue`, `TcImposedValue`, `TdImposedValue` | `double` | s | 0 | Periods Tb, Tc, Td |

Also, for one annex only: `ST_CoefAmplifTopo` and `ModifiedConstruction` (France); `Contribution_factor_K`,
`Vs30`, `Longitude` and `Latitude` (Spain); `AmplificationFactorF0`, `PeriodTcStar` and `TopographicCategory`
(NTC 2008 / 2018).

The schema describes `S`, `Tb`, `Tc` and `Td` as calculated from the soil class, with a default of `0`. A block is
applied whole, so these four are written as you send them.

> **Note:** After creating a family, read it back with `GetInformationalElementsObject` and check the spectrum
> values that the model holds.

#### `StructureSeismicEN1998` and `CalculationMethodEN1998`

| Property | Type | Default | Description |
|---|---|---|---|
| `Structure.ImportanceCategory` | `ImportanceCategoryEN1998_API` | `CAT_II` | Importance class `CAT_I` to `CAT_IV` (France annex only) |
| `Structure.CoefGammaI` | `double` | 1.0 | Importance factor γI |
| `Structure.CoefQHoriz`, `CoefQHorizY`, `CoefQVert` | `double` | 1.5 | Behaviour factor q: horizontal X, horizontal Y, vertical |
| `Structure.Correction`, `CoefBeta` | `bool`, `double` | `false`, 0.2 | Apply the lower-bound correction, and its factor β |
| `Structure.DuctilityClass` | `DuctilityClassEN1998_API` | `MEDIUM` | `LOW`, `MEDIUM`, `HIGH` (NTC 2008 / 2018 and some annexes) |
| `Method.CalculMethod` | `CalculMethod_API` | `CQC` | Combination of the modal responses: `SRSS`, `CQC`, `ABS` |
| `Method.ResidualMode` | `bool` | `false` | Include the residual mode |

`CalculationMethodCBN` and `CalculationMethodIBC` have the same two properties and defaults.

#### Response spectrum curves

`ResponseSpectrumDefinitions` (`SpectrumDefinitions`) is a property of every seismic family, the generic one
included. It carries the spectra as curves: `HorizontalSpectrumX`, `HorizontalSpectrumY` and `VerticalSpectrum`.
Each is a `Function` (`Name`, `Points` of `ValueX` and `ValueFX`; see `swagger.json`). A curve that is `null` is
not written.

#### `LoadCase_SeismicEN_1998_1`

| Property | Type | Default | Description |
|---|---|---|---|
| `Direction.DirectionChoice` | `DirectionChoiceEN1998_API` | `X` | Direction of the effect: `X`, `Y` or `Z` (`XY` is applied as `X`) |
| `Direction.SignChoice` | `SignChoice_API` | `UNSIGNED` | Sign of the results: `UNSIGNED`, `SIGNED_PREPONDERANT_MODE`, `SIGNED_SELECTED_MODE` |
| `Direction.Mode` | `int` | 1 | Mode number used to sign the results, with `SIGNED_SELECTED_MODE` |
| `Ductility.DuctilityDetermination` | `DuctilityDeterminationEN1998_API` | `Q_IMPOSE` | `Q_CALCULATED` or `Q_IMPOSE` |
| `Ductility.DuctilityCoefficient` | `double` | 1.0 | Behaviour factor q, from 1.0 to 10.0 |

```csharp
var seismic = new LoadCase_SeismicEN_1998_1
{
    Name             = "SX+",
    LoadCaseFamilyID = new EID { Value = seismicFamilyId },
    Direction = new DirectionConfigEN1998
    {
        DirectionChoice = DirectionChoiceEN1998_API.X,
        SignChoice      = SignChoice_API.SIGNED_PREPONDERANT_MODE
    },
    Ductility = new DuctilityConfigEN1998
    {
        DuctilityDetermination = DuctilityDeterminationEN1998_API.Q_IMPOSE,
        DuctilityCoefficient   = 1.5
    }
};
client.CreateInformationalElement(seismic);
```

### 12.8 Seismic CBN

`LoadCaseFamily_SeismicCBN` (Canadian code) has `Implantation`, `Structure`, `Method` (`CalculMethod` and
`ResidualMode`, as in [12.7](#127-seismic-en-1998-1)) and `ResponseSpectrumDefinitions`, all optional. The
spectral accelerations are fractions of g.

#### `ImplantationSeismicCBN`

| Property | Type | Unit | Default | Description |
|---|---|---|---|---|
| `Sa02`, `Sa05`, `Sa10`, `Sa20` | `double` | g | 0 | Spectral acceleration Sa(0.2), Sa(0.5), Sa(1.0), Sa(2.0) |
| `SiteClass` | `SiteClass_API` | — | `C` | Site class `A` to `F` |
| `Fa`, `Fv` | `double` | — | 0 | Site coefficients, short period and 1 s period |
| `IeFaSa02` | `double` | g | 0 | IE·Fa·Sa(0.2) (CBN 2005, 2015); IE·S(0.2) = max(Sa(0.2), Sa(0.5))·IE (CBN 2020) |
| `ImportanceFactorIE` | `ImportanceFactorIE_API` | — | `IE_1_0` | `IE_0_8`, `IE_1_0`, `IE_1_3`, `IE_1_5` |

Also: `ZoneHash`; `Sa50`, `Sa100` and `Pga` (CBN 2015 and 2020 only); `Sa02_X450`, `Sa05_X450`, `Sa10_X450`,
`Sa20_X450`, `IeS10` and `SeismicCategory` (`SC1` to `SC4`; CBN 2020 only).

> **Note:** The defaults of `Fa`, `Fv` and `IeFaSa02` are `0`. A block is applied whole, so these values are
> written as you send them. Send them together with `SiteClass`, then read the family back to check the values
> the model holds.

`StructureSeismicCBN`: `TotalHeight` (m), `FloorsNo`, `RegularStructure` (default `true`), `SfsrCheck`,
`AccidentalTorsion`, `ApplyDuctilityEffects` (default `true`).

#### `LoadCase_SeismicCBN`

`Direction` (`DirectionSeismicCBN`) has `DirectionChoice`, `SignChoice` and `Mode` with the same values and
defaults as in [12.7](#127-seismic-en-1998-1) (`XY` is applied as `X`). The other blocks:

| Property | Type | Default | Description |
|---|---|---|---|
| `StructureType.TypeStructure` | `TypeStructure_API` | `StructureResistMomentFrame` | Structural system (moment frame, shear wall, braced frame, dual system, other) |
| `StructureType.TypeSRFS` | `MaterialSRFS_API` | `Concrete` | Material of the SFRS: `Concrete`, `Steel`, `Masonry`, `Timber` |
| `StructureType.SfrsConcrete`, `SfrsSteel`, `SfrsMasonry`, `SfrsTimber` | one enumeration per material | `DuctileMomentResistingFrame` (steel, concrete), `DuctileShearWall` (masonry, timber) | SFRS type for the material chosen in `TypeSRFS` |
| `DirectionVector.X`, `Y`, `Z` | `double` | 1, 0, 0 | Components of the direction vector (dimensionless) |
| `DuctilityFactors.Rd`, `Ro` | `double` | 1.0 | Ductility-related and overstrength-related force modification factors |
| `DuctilityFactors.Calibration`, `Torsion` | `bool` | `true`, `false` | Calibration flag, and consider torsion |
| `DuctilityFactors.L` | `double` | 0 | Parameter L (CBN 2015 and 2020 only) |

### 12.9 Seismic IBC

`LoadCaseFamily_SeismicIBC` has `Implantation`, `Structure`, `Method` (`CalculMethod` and `ResidualMode`, as in
[12.7](#127-seismic-en-1998-1)) and `ResponseSpectrumDefinitions`, all optional. The spectral accelerations are
dimensionless (fractions of g).

#### `ImplantationSeismicIBC`

| Property | Type | Unit | Default | Description |
|---|---|---|---|---|
| `Ss`, `S1` | `double` | — | 0 | Mapped spectral acceleration, short periods and 1 s period |
| `SiteClass` | `SiteClassIBC_API` | — | `D` | Site class `A` to `F` |
| `Fa`, `Fv` | `double` | — | 0 | Site coefficients, short period and 1 s period |
| `T0`, `Ts`, `Tl` | `double` | s | 0 | Transition periods T0 = 0.2·Fv·S1 / (Fa·Ss), Ts = Fv·S1 / (Fa·Ss), and TL |
| `SaT0`, `SaTS`, `SaTL` | `double` | — | 0 | Spectral accelerations at T0, Ts and TL |
| `OccupancyCategory` | `OccupancyCategoryIBC_API` | — | `II` | `I`, `II`, `III`, `IV` |

> **Note:** The schema describes `T0`, `SaT0`, `Ts`, `SaTS`, `SaTL`, `Fa` and `Fv` as calculated, with a default of
> `0`. A block is applied whole, so these values are written as you send them. Read the family back to check
> the values the model holds.

`StructureSeismicIBC`: `TotalHeight` (m, from 0 to 2000), `FloorsNo`, `RegularStructure` (default `true`),
`SfsrCheck`, `AccidentalTorsion`.

#### `LoadCase_SeismicIBC`

`Direction` (`DirectionSeismicIBC`) and `DirectionVector` have the properties and defaults of the CBN ones
([12.8](#128-seismic-cbn)); `XY` is applied as `X`.

| Property | Type | Default | Description |
|---|---|---|---|
| `StructureType.TypeStructure` | `TypeStructureIBC_API` | `StructureResistMomentFrame` | Structural system (moment frame, shear wall, braced frame, dual system, other) |
| `StructureType.TypeSRFS` | `int` | 0 | Index of the SFRS (selected by material) |
| `DuctilityFactors.R`, `Cd` | `double` | 0 | Response modification coefficient R and deflection amplification factor Cd. The schema says they are calculated from the SFRS |
| `DuctilityFactors.Calibration`, `Torsion` | `bool` | `true`, `true` | Calibration flag, and consider torsion |

---

## 13. Enumerations Reference

The schema has 247 enumerations. The tables list those that you send in a request or test in a response,
grouped by area. A value in parentheses is a short meaning. The complete lists, with a description of every
value, are in `swagger.json` (`components.schemas.<name>.enum`).

- Values travel as names. Send the name, never the number: the numbers are internal storage values, some
  lists are not contiguous, and a generated client numbers its own copy from 0.
- In the C# client the first letter of every enumeration type and member is upper-cased:
  `eMeshState.mesh_available` is `EMeshState.Mesh_available`. Names that already start with a capital do not
  change.
- These spellings are correct, as in the schema: `WebCripgling_G2`, `CombinedMomentAndTrosion_H4`,
  `eComboProjectSituationElsFrecvent`, `eTriCriteriaEnveloppe`, `eOptimizationNonExistant`,
  `WIND_SURPRESSION`, `LOCALIZATION_MAROCCO`.

### Project and environment

| Enumeration | Values |
|---|---|
| `Language_Code` | `eLanguageFrench` · `eLanguageEnglish` · `eLanguageRomanian` · `eLanguageGerman` · `eLanguagePolish` · `eLanguageCzech` · `eLanguageDutch` · `eLanguageRussian` · `eLanguageSpanish` · `eLanguageEnglishUK` · `eLanguageHungarian` · `eLanguageChinese` · `eLanguageTaiwan` · `eLanguageJapanese` · `eLanguageKorean` · `eLanguageBulgarian` · `eLanguageGreek` · `eLanguageTurkish` · `eLanguageQuebec` · `eLanguageItalian` · `eLanguagePortuguese` · `eLanguagePortugueseBrazilian` · `eLanguageSlovak` · `eLanguageEndMark` |
| `Localization_Code` | `LOCALIZATION_ALGERIA` · `LOCALIZATION_ASIA` · `LOCALIZATION_CANADA` · `LOCALIZATION_CZECH_REPUBLIC` · `LOCALIZATION_EUROPE` · `LOCALIZATION_FRANCE` · `LOCALIZATION_GERMANY` · `LOCALIZATION_MAROCCO` · `LOCALIZATION_POLAND` · `LOCALIZATION_ROMANIA` · `LOCALIZATION_TUNISIA` · `LOCALIZATION_UK` · `LOCALIZATION_US` · `LOCALIZATION_ITALIA` · `LOCALIZATION_SLOVAKIA` · `LOCALIZATION_BULGARIA` · `LOCALIZATION_SPAIN` · `LOCALIZATION_PORTUGAL` |
| `LogVerboseLevel` | `Errors` (only errors) · `ErrorsAndWarnings` (errors and warnings, default) · `AllDetails` (errors, warnings and information) |
| `DiagnosticSeverity` | `Information` · `Warning` · `Error` · `Critical` |

- `DiagnosticSeverity` is ordered by increasing severity.
- `Language_Code`, `Localization_Code` and `LogVerboseLevel` are the values of
  [`Environments`](#311-the-environment-of-a-session).

### Design standards and national annexes

The values of [`DesignCodes`](#311-the-environment-of-a-session). Which annex goes with which standard is
the table in 3.1.1.

| Enumeration | Values |
|---|---|
| `eCombinationsStandard` | `Eurocode` (EN 1990, annex of the localization) · `US` (editions before 2018) · `Canada` · `EurocodeItalia` (NTC 2008 coefficients) · `CanadaNBC2015` · `NTC2018` · `Greenhouse_EN13031_1` (needs the greenhouse licence option) · `US2018` · `CanadaCNB2020` |
| `SeismicDesignCode` | `ps92_2010` (French PS92, 2010 edition) · `p100` · `rps2000` · `en1998_1` (Eurocode 8, the only one with a national annex) · `ps92` (French PS92, original edition) · `rpa99_2003` · `p100_1_2013` · `ntc2008` · `cnb2005` · `ibc2012` · `cnb2015` · `ntc2018` · `ibc2018` · `cnb2020` |
| `ClimaticDesignCode` | `france` (NV65 / N84) · `romania` · `eurocode` (EN 1991) · `ntc2008` · `cbnsw2010` (NBC 2010, labelled CNB2005 in the dialog) · `cbnsw2015` · `ibc2015` · `ntc2018` · `pr_en13031_1` (greenhouses, needs the greenhouse licence option) · `ibc2018` · `cbnsw2020` · `asce_7_22` |
| `ConcreteDesignCode` | `bael91` · `eurocode` (any annex except `italy`) · `aci` · `can_csa` (2014 edition) · `ntc2008` (annex is always `italy`) · `ntc2018` (annex is always `italy`) · `can_csa_2019` (current edition) |
| `SteelDesignCode` | `eurocode` (any annex except `italy`) · `aisc` (AISC 360) · `can_csa_s16_2009` · `ntc2008` · `can_csa_s16_2014` · `ntc2018` · `aisc_2016` · `can_csa_s16_2019` · `can_csa_s16_2024` (refused as an input, calculates as `can_csa_s16_2019`) |
| `TimberDesignCode` | `eurocode` (the only one with a national annex) · `ntc2008` · `ntc2018` · `nds_2018` · `o86_2019` |
| `MasonryDesignCode` | `eurocode` (the only one with a national annex) · `ntc2018` · `cr6_2013` |
| `CompositeDesignCode` | `eurocode` (the only one with a national annex) · `aisc_360_16` |
| `eAiscDesignMethod` | `AllowableStrengthDesign` (ASD) · `LoadAndResistanceFactorDesign` (LRFD) |
| `eCombinationsEurocodeAnnex` | `General` · `France` · `UK` · `Germany` · `Czech` · `Romania` · `Slovakia` · `Poland` · `Spain` · `Portugal` |
| `SeismicEurocodeAnnex` | `france` · `general` · `czech` · `romania` · `germany` · `slovakia` · `spain` · `portugal` |
| `ClimaticEurocodeAnnex` | `france` · `romania` · `general` · `uk` · `germany` · `czech` · `poland` · `slovakia` · `spain` · `portugal` |
| `ConcreteEurocodeAnnex` | `france` · `uk` · `germany` · `romania` · `general` · `czech` · `italy` · `poland` · `slovakia` · `spain` · `portugal` |
| `SteelEurocodeAnnex` | `france` · `romania` · `general` · `uk` · `germany` · `czech` · `italy` · `poland` · `slovakia` · `spain` · `portugal` |
| `TimberEurocodeAnnex` | `france` · `uk` · `germany` · `romania` · `general` · `czech` · `italy` · `poland` · `slovakia` · `spain` · `portugal` |
| `MasonryEurocodeAnnex` | `general` · `czech` · `france` · `poland` · `romania` · `slovakia` · `uk` · `germany` · `portugal` · `spain` |
| `CompositeEurocodeAnnex` | `general` · `france` · `uk` · `romania` · `germany` · `czech` · `poland` · `slovakia` · `spain` · `portugal` |

- `Localization` selects the standards. For the combinations, `LOCALIZATION_US` and `LOCALIZATION_ASIA` give
  `US`, `LOCALIZATION_CANADA` gives `CanadaCNB2020`, `LOCALIZATION_ITALIA` gives `NTC2018`, and every other
  localization gives `Eurocode`.
- A property that is `null` keeps what the localization selected.
- `can_csa_s16_2024` of `SteelDesignCode` is refused as an input. Send `can_csa_s16_2019`.
- No 2nd generation Eurocode member exists in any of these enumerations. `GetEnvironment` returns `null` for
  such a standard.
- `general` is the general Eurocode annex, without national deviations. The other annex values are country
  names.
- `italy` of `ConcreteEurocodeAnnex` is accepted only with `ntc2008` and `ntc2018`, which set it themselves.
  `italy` of `SteelEurocodeAnnex` and `TimberEurocodeAnnex` cannot be written: Italy is a standard there,
  not an annex. `GetEnvironment` can report it.
- `eAiscDesignMethod` (`DesignCodes.AiscMethod`) is accepted when `Combinations` is `US` or `US2018`, or
  `Steel` is `aisc` or `aisc_2016`, and refused otherwise. `null` with a `Localization` means ASD.
- `eCombinationsStandard` is also reported, read-only, by `CombinationsGenerationSettings.Standard`
  ([3.6.3](#363-settings-of-the-automatic-generation)).

### Materials and sections

| Enumeration | Values |
|---|---|
| `MaterialBehaviourType` | `isotropic_behavior` · `orthotropic_behavior` |
| `SectionType_API` | `st_ANGLE` · `st_ANGULAR` · `st_CIRCULAR` · `st_POLYGONAL` · `st_RECTANGULAR` · `st_SQUARE` · `st_TRIANGULAR` · `st_T` · `st_CIRCULAR_TUBE` · `st_RECTANGULAR_TUBE` · `st_SQUARE_TUBE` · `st_I_DISSYMETRIC` · `st_I_SYMETRIC` · `st_OBLONG` · `st_UNEQUAL_ANGLE` · `st_U` · `st_USER` · `st_RECTANGULAR_CHAMFER` · `st_DOUBLE_ANGLE` · `st_T_DISSYMETRIC` · `st_ASTOR` · `st_COMBINED_SECTION` · `st_SQUARE_OPEN_TUBE` · `st_DOUBLE_ANGLE_DIAGONAL` · `st_OVAL_TUBE` · `st_TUBE_IN_TUBE` · `st_OMEGA` · `st_SIGMA` · `st_Z` · `st_CE` · `st_CCF` · `st_CEE_ASYM` · `st_C_ASYM` · `st_L_CF` · `st_ZED` · `st_ZED_IL` · `st_SIGMA_ASYM` · `st_SIGMA_IL` · `st_ZETA` · `st_SPACED_SHAFTS` · `st_DOUBLE_CEE_BACK_TO_BACK` · `st_DOUBLE_CEE_FRONT_TO_FRONT` · `st_DOUBLE_SIGMA_BACK_TO_BACK` · `st_DOUBLE_SIGMA_FRONT_TO_FRONT` · `st_CEE_DOUBLE_EDGE_FOLD` · `st_CEE_DOUBLE_EDGE_FOLD_BACK_TO_BACK` · `st_CEE_DOUBLE_EDGE_FOLD_FRONT_TO_FRONT` · `st_C_EAVE_CF` · `st_CEE_EAVE` · `st_cucu` · `st_UNKNOWN` |
| `SectionOffsetOption_API` | `topleft` · `gauche_centre` · `gauche_bas` · `centre_haut` · `center_alignment` · `centre_bas` · `droite_haut` · `droite_centre` · `droite_bas` · `autre` |
| `SectionRollingType_API` | `Rolled` · `HotRolled` · `ColdRolled` · `Welded` (assigned to a user section) · `ThickWelded` · `RolledColdFormed` · `BendColdFormed` · `Unknown` |
| `UserSectionContourType_API` | `Polygon` (vertices, closed automatically) · `Circle` (centre and radius) |
| `UserSectionProfileType_API` | `Unknown` (shape-dependent checks are skipped) · `Angle` · `UnequalAngle` · `DoubleAngle` · `DoubleAngleDiagonal` · `Circular` · `Rectangular` · `Square` · `ISymmetric` · `IDissymmetric` · `U` · `T` · `TDissymmetric` · `CircularTube` · `RectangularTube` · `SquareTube` · `OvalTube` · `TubeInTube` · `RectangularChamfer` · `SquareOpenTube` |

- `SectionOffsetOption_API`: `gauche` is left, `droite` right, `haut` top, `bas` bottom, `centre` / `center`
  centre, `autre` custom.
- `SectionRollingType_API` is not the same enumeration as `Ec3SectionRollingType` of the results.

### Elements and supports

| Enumeration | Values |
|---|---|
| `LinearElementFEMType_API` | `eLinearElementFEMTypeGeneral` · `eLinearElementFEMTypeCompositeBeam` |
| `GeneralBeamType_API` | `bar` · `beamWStandardBending` · `sbeam` (with shear deformation) · `variablebeam` · `tie` (tension only) · `strut` (compression only) · `cable` · `rigid` · `CompositeBeamSimpleBeam` · `CompositeBeamSbeam` · `CompositeBeamVariable` |
| `PlanarElementType` | `membrane` · `plate` · `shell` · `deformation_plane` · `steeldeck` · `layeredShell` · `slabonsteeldeck` · `planar_no_of_types` (the number of types, not a type) |
| `PlanarMeshDensity` | `global` · `simplified` · `detailed` · `element_size` |
| `PlanarMeshType` | `complete` · `triangulation` · `none` |
| `DetailedMeshSpacingOption` | `uniform` (default; the others use `SpacingFactor`) · `beginning` · `end` · `center` · `both_sides` |
| `ClippingCalculationType_API` | `clipping_auto` · `clipping_imposed` |
| `HaunchPosition_API` | `no_haunch` · `haut_et_bas` (top and bottom) · `haut` (top) · `bas` (bottom) |
| `HaunchSectionType_API` | `identical` · `suivant` (following section) · `precedent` (preceding section) · `imposee` (imposed) |
| `HaunchLengthSizeType_API` | `ratio` (relative) · `valeur_axe_local` (along the local axis) · `valeur_projete` (projected) · `valeur` (absolute) |
| `LinearReleaseType` | `default_release` (Free, Pinned, Fixed: cannot be modified or deleted) · `user_defined_release` (created by the user) |
| `LinearReleaseNonlinearityType` | `none` · `compression_only` · `tension_only` |
| `RigidSupportConstraint` | `fix` (all six degrees of freedom) · `SupportHinge` (translations only) · `other` (restraints from `DegreeOfFreedomRestraints`) |
| `TractionOrCompressionBehaviour` | `compression` · `traction` |
| `PilesRestraintType` | `ePileSupportRestraintTypeFree` · `ePileSupportRestraintTypeElastic` · `ePileSupportRestraintTypeFixed` |
| `AdvancedRestraintType` | `eFree` · `eFixed` · `eElastic` · `eNLActiveForCompression` · `eNLActiveForTension` · `eNLGap` · `eNLHardening` · `eNLDiagram` |
| `FootingTemplate` | `TypeAuto` · `Rectangular_pedestal` · `Rectangular_column` · `Rectangular_column_with_expansion_joint` · `Circular_pedestal` · `Circular_column` · `Circular_column_with_expansion_joint` · `Pocket_on_footing` · `Pocket_on_pedestal` · `Hinged_pocket_on_pedestal` · `Hinged_pocket_on_footing` · `Pocket_fixed_on_footing` |
| `SoilType` | `eSoilType_Peat` · `eSoilType_Mud_clay_good_cohesion` · `eSoilType_Clay_squishy` · `eSoilType_Clay_soft` · `eSoilType_Mud_clay_bad_cohesion` · `eSoilType_Clay_hard` · `eSoilType_Silt` · `eSoilType_Loess` · `eSoilType_Clay_semi_solid` · `eSoilType_Loam_sand_silt_and_clay` · `eSoilType_Clay_solid` · `eSoilType_Sand_loose_round` · `eSoilType_Sand_loose_angular` · `eSoilType_Sand_medium_round` · `eSoilType_Sand_compact_angular` · `eSoilType_Gravel_no_sand` · `eSoilType_Stone` |
| `PunchingPosition` | `Auto` · `Edge` (beta = 1.4) · `Angle` (beta = 1.5) · `Inside` (beta = 1.15) · `Imposed` (your beta value) · `None` (beta = 0) |
| `PunchingShapeType` | `Rectangular` · `Square` · `Circular` |

- `LinearReleaseType` is output only.

### Loads, load cases and families

| Enumeration | Values |
|---|---|
| `eLoadCaseType` | `eADLoadCaseNone` · `eADLoadCaseGravity` · `eADLoadCaseExploitation` · `eADLoadCaseSnow` · `eADLoadCaseWind` · `eADLoadCaseThermic` · `eADLoadCaseSeismic` · `eADLoadCaseAccidental` · `eADLoadCaseTemporal` · `eADLoadCaseOther` · `eADLoadCaseEnvelope` · `eADLoadCaseBuckling` · `eADLoadCaseNonLinear` · `eADLoadCaseModal` · `eADLoadCaseCombination` · `eADLoadCaseTrafficLoads` · `eADLoadCaseSnowAcc` · `eADLoadCasePushOver` · `eADLoadCasePushOverAnalysis` · `eADLoadCaseCraneLoads` · `eADLoadCaseConstructionStagesAnalysis` · `eADLoadCaseWindAccidental` · `eADLoadCaseConstructionActions` · `eADLoadCaseIcing` · `eADLoadCaseWaterActions` · `eADLoadCaseWavesAndCurrents` · `eADLoadCaseGravityWater` |
| `CoordinateSystemForLinearOrPlanarGeometry` | `global_or_user` · `local_frame` · `projected_frame` |
| `CoordinateSystemForPunctualGeometry` | `global_or_user` · `local_frame` |
| `Load_Area_Type` | `CG_LOADAREA_BUILDING` (wall of 75 degrees or more, or roof) · `CG_LOADAREA_PROTRUDING_ROOF` · `CG_LOADAREA_PARAPET` · `CG_LOADAREA_ISOLATED_1_SLOPED_ROOF` · `CG_LOADAREA_ISOLATED_2_SLOPED_ROOF` · `CG_LOADAREA_PANEL` · `CG_LOADAREA_FOR_SCAFFOLDING` · `CG_LOADAREA_SHED_VERTICAL_ROOF` (monopitch sheds, roofs between 75 and 80 degrees) · `CG_LOADAREA_AWNING` · `CG_LOADAREA_VAULTED_ROOFS_AND_DOMES` · `CG_LOADAREA_LOWER_ANGLE_INCLINED_WALL` (lower angle between 45.1 and 75 degrees) · `CG_LOADAREA_FREE_STANDING_WALL` |
| `LoadArea_Opening_type` | `CG_WINDWALL_OPENING_TYPE_CLOSED_NO_OPENINGS` · `CG_WINDWALL_OPENING_TYPE_FULL` · `CG_WINDWALL_OPENING_TYPE_AREA` · `CG_WINDWALL_OPENING_TYPE_AREA_PERCENTAGE` |
| `LoadArea_positioning` | `GRCG_WIND_EXPOSURE_WINDWARD` · `GRCG_WIND_EXPOSURE_LEEWARD` · `GRCG_WIND_EXPOSURE_WINDWARD_SHELTERED` · `GRCG_WIND_EXPOSURE_LEEWARD_SHELTERED` · `GRCG_WIND_EXPOSURE_PARALLEL_RIGHT` · `GRCG_WIND_EXPOSURE_PARALLEL_LEFT` |
| `LoadAreaSpanDirection` | `eFloorDeckLoadSpanDirectionX` · `eFloorDeckLoadSpanDirectionY` · `eFloorDeckLoadSpanDirectionXY` |
| `LoadTransferMethod` | `eLoadTransferMethodFailureLines` · `eLoadTransferMethodFemTransfer` · `eLoadTransferMethodAuto` |
| `Gutter_type` | `CG_GUTTER_EFFECT_NONE` · `CG_GUTTER_EFFECT_STRONG` (gutter on a slope below 3 %) |
| `No_AutoYES_Imposed_Options` | `e_Option_No` · `e_Option_Auto` · `e_Option_UserImposed` |

- `eLoadCaseType` appears in the schema only through `AuxiliaryEnums`, the body of `ExportAdditionalEnums`.
- `Gutter_type` is the gutter effect of the automatic snow load generation (NF EN 1991-1-3/NA, 5.2(6)).

### Climatic and seismic parameters

| Enumeration | Values |
|---|---|
| `PlacementType_Wind_EN1991_1_4_API` | `TERRAIN_CATEGORY_0` · `TERRAIN_CATEGORY_I` · `TERRAIN_CATEGORY_II` · `TERRAIN_CATEGORY_III` · `TERRAIN_CATEGORY_IV` |
| `WindDirection_Wind_EN1991_1_4_API` | `WIND_X_PLUS` · `WIND_X_MINUS` · `WIND_Y_PLUS` · `WIND_Y_MINUS` |
| `InternalPressure_Wind_EN1991_1_4_API` | `WIND_SURPRESSION` · `WIND_DEPRESSION` · `WIND_PRESSURE_COEFF` |
| `LoadingsCaseType_Wind` | `NORMAL_TYPE_WIND` · `ACCIDENTAL_TYPE_WIND` |
| `CalculOfTurbulenceFactorKL_Wind_EN1991_1_4_API` | `WINDEC1_EN1991_1_4_KLFORMULA_CASE1` · `WINDEC1_EN1991_1_4_KLFORMULA_CASE2` · `WINDEC1_EN1991_1_4_KLFORMULA_OTHER` |
| `ComputingProbabilityCoeff_Wind_EN1991_1_4_API` | `eWindComputingProbabilityCoeffByKParameter` · `eWindComputingProbabilityCoeffByAdjustmentFactor` · `eWindComputingProbabilityCoeffImposed` |
| `ComputingType_Wind_EN1991_1_4_API` | `eComputingTypeAuto` · `eComputingTypeImposed` |
| `CorrelationCoefficientType_Wind_EN1991_1_4_API` | `CORREL_AUTO` · `CORREL_IMPOSED` · `CORREL_NO` |
| `CsCdCalcMode_Wind_EN1991_1_4_API` | `CS_CD_AUTO` · `CS_CD_IMPOSED` · `CS_CD_NO` |
| `GreenhouseClass_Wind_EN1991_1_4_API` | `eWindGreenhouseClassA` · `eWindGreenhouseClassB` |
| `ImposedCpeType_Wind_EN1991_1_4_API` | `e_CPE_MODE_default` · `e_CPE_MODE_FORCE` (Cpe,10) · `e_CPE_MODE_1` (Cpe,1) · `e_CPE_MODE_1_10` (Cpe,1 and Cpe,10) |
| `IsoLoadedAreas_Wind_EN1991_1_4_API` | `WINDEC1_ISOLA_EXPOSED_AREA` · `WINDEC1_ISOLA_ISOLATED_ELEMENTS` |
| `PhiType_Wind_EN1991_1_4_API` | `PHI_TYPE_AUTO` · `PHI_TYPE_IMPOSED` |
| `WindEffortType_Wind_EN1991_1_4_API` | `WINDEC1_EFF_TYPE_ISOL_BOTH` · `WINDEC1_EFF_TYPE_ISOL_FRICTION` (friction coefficient Cf) · `WINDEC1_EFF_TYPE_ISOL_PRESSURE` (net pressure coefficient Cp,net) |
| `PitchType_Wind_EN1991_1_4_API` | `PITCH_TYPE_MONO` · `PITCH_TYPE_DUO` · `PITCH_TYPE_POSITIVE_SLOPE` (Cpe of the positive pitch angle) · `PITCH_TYPE_NEGATIVE_SLOPE` (Cpe of the negative pitch angle) · `PITCH_TYPE_MAX_CPE` (per zone, the minimum Cpe of the two) |
| `Wind2DLoadsDeterminationMethod_Wind_EN1991_1_4_API` | `eWind2DRealLoadsInSpan` · `eWind2DLoadsInSectionInPortalPosition` |
| `Wind2DPositioningMode_Wind_EN1991_1_4_API` | `eWind2DPositioningModeValue` · `eWind2DPositioningModeZone` |
| `Wind2DPositioningZone_Wind_EN1991_1_4_API` | `eWind2DPositioningZoneEC1_A_F_G` · `eWind2DPositioningZoneEC1_A_H` · `eWind2DPositioningZoneEC1_B_H` · `eWind2DPositioningZoneEC1_B_I` · `eWind2DPositioningZoneEC1_C_I` |
| `PortalOpeningPosition_Wind` | `CG_WINDEC1_OPENING2D_NONE` · `CG_WINDEC1_OPENING2D_XP` · `CG_WINDEC1_OPENING2D_XM` · `CG_WINDEC1_OPENING2D_YP` · `CG_WINDEC1_OPENING2D_YM` · `CG_WINDEC1_OPENING2D_YP_AND_YM` |
| `Wind_Directions_type` | `DIRECTION_X_PLUS` · `DIRECTION_X_MINUS` · `DIRECTION_Y_PLUS` · `DIRECTION_Y_MINUS` · `GRCG_WIND_DIRECTIONS_NO` (the number of directions, not a direction) |
| `ExternalPressureArticle_WindCBN_API` | `cp_zones_cornersenvelope_using_4_1_7_6__2` · `cp_zones_nocorners_4_1_7_5__2and3` |
| `CeMode_WindCBN_API` | `WindCBNWd2010_CE_A` · `WindCBNWd2010_CE_B` · `WindCBNWd2010_CE_C_MANUAL` |
| `ExposureCategory_WindIBC_API` | `WINDIBCWd2015_EXPOSURE_B` · `WINDIBCWd2015_EXPOSURE_C` · `WINDIBCWd2015_EXPOSURE_D` |
| `RiskCategory_WindIBC_API` | `CLIMATIC_IBC2015_RISK_I` · `CLIMATIC_IBC2015_RISK_II` · `CLIMATIC_IBC2015_RISK_III` · `CLIMATIC_IBC2015_RISK_IV` |
| `LoadingsCaseType_Snow` | `NORMAL_SNOW` · `DRIFTED_SNOW` · `ACCIDENTAL_SNOW` |
| `SnowLoadCategory_API` | `eSnowZoneNord` (psi0 / psi1 / psi2 = 0.70 / 0.50 / 0.20) · `eSnowZoneOver1000` (above 1000 m, 0.70 / 0.50 / 0.20) · `eSnowZoneUnder1000` (at or below 1000 m, 0.50 / 0.20 / 0, default) · `eSnowZoneUserDefined` |
| `AdjustFactorOption_SnowNF_EC1_API` | `e_Option_A_U_auto` · `e_Option_A_U_imposed` |
| `ExposureCoefficient_SnowNF_EC1_API` | `GRCG_SNOWNF_NE_1991_1_3_UNDER_WIND_08` · `GRCG_SNOWNF_NE_1991_1_3_NORMAL_TOPO_1` · `GRCG_SNOWNF_NE_1991_1_3_OVER_WIND_12` (sheltered, Ce = 1.2) · `GRCG_SNOWNF_NE_1991_1_3_COEXP_NORMAL_1` (duplicate of `GRCG_SNOWNF_NE_1991_1_3_NORMAL_TOPO_1`, Ce = 1.0) · `GRCG_SNOWNF_EC1_COEXP_OTHER` (Ce from `GreenhouseParameters.ExpCoeffCe`) |
| `GreenhouseClass_SnowNF_EC1_API` | `GREENHOUSE_CLASS_A` · `GREENHOUSE_CLASS_B` · `GREENHOUSE_CLASS_C` |
| `SurfaceMaterialCm_SnowNF_EC1_API` | `GREENHOUSE_MATERIAL_CM_1_2_PLUS` · `GREENHOUSE_MATERIAL_CM_1_0` (Cm = 1.0) · `GREENHOUSE_MATERIAL_CM_0_8` · `GREENHOUSE_MATERIAL_CM_1_333` (Cm = 1.333) · `GREENHOUSE_MATERIAL_CM_1_2` (Cm = 1.2) |
| `Exposure_SnowASCE722_API` | `SNOWIBCSn2015_EXPOSURE_FULLY` · `SNOWIBCSn2015_EXPOSURE_PARTIALLY` · `SNOWIBCSn2015_EXPOSURE_SHELTERED` |
| `RiskCategory_SnowASCE722_API` | `CLIMATIC_IBC2015_RISK_I` · `CLIMATIC_IBC2015_RISK_II` · `CLIMATIC_IBC2015_RISK_III` · `CLIMATIC_IBC2015_RISK_IV` |
| `SnowZone_ASCE722_API` | `GRCG_ASCE_ZONE_0_OTHER` |
| `TerrainCategory_SnowASCE722_API` | `SNOWIBCSn2015_TERRAIN_B` · `SNOWIBCSn2015_TERRAIN_C` · `SNOWIBCSn2015_TERRAIN_D` · `SNOWIBCSn2015_TERRAIN_ABOVE_TREE_LINE_MOUNTAIN` · `SNOWIBCSn2015_TERRAIN_ALASKA_WO_TREES` |
| `ThermalFactorCt_SnowASCE722_API` | `SNOWIBCSn2015_CT_10` · `SNOWIBCSn2015_CT_11` · `SNOWIBCSn2015_CT_12` · `SNOWIBCSn2015_CT_13` · `SNOWIBCSn2015_CT_085` |
| `ImportanceFactorIE_SnowCBN_API` | `SNOWCBNSn2010_IE_LOW` · `SNOWCBNSn2010_IE_NORMAL` · `SNOWCBNSn2010_IE_HIGH` · `SNOWCBNSn2010_IE_POST_DISASTER` |
| `WindExposureCw_SnowCBN_API` | `SNOWCBNSn2010_CW_1` · `SNOWCBNSn2010_CW_075` · `SNOWCBNSn2010_CW_05` |
| `CalculMethod_API` | `SRSS` · `CQC` · `ABS` |
| `DirectionChoice_API` | `X` · `Y` · `Z` · `XY` |
| `DirectionChoiceEN1998_API` | `X` · `Y` · `Z` · `XY` |
| `SignChoice_API` | `UNSIGNED` · `SIGNED_PREPONDERANT_MODE` · `SIGNED_SELECTED_MODE` |
| `SpectrumType_API` | `SPECTRE_ELASTIC_1` · `SPECTRE_DESIGN_1` · `SPECTRE_ELASTIC_2` · `SPECTRE_DESIGN_2` |
| `SoilClassEN1998_API` | `A` · `B` · `C` · `D` · `E` |
| `TopographicCategoryNTC_API` | `T1` · `T2` · `T3` · `T4` |
| `ImportanceCategoryEN1998_API` | `CAT_I` · `CAT_II` · `CAT_III` · `CAT_IV` |
| `DuctilityClassEN1998_API` | `LOW` · `MEDIUM` · `HIGH` |
| `DuctilityDeterminationEN1998_API` | `Q_CALCULATED` · `Q_IMPOSE` |
| `SiteClass_API` | `A` · `B` · `C` · `D` · `E` · `F` |
| `SeismicCategory_API` | `SC1` · `SC2` · `SC3` · `SC4` |
| `ImportanceFactorIE_API` | `IE_0_8` · `IE_1_0` · `IE_1_3` · `IE_1_5` |
| `TypeStructure_API` | `StructureResistMomentFrame` · `StructureResistShearWall` · `StructureResistBracedFrame` · `StructureResistDualSystem` · `StructureResistOther` |
| `MaterialSRFS_API` | `Concrete` · `Steel` · `Masonry` · `Timber` |
| `SFRSConcrete_API` | `DuctileMomentResistingFrame` · `DuctileWallSystem` · `ModeratelyDuctileMomentResistingFrame` · `ModeratelyDuctileShearWall` · `ConventionalConstruction` |
| `SFRSSteel_API` | `DuctileMomentResistingFrame` · `DuctileBracedFrame` · `ModeratelyDuctileMomentResistingFrame` · `ModeratelyDuctileBracedFrame` · `ConventionalConstruction` |
| `SFRSMasonry_API` | `DuctileShearWall` · `ModeratelyDuctileShearWall` · `ConventionalConstruction` |
| `SFRSTimber_API` | `DuctileShearWall` · `ModeratelyDuctileShearWall` · `ConventionalConstruction` |
| `SiteClassIBC_API` | `A` · `B` · `C` · `D` · `E` · `F` |
| `OccupancyCategoryIBC_API` | `I` · `II` · `III` · `IV` |
| `TypeStructureIBC_API` | `StructureResistMomentFrame` · `StructureResistShearWall` · `StructureResistBracedFrame` · `StructureResistDualSystem` · `StructureResistOther` |

Values that are accepted but applied as another value:

- `InternalPressure_Wind_EN1991_1_4_API`: `WIND_PRESSURE_COEFF` is applied as `WIND_SURPRESSION`.
- `ComputingProbabilityCoeff_Wind_EN1991_1_4_API`: `eWindComputingProbabilityCoeffByAdjustmentFactor` is
  applied as `eWindComputingProbabilityCoeffImposed`.
- `PitchType_Wind_EN1991_1_4_API`: `PITCH_TYPE_MONO` is applied as `PITCH_TYPE_POSITIVE_SLOPE`, and
  `PITCH_TYPE_DUO` as `PITCH_TYPE_NEGATIVE_SLOPE`.
- `GreenhouseClass_SnowNF_EC1_API`: `GREENHOUSE_CLASS_C` is applied as `GREENHOUSE_CLASS_B`.
- `SurfaceMaterialCm_SnowNF_EC1_API`: `GREENHOUSE_MATERIAL_CM_0_8` is applied as `GREENHOUSE_MATERIAL_CM_1_0`.
- `DirectionChoice_API` and `DirectionChoiceEN1998_API`: the seismic effect takes one direction, so `XY` is
  applied as `X`.

> **Warning:** `GRCG_SNOWNF_EC1_COEXP_OTHER` gives Ce = 0 under EN 1991-1-3, because only the EN 13031
> greenhouse standards read `ExpCoeffCe`.

### Combinations and their generation

The enumerations of [Section 10.3](#103-combinations) and of the combination generation settings
([3.6](#36-model--combinations)). The combination standards are under [Design standards and national
annexes](#design-standards-and-national-annexes).

| Enumeration | Values |
|---|---|
| `eCombinationType` | `eComboProjectSituationEluStrgeo` · `eComboProjectSituationEluEqu` · `eComboProjectSituationElsCharacteristic` · `eComboProjectSituationElsFrecvent` · `eComboProjectSituationElsQuasiPermanent` · `eComboProjectSituationELUA` · `eComboProjectSituationElsA` · `eComboProjectSituationNone` |
| `eDesignVerificationState` | `Active` (default) · `Inactive` (skipped; the combination keeps its coefficients) |
| `eConcreteCombinationType` | `Uls` (default for ULS) · `UlsAccidental` (default for accidental situations) · `Sls` (default for SLS) · `None` (not checked) |
| `eConcreteLoadDuration` | `ShortTerm` (under 1 h, default for accidental) · `MediumTerm` (1 to 24 h) · `LongTerm` (over 24 h, default otherwise) · `None` |
| `eDeflectionCriterion` | `FirstCriterion` (default) · `SecondCriterion` · `BothCriteria` (Eurocode 3 only; read as "no criterion" under other codes) · `None` (not used under other codes) |
| `eTimberCombinationType` | `Uls` (default) · `Sls` · `UlsAccidental` |
| `eTimberLoadDuration` | `Permanent` (over 10 years, default) · `LongTerm` (6 months to 10 years) · `MediumTerm` (1 week to 6 months) · `ShortTerm` (under 1 week) · `Instantaneous` |
| `eTimberDeflectionCriterion` | `FirstCriterion` (default) · `SecondCriterion` · `None` |
| `eCombinationsGenerationMode` | `Simplified` (the Simplified Comb. workflow) · `DetailedMatrix` (the Detailed Comb. workflow) |
| `eEurocodeCombinationEquation` | `Equation_6_10` (one combination per leading action) · `Equation_6_10a_6_10b` (reduced permanent action) |
| `ePermanentLoadSituation` | `Unfavorable` (default) · `Favorable` · `FavorableAndUnfavorable` · `UserDefined` (gamma values as sent; the only case in which they are writable) |
| `eSlsCombinationSituation` | `Characteristic` · `Frequent` · `QuasiPermanent` |
| `eExploitationCategory` | `CategoryA` · `CategoryB` · `CategoryC` · `CategoryD` · `CategoryE` · `CategoryF` · `CategoryG` · `CategoryH` · `CategoryI` · `CategoryJ` · `UserDefined` (psi0 / psi1 / psi2 as sent) |
| `eThermalLoadCategory` | `Standard` (psi values of the standard) · `UserDefined` (psi values as sent) |
| `eWindLoadCategory` | `Standard` (psi values of the standard) · `UserDefined` (psi values as sent) |
| `eCanadaImportanceCategory` | `Low` (Is 0.80, Iw 0.80) · `Normal` (Is 1.00, Iw 1.00) · `High` (Is 1.15, Iw 1.15) · `PostDisaster` (Is 1.25, Iw 1.25) |
| `eUSLiveLoadFactorF1` | `PublicAssembly` (f1 = 1.0) · `Others` (f1 = 0.5) |
| `eUSSnowLoadFactorF2` | `RoofNotShedding` (f2 = 0.7) · `RoofOthers` (f2 = 0.2) |
| `eGreenhouseConsequenceClass` | `C0` (class A, the lowest) · `C1` · `C2` · `Other` (factors of the settings as sent) |

| Enumeration | Values |
|---|---|
| `e2ndGenCombinationEquation` | 3 values: 2nd generation, not released. See `swagger.json` |
| `e2ndGenConsequenceClass` | 4 values: 2nd generation, not released. See `swagger.json` |
| `eCombinationsActionFamily` | 10 values: 2nd generation, not released. See `swagger.json` |
| `ePermanentLoadSituation2ndGen` | 6 values: 2nd generation, not released. See `swagger.json` |

- `eEurocodeCombinationEquation` is offered only by the General, Poland, Czech and Slovakia annexes. For the
  other annexes the annex fixes the equation.
- `eExploitationCategory` lists the general categories (UK, German, Polish, Romanian and Slovak annexes).
  The French annex has thirteen (A, B, C, D, E1, E2a, E2b, E2c, F, G, H, K, K1, then user defined); the
  Czech annex stops at H, so `CategoryI` and `CategoryJ` do not exist there. A value that the active annex
  does not have is refused.
- `eConcreteCombinationType`, `eConcreteLoadDuration` and the timber enumerations are the limit state and
  duration used by the design engines.

#### `eCombinationDesignCalculation`

The design (Expert) calculations a combination takes part in. Used as a list in
`Combination.DesignCalculations`.

| Value | Meaning |
|---|---|
| `ConcreteDesign` | Reinforced concrete design |
| `SteelProfiles` | Steel member resistance and stability |
| `SteelConnections` | Steel connections |
| `SteelDeflections` | Steel deflections |
| `SteelFire` | Steel fire resistance (Eurocode 3 only) |
| `TimberDeflections` | Timber deflections |
| `TimberProfiles` | Timber member strength |
| `TimberFire` | Timber fire resistance |
| `PreCompositeDeflections` | Composite beam deflections, construction stage (steel section alone) |
| `PostCompositeDeflections` | Composite beam deflections, final stage (composite section) |
| `PreCompositeProfiles` | Composite beam strength, construction stage |
| `PostCompositeProfiles` | Composite beam strength, final stage |

### General settings (steel calculation, work space)

The enumerations of the model settings ([Section 3.7](#37-model--general-settings)).

| Enumeration | Values |
|---|---|
| `eStructureWorkspace` | `planeWorkspace` (2D in the global XZ plane) · `gridWorkspace` (2D in the XY plane, loaded along Z) · `spaceWorkspace` (3D, needs the 3D licence module) |
| `eSteelCalculationType` | `eTypeDeCalculTractionCompression` · `eTypeDeCalculComposedBending` · `eTypeDeCalculBiaxialBending` |
| `eSteelBucklingLengthMethod` | `eMethodBucklingRoA_RoB` (CM66 only) · `eMethodBucklingKa_Kb` (also offered under the Eurocode) · `eMethodBucklingN1_N2` (eta1 / eta2, default of a Eurocode model) · `eMethodBucklingGa_Gb` (AISC and CAN/CSA only) · `eMethodBucklingGu_Gl` · `eMethodBucklingNone` |
| `eSteelBucklingLengthOption` | `elementLength` · `imposed` (default of a new model) · `neglected` |
| `eSteelImperfectionGeneration` | `eImperfectionGenerationForGoverningComb` (default of a Eurocode model) · `eImperfectionGenerationForAllComb` · `eImperfectionGenerationUndefined` (read-only in practice) |
| `eSteelPlateBucklingMethod` | `eMethodWebBucklingEffectiveCrossSections` · `eMethodWebBucklingReducedStresses` (not offered by the Czech and Slovak annexes) · `eMethodWebBucklingUndefined` (no option selected) |
| `eSteelSafetyFactorsOption` | `eSafetyFactorsAuto` (default; a gamma value sent is dropped with a warning) · `eSafetyFactorsImposed` (gamma values stored as sent) |
| `eSteelOptimizationMode` | `eOptimizationParElements` · `eOptimizationParSections` · `eOptimizationParRole` · `eOptimizationParName` · `eOptimizationParSystems` · `eOptimizationNonExistant` (refused in a request) |
| `eSteelOptimizationSearchRange` | `eProfileOptimizationSearchRangeAuto` · `eProfileOptimizationSearchRangeAutoWithMaxLimit` |
| `eSteelProfileOrderingCriterion` | `eProfileOptimizationOrderedByHeight` · `eProfileOptimizationOrderedByWidth` · `eProfileOptimizationOrderedByIyInertia` · `eProfileOptimizationOrderedByIzInertia` · `eProfileOptimizationOrderedByWeight` |
| `eSteelProfileSortCriterion` | `eTriCriteriaDeflections` · `eTriCriteriaWorkRatio` · `eTriCriteriaEnveloppe` (default of a Eurocode model) |

| Enumeration | Values |
|---|---|
| `eSteelInteractionFactorMethod` | 2 values: 2nd generation Eurocode 3 verification options, not released. See `swagger.json` |

- A request that names a buckling length method the active steel design code does not offer is refused.
  Under the Eurocode only `eMethodBucklingKa_Kb` and `eMethodBucklingN1_N2` are offered.

#### `eModelSettingsCategory`

The group of settings that `GetModelSettings` and `ResetModelSettings` name. It is not the `$type` of the
settings object.

| Value | Meaning |
|---|---|
| `None` | Not a settings group. It is the value of an omitted parameter, and a request that carries it is refused |
| `CombinationsGeneration` | Settings of the automatic combination generation |
| `SteelEc3Calculation` | General options of the Eurocode 3 steel design calculation |
| `SteelAiscCalculation` | General options of the steel design calculation to a North American code (AISC 360, AISC 360-16, four CAN/CSA S16 editions) |
| `Tolerances` | Model tolerances |
| `Structure` | Work space of the structure (*Structure* dialog) |

### Expert design templates

The enumerations of the [Expert design templates](#106-expert-design-templates). The C# client upper-cases
the first letter: `class_4` is `Class_4`. `SteelDesignCode` is listed under [Design standards and national
annexes](#design-standards-and-national-annexes).

| Enumeration | Values |
|---|---|
| `DesignTemplateMaterialType` | `concrete` · `steel` (the only family writable through the API) · `timber` · `composite` · `rigid` · `undefined_material` (the built-in default template (*None*)) |
| `DesignTemplateElementType` | `linear` (the only element type writable through the API) · `planar` · `undefined_element` (the built-in default template (*None*)) |
| `SteelSectionClass` | `auto_class` · `class_1` · `class_2` · `class_3` · `class_4` |
| `SteelSectionClassAISC` | `auto_class` · `compact` · `noncompact` · `slender_element` |
| `SteelManufacturingQuality` | `class_a` (Q = 40) · `class_b` (Q = 25) · `class_c` (Q = 16, default) |
| `SteelExecutionClassType` | `global_execution_class` (model-wide class) · `imposed_execution_class` (uses `ExecutionClassValue`) |
| `SteelExecutionClass` | `exc1` · `exc2` · `exc3` · `exc4` · `not_set` |
| `SteelFamilyOfElement` | `family_n` · `family_a` · `family_b` · `family_c` · `family_d` · `family_d_plus` · `family_e` · `family_e_plus` |
| `SteelDeflectionReferenceLengthOption` | `aligned` (chord length, default) · `imposed` (the only option with an editable length) · `projected` (projected length) |
| `SteelDeflectionLocation` | `extremity` · `span` · `envelope` (default) |
| `SteelBucklingLengthMode` | `auto_length` (default) · `imposed` · `calculated_from_element` · `calculated_from_mesh_size` · `calculated_from_super_element` · `lateral_torsional_buckling_length_upper_flange` · `lateral_torsional_buckling_length_lower_flange` |
| `SteelTorsionalBucklingLengthMode` | `equal_to_lfz` · `equal_to_lfy` · `max_of_lfy_lfz` (default) · `imposed` · `calculated_from_super_element` · `calculated_from_element` · `calculated_from_mesh_size` |
| `SteelBucklingNodesType` | `braced` · `unbraced` |
| `SteelBucklingMuMode` | `auto_mu` · `imposed_mu` |
| `SteelBucklingCurve` | `auto_curve` · `curve_a0` · `curve_a` · `curve_b` · `curve_c` · `curve_d` |
| `SteelUniformMomentFactorMode` | `auto_factor` · `imposed` |
| `SteelLtbEndRestraint` | `no_restraint` · `pinned` (default) · `fixed` |
| `SteelLtbIntermediateRestraintType` | `pinned` · `fixed` |
| `SteelLtbFactorCbMode` | `auto_cb` · `imposed_cb` |
| `SteelLtbRestraintDefinitionType` | `by_number` (count, spread evenly) · `by_abscissa` (abscissae in metres) · `by_ratio` (ratios from 0 to 1) |
| `SteelLtbLoadApplicationPoint` | `neutral_fiber` (default, except under the British annex) · `upper_fiber` · `lower_fiber` · `destabilising` · `stabilising` · `imposed_value` (zg / zj values) |
| `SteelLtbLoadType` | `auto_load_type` (default) · `column` · `simple_beam_with_uniform_loads` · `fixed_beam_with_uniform_loads` · `simple_beam_with_point_load` · `fixed_beam_with_point_load` · `another_case` (imposed C1 / C2 / C3) |
| `SteelLtbCurveMethod` | `general_case` · `rolled_welded_profiles` · `least_favorable` (default) · `most_favorable` · `second_generation_most_favorable` (2nd generation only, do not send) |
| `SteelLtbCriticalMomentMode` | `auto_mcr` · `imposed_mcr` · `from_advanced_stability` (only for a cold-formed section under the 2nd generation Eurocode) |
| `SteelLtbReferenceCriticalMomentMode` | `auto_mcr0` · `imposed_mcr0` · `same_as_mcr` (equal to Mcr) |
| `SteelLtbFCoefficientMode` | `auto_coefficient` · `none` (f = 1.0, default) · `imposed_kc` · `imposed_f` |
| `SteelLtbFactorFMMode` | `auto_factor` · `value_1` (fM = 1.0) · `user_defined` |
| `SteelLocalBowImperfectionEffect` | `flexural_buckling` (default for columns) · `lateral_torsional_buckling` (default for beams) · `max_of_buckling_and_ltb` |
| `SteelImperfectionForceDirectionPivot` | `none` (default) · `use_points` · `use_dof_results` |
| `SteelBeamWeakAxisDirection` | `auto_direction` · `direction_y` · `direction_z` |
| `SteelBeamKMemberImperfectionMode` | `auto_k` · `imposed` |
| `SteelFireExposedFaces` | `four_faces` · `three_faces` |
| `SteelFireEmissivityCoeffType` | `carbon_steel` (0.7, default) · `hot_dip_galvanised` · `imposed` (the only value that uses the companion emissivity) |
| `SteelFireProtectionMode` | `contour_encasement` · `hollow_encasement` |
| `SteelAngleSectionLeg` | `larger_leg` · `smaller_leg` |
| `SteelAngleDiameterMode` | `auto_diameter` · `imposed` |
| `SteelAngleEffectiveSlendernessKMode` | `auto_k` · `imposed_k` |
| `SteelWebStiffenersSides` | `one_side_of_web` · `both_sides_of_web` |
| `SteelWebLongitudinalStiffenersNumber` | `one_stiffener` · `two_stiffeners` |
| `SteelHssDesignThickness` | `reduced` (0.93 under AISC, 0.90 under CAN/CSA, default) · `nominal` (factor 1.00) |
| `SteelCFDSupportCondition` | `fastened` · `unfastened` |
| `SteelCFDFlangeCondition` | `stiffened_or_partially_stiffened` · `unstiffened` |
| `SteelCFDFlangeLoadingType` | `one_flange_loading_or_reaction` · `two_flange_loading_or_reaction` |
| `SteelInitialDeformationShape` | `dominant_eigen_shape` · `parabolic_variation` |
| `SteelNonLinearFEMOffsetPosition` | `neutral_fiber` · `upper_fiber` · `bottom_fiber` · `imposed_value` · `indeterminate` |
| `SteelNonLinearFEMDofType` | `fully_rigid` · `auto_dof` · `hinged` · `custom` · `ty_rx_restraint` · `ty_restraint` · `fully_free` · `indeterminate` |
| `SteelNonLinearFEMDofStatus` | `auto_status` · `rigid` · `free` · `elastic` · `indeterminate` |
| `SteelNonLinearFEMLoadsOffset` | `top_left` · `top_center` · `top_right` · `center_left` · `center` · `center_right` · `bottom_left` · `bottom_center` · `bottom_right` · `user_defined` · `indeterminate` |

- `SteelSectionClass` is used by the Eurocode and CAN/CSA leaves, `SteelSectionClassAISC` by the AISC leaf.
- `SteelManufacturingQuality`, `SteelExecutionClassType`, `SteelExecutionClass` and `SteelFamilyOfElement`
  exist on the Eurocode leaf only. `SteelLtbFactorCbMode` and the `SteelCFD*` and `SteelHssDesignThickness`
  enumerations belong to the North American leaves; `SteelLtbEndRestraint` is used by both; the other
  `SteelLtb*` enumerations are Eurocode only.
- `SteelBucklingNodesType` has no default: `YNodesType` and `ZNodesType` are nullable, and a new template
  takes `braced` under AISC and CAN/CSA and `unbraced` under the Eurocode.
- `SteelNonLinearFEM*`: `indeterminate` means never set. Do not send it.

### Actions, analysis and results

| Enumeration | Values |
|---|---|
| `AD_API_ActionType` | `ClimaticAutoGeneration` (generates the wind and snow loads of the climatic families) |
| `eMeshState` | `mesh_no` (no mesh) · `mesh_available` (complete mesh) · `mesh_incomplete` (part of the model is not meshed; a calculation still runs, so check the mesh diagnostics before trusting results) |
| `eAnalysisModelState` | `NotCreated` (no analysis model) · `Created` (no mesh, no results) · `Meshed` (mesh, no results) · `Calculated` (results are current) · `OutOfDate` (results exist but the model changed) |
| `SteelDesignResultSet` | `Standard` (standard steel engine) · `AdvancedStability` (non-linear FEM engine) · `ColdFormed` (cold-formed engine, EN 1993-1-3) |
| `SteelLinearDesignResultType` | `All` (every family available under the active code) · `Deflection1` · `Deflection2` · `BucklingLengths` · `LateralTorsionalBucklingLengths` · `Stability` (Eurocode 3 only) · `CrossSectionResistance` · `FireResistance` (Eurocode 3 only) · `AdvancedStabilityResistance` (Eurocode 3 only; implies `AdvancedStability`) · `MaxWorkRatio` (Eurocode 3 (1st generation) and North American codes) · `CrossSectionClass` · `FireCrossSectionClass` (Eurocode 3 only) · `FireTemperature` (Eurocode 3 only) · `WebShearBuckling` (Eurocode 3 only) · `CrossSectionInfo` (North American codes) · `ColdFormedResistance` (implies `ColdFormed`) · `ColdFormedDeflection1` (implies `ColdFormed`) · `ColdFormedDeflection2` (implies `ColdFormed`) · `ColdFormedSectionInfo` (cold-formed elements only) · `ProfileOptimisation` · `HaunchSectionProperties` (Eurocode 3 (1st generation) only) · `BucklingLengthCoefficients` (Eurocode 3 only) · `BucklingLengthNodeRestraints` (Eurocode 3 only) · `GlobalSwayImperfectionForces` (Eurocode 3 only) · `LocalBowImperfectionForces` (Eurocode 3 only) |
| `ElementTypeEnum` | `ElementBase` · `ElementLinear` · `ElementPlanar` · `ElementLoadArea` · `ElementMass` · `ElementLoadPunctual` · `ElementLoadLinear` · `ElementLoadPlanar` · `ElementImposedDisplacement` · `ElementRigidPunctualSupport` · `ElementElasticPunctualSupport` · `ElementTCPunctualSupport` · `ElementAdvancedPunctualSupport` · `ElementSinglePile` · `ElementRigidLinearSupport` · `ElementElasticLinearSupport` · `ElementTCLinearSupport` · `ElementAdvancedLinearSupport` · `ElementRigidPlanarSupport` · `ElementElasticPlanarSupport` · `ElementTCPlanarSupport` · `ElementAdvancedPlanarSupport` · `ElementPoint` · `ElementPolyLine` |
| `InformationalElementTypeEnum` | `InfoObjBase` · `LoadCase` · `LoadCaseFamily` · `LoadCaseFamily_Accidental` · `LoadCase_Accidental` · `LoadCaseFamily_DeadLoads` · `LoadCase_DeadLoads` · `LoadCaseFamily_DynamicTemporal` · `LoadCase_DynamicTemporal` · `LoadCaseFamily_LiveLoads` · `LoadCase_LiveLoads` · `LoadCaseFamily_Other` · `LoadCase_Other` · `LoadCaseFamily_PushOver` · `LoadCase_PushOver` · `LoadCaseFamily_Seismic` · `LoadCase_Seismic` · `LoadCaseFamily_SeismicCBN` · `LoadCase_SeismicCBN` · `LoadCaseFamily_SeismicEN_1998_1` · `LoadCase_SeismicEN_1998_1` · `LoadCaseFamily_SeismicIBC` · `LoadCase_SeismicIBC` · `LoadCaseFamily_Snow` · `LoadCase_Snow` · `LoadCaseFamily_Snow_1991_1_3` · `LoadCase_Snow_1991_1_3` · `LoadCaseFamily_SnowCBN` · `LoadCase_SnowCBN` · `LoadCaseFamily_SnowIBC` · `LoadCase_SnowIBC` · `LoadCaseFamily_Thermic` · `LoadCase_Thermic` · `LoadCaseFamily_Wind` · `LoadCase_Wind` · `LoadCaseFamily_Wind_1991_1_4` · `LoadCase_Wind_1991_1_4` · `LoadCaseFamily_WindCBN` · `LoadCase_WindCBN` · `LoadCaseFamily_WindIBC` · `LoadCase_WindIBC` · `Combination` · `LoadCaseFamily_AnalysisBuckling` · `LoadCase_Buckling` · `StructuralSystem` · `LinearReleaseTemplate` · `DesignTemplate` |

- `ElementTypeEnum` filters `GetElementsID`; `InformationalElementTypeEnum` filters the queries of
  informational elements ([3.10](#310-model--query--retrieval)). `SteelDesignResultSet` and
  `SteelLinearDesignResultType` are the request side of `GetDesignResults` ([3.8](#38-model--analysis));
  families that the element has no results for are absent from the response, not an error.
- `InformationalElementTypeEnum.DesignTemplate` returns every [Expert design
  template](#106-expert-design-templates), in pool order, without the built-in *None*. `StructuralSystem`
  returns the root system (*Structure 0*) and all sub-systems at any depth. `LinearReleaseTemplate` returns
  the whole [release pool](#105-linear-release-templates): *Free*, *Pinned*, *Fixed*, then the user-defined
  ones. Identify a template or a release by its EID or name, never by its position in the list.

#### `DesignCalculationType`

The family whose Expert design engine `LaunchDesignCalculation` runs. It is also
`DesignCalculationLicence.Type` in `GetStatus` ([3.11](#311-service--version-status--liveness)).

| Value | Meaning |
|---|---|
| `Concrete` | Reinforced concrete design, to the concrete design code of the model |
| `Steel` | Steelwork design: Eurocode 3, CM66, AISC or CSA, to the steel design code of the model |
| `Timber` | Timber design: Eurocode 5 or North American codes (O86 / NDS), to the timber design code of the model |
| `Composite` | Composite beam design (steel section and concrete slab): Eurocode 4, AISC 360 or CSA S16 |

#### `ResultType`

The kind of analysis result returned by `GetResults` and `ResultsQueryFEM`.

| Value | Meaning |
|---|---|
| `displacement` | Displacements |
| `forces` | Internal forces |
| `stresses` | Stresses |
| `resultantforces` | Resultant forces |
| `generalizedBucklingRes` | Generalized buckling results (lambda) |

**Result vocabularies.** These appear in the results of `GetDesignResults`. You test them in a response and
never send them.

| Enumeration | Values |
|---|---|
| `Ec3CrossSectionClass` | `Auto` · `Class1` · `Class2` · `Class3` · `Class4` · `Unknown` |
| `Ec3ClassLoadingPattern` | `TensionCompression` · `ComposedBending` · `FlexuralBuckling` · `LateralTorsionalBuckling` |
| `Ec3SectionPartType` | `Web` · `Internal` · `Flange` · `None` |
| `Ec3GoverningCheck` | `None` · `Axial` · `ShearY` · `ShearZ` · `BendingYY` · `BendingZZ` · `CombinedBending` · `BendingYYAndAxial` · `BendingZZAndAxial` · `BendingYYAndShearZ` · `BendingZZAndShearY` · `BiaxialBending` · `Torsion` · `Stability` |
| `Ec3ResistanceCheckId` | `Axial` · `ShearY` · `ShearZ` · `BendingYY` · `BendingZZ` · `BendingYYAndAxial` · `BendingZZAndAxial` · `BendingYYAndShearZ` · `BendingZZAndShearY` · `BiaxialBending` · `Torsion` · `ElasticStress` |
| `Ec3FireResistanceCheckId` | `Axial` · `BendingYY` · `BendingZZ` · `ObliqueBending1` · `ObliqueBending2` · `Class4` |
| `ShearBucklingVerificationType` | `None` · `ShearResistance` · `Interaction` · `ReducedStressMethod` |
| `Ec3CylinderLength` | `Short` · `Medium` · `Long` |
| `Ec3FabricationQuality` | `ClassA` · `ClassB` · `ClassC` |
| `DesignAxisDirection` | `Y` · `Z` |
| `SteelMaxWorkRatioComponent` | `Overall` · `Deflections` · `CrossSectionResistance` · `Stability` · `FireResistance` · `FireStability` |
| `Ec3SectionRollingType` | `Unknown` · `Rolled` · `HotRolled` · `ColdRolled` · `Welded` · `ThickWelded` · `ColdFormedRolled` · `ColdFormedBend` |
| `Ec3DeflectionCriterion` | `Criterion1` · `Criterion2` |
| `DeflectionEntity` | `Element` · `SuperElement` |
| `DesignParameterUnitType` | `InequalitySign` · `Force` · `Moment` · `BiMoment` · `Pressure` · `Adimensional` · `LengthFeature` · `LengthDimension` · `LengthElement` · `Area` · `SectionModulus` · `Inertia` · `WarpingConstant` · `Text` · `None` |
| `NorthAmericanSteelCode` | `Aisc360` · `CsaS16_2009` · `CsaS16_2014` · `Aisc360_2016` · `CsaS16_2019` · `CsaS16_2024` |
| `AiscDesignMethod` | `Unknown` · `ASD` · `LRFD` |
| `AiscCrossSectionClass` | `Auto` · `Compact` · `NonCompact` · `SlenderElement` · `Unknown` |
| `CsaCrossSectionClass` | `Auto` · `Class1` · `Class2` · `Class3` · `Class4` · `Unknown` |
| `AiscClassLoadingPattern` | `AxialForce` · `BendingYY` · `BendingZZ` · `CombinedForces` |
| `AiscSectionPartType` | `Web` · `Flange` · `Round` |
| `AiscResistanceCheckId` | `Axial` · `ShearY` · `ShearZ` · `BendingYY` · `BendingZZ` · `Torsion` · `CombinedForces` |
| `AiscResistanceError` | `None` · `NotDone` · `Class` · `Segments` |
| `AiscResistanceWarning` | `None` · `SlendernessRatioAbove200` |
| `AisiVerificationType` | `noVerifStrength` · `AxialTension_D2` · `AxialCompression_E2` · `BendingMoment_F_XX` · `BendingMoment_F_YY` · `Shear_G` · `WebCripgling_G2` · `CombinedMomentAndAxial_H1` · `CombinedMomentAndShear_H2` · `CombinedMomentAndWebCrippling_H3` · `CombinedMomentAndTrosion_H4` |
| `AisiSupportLocation` | `End` · `Interior` |

| Enumeration | Values |
|---|---|
| `Ec3G2Symbol` | 308 values: quantities of a 2nd generation EN 1993 verification. See `swagger.json` |
| `Ec3G2Equation` | 271 values: formulae of such a verification. See `swagger.json` |
| `Ec3G2Message` | 32 values: status notes and warnings of such a verification. See `swagger.json` |
| `Ec3G2VerificationId` | 44 values: design checks of 2nd generation results. See `swagger.json` |
| `Ec3DeflectionVerificationId` | 16 values: key of a 2nd generation deflection verification. See `swagger.json` |
| `Ec3ColdFormedEquation` | 20 values: EN 1993-1-3 formulae applied. See `swagger.json` |
| `AiscFormula` | 32 values: AISC 360 equations applied. See `swagger.json` |
| `CsaFormula` | 22 values: CSA S16 clauses applied. See `swagger.json` |
| `AisiEquation` | 23 values: AISI S100 equations applied. See `swagger.json` |
| `Ec3SectionPartPosition` | 20 values: the plate that a per-part classification belongs to. See `swagger.json` |

- `Ec3ClassLoadingPattern`: `FlexuralBuckling` is the class of the clause 6.3.1 check (shown as *Buckling*
  in Advance Design), and `ComposedBending` is biaxial bending with axial force (*Oblique bending*).
- Every buckling curve of the results, including the lateral-torsional one, uses `SteelBucklingCurve`.
  `auto_curve` there is not a missing value: it is the general case of clause 6.3.2.2, where alpha,LT is
  computed. Advance Design prints it as "Curve -".

### Service

#### `LicenceProtectionType`

The kind of licence the server runs under: `LicenceStatus.ProtectionType` of `GetStatus`
([3.11](#311-service--version-status--liveness)). A new member is a change of the contract.
`DesignCalculationType` is listed under [Actions, analysis and results](#designcalculationtype).

| Value | Meaning |
|---|---|
| `NoLicence` | No licence |
| `Demo` | Demonstration licence |
| `Evaluation` | Evaluation (trial) licence |
| `Education` | Education licence |
| `Commercial` | Commercial licence |
| `Reseller` | Reseller licence |
| `Internal` | Graitec internal licence |
| `HomeUse` | Home-use licence |
| `Unknown` | A kind that this server does not know |

---

## 14. Error Handling & Diagnostics

A call can fail in two ways.

| Kind of failure | How you see it | Examples |
|---|---|---|
| The request was understood but could not be done | HTTP 200. The envelope has `Details.Success = false` and the reason in `Details.Diagnostics` | No project open, invalid geometry, unknown section name, analysis failed |
| The request could not be read | HTTP 400, no envelope. The C# client throws `ApiException` | Missing body, malformed JSON, unknown `$type`, unknown enumeration value |
| The server is shutting down | HTTP 503 with the code `SERVICE_STOPPING`. The C# client throws `ApiException` | Any `/api/Model/*` call after `CloseSession` |

```csharp
try
{
    EIDApiResponse r = client.CreateElement(element);

    foreach (DiagnosticEntry d in r.Details.Diagnostics)
        Console.WriteLine($"[{d.Severity}] {d.Code}: {d.Message}");

    if (!r.Details.Success)
        throw new InvalidOperationException("The element was not created.");

    long id = r.Data.Value;
}
catch (ApiException ex)
{
    // The request did not reach the model: HTTP 400 or 503.
    Console.Error.WriteLine($"HTTP {ex.StatusCode}: {ex.Response}");
}
```

Good practice:

- Open the project with `LogVerbosity = ErrorsAndWarnings` (or `AllDetails` while developing). With
  `Errors`, the warnings are not returned.
- Test `Details.Success` after every call, and `Data` as well when it is a `bool`.
- Read the warnings. Many rules in this guide say "ignored with a warning": the call succeeds, but a value
  you sent was not applied.
- Test `Code`, not `Message`. Codes are stable; messages can change and some are translated.

### Diagnostic codes

Most codes are the name of the operation followed by its outcome:

| Ending | Severity | Meaning |
|---|---|---|
| `..._CREATED`, `_UPDATED`, `_DELETED`, `_RETRIEVED`, `_READ`, `_WRITTEN`, `_COMPLETED`, `_SAVED` | Information | The call succeeded. Only returned with `LogVerbosity = AllDetails` |
| `..._EMPTY_INPUT` on a read, `..._PARTIAL`, `COMBINATIONS_GENERATION_EMPTY` | Warning | The call succeeded with nothing, or with only part of what was asked |
| `..._FAILED`, `_REFUSED`, `_INVALID`, `_NULL_INPUT`, `_UNAVAILABLE`, `_EXCEPTION` | Error | The call failed. `_EXCEPTION` and a `_FAILED` with a generic message mean an internal error: see the server log |
| `ENGINE_ERROR`, `ENGINE_WARNING`, `ENGINE_INFO` | As named | A message of the Advance Design engine itself. These explain most failures |

The codes you are most likely to test:

| Code | Returned by | Meaning |
|---|---|---|
| `PROJECT_NOT_OPEN` | Every `/api/Model/*` call that needs a project | No project is open. Call `NewProject` or `OpenProject` first and check that it succeeded |
| `PROJECT_INVALID_FILENAME`, `PROJECT_INVALID_EXTENSION` | `NewProject`, `OpenProject`, `SaveProjectAs` | The file name is not valid, or does not end in `.fto` |
| `PROJECT_FILE_NOT_FOUND` | `OpenProject` | The file does not exist |
| `PROJECT_FOLDER_NOT_FOUND` | `NewProject`, `SaveProjectAs` | The folder for the new file does not exist |
| `PROJECT_INVALID_ENV` | `NewProject`, `OpenProject` | The `Environments` body is missing |
| `PROJECT_ENVIRONMENT_REFUSED` | `NewProject`, `OpenProject` | A project is already open, or the environment is not valid — see [Section 3.1.1](#311-the-environment-of-a-session) |
| `PROJECT_CREATION_FAILED`, `PROJECT_OPEN_FAILED` | `NewProject`, `OpenProject` | The project could not be created or opened; the other messages say why |
| `ENVIRONMENT_NO_SESSION` | `GetEnvironment` | No project is open |
| `PROJECT_SAVE_FAILED`, `PROJECT_SAVE_AS_FAILED`, `PROJECT_TARGET_EXISTS`, `PROJECT_TARGET_INVALID` | `SaveProject`, `SaveProjectAs` | See [Section 3.1](#31-project-management) |
| `PROJECT_CLOSE_FAILED` | `CloseProject`, `CloseProjectWithoutSave` | The project was closed but not cleanly; changes may be lost |
| `SESSION_CLOSING` | `CloseSession`, `CloseSessionWithoutSave` | Information. The server is going to stop; the message says whether the project is saved |
| `SERVICE_STOPPING` | Any `/api/Model/*` call (HTTP 503) | The server is shutting down |
| `ELEMENT_NULL_INPUT`, `MATERIAL_NULL_INPUT`, `USER_SECTION_NULL_INPUT`, `INFORMATIONAL_ELEMENT_NULL_INPUT`, `COMBINATION_NULL_INPUT`, `MODEL_SETTINGS_NULL_INPUT` | The create, update and settings calls | The body is missing |
| `ELEMENT_DELETE_EMPTY_INPUT`, `DESIGN_TEMPLATE_ASSIGN_EMPTY_INPUT` | `DeleteElements`, `AssignDesignTemplateToElements` | Error. The list of identifiers is empty |
| `ELEMENTS_OBJECT_PARTIAL`, `INFORMATIONAL_ELEMENTS_PARTIAL` | `GetElementsObject`, `GetInformationalElementsObject` | Warning. Some identifiers were of the other kind and were skipped |
| `ELEMENTS_QUERY_MISSING_DISCRIMINATOR`, `ELEMENTS_QUERY_TYPE_NOT_QUERYABLE` | `GetElementsID` | A query without `$type`, or a type that cannot be used as a filter |
| `COMBINATIONS_GENERATION_EMPTY` | `GenerateCombinations` | Warning. Nothing was generated |
| `MODEL_SETTINGS_UNAVAILABLE`, `MODEL_SETTINGS_WRITE_REFUSED`, `MODEL_SETTINGS_RESET_REFUSED` | `GetModelSettings`, `SetModelSettings`, `ResetModelSettings` | See [Section 3.7](#37-model--general-settings) |
| `ANALYSIS_FAILED`, `DESIGN_CALCULATION_FAILED` | `LaunchAnalysis`, `LaunchDesignCalculation` | The calculation did not complete; the engine messages say why |
| `ProcessAction_FAILED` | `ProcessAction` | The action did not complete; the engine messages say why. (This code is written in mixed case) |
| `RESULTS_QUERY_INVALID`, `DESIGN_RESULTS_QUERY_INVALID` | `GetResultsByQuery`, `GetDesignResults` | The query object is not of an accepted type |
| `ANALYSIS_MODEL_STATUS_UNAVAILABLE` | `GetAnalysisModelStatus` | The status could not be read |

---

## 15. Complete C# Usage Examples

The five examples below are complete. Create a .NET 8 console project that references `AD.API.Client` (with
implicit usings on), paste the setup block into `Program.cs`, then paste the examples into the same class.
`Main` calls all five; remove the calls of the examples you do not paste. All values are in SI units:
m, N, N·m, Pa, kg/m³, rad.

### Setup — client, helpers and entry point

```csharp
using AD.API.Client;

internal static class Program
{
    static readonly HttpClient http = new();
    // Default server address. To start a server of your own instead, call
    // CSessionManager.StartNewSession(folderOfTheServer) - it returns the address (Section 2.2).
    static readonly AD_Client client = new("http://localhost:52000", http);

    static void Main()
    {
        try
        {
            Console.WriteLine($"Server version: {client.GetVersion()}");
            string frame = Example1();   // creates the frame project
            Example2();                  // separate project
            Example3(frame);             // reads the frame project
            Example5(frame);             // steel design on the frame project
            Example4(frame);             // adds seismic cases to the frame project: run it last
        }
        finally
        {
            client.CloseSession();       // saves the open project and stops the server
        }
    }

    // Throws when a call failed; prints the warnings and errors the server reported.
    static void Check(ApiResponseDetails details, string call)
    {
        foreach (var d in details.Diagnostics ?? new List<DiagnosticEntry>())
            if (d.Severity != DiagnosticSeverity.Information)
                Console.WriteLine($"  [{d.Severity}] {d.Code}: {d.Message}");
        if (!details.Success)
            throw new InvalidOperationException($"{call} failed.");
    }

    static void Check(BooleanApiResponse response, string call)
    {
        Check(response.Details, call);
        if (!response.Data)
            throw new InvalidOperationException($"{call} returned false.");
    }

    // Checks a create call and returns the identifier of the new object.
    static EID Id(EIDApiResponse response, string call)
    {
        Check(response.Details, call);
        return response.Data;
    }

    // The environment of the session. Send the same one every time you open a project (Section 3.1.1).
    static Environments Env() => new()
    {
        Language = Language_Code.ELanguageEnglish,
        Localization = Localization_Code.LOCALIZATION_FRANCE,   // Eurocodes, French national annexes
        LogVerbosity = LogVerboseLevel.ErrorsAndWarnings
    };

    // A new .fto path in %TEMP%\AD_Examples. The server does not create the folder, so create it here.
    static string ProjectPath(string name)
    {
        string dir = Path.Combine(Path.GetTempPath(), "AD_Examples");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, $"{name}_{Guid.NewGuid():N}.fto");
    }

    // ---- paste Example1 ... Example5 here ----
}
```

- `LOCALIZATION_FRANCE` selects the Eurocode standards with the French annexes. Use the localization of your own
  project, see [Section 3.1.1](#311-the-environment-of-a-session).
- Example 1 creates the project that Examples 3, 4 and 5 reopen. Example 2 builds its own project.
- Every example closes its project at the end. A project must be closed before the next one is created or
  opened.

---

### Example 1 — Create a simple steel frame

Builds a portal frame (two columns and a beam), loads it, runs the analysis and prints the largest bending
moment of the beam. Returns the path of the project.

```csharp
static string Example1()
{
    string path = ProjectPath("frame");
    Check(client.NewProject(path, Env()), "NewProject");

    // A material and a section are given by catalogue name. The values of the catalogue material apply.
    EID steel = Id(client.CreateMaterial(new Material { Name = "S275" }), "CreateMaterial");
    EID ipe = Id(client.CreateSection("IPE300"), "CreateSection");

    // Your own material needs a name the catalogue does not have. SI units: Pa, kg/m3 (not used below).
    Id(client.CreateMaterial(new MaterialSteel
    {
        Name = "S275 user", E = 2.1e11, Ro = 7850, Nu = 0.3, SigmaE = 2.75e8
    }), "CreateMaterial (user steel)");

    // Two columns and a beam, in metres (Z is up)
    EID Linear(double x1, double z1, double x2, double z2) => Id(client.CreateElement(new ElementLinear
    {
        Material = steel, Section = ipe,
        GeomPtStart = new Pt3D { X = x1, Y = 0, Z = z1 },
        GeomPtEnd = new Pt3D { X = x2, Y = 0, Z = z2 }
    }), "CreateElement");
    Linear(0, 0, 0, 4);
    EID beam = Linear(0, 4, 5, 4);
    Linear(5, 4, 5, 0);

    // Fixed supports at the column bases
    foreach (double x in new[] { 0.0, 5.0 })
        Id(client.CreateElement(new ElementRigidPunctualSupport
        {
            GeomPt = new Pt3D { X = x, Y = 0, Z = 0 }, Material = steel,
            ConstraintsType = RigidSupportConstraint.Fix
        }), "CreateElement (support)");

    // A load case belongs to a family: LoadCaseFamilyID. A load belongs to a load case: LoadCase.
    EID deadFamily = Id(client.CreateInformationalElement(
        new LoadCaseFamily_DeadLoads { Name = "G" }), "CreateInformationalElement");
    EID dead = Id(client.CreateInformationalElement(new LoadCase_DeadLoads
    {
        Name = "Self weight", LoadCaseFamilyID = deadFamily,
        Field = new GravityFieldDefinition { Z = -1 }   // self weight, downward
    }), "CreateInformationalElement");
    EID liveFamily = Id(client.CreateInformationalElement(
        new LoadCaseFamily_LiveLoads { Name = "Q" }), "CreateInformationalElement");
    EID live = Id(client.CreateInformationalElement(
        new LoadCase_LiveLoads { Name = "Imposed", LoadCaseFamilyID = liveFamily }), "CreateInformationalElement");

    // 50 kN downward at mid-span of the beam (forces in N)
    Id(client.CreateElement(new ElementLoadPunctual
    {
        GeomPt = new Pt3D { X = 2.5, Y = 0, Z = 4 }, LoadCase = live, Fz = -50000
    }), "CreateElement (load)");

    // Ultimate limit state combination 1.35 G + 1.5 Q
    EID uls = Id(client.AddCombination(new Combination
    {
        ECombinationType = ECombinationType.EComboProjectSituationEluStrgeo,
        ListCasesCoeffs = new List<EIDDoublePair>
        {
            new EIDDoublePair { Key = dead, Value = 1.35 },
            new EIDDoublePair { Key = live, Value = 1.5 }
        }
    }), "AddCombination");

    // Analysis, then the internal forces of the beam on the combination
    Check(client.LaunchAnalysis(), "LaunchAnalysis");
    var forces = client.GetResults(ResultType.Forces, uls.Value, new[] { beam.Value });
    Check(forces.Details, "GetResults");
    var my = forces.Data.OfType<ResElementLinear>().First().ResDiagrams.ResForces.My;
    Console.WriteLine($"Largest |My| in the beam: {my.Max(p => Math.Abs(p.Value)) / 1000:F1} kN.m");

    Check(client.CloseProject(), "CloseProject");   // saves the project
    return path;
}
```

---

### Example 2 — Wind load generation (EN 1991-1-4)

Builds a small hall (two portal frames, purlins, six load areas), creates the wind load case family and lets the
server generate the wind load cases and their loads.

```csharp
static void Example2()
{
    Check(client.NewProject(ProjectPath("hall"), Env()), "NewProject");
    EID steel = Id(client.CreateMaterial(new Material { Name = "S275" }), "CreateMaterial");   // catalogue material
    EID frameSection = Id(client.CreateSection("IPE300"), "CreateSection");
    EID purlinSection = Id(client.CreateSection("IPE140"), "CreateSection");

    void Member(EID section, double x1, double y1, double z1, double x2, double y2, double z2) =>
        Id(client.CreateElement(new ElementLinear
        {
            Material = steel, Section = section,
            GeomPtStart = new Pt3D { X = x1, Y = y1, Z = z1 }, GeomPtEnd = new Pt3D { X = x2, Y = y2, Z = z2 }
        }), "CreateElement");

    foreach (double y in new[] { 0.0, 5.0 })            // two frames, 20 m span, ridge at 6 m
    {
        Member(frameSection, 0, y, 0, 0, y, 5);   Member(frameSection, 0, y, 5, 10, y, 6);
        Member(frameSection, 20, y, 5, 10, y, 6); Member(frameSection, 20, y, 0, 20, y, 5);
        foreach (double x in new[] { 0.0, 20.0 })       // pinned supports
            Id(client.CreateElement(new ElementRigidPunctualSupport
            {
                GeomPt = new Pt3D { X = x, Y = y, Z = 0 }, Material = steel,
                ConstraintsType = RigidSupportConstraint.SupportHinge
            }), "CreateElement (support)");
    }
    for (double x = 0; x <= 20; x += 2)                 // purlins between the frames, on the roof line
    {
        double z = 5 + (10 - Math.Abs(x - 10)) / 10;
        Member(purlinSection, x, 0, z, x, 5, z);
    }

    // Load areas receive the wind: two walls, two roof slopes, two gables
    void Area(params (double X, double Y, double Z)[] points) => Id(client.CreateElement(new ElementLoadArea
    {
        GeomPtsList = points.Select(p => new Pt3D { X = p.X, Y = p.Y, Z = p.Z }).ToList(),
        ClimaticProperties = new LoadAreaClimaticProperties { ClimaticType = Load_Area_Type.CG_LOADAREA_BUILDING }
    }), "CreateElement (load area)");
    Area((0, 5, 0), (0, 5, 5), (0, 0, 5), (0, 0, 0));
    Area((20, 5, 0), (20, 5, 5), (20, 0, 5), (20, 0, 0));
    Area((0, 0, 5), (10, 0, 6), (10, 5, 6), (0, 5, 5));
    Area((20, 0, 5), (10, 0, 6), (10, 5, 6), (20, 5, 5));
    Area((0, 0, 0), (20, 0, 0), (20, 0, 5), (10, 0, 6), (0, 0, 5));
    Area((0, 5, 0), (20, 5, 0), (20, 5, 5), (10, 5, 6), (0, 5, 5));

    // The wind family holds the parameters; the load cases are generated from it
    EID wind = Id(client.CreateInformationalElement(new LoadCaseFamily_Wind_1991_1_4
    {
        Name = "Wind EN 1991-1-4",
        BasePressure = new BasePressureParameters_Wind_EN1991_1_4
        {
            Speed = 26,   // basic wind speed vb, m/s
            PlacementType = PlacementType_Wind_EN1991_1_4_API.TERRAIN_CATEGORY_II
        },
        AutoGeneration = new AutoGenerationParameters_Wind_EN1991_1_4
        {
            PitchType = PitchType_Wind_EN1991_1_4_API.PITCH_TYPE_DUO
        }
    }), "CreateInformationalElement (wind family)");
    Check(client.ProcessAction(AD_API_ActionType.ClimaticAutoGeneration, null), "ProcessAction");

    // Read back the generated load cases and count their loads
    var cases = client.GetElementsID(new List<QueryBase> { new QueryInfoLoadCase
        { InformationalElementType = InformationalElementTypeEnum.LoadCase_Wind, CaseFamilyId = wind } });
    Check(cases.Details, "GetElementsID");
    foreach (long caseId in cases.Data)
    {
        var wc = client.GetInformationalElementsObject(new[] { caseId }).Data.OfType<LoadCase>().First();
        var effect = (wc as LoadCase_Wind_1991_1_4)?.EffectParameters;
        var loadTypes = new[] { ElementTypeEnum.ElementLoadLinear, ElementTypeEnum.ElementLoadPunctual,
                                ElementTypeEnum.ElementLoadPlanar };
        var loads = client.GetElementsID(loadTypes.Select(t => (QueryBase)new QueryElementsLoads
            { ElementType = t, LoadCaseId = new EID { Value = caseId } }).ToList());
        Console.WriteLine($"{wc.Name}: {effect?.WindDirection}, {loads.Data.Count} loads");
    }
    Check(client.CloseProject(), "CloseProject");
}
```

---

### Example 3 — Query and retrieve elements

Reopens the project of Example 1 and reads its elements, materials, sections, load case families and loads.

```csharp
static void Example3(string projectPath)
{
    Check(client.OpenProject(projectPath, Env()), "OpenProject");

    // Identifiers of all linear elements, then the objects themselves
    var ids = client.GetElementsID(new List<QueryBase>
        { new QueryElementsModel { ElementType = ElementTypeEnum.ElementLinear } });
    Check(ids.Details, "GetElementsID");
    Console.WriteLine($"Found {ids.Data.Count} linear elements.");
    var elements = client.GetElementsObject(ids.Data);
    Check(elements.Details, "GetElementsObject");
    foreach (var e in elements.Data.OfType<ElementLinear>())
        Console.WriteLine($"#{e.UserID}: ({e.GeomPtStart.X:F2}, {e.GeomPtStart.Y:F2}, {e.GeomPtStart.Z:F2}) -> " +
                          $"({e.GeomPtEnd.X:F2}, {e.GeomPtEnd.Y:F2}, {e.GeomPtEnd.Z:F2}), " +
                          $"section {e.Section.Value}, material {e.Material.Value}");

    // Materials and sections of the project
    foreach (var m in client.GetMaterials(client.GetListMaterials().Data).Data)
        Console.WriteLine($"Material {m.Name}");
    foreach (var s in client.GetSections(client.GetListSections().Data).Data)
        Console.WriteLine($"Section {s.Name}");

    // Informational objects are queried by their concrete type and read with their own call
    var familyIds = client.GetElementsID(new List<QueryBase>
    {
        new QueryInfoModel { InformationalElementType = InformationalElementTypeEnum.LoadCaseFamily_DeadLoads },
        new QueryInfoModel { InformationalElementType = InformationalElementTypeEnum.LoadCaseFamily_LiveLoads }
    });
    foreach (var f in client.GetInformationalElementsObject(familyIds.Data).Data)
        Console.WriteLine($"{f.GetType().Name}: {(f as LoadCaseFamily)?.Name}");

    // The punctual loads of every live-load case
    var caseIds = client.GetElementsID(new List<QueryBase>
        { new QueryInfoLoadCase { InformationalElementType = InformationalElementTypeEnum.LoadCase_LiveLoads } });
    foreach (long caseId in caseIds.Data)
    {
        var loadIds = client.GetElementsID(new List<QueryBase> { new QueryElementsLoads
            { ElementType = ElementTypeEnum.ElementLoadPunctual, LoadCaseId = new EID { Value = caseId } } });
        foreach (var load in client.GetElementsObject(loadIds.Data).Data.OfType<ElementLoadPunctual>())
            Console.WriteLine($"Load case {caseId}: Fz = {load.Fz / 1000:F1} kN at X = {load.GeomPt.X:F2} m");
    }

    Check(client.CloseProject(), "CloseProject");
}
```

---

### Example 4 — Seismic analysis (EN 1998-1)

Reopens the project of Example 1, adds an EN 1998-1 seismic family with four cases (X and Y, signed and
unsigned) and reads the cases back. This example changes the project, so it runs last. Generating the
combinations and running the analysis are the calls of
[Example 5](#example-5--steel-design-combinations-analysis-design-results).

```csharp
static void Example4(string projectPath)
{
    Check(client.OpenProject(projectPath, Env()), "OpenProject");

    EID family = Id(client.CreateInformationalElement(new LoadCaseFamily_SeismicEN_1998_1
    {
        Name = "Seismic EN 1998-1",
        Implantation = new ImplantationSeismicEN1998
        {
            SpectrumType = SpectrumType_API.SPECTRE_DESIGN_1,
            AgRg = 0.16,                                  // ag * gammaI, as a fraction of g
            SoilClass = SoilClassEN1998_API.B,
            S = 1.2, TbImposedValue = 0.15, TcImposedValue = 0.5, TdImposedValue = 2.0   // seconds
        },
        Structure = new StructureSeismicEN1998
        {
            ImportanceCategory = ImportanceCategoryEN1998_API.CAT_II,
            CoefGammaI = 1.0, CoefQHoriz = 3.9, CoefQHorizY = 3.9, CoefQVert = 1.5,
            DuctilityClass = DuctilityClassEN1998_API.MEDIUM
        },
        Method = new CalculationMethodEN1998 { CalculMethod = CalculMethod_API.CQC, ResidualMode = true }
    }), "CreateInformationalElement (seismic family)");

    // One case per direction and sign choice. A case carries its family in LoadCaseFamilyID.
    var directions = new[] { DirectionChoiceEN1998_API.X, DirectionChoiceEN1998_API.Y };
    var signs = new[] { SignChoice_API.SIGNED_PREPONDERANT_MODE, SignChoice_API.UNSIGNED };
    foreach (var direction in directions)
        foreach (var sign in signs)
            Id(client.CreateInformationalElement(new LoadCase_SeismicEN_1998_1
            {
                Name = $"E_{direction}_{sign}",
                LoadCaseFamilyID = family,
                Direction = new DirectionConfigEN1998 { DirectionChoice = direction, SignChoice = sign },
                Ductility = new DuctilityConfigEN1998
                {
                    DuctilityDetermination = DuctilityDeterminationEN1998_API.Q_IMPOSE,
                    DuctilityCoefficient = 3.9
                }
            }), "CreateInformationalElement (seismic case)");

    // Read the cases of the family back
    var caseIds = client.GetElementsID(new List<QueryBase> { new QueryInfoLoadCase
        { InformationalElementType = InformationalElementTypeEnum.LoadCase_SeismicEN_1998_1, CaseFamilyId = family } });
    Check(caseIds.Details, "GetElementsID");
    foreach (long caseId in caseIds.Data)
    {
        var seismicCase = client.GetInformationalElementsObject(new[] { caseId }).Data
            .OfType<LoadCase_SeismicEN_1998_1>().First();
        Console.WriteLine($"{seismicCase.Name}: {seismicCase.Direction?.DirectionChoice}, " +
                          $"{seismicCase.Direction?.SignChoice}, q = {seismicCase.Ductility?.DuctilityCoefficient}");
    }

    Check(client.CloseProject(), "CloseProject");
}
```

---

### Example 5 — Steel design: combinations, analysis, design, results

Reopens the project of Example 1, generates the combinations, runs the analysis and the Eurocode 3 steel design,
and prints the largest work ratio of each member. The order is fixed: combinations, then analysis, then design,
then results.

```csharp
static void Example5(string projectPath)
{
    Check(client.OpenProject(projectPath, Env()), "OpenProject");

    // 1. Combinations. The generated combinations already list their design calculations.
    var generated = client.GenerateCombinations(new CombinationsGenerationRequest
        { Mode = ECombinationsGenerationMode.Simplified });
    Check(generated.Details, "GenerateCombinations");
    Console.WriteLine($"{generated.Data.CreatedCombinations.Count} combinations generated.");

    // Send each one back: this writes the default design coefficients the steel design needs (Section 3.6.2).
    foreach (EID id in generated.Data.CreatedCombinations)
    {
        var read = client.GetInformationalElementsObject(new List<long> { id.Value });
        Check(read.Details, "GetInformationalElementsObject");
        Combination combination = read.Data.OfType<Combination>().First();
        Check(client.UpdateInformationalElement(id.Value, combination), "UpdateInformationalElement");
    }

    // 2. Finite element analysis, then the steel design (an empty list means the whole model)
    Check(client.LaunchAnalysis(), "LaunchAnalysis");
    Check(client.LaunchDesignCalculation(DesignCalculationType.Steel, new List<long>()), "LaunchDesignCalculation");

    // 3. One result type for the linear elements. The element list must not be empty.
    var ids = client.GetElementsID(new List<QueryBase>
        { new QueryElementsModel { ElementType = ElementTypeEnum.ElementLinear } });
    Check(ids.Details, "GetElementsID");
    var results = client.GetDesignResults(new ResultsQueryDesignSteelLinear
    {
        EResType = SteelLinearDesignResultType.MaxWorkRatio,
        EResultSet = SteelDesignResultSet.Standard,
        ListElementsIDs = ids.Data
    });
    Check(results.Details, "GetDesignResults");
    foreach (var element in results.Data.OfType<ResDesignElementLinearSteel>())
    {
        double max = element.ResMeshes.Select(mesh => mesh.Data).OfType<ResEc3MaxWorkRatio>()
            .SelectMany(r => r.WorkRatios).Select(w => w.WorkRatio).DefaultIfEmpty().Max();
        Console.WriteLine($"Element {element.UserID}: largest work ratio {max:F2}");
    }

    Check(client.CloseProject(), "CloseProject");
}
```

---

## 16. Change History

What was added to the API, most recent first. Each line links to the section that describes it.

| Area | What was added |
|---|---|
| Model settings | `StructureSettings`: the work space of the structure (plane, grid or 3D), bending stiff / truss, and the reference temperature — [3.7.4](#374-structure-work-space) |
| Model settings | `ToleranceSettings`: the geometric tolerances of the model — [3.7.3](#373-model-tolerances) |
| Projects | `SaveProject`, `SaveProjectAs`, `CloseProjectWithoutSave`, `CloseSessionWithoutSave` — [3.1](#31-project-management) |
| Projects | A new project starts with the default project settings of the desktop application |
| Service | `GetStatus`: whether the server can work, and the state of its licence. The server now checks its licence when it starts — [3.11](#311-service--version-status--liveness) |
| Projects | `Environments.DesignCodes`: the design standard and national annex of each family, and the AISC method (ASD / LRFD). `GetEnvironment` reads back what is in force — [3.1.1](#311-the-environment-of-a-session) |
| Elements | `ElementPolyLine` and `ElementPoint`: reference geometry that guides the mesh. `ElementLinear.SectionOrientationPointId` orients a section towards a point — [7](#7-data-models--elements-geometric) |
| Service | `GetVersion`: the version of the API, also usable as a "ping" — [3.11](#311-service--version-status--liveness) |
| Design templates | `AISCSteelLinearDesignTemplate` and `CSASteelLinearDesignTemplate`: steel design templates for AISC 360 and CAN/CSA S16 — [10.6](#106-expert-design-templates) |
| Model settings | `AiscSteelCalculationSettings`: the steel calculation settings for AISC 360 and CAN/CSA S16 — [3.7.2](#372-steel-aisc--can-csa-calculation-settings) |
| Model settings | `Ec3SteelCalculationSettings`: the steel calculation settings for Eurocode 3 — [3.7.1](#371-steel-eurocode-3-calculation-settings) |
| Sections | `CreateUserSection`, `UserSection`, `UserSectionShape`: sections defined by their characteristics and their outline — [3.3](#33-model--sections) |
| Design | `LaunchDesignCalculation`: runs the Expert design of one material family — [3.8](#38-model--analysis) |
| Design templates | `EC3SteelLinearDesignTemplate`, `ElementLinear.DesignTemplateId`, `AssignDesignTemplateToElements` — [10.6](#106-expert-design-templates) |
| Planar elements | `LinearReleaseTemplate`, `ElementPlanar.SelectedReleaseName`, `ElementPlanar.LinearReleases`: releases on the edges of planar elements — [10.5](#105-linear-release-templates) |
| Model organisation | `StructuralSystem` and `SystemIDs`: systems and building levels — [10.4](#104-structural-systems) |

### Changes that can break existing code

| Change | What to do |
|---|---|
| The 2nd generation Eurocodes can no longer be selected: their values were removed from `SteelDesignCode` and `eCombinationsStandard` | Use the 1st generation values. A model already configured for a 2nd generation Eurocode is refused by the settings, generation, design calculation and design result calls |
| A second `NewProject` / `OpenProject` while a project is open is refused (`PROJECT_ENVIRONMENT_REFUSED`) | Close the open project first |
| Five settings classes were renamed: `Ec3SteelOptimizationSettings`, `Ec3SteelProfileSortingSettings`, `Ec3SteelBucklingSettings`, `Ec3SteelAngleVerificationSettings`, `Ec3SteelCalculationSequenceSettings` lost their `Ec3` prefix | Use `SteelOptimizationSettings`, `SteelProfileSortingSettings`, `SteelBucklingSettings`, `SteelAngleVerificationSettings`, `SteelCalculationSequenceSettings` |
| Design templates: `McrMode` and `Mcr0Mode` are enumerations (`SteelLtbCriticalMomentMode`, `SteelLtbReferenceCriticalMomentMode`) instead of integers; `EffectiveSlendernessKMode` uses `SteelAngleEffectiveSlendernessKMode` | Send the enumeration values |
| Design templates: `ColdFormedDesign.DesignInMainAxesUuVv = true` is refused; `Buckling.BucklingLength.TorsionalBucklingVerification = false` is refused except on a CAN/CSA template | See [10.6](#106-expert-design-templates) |
| Design templates: `Buckling.BucklingLength.YNodesType` / `ZNodesType` are nullable; `null` keeps the stored value | Send them only when you mean to change them |
| The enumeration `WindDesign_WindIBC_API` was removed: `LoadCaseFamily_WindIBC.WindDesign` is an integer | See [12.3](#123-wind-ibc--asce-7-22) |
| The class `MaterialBase` was removed: `Material` is the base class of all materials | Use `Material` |

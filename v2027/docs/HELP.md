# Advance Design API v1 — Developer Reference (C#)

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
   - 3.7 [Model — Analysis](#37-model--analysis)
   - 3.8 [Model — Actions](#38-model--actions)
   - 3.9 [Model — Query & Retrieval](#39-model--query--retrieval)
4. [Response Envelope Pattern](#4-response-envelope-pattern)
5. [Polymorphism & the `$type` Discriminator](#5-polymorphism--the-type-discriminator)
6. [Data Models — Materials](#6-data-models--materials)
7. [Data Models — Elements (geometric)](#7-data-models--elements-geometric)
8. [Data Models — Supports](#8-data-models--supports)
9. [Data Models — Loads](#9-data-models--loads)
10. [Data Models — Informational Elements (Load Cases & Families)](#10-data-models--informational-elements-load-cases--families)
    - 10.1 [Load Case Families](#101-load-case-families)
    - 10.2 [Load Cases](#102-load-cases)
    - 10.3 [Combinations](#103-combinations)
11. [Data Models — Analysis Results](#11-data-models--analysis-results)
12. [Data Models — Climatic Parameters](#12-data-models--climatic-parameters)
    - 12.1 [Wind EN 1991-1-4](#121-wind-en-1991-1-4)
    - 12.2 [Wind CBN](#122-wind-cbn)
    - 12.3 [Wind IBC / ASCE 7-22](#123-wind-ibc--asce-7-22)
    - 12.4 [Snow EN 1991-1-3 (NF EC1)](#124-snow-en-1991-1-3-nf-ec1)
    - 12.5 [Snow CBN](#125-snow-cbn)
    - 12.6 [Snow IBC / ASCE 7-22](#126-snow-ibc--asce-7-22)
    - 12.7 [Seismic EN 1998-1](#127-seismic-en-1998-1)
    - 12.8 [Seismic CBN](#128-seismic-cbn)
    - 12.9 [Seismic IBC](#129-seismic-ibc)
13. [Enumerations Reference](#13-enumerations-reference)
14. [Error Handling & Diagnostics](#14-error-handling--diagnostics)
15. [Complete C# Usage Examples](#15-complete-c-usage-examples)

---

## 1. Overview

The **Advance Design API** exposes a REST/HTTP interface that allows external C# applications (add-ins, automation scripts, test harnesses) to:

- Open, create and close Advance Design projects.
- Populate the structural model with materials, sections, linear/planar elements and supports.
- Define load cases, load case families, climatic loads (wind, snow, seismic) and combinations.
- Trigger analysis and post-process finite-element results (displacements, forces, stresses).
- Query element IDs and retrieve fully deserialised element objects.

All responses are wrapped in a **typed envelope** (`BooleanApiResponse`, `EIDApiResponse`, `ResBaseListApiResponse`, …) that carries both the payload and structured diagnostics.

### OpenAPI & Multi-language Support

The Advance Design API is fully described by an **OpenAPI (Swagger) specification**, which means it can be consumed from **any programming language or platform** that supports HTTP — not just C#. Client code can be automatically generated from the OpenAPI schema using tools such as:

| Tool | Languages supported |
|---|---|
| [NSwag](https://github.com/RicoSuter/NSwag) | C#, TypeScript |
| [openapi-generator](https://openapi-generator.tech/) | Python, Java, Go, JavaScript/TypeScript, Ruby, PHP, Rust, Swift, Kotlin, and many more |
| [Swagger Codegen](https://swagger.io/tools/swagger-codegen/) | Java, Python, Ruby, PHP, JavaScript, Go, C++, and others |
| [kiota](https://github.com/microsoft/kiota) | C#, Python, TypeScript, Java, Go, PHP |

> ℹ️ **Note:** All code examples in this document are written in **C#** using the auto-generated `swaggerClient` class produced by NSwag. The underlying HTTP calls and JSON payloads are identical regardless of the language used — refer to the [Raw JSON examples in Section 5](#5-polymorphism--the-type-discriminator) when implementing a client in another language.

---

## 2. Getting Started

### 2.1 Instantiate the client

```csharp
using System.Net.Http;

var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("http://localhost:5000"); // adjust port

var client = new swaggerClient("http://localhost:5000", httpClient);
```

### 2.2 Typical workflow

```csharp
// 1. Open (or create) a project
var env = new Environments
{
    Language    = Language_Code.eLanguageFrench,
    Localization = Localization_Code.LOCALIZATION_FRANCE,
    LogVerbosity = LogVerboseLevel.ErrorsAndWarnings
};
BooleanApiResponse openResp = client.NewProject("C:/Projects/MyModel.fto", env);
if (!openResp.Details.Success) throw new Exception("Cannot create project");

// 2. Add material
var steel = new MaterialSteel { Name = "S235", E = 210000, Ro = 7850, Nu = 0.3 };
EIDApiResponse matResp = client.CreateMaterial(steel);
long materialId = matResp.Data.Value;

// 3. Add section
EIDApiResponse secResp = client.CreateSection("HEA200");
long sectionId = secResp.Data.Value;

// 4. Add linear element (beam)
var beam = new ElementLinear
{
    GeomPtStart = new Pt3D { X = 0, Y = 0, Z = 0 },
    GeomPtEnd   = new Pt3D { X = 5, Y = 0, Z = 0 },
    Material    = new EID { Value = materialId },
    Section     = new EID { Value = sectionId },
    LinearElementType = LinearElementFEMType_API.eLinearElementFEMTypeGeneral,
    GeneralBeamType   = GeneralBeamType_API.beamWStandardBending
};
EIDApiResponse beamResp = client.CreateElement(beam);

// 5. Launch analysis
BooleanApiResponse analysisResp = client.LaunchAnalysis();

// 6. Get results
var results = client.GetResults(ResultType.Forces, analysisResp_caseId, new List<long> { beamResp.Data.Value });

// 7. Close project & session
client.CloseProject();
client.CloseSession();
```

### 2.3 Async pattern

Every endpoint has three overloads:

| Overload | Description |
|---|---|
| `T Method(args)` | Blocking synchronous call (wraps async) |
| `Task<T> MethodAsync(args)` | Fire-and-forget async (no cancellation) |
| `Task<T> MethodAsync(args, CancellationToken ct)` | Full async with cancellation |

```csharp
// Async example
var response = await client.LaunchAnalysisAsync(cancellationToken);
```

> ℹ️ **Note:** All code examples in this document are written in **C#** using the auto-generated `swaggerClient` class produced by NSwag. The underlying HTTP calls and JSON payloads are identical regardless of the language used — refer to the [Raw JSON examples in Section 5](#5-polymorphism--the-type-discriminator) when implementing a client in another language.

---

## 3. API Endpoints Reference

### 3.1 Project Management

#### `POST /api/Model/management/NewProject`

Creates a new Advance Design project file.

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| `filename` | query | `string` | No | Full path of the `.fto` file to create |
| *(body)* | body | `Environments` | No | Language, localization and log verbosity settings |

**Returns:** `BooleanApiResponse` · `Data = true` on success.

```csharp
var env = new Environments
{
    Language     = Language_Code.eLanguageEnglish,
    Localization = Localization_Code.LOCALIZATION_EUROPE,
    LogVerbosity = LogVerboseLevel.Errors
};
BooleanApiResponse r = client.NewProject(@"C:\Projects\test.fto", env);
```

---

#### `POST /api/Model/management/OpenProject`

Opens an existing Advance Design project file.

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| `filename` | query | `string` | No | Full path of the `.fto` file to open |
| *(body)* | body | `Environments` | No | Environment settings |

**Returns:** `BooleanApiResponse`

```csharp
BooleanApiResponse r = client.OpenProject(@"C:\Projects\existing.fto", new Environments());
```

---

#### `POST /api/Model/management/CloseProject`

Closes the currently open project (does not shut down the API host).

**Returns:** `BooleanApiResponse`

```csharp
client.CloseProject();
```

---

#### `POST /api/Model/management/CloseSession`

Closes the current session **and requests the local API host to shut down**. Use this when running multiple parallel automation sessions and you want to release resources.

**Returns:** `BooleanApiResponse`

```csharp
client.CloseSession();
```

> ⚠️ **Important:** After calling `CloseSession`, the HTTP server process will terminate. Do not make further calls on this client instance.

---

### 3.2 Model — Materials

#### `POST /api/Model/materials/CreateMaterial`

Creates a new material in the current project.

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| *(body)* | body | `Material` *(polymorphic)* | Yes | The material to create. Must include `$type` discriminator. |

**Returns:** `EIDApiResponse` · `Data.Value` contains the new material's internal ID (`long`).

**Supported concrete types:**

| C# class | `$type` value | Description |
|---|---|---|
| `Material` | `"Material"` | Generic material (base) |
| `MaterialSteel` | `"MaterialSteel"` | Steel |
| `MaterialReinforcedConcrete` | `"MaterialReinforcedConcrete"` | Reinforced concrete |
| `MaterialWood` | `"MaterialWood"` | Timber (EN) |
| `MaterialWoodNorthAmerica` | `"MaterialWoodNorthAmerica"` | Timber (North America) |
| `MaterialRigid` | `"MaterialRigid"` | Rigid material |
| `MaterialOther` | `"MaterialOther"` | User-defined material |

---

### 3.3 Model — Sections

#### `POST /api/Model/sections/CreateSection`

Creates a new section by name (looked up from the Advance Design section database).

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| `sectionName` | query | `string` | No | Section name as it appears in the database (e.g. `"HEA200"`, `"IPE300"`) |

**Returns:** `EIDApiResponse` · `Data.Value` contains the new section's internal ID.

```csharp
EIDApiResponse sec = client.CreateSection("IPE300");
long sectionId = sec.Data.Value;
```

---

### 3.4 Model — Elements (geometric)

#### `POST /api/Model/elements/CreateElement`

Creates a new geometric/structural element (linear, planar, load, support, pile, etc.).

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| *(body)* | body | `ElementBase` *(polymorphic)* | Yes | Element definition. Must include `$type` discriminator. |

**Returns:** `EIDApiResponse`

**Supported concrete types:** see [Section 7](#7-data-models--elements-geometric) and [Section 8](#8-data-models--supports).

---

### 3.5 Model — Informational Elements

#### `POST /api/Model/elements/CreateInformationalElement`

Creates a new non-geometric informational element: load cases, load case families, or combinations.

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| *(body)* | body | `InformationalElementBase` *(polymorphic)* | Yes | Load case / family / combination definition. Must include `$type`. |

**Returns:** `EIDApiResponse`

---

### 3.6 Model — Combinations

#### `POST /api/Model/combinations/AddCombination`

Creates a new load combination in the current project.

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| *(body)* | body | `Combination` | Yes | The combination object |

**Returns:** `EIDApiResponse`

> **Note:** `Combination.idComb` is **read-only** — it is managed internally by Advance Design and should not be set when creating a combination. Use it only when reading existing combinations.

```csharp
var combo = new Combination
{
    ECombinationType = ECombinationType.eComboProjectSituationEluStrgeo,
    ListCasesCoeffs  = new List<EIDDoublePair>
    {
        new EIDDoublePair { Key = new EID { Value = deadLoadId }, Value = 1.35 },
        new EIDDoublePair { Key = new EID { Value = liveLoadId }, Value = 1.5  }
    }
};
client.AddCombination(combo);
```

---

### 3.7 Model — Analysis

#### `POST /api/Model/analysis/LaunchAnalysis`

Launches the finite-element calculation of the current model.

**Returns:** `BooleanApiResponse` · `Data = true` when analysis completed successfully.

```csharp
BooleanApiResponse r = client.LaunchAnalysis();
if (!r.Details.Success)
    Console.WriteLine(string.Join("\n", r.Details.Diagnostics.Select(d => d.Message)));
```

---

#### `POST /api/Model/analysis/GetResults`

Retrieves finite-element results for the specified result type and element EIDs.

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| `eResType` | query | `ResultType` | No | Result type (displacement, forces, stresses, resultant forces) |
| `IDAnalysisCase` | query | `long` | No | Analysis case (load case or combination) EID |
| *(body)* | body | `long[]` | No | Array of element EIDs to fetch results for |

**`ResultType` values:**

| Value | Integer | Description |
|---|---|---|
| `displacement` | 0 | Nodal displacements |
| `forces` | 1 | Internal forces (efforts) |
| `stresses` | 2 | Stresses |
| `resultantforces` | 4 | Resultant forces (torsors) |

**Returns:** `ResBaseListApiResponse` — polymorphic list of `ResBase` items.

```csharp
var elementIds = new List<long> { beamEid, columnEid };
ResBaseListApiResponse res = client.GetResults(ResultType.Forces, analysisCaseId, elementIds);
foreach (var item in res.Data)
{
    if (item is ResElementLinear linear)
    {
        foreach (var node in linear.ResNodes)
            Console.WriteLine($"Fx={node.ResForces.Fx}, My={node.ResForces.My}");
    }
}
```

---

#### `POST /api/Model/analysis/GetMeshConnectivity`

Returns mesh connectivity (finite-element topology) for the specified element IDs.

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| *(body)* | body | `long[]` | No | Element EIDs |

**Returns:** `MeshElementListApiResponse` — list of `MeshElement` objects, each containing an `Int32Matrix` connectivity matrix.

---

#### `POST /api/Model/analysis/GetMeshNodesPosition`

Returns the 3D positions of all mesh nodes after meshing/analysis.

**Returns:** `Pt3DListApiResponse` — list of `Pt3D` coordinates.

---

### 3.8 Model — Actions

#### `POST /api/Model/actions/ProcessAction`

Executes a specific model action (e.g. automatic climatic load generation).

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| `ActionToProcess` | query | `AD_API_ActionType` | No | The action to perform |
| *(body)* | body | `ActionArgumentsBase[]` | No | Action-specific arguments |

**`AD_API_ActionType` values:**

| Value | Description |
|---|---|
| `ClimaticAutoGeneration` | Triggers automatic generation of wind and snow loads for all existing climatic load case families |

```csharp
client.ProcessAction(AD_API_ActionType.ClimaticAutoGeneration, new List<ActionArgumentsBase>());
```

---

#### `POST /api/Model/elements/ExportAdditionalEnums`

Helper endpoint — exports auxiliary enumeration values not used in primary model classes. Used mainly for discovery and client-side enum mapping.

| Parameter | Location | Type | Required |
|---|---|---|---|
| *(body)* | body | `AuxiliaryEnums` | No |

**Returns:** `BooleanApiResponse`

---

### 3.9 Model — Query & Retrieval

#### `POST /api/Model/elements/GetElementsID`

Returns a list of element EIDs matching the provided query filters.

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| *(body)* | body | `QueryBase[]` | No | Array of query objects (polymorphic) |

**Supported query types:**

| C# class | `$type` | Description |
|---|---|---|
| `QueryElementsModel` | `"QueryElementsModel"` | Filter by `ElementTypeEnum` |
| `QueryElementsLoads` | `"QueryElementsLoads"` | Filter loads by `LoadCaseId` |
| `QueryInfoModel` | `"QueryInfoModel"` | Filter informational elements by `InformationalElementTypeEnum` |
| `QueryInfoLoadCase` | `"QueryInfoLoadCase"` | Filter load cases by parent family `CaseFamilyId` |

**Returns:** `Int64ListApiResponse`

```csharp
// Get all linear element IDs
var query = new List<QueryBase>
{
    new QueryElementsModel { ElementType = ElementTypeEnum.LinearElement }
};
Int64ListApiResponse ids = client.GetElementsID(query);
```

---

#### `POST /api/Model/elements/GetElementsObject`

Returns fully populated `ElementBase` objects for the given list of EIDs.

| Parameter | Location | Type | Required |
|---|---|---|---|
| *(body)* | body | `long[]` | No |

**Returns:** `ElementBaseListApiResponse`

```csharp
ElementBaseListApiResponse elements = client.GetElementsObject(ids.Data.ToList());
foreach (var el in elements.Data)
{
    if (el is ElementLinear beam)
        Console.WriteLine($"Beam from {beam.GeomPtStart.X} to {beam.GeomPtEnd.X}");
}
```

---

#### `POST /api/Model/elements/GetInformationalElementsObject`

Returns fully populated `InformationalElementBase` objects (load cases, families, combinations) for the given EIDs.

| Parameter | Location | Type | Required |
|---|---|---|---|
| *(body)* | body | `long[]` | No |

**Returns:** `InformationalElementBaseListApiResponse`

---

## 4. Response Envelope Pattern

Every API call returns a typed response envelope. All envelopes follow the same pattern:

```csharp
public class BooleanApiResponse
{
    public bool Data { get; set; }           // The actual result
    public ApiResponseDetails Details { get; set; }  // Diagnostics
}

public class EIDApiResponse
{
    public EID Data { get; set; }            // { Value: long }
    public ApiResponseDetails Details { get; set; }
}
```

### `ApiResponseDetails`

| Property | Type | Description |
|---|---|---|
| `Success` | `bool` | `true` when no error-level diagnostics were produced |
| `HasWarnings` | `bool` | `true` if at least one warning exists |
| `HasErrors` | `bool` | `true` if at least one error or critical diagnostic exists |
| `Diagnostics` | `ICollection<DiagnosticEntry>` | Structured list of messages |

### `DiagnosticEntry`

| Property | Type | Description |
|---|---|---|
| `Severity` | `DiagnosticSeverity` | `Information`, `Warning`, `Error`, `Critical` |
| `Code` | `string` | Machine-readable diagnostic code |
| `Message` | `string` | Human-readable message |
| `Source` | `string` | Component that produced the diagnostic |
| `Timestamp` | `DateTimeOffset` | When the diagnostic was produced |

### Checking results

```csharp
var r = client.CreateMaterial(new MaterialSteel { Name = "S275", E = 210000, Ro = 7850, Nu = 0.3 });

if (!r.Details.Success)
{
    foreach (var diag in r.Details.Diagnostics)
        Console.Error.WriteLine($"[{diag.Severity}] {diag.Code}: {diag.Message}");
    return;
}

long id = r.Data.Value;
```

---

## 5. Polymorphism & the `$type` Discriminator

The API uses **NJsonSchema's `JsonInheritanceConverter`** with the `"$type"` discriminator field. When sending a polymorphic object (material, element, load case, etc.) you **must** include the `"$type"` property in the JSON body.

The generated `swaggerClient` handles serialisation automatically when you pass the correct derived C# type. No manual `$type` injection is needed in C#.

```csharp
// Correct — the serializer injects "$type": "MaterialSteel" automatically
EIDApiResponse r = client.CreateMaterial(new MaterialSteel { Name = "S235" });

// Also correct — base type, minimal payload
EIDApiResponse r2 = client.CreateMaterial(new Material { Name = "Generic" });
```

### Raw JSON examples (for reference / non-C# callers)

**Creating a steel material:**
```json
{
  "$type": "MaterialSteel",
  "name": "S235",
  "e": 210000,
  "ro": 7850,
  "nu": 0.3,
  "damping": 0.02,
  "alpha": 1.2e-5,
  "sigmaE": 235
}
```

**Creating a linear element (beam):**
```json
{
  "$type": "ElementLinear",
  "userName": 1,
  "geomPtStart": { "x": 0, "y": 0, "z": 0 },
  "geomPtEnd":   { "x": 5, "y": 0, "z": 0 },
  "section":  { "value": 1 },
  "material": { "value": 2 },
  "linearElementType": "Beam",
  "generalBeamType": "Column"
}
```

---

## 6. Data Models — Materials

All material classes inherit from `Material` (which inherits from `MaterialBase`).

### `Material` (base)

| Property | Type | Required | Description |
|---|---|---|---|
| `Name` | `string` | ✓ | Material name |

### `MaterialSteel`

Inherits `Material`. Additional properties:

| Property | Type | Default | Description |
|---|---|---|---|
| `BehaviorType` | `MaterialBehaviourType` | — | Behaviour type (elastic, plastic, —) |
| `E` | `double` | — | Young's modulus (kN/m²) |
| `Ro` | `double` | — | Density (kg/m—) |
| `Nu` | `double` | — | Poisson's ratio |
| `Damping` | `double` | — | Damping ratio |
| `Alpha` | `double` | — | Thermal expansion coefficient (1/°C) |
| `SigmaE` | `double` | — | Yield strength (kN/m²) |
| `Color` | `double` | — | Display colour index |
| `Diffusion` | `double` | — | Diffusion coefficient |
| `G` | `double` | — | Shear modulus (kN/m²) |

### `MaterialReinforcedConcrete`

Inherits `Material`. Additional properties:

| Property | Type | Description |
|---|---|---|
| `BehaviorType` | `MaterialBehaviourType` | Behaviour type |
| `Ms` | `double` | Unit weight (kN/m²) |
| `Mu` | `double` | Dynamic unit weight |
| `E` | `double` | Young's modulus (kN/m²) |
| `Nu` | `double` | Poisson's ratio |
| `Damping` | `double` | Damping ratio |
| `Alpha` | `double` | Thermal coefficient |
| `Fck` | `double` | Characteristic compressive strength (kN/m²) |
| `Fcu` | `double` | Cube compressive strength |
| `Fyk` | `double` | Rebar yield strength |
| `Es` | `double` | Rebar Young's modulus |
| `EiEv` | `double` | Short/long-term stiffness ratio |
| `G` | `double` | Shear modulus |
| `Fykl` | `double` | Longitudinal yield strength |
| `Fykt` | `double` | Transversal yield strength |

### `MaterialWood` / `MaterialWoodNorthAmerica`

Inherits `Material`. Key additional properties:

| Property | Type | Description |
|---|---|---|
| `E` | `double` | Mean modulus of elasticity along grain (kN/m²) |
| `E0_05` | `double` | 5th-percentile MOE |
| `G` | `double` | Mean shear modulus |
| `G0_05` | `double` | 5th-percentile shear modulus |
| `GammaM` | `double` | Partial material factor |
| `Fmk` | `double` | Bending strength |
| `Ft0k` | `double` | Tensile strength parallel to grain |
| `Fc0k` | `double` | Compressive strength parallel to grain |
| `Fvk` | `double` | Shear strength |
| `Ft90k` | `double` | Tensile strength perpendicular to grain |
| `Fc90k` | `double` | Compressive strength perpendicular to grain |
| `BurningRatioCoeff` | `double` | Charring rate coefficient |
| `E90` | `double` | MOE perpendicular to grain (`MaterialWood` only) |

### `MaterialRigid` / `MaterialOther`

Follow the same pattern as `MaterialReinforcedConcrete` but without reinforcement-specific fields.

---

## 7. Data Models — Elements (geometric)

All geometric elements inherit from `ElementBase`.

### `ElementBase` (base)

| Property | Type | Required | Description |
|---|---|---|---|
| `UserName` | `int` | — | User-assigned integer label |

### `ElementLinear`

Represents a 1D beam/column/truss element.

| Property | Type | Required | Description |
|---|---|---|---|
| `GeomPtStart` | `Pt3D` | ✓ | Start node coordinates (m) |
| `GeomPtEnd` | `Pt3D` | ✓ | End node coordinates (m) |
| `Section` | `EID` | ✓ | Reference to section by EID |
| `Material` | `EID` | ✓ | Reference to material by EID |
| `LinearElementType` | `LinearElementFEMType_API` | ✓ | FEM element type (`eLinearElementFEMTypeGeneral`, `eLinearElementFEMTypeCompositeBeam`) |
| `GeneralBeamType` | `GeneralBeamType_API` | ✓ | Structural role (`bar`, `beamWStandardBending`, `sbeam`, `variablebeam`, `tie`, `strut`, `cable`, `rigid`, `CompositeBeamSimpleBeam`, `CompositeBeamSbeam`, `CompositeBeamVariable`) |

| Property | Type | Required | Description |
|---|---|---|---|
| `ElementActive` | `bool?` | — | Whether element is active |
| `AutoMesh` | `bool?` | — | Automatic meshing |
| `DetailedMeshProperties` | `LinearElementMeshStyle` | — | Custom mesh settings |
| `ExtendIntoWall` | `bool?` | — | Embed into wall |
| `SectionOrientationAngle` | `double?` | — | Section rotation angle (–360° to 360—) |
| `SectionExcentration` | `SectionOffsetStyle` | — | Section eccentricity offset |
| `InertiaProperties` | `LinearElementInnertia` | — | Cracked inertia settings |
| `InitialConstraintProperties` | `InitialConstraint` | — | Pre-stress / initial stress |
| `RelaxationTotale` | `ConnectionTotale` | — | Total (rigid) end releases |
| `RelaxationElastique` | `ConnectionElastique` | — | Elastic end-spring releases |
| `HaunchStart` | `HaunchProperties` | — | Start haunch |
| `HaunchEnd` | `HaunchProperties` | — | End haunch |
| `Clipping` | `ClippingProperties` | — | Clipping planes (XY, XZ) |
| `LoadAreaLoadTransferProperties` | `LinearLoadAreaLoadTransfer` | — | Load area transfer settings |

### `ElementPlanar`

Represents a 2D slab/wall/plate element.

| Property | Type | Required | Description |
|---|---|---|---|
| `GeomPtsList` | `ICollection<Pt3D>` | ✓ | List of corner coordinates (min 3 points) |
| `Material` | `EID` | ✓ | Material EID |
| `ElementType` | `PlanarElementType` | ✓ | Slab, wall, membrane, — |
| `Eccentricity` | `double` | ✓ | Eccentricity (m) |
| `EccentricityCOnsideredAlsoForFEM` | `bool` | ✓ | Apply eccentricity in FEM |
| `ThicknessIn1stVertex` | `double` | ✓ | Thickness at first vertex (m) |
| `SlopeX` | `double` | ✓ | Thickness slope X |
| `SlopeY` | `double` | ✓ | Thickness slope Y |
| `SupportingElement` | `bool` | ✓ | Acts as a support for other elements |
| `MeshProperties` | `PlanarElementMeshProperties` | — | Mesh configuration |

### `ElementSinglePile`

| Property | Type | Required | Description |
|---|---|---|---|
| `GeomPt` | `Pt3D` | ✓ | Pile head position |
| `Material` | `EID` | ✓ | Material EID |
| `CrossSection` | `EID` | ✓ | Section EID |
| `TotalLength` | `double` | ✓ | Pile length (m) |
| `ElementActive` | `bool?` | — | Active flag |
| `Bearing` | `PileBearing` | — | Bearing capacity values |
| `RestraintsOptions` | `PileRestraintsOptions` | — | DOF restraint types |
| `RestraintsDiagrams` | `AdvancedRestraintsDiagramDefinitions` | — | Nonlinear diagrams per DOF |

### `ElementLoadArea`

Defines a load area (tributary area) that distributes climatic or mechanical loads onto structural elements.

| Property | Type | Required | Description |
|---|---|---|---|
| `GeomPtsList` | `ICollection<Pt3D>` | ✓ | Polygon vertices |
| `LoadTransferProperties` | `LoadAreaLoadTransfer` | — | Transfer method and direction |
| `MechanicalProperties` | `LoadAreaMechanicalProperties` | — | Self-weight, material, thickness |
| `ClimaticProperties` | `LoadAreaClimaticProperties` | — | Wind/snow availability, gutters, exposure |

---

## 8. Data Models — Supports

All support families use a two-level hierarchy:
- **Family** (`FamilySupportLinear`, `FamilySupportPlanar`, `FamilySupportPunctual`) — defines geometry and material.
- **Type** (rigid, elastic, tension-compression, advanced) — defines mechanical behaviour.

### Family base classes

#### `FamilySupportLinear`

| Property | Type | Required |
|---|---|---|
| `GeomPtStart` | `Pt3D` | ✓ |
| `GeomPtEnd` | `Pt3D` | ✓ |
| `Material` | `EID` | ✓ |
| `Footing` | `FootingDimensionsLinear` | — |
| `SuppElement` | `SupportingElementDimensionsLinear` | — |
| `ElementActive` | `bool?` | — |

#### `FamilySupportPlanar`

| Property | Type | Required |
|---|---|---|
| `GeomPtsList` | `ICollection<Pt3D>` | ✓ |
| `ElementActive` | `bool?` | — |

#### `FamilySupportPunctual`

| Property | Type | Required |
|---|---|---|
| `GeomPt` | `Pt3D` | ✓ |
| `Material` | `EID` | ✓ |
| `Footing` | `FootingDimensions` | — |
| `SuppElement` | `SupportingElementDimensions` | — |
| `Punching` | `PunchingProperties` | — |
| `ElementActive` | `bool?` | — |

### Support behaviour types

| `$type` | Applies to | Description |
|---|---|---|
| `ElementRigidLinearSupport` | Linear | Rigid support with configurable DOF restraints |
| `ElementElasticLinearSupport` | Linear | Elastic spring support |
| `ElementTCLinearSupport` | Linear | Tension/compression-only |
| `ElementAdvancedLinearSupport` | Linear | Nonlinear per-DOF |
| `ElementRigidPlanarSupport` | Planar | Rigid |
| `ElementElasticPlanarSupport` | Planar | Elastic spring |
| `ElementTCPlanarSupport` | Planar | Tension/compression-only |
| `ElementAdvancedPlanarSupport` | Planar | Nonlinear per-DOF |
| `ElementRigidPunctualSupport` | Punctual | Rigid |
| `ElementElasticPunctualSupport` | Punctual | Elastic spring |
| `ElementTCPunctualSupport` | Punctual | Tension/compression-only |
| `ElementAdvancedPunctualSupport` | Punctual | Nonlinear per-DOF |

### `ElementRigidLinearSupport` / `ElementRigidPlanarSupport`

| Property | Type | Required | Description |
|---|---|---|---|
| `ConstraintsType` | `RigidSupportConstraint` | ✓ | Predefined restraint configuration (`fix`, `SupportHinge`, `other`) |
| `Restraints` | `DegreeOfFreedomRestraints` | — | Custom per-DOF booleans (Tx, Ty, Tz, Rx, Ry, Rz) — used when `ConstraintsType = other` |

### `ElementElasticLinearSupport` / `..PlanarSupport` / `..PunctualSupport`

| Property | Type | Description |
|---|---|---|
| `Stiffness` | `SupportElasticStiffness` | Spring stiffness per DOF (Ktx…Krz) |
| `DampingRatio` | `SeismicSupportDampingRatio` | Damping per DOF |
| `VerticalStiffness` | `SupportVerticalStiffness` | Soil layer-based vertical stiffness |

### `ElementAdvancedLinearSupport` / `..PlanarSupport` / `..PunctualSupport`

| Property | Type | Description |
|---|---|---|
| `RestraintsOptions` | `AdvancedRestraintsOptions` | Per-DOF `AdvancedRestraintType` |
| `RestraintsDiagrams` | `AdvancedRestraintsDiagramDefinitions` | Piecewise stiffness diagrams |

**`AdvancedRestraintType` values:**

| Value | Description |
|---|---|
| `eFree` | No restraint |
| `eFixed` | Fully fixed |
| `eElastic` | Linear spring |
| `eNLActiveForCompression` | Active only in compression |
| `eNLActiveForTension` | Active only in tension |
| `eNLGap` | Gap (limit in distance) |
| `eNLHardening` | Hardening (limit in force) |
| `eNLDiagram` | User-defined piecewise diagram |

---

## 9. Data Models — Loads

### `ElementLoadLinear`

Linear distributed load applied along a line.

| Property | Type | Required | Description |
|---|---|---|---|
| `GeomPtStart` | `Pt3D` | ✓ | Start point |
| `GeomPtEnd` | `Pt3D` | ✓ | End point |
| `CoordinateSystemType` | `CoordinateSystemForLinearOrPlanarGeometry?` | — | Local or global |
| `LoadCase` | `EID` | ✓ | Parent load case EID |
| `Fx` | `double` | ✓ | Force along X (kN/m) |
| `Fy` | `double` | ✓ | Force along Y (kN/m) |
| `Fz` | `double` | ✓ | Force along Z (kN/m) |
| `Moment` | `MomentComponents` | — | Moment components (kN·m/m) |
| `Variation` | `LinearVariation` | — | Trapezoidal variation coefficients |

### `ElementLoadPlanar`

Surface distributed load applied over a polygon.

| Property | Type | Required | Description |
|---|---|---|---|
| `GeomPtsList` | `ICollection<Pt3D>` | ✓ | Polygon vertices |
| `CoordinateSystemType` | `CoordinateSystemForLinearOrPlanarGeometry?` | — | |
| `LoadCase` | `EID` | ✓ | Parent load case EID |
| `Fx`, `Fy`, `Fz` | `double` | ✓ | Force per unit area (kN/m²) |
| `Moment` | `MomentComponents` | — | |
| `Variation` | `PlanarVariation` | — | Variation over 3 points |

### `ElementLoadPunctual`

Point load applied at a node.

| Property | Type | Required | Description |
|---|---|---|---|
| `GeomPt` | `Pt3D` | ✓ | Application point |
| `CoordinateSystemType` | `CoordinateSystemForPunctualGeometry?` | — | |
| `LoadCase` | `EID` | ✓ | Parent load case EID |
| `Fx`, `Fy`, `Fz` | `double` | ✓ | Force components (kN) |
| `Moment` | `MomentComponents` | — | Moment components (kN·m) |
| `ImpactingSurface` | `ImpactingSurface` | — | Punching impact surface |
| `Punching` | `PunchingProperties` | — | Punching shear parameters |

### `ElementImposedDisplacement`

Prescribed nodal displacement.

| Property | Type | Required | Description |
|---|---|---|---|
| `GeomPt` | `Pt3D` | ✓ | Node position |
| `LoadCase` | `EID` | ✓ | Parent load case EID |
| `Dx`, `Dy`, `Dz` | `double` | — | Translation components (m) |
| `Rx`, `Ry`, `Rz` | `double` | — | Rotation components (rad) |

### `ElementMass`

Lumped mass applied at a point.

| Property | Type | Required | Description |
|---|---|---|---|
| `GeomPt` | `Pt3D` | ✓ | Application point |
| `Mx`, `My`, `Mz` | `double` | — | Mass components (kg) |
| `Ix`, `Iy`, `Iz` | `double` | — | Rotational inertia (kg·m²) |

---

## 10. Data Models — Informational Elements (Load Cases & Families)

### 10.1 Load Case Families

All load case families inherit from `LoadCaseFamily`.

#### `LoadCaseFamily` (base)

| Property | Type | Required | Description |
|---|---|---|---|
| `Name` | `string` | ✓ | Family name |
| `CombinationsCoefficients` | `LoadCaseFamilyCombinationsCoefficients` | — | Default combination coefficients |

**Concrete family types:**

| `$type` | Description |
|---|---|
| `LoadCaseFamily_Accidental` | Accidental loads |
| `LoadCaseFamily_DeadLoads` | Permanent/dead loads |
| `LoadCaseFamily_LiveLoads` | Variable/live loads |
| `LoadCaseFamily_Other` | User-defined |
| `LoadCaseFamily_Thermic` | Thermal loads |
| `LoadCaseFamily_DynamicTemporal` | Time-history dynamic |
| `LoadCaseFamily_PushOver` | Pushover analysis |
| `LoadCaseFamily_Seismic` | Generic seismic (spectrum defined manually) |
| `LoadCaseFamily_SeismicEN_1998_1` | Seismic per EN 1998-1 |
| `LoadCaseFamily_SeismicCBN` | Seismic per Canadian code (NBC/CSA) |
| `LoadCaseFamily_SeismicIBC` | Seismic per IBC/ASCE 7 |
| `LoadCaseFamily_Snow` | Generic snow |
| `LoadCaseFamily_Snow_1991_1_3` | Snow per EN 1991-1-3 (NF EC1) |
| `LoadCaseFamily_SnowCBN` | Snow per Canadian code |
| `LoadCaseFamily_SnowIBC` | Snow per IBC/ASCE 7-22 |
| `LoadCaseFamily_Wind` | Generic wind |
| `LoadCaseFamily_Wind_1991_1_4` | Wind per EN 1991-1-4 |
| `LoadCaseFamily_WindCBN` | Wind per Canadian code |
| `LoadCaseFamily_WindIBC` | Wind per IBC/ASCE 7-22 |

#### `LoadCaseFamily_Wind_1991_1_4` (example — detailed)

| Property | Type | Description |
|---|---|---|
| `Building2D` | `Building2DParameters_Wind_EN1991_1_4` | 2D building parameters |
| `AutoGeneration` | `AutoGenerationParameters_Wind_EN1991_1_4` | Auto-generation options |
| `BasePressure` | `BasePressureParameters_Wind_EN1991_1_4` | Wind speed, terrain category, dynamic factors |
| `SiteCoefficients` | `SiteCoefficientsParameters_Wind_EN1991_1_4` | Phi, land factor, wind effort type |
| `ResponseCoefficients` | `ResponseCoefficientsParameters_Wind_EN1991_1_4` | Turbulence intensity, exposure factor |
| `GreenhouseParameters` | `GreenhouseParameters_Wind_EN1991_1_4` | Greenhouse-specific parameters |

```csharp
var windFamily = new LoadCaseFamily_Wind_1991_1_4
{
    Name = "Wind EN1991",
    BasePressure = new BasePressureParameters_Wind_EN1991_1_4
    {
        Speed    = 28,    // m/s
        CDir     = 1.0,
        CSeas    = 1.0,
        PlacementType = PlacementType_Wind_EN1991_1_4_API.TERRAIN_CATEGORY_II,
        CsCdCalcMode  = CsCdCalcMode_Wind_EN1991_1_4_API.CS_CD_AUTO
    },
    AutoGeneration = new AutoGenerationParameters_Wind_EN1991_1_4
    {
        WindWallStatus = true,
        LoadGeneration = true,
        AutoCalculationCpeCpi = true
    }
};
client.CreateInformationalElement(windFamily);
```

### 10.2 Load Cases

All load cases inherit from `LoadCase`.

#### `LoadCase` (base)

| Property | Type | Required | Description |
|---|---|---|---|
| `Name` | `string` | ✓ | Load case name |
| `LoadCaseFamilyID` | `EID` | ✓ | Parent family EID |
| `CombinationsCoefficients` | `LoadCaseCombinationsCoefficients` | — | Override combination coefficients |

**Concrete load case types:**

| `$type` | Description |
|---|---|
| `LoadCase_Accidental` | Accidental |
| `LoadCase_DeadLoads` | Dead/permanent |
| `LoadCase_LiveLoads` | Live/variable |
| `LoadCase_Other` | User-defined |
| `LoadCase_Thermic` | Thermal |
| `LoadCase_DynamicTemporal` | Time-history |
| `LoadCase_PushOver` | Pushover |
| `LoadCase_Seismic` | Generic seismic |
| `LoadCase_SeismicEN_1998_1` | Seismic EN 1998-1 |
| `LoadCase_SeismicCBN` | Seismic CBN |
| `LoadCase_SeismicIBC` | Seismic IBC |
| `LoadCase_Snow` | Generic snow |
| `LoadCase_Snow_1991_1_3` | Snow NF EC1 |
| `LoadCase_SnowCBN` | Snow CBN |
| `LoadCase_SnowIBC` | Snow IBC |
| `LoadCase_Wind` | Generic wind |
| `LoadCase_Wind_1991_1_4` | Wind EN 1991-1-4 |
| `LoadCase_WindCBN` | Wind CBN |
| `LoadCase_WindIBC` | Wind IBC |

#### `LoadCase_Wind_1991_1_4` (example)

```csharp
var windCase = new LoadCase_Wind_1991_1_4
{
    Name            = "WX+",
    LoadCaseFamilyID = new EID { Value = windFamilyId },
    EffectParameters = new WindEffectParameters_EN1991_1_4
    {
        WindDirection    = WindDirection_Wind_EN1991_1_4_API.X_PLUS,
        InternalPressure = InternalPressure_Wind_EN1991_1_4_API.POSITIVE,
        IsAutomatic      = true,
        TypeOfLoadingsCase = LoadingsCaseType_Wind.GUST_WIND
    }
};
client.CreateInformationalElement(windCase);
```

#### `LoadCase_SeismicEN_1998_1` (example)

```csharp
var seisCase = new LoadCase_SeismicEN_1998_1
{
    Name            = "SX+",
    LoadCaseFamilyID = new EID { Value = seismicFamilyId },
    Direction = new DirectionConfigEN1998
    {
        DirectionChoice = DirectionChoiceEN1998_API.X_Direction,
        SignChoice      = SignChoice_API.Positive,
        Mode            = 1
    },
    Ductility = new DuctilityConfigEN1998
    {
        DuctilityDetermination = DuctilityDeterminationEN1998_API.Auto,
        DuctilityCoefficient   = 1.0
    }
};
```

#### `LoadCase_DeadLoads` (example)

```csharp
var dead = new LoadCase_DeadLoads
{
    Name            = "G1 - Self weight",
    LoadCaseFamilyID = new EID { Value = deadFamilyId },
    Field = new GravityFieldDefinition { X = 0, Y = 0, Z = -9.81 }
};
```

#### `LoadCase_Thermic`

| Property | Type | Description |
|---|---|---|
| `ThermicCoefficients` | `LoadCaseThermicCoefficients` | Gamma coefficients and category |
| `Field` | `TemperatureFieldDefinition` | Dilatation X and gradients Y, Z |
| `SystemSelection` | `SystemSelection` | System selection type |

#### `LoadCase_DynamicTemporal`

| Property | Type | Description |
|---|---|---|
| `VariationProperties` | `TemporalVariationProperties` | Kind of variation, accelerogram direction & scale |
| `TimeIntervalProperties` | `EquivalentStaticCaseProperties` | Time interval settings |
| `ResultsProperties` | `TemporalResultsProperties` | Output selection |
| `Function` | `Function` | Time-function definition |
| `Harmoniques` | `ICollection<FunctionHarmonique>` | Harmonic components |

#### `LoadCase_PushOver`

| Property | Type | Description |
|---|---|---|
| `BaseProperties` | `PushOverBaseProperties` | Automatic/updated flags, structural behaviour |
| `DirectionConfig` | `PushOverDirectionConfig` | Direction and point of application |
| `PositionConfig` | `PushOverPositionConfig` | Eccentricity settings |
| `LoadPattern` | `PushOverLoadPatternConfig` | Load distribution type, mode number |
| `Incrementation` | `PushOverIncrementationConfig` | Step sizes and lateral load percentages |
| `TargetDisplacement` | `PushOverTargetDisplacementConfig` | Control node, direction, max displacement |

### 10.3 Combinations

#### `Combination`

| Property | Type | Required | Description |
|---|---|---|---|
| `IdComb` | `int` | — | User combination ID |
| `ECombinationType` | `ECombinationType` | ✓ | Combination type (ULS, SLS, —) |
| `ListCasesCoeffs` | `ICollection<EIDDoublePair>` | — | Load case EIDs with their coefficients |

---

## 11. Data Models — Analysis Results

### Response types

| `$type` | Description |
|---|---|
| `ResElementLinear` | Results for a 1D element |
| `ResNode` | Results at a single node |
| `ResElementPlanar` | Results for a 2D element |
| `ResPlanarNode` | Results at a node on a planar element |
| `ResLinearSupport` | Results for a linear support |
| `ResPlanarSupport` | Results for a planar support |
| `ResPunctualSupport` | Results for a punctual support |

### `ResBase`

| Property | Type | Description |
|---|---|---|
| `Id` | `EID` | Element EID |

### `ResElementLinear`

| Property | Type | Description |
|---|---|---|
| `ResNodes` | `ICollection<ResNode>` | Per-node results |
| `ResDiagrams` | `ResLinearDiagrams` | Diagram results along element length |

### `ResNode`

| Property | Type | Description |
|---|---|---|
| `ResDisplacements` | `ResDisplacements` | Dx, Dy, Dz, D (total), Rx, Ry, Rz, R (total) |
| `ResForces` | `ResForces` | Fx, Fy, Fz, Mx, My, Mz |
| `ResStresses` | `ResStresses` | Support stresses or linear element stresses |

### `ResDisplacements`

| Property | Unit | Description |
|---|---|---|
| `Dx`, `Dy`, `Dz` | m | Translation components |
| `D` | m | Resultant displacement |
| `Rx`, `Ry`, `Rz` | rad | Rotation components |
| `R` | rad | Resultant rotation |

### `ResForces`

| Property | Unit | Description |
|---|---|---|
| `Fx`, `Fy`, `Fz` | kN | Force components |
| `Mx`, `My`, `Mz` | kN·m | Moment components |

### `ResDiagramForces`

Diagram along the element length as `ICollection<ResAbscissaValue>` (abscissa in m, value in kN or kN·m).

Fields: `Fx`, `Fy`, `Fz`, `Mx`, `My`, `Mz`.

### `ResElementPlanar`

| Property | Type | Description |
|---|---|---|
| `ResNodes` | `ICollection<ResPlanarNode>` | Per-node results |
| `ResTorsors` | `ICollection<ResPlanarTorsors>` | Resultant torsors per group/storey |

### `ResPlanarNode`

| Property | Type | Description |
|---|---|---|
| `ResDisplacements` | `ResDisplacements` | Nodal displacements |
| `ResForces` | `ResPlanarForces` | Membrane + bending forces (Nxx, Nyy, Mxx, Myy, —) |
| `ResStresses` | `ResPlanarStresses` | Stresses at top, mid, bottom faces |

### `ResLinearSupport` / `ResPlanarSupport`

In addition to `ResNodes`, these include resultant torsor components:
`TorsorsFx`, `TorsorsFy`, `TorsorsFz`, `TorsorsMx`, `TorsorsMy`, `TorsorsMz`.

---

## 12. Data Models — Climatic Parameters

### 12.1 Wind EN 1991-1-4

#### `BasePressureParameters_Wind_EN1991_1_4`

| Property | Type | Default | Unit | Description |
|---|---|---|---|---|
| `Speed` | `double` | 0 | m/s | Basic wind speed vb,0 |
| `Pressure` | `double` | 0 | Pa | Wind pressure (overrides speed if set) |
| `CDir` | `double` | 1.0 | — | Direction coefficient |
| `CSeas` | `double` | 1.0 | — | Season coefficient |
| `HeightOfStructureBase` | `double` | 0 | m | Height above ground |
| `HeightOfStructureBaseMaxForAllComputations` | `bool` | false | — | Use max height for all computations |
| `PlacementType` | `PlacementType_Wind_EN1991_1_4_API` | `TERRAIN_CATEGORY_II` | — | Terrain category |
| `RigLength` | `double` | 1.0 | m | Roughness length z0 |
| `Kr` | `double` | 1.0 | — | Terrain factor |
| `CsCdCalcMode` | `CsCdCalcMode_Wind_EN1991_1_4_API` | `CS_CD_AUTO` | — | Cs·Cd calculation mode |
| `CsCdVal` | `double` | 1.0 | — | Cs·Cd value |
| `Delta` | `double` | 0.1 | — | Logarithmic decrement |
| `N` | `double` | 0 | Hz | Fundamental frequency |
| `NAuto` | `bool` | true | — | Auto-calculate N |
| `PhiScaffolding` | `double` | 0.4 | — | Scaffolding factor φ |
| `CorrelationCoefficientType` | `CorrelationCoefficientType_Wind_EN1991_1_4_API` | `CORREL_AUTO` | — | |
| `CalculOfTurbulenceFactorKL` | `CalculOfTurbulenceFactorKL_Wind_EN1991_1_4_API` | `WINDEC1_EN1991_1_4_KLFORMULA_OTHER` | — | |
| `TurbulenceFactorKL` | `double` | 1.0 | — | Turbulence factor KL |

#### `AutoGenerationParameters_Wind_EN1991_1_4`

| Property | Default | Description |
|---|---|---|
| `WindWallStatus` | `true` | Enable wind walls |
| `PressureCoeff` | `true` | Calculate pressure coefficients |
| `SplitWindWalls` | `false` | Split wind walls |
| `LoadGeneration` | `true` | Generate loads |
| `AutoCalculationCpeCpi` | `true` | Auto-calculate Cpe/Cpi |
| `UseCTICMCodeUpdates` | `true` | Apply CTICM amendments |
| `ImposedCpeType` | `e_CPE_MODE_default` | Imposed Cpe type |
| `PitchType` | `PITCH_TYPE_MONO` | Roof pitch type |

#### `Building2DParameters_Wind_EN1991_1_4`

| Property | Default | Unit | Description |
|---|---|---|---|
| `BuildingLength` | 30.0 | m | Building length |
| `PortalPosition` | 5.0 | m | Portal position |
| `OpeningPosition` | `CG_WINDEC1_OPENING2D_NONE` | — | Opening type |
| `Wind2DLoadsDeterminationMethod` | `eWind2DRealLoadsInSpan` | — | Method for 2D loads |
| `Wind2DPositioningMode` | `eWind2DPositioningModeValue` | — | Positioning mode |
| `Wind2DPositioningZone` | `eWind2DPositioningZoneEC1_A_F_G` | — | Zone |

---

### 12.2 Wind CBN

#### `BasePressureParametersWindCBN`

| Property | Default | Unit | Description |
|---|---|---|---|
| `ZoneHash` | `" "` | — | Location zone hash |
| `Pressure` | 420.0 | Pa | Reference pressure q |
| `ImportanceFactorIE` | `SNOWCBNSn2010_IE_NORMAL` | — | Importance factor |
| `CeMode` | `WindCBNWd2010_CE_C_MANUAL` | — | Ce calculation mode |
| `Ce` | 1.0 | — | Exposure factor |
| `Cei` | 1.0 | — | Internal exposure factor |
| `Cg` | 2.5 | — | Gust effect factor (external) |
| `Cgi` | 2.0 | — | Gust effect factor (internal) |
| `Ct` | 1.0 | — | Topographic factor |
| `HeightOfStructureBase` | 0.0 | m | Height of base |

---

### 12.3 Wind IBC / ASCE 7-22

#### `BasePressureWindIBC`

| Property | Default | Unit | Description |
|---|---|---|---|
| `V` | 67.0 | mph | Basic wind speed |
| `ExposureCategory` | `WINDIBCWd2015_EXPOSURE_B` | — | Terrain exposure category |
| `RiskCategory` | `CLIMATIC_IBC2015_RISK_I` | — | Risk category |
| `Kzt` | 1.0 | — | Topographic factor |
| `Kd` | 0.85 | — | Directionality factor |
| `Ke` | 1.0 | — | Ground elevation factor |
| `DGust` | 0.85 | — | Gust effect factor |
| `Ri` | 1.0 | — | Reduction factor |
| `WindDesign` | `GRCG_WIND_IBC_METHOD_ENVELOPPE_LOW_RISE_BUIILDING` | — | Design method |
| `TorsionalLoadCases` | `false` | — | Generate torsional cases |
| `HeightOfStructureBase` | 0.0 | m | Height of base |

---

### 12.4 Snow EN 1991-1-3 (NF EC1)

#### `LoadCaseFamily_Snow_1991_1_3`

| Property | Type | Description |
|---|---|---|
| `ProjectSituation` | `ProjectSituationSnowNF_EC1` | Persistent/accidental exposure |
| `EurocodeParameters` | `EurocodeGeneralParametersSnowNF_EC1` | sk, Ce, Ct, altitude, etc. |
| `GreenhouseParameters` | `GreenhouseParametersSnowNF_EC1` | Greenhouse class, Cm |
| `SnowLoadCategory` | `SnowLoadCategory_API?` | Load category |

#### `EurocodeGeneralParametersSnowNF_EC1`

| Property | Description |
|---|---|
| `PressureEuGen` | Ground snow load sk (kN/m²) |
| `ExceptCoefEuGen` | Exceptional coefficient |
| `CoefficientExpEuGen` | Exposure coefficient Ce |
| `CoefficientTermicEuGen` | Thermal coefficient Ct |
| `AltitudeEuGen` | Site altitude (m) |
| `PeriodN` | Return period N (years) |

---

### 12.5 Snow CBN

#### `ImplantationSnowCBN`

| Property | Default | Description |
|---|---|---|
| `ZoneHash` | — | Location zone hash |
| `Ss50` | — | Ground snow load Ss (50-year return) |
| `Sr50` | — | Rain-on-snow Sr (50-year) |
| `Cw` | — | Wind exposure factor |
| `ImportanceFactorIE` | — | Importance factor |

---

### 12.6 Snow IBC / ASCE 7-22

#### `ImplantationSnowASCE722`

| Property | Default | Description |
|---|---|---|
| `SnowZone` | — | Geographic snow zone |
| `Altitude` | — | Site elevation (ft) |
| `Pg` | — | Ground snow load (psf) |
| `TerrainCategory` | — | Terrain category |
| `Exposure` | — | Roof exposure factor |
| `Ct` | — | Thermal factor |
| `RiskCategory` | — | Risk category |

---

### 12.7 Seismic EN 1998-1

#### `LoadCaseFamily_SeismicEN_1998_1`

| Property | Type | Description |
|---|---|---|
| `Implantation` | `ImplantationSeismicEN1998` | Seismicity, soil class, spectrum type |
| `Structure` | `StructureSeismicEN1998` | Importance class, behaviour factor q |
| `Method` | `CalculationMethodEN1998` | Modal/equivalent static, residual modes |

#### `ImplantationSeismicEN1998`

| Property | Description |
|---|---|
| `SpectrumType` | Type 1 or Type 2 |
| `AgRg` | Design ground acceleration (m/s²) |
| `SoilClass` | Soil class (A, B, C, D, E) |
| `S` | Soil factor S |
| `TbImposedValue`, `TcImposedValue`, `TdImposedValue` | Spectral periods |
| `Vs30` | Average shear-wave velocity |
| `Longitude`, `Latitude` | Site coordinates |
| `AmplificationFactorF0` | Amplification factor F0 (NTC) |

#### `StructureSeismicEN1998`

| Property | Description |
|---|---|
| `ImportanceCategory` | Importance class (I–IV) |
| `CoefGammaI` | Importance factor γI |
| `CoefQHoriz` | Behaviour factor q (horizontal X) |
| `CoefQHorizY` | Behaviour factor q (horizontal Y) |
| `CoefQVert` | Behaviour factor q (vertical) |
| `DuctilityClass` | DCL / DCM / DCH |

#### `CalculationMethodEN1998`

| Property | Type | Description |
|---|---|---|
| `CalculMethod` | `CalculMethod_API` | SRSS, CQC, ABS |
| `ResidualMode` | `bool` | Include residual modes in analysis |

---

### 12.8 Seismic CBN

#### `ImplantationSeismicCBN`

Key properties: `Sa02`, `Sa05`, `Sa10`, `Sa20`, `Sa50`, `Sa100`, `Pga`, `SiteClass`, `Fa`, `Fv`, `IeFaSa02`, `SeismicCategory`, `ImportanceFactorIE`.

#### `StructureSeismicCBN`

| Property | Description |
|---|---|
| `TotalHeight` | Total structure height (m) |
| `FloorsNo` | Number of floors |
| `RegularStructure` | Regularity flag |
| `AccidentalTorsion` | Accidental torsion percentage |
| `ApplyDuctilityEffects` | Enable ductility effects |

---

### 12.9 Seismic IBC

#### `ImplantationSeismicIBC`

Key properties: `Ss`, `S1`, `T0`, `Ts`, `Tl`, `SiteClass`, `Fa`, `Fv`, `OccupancyCategory`.

#### `StructureSeismicIBC`

Same structure as CBN but without `ApplyDuctilityEffects`.

---

## 13. Enumerations Reference

### `AD_API_ActionType`
| Value | Description |
|---|---|
| `ClimaticAutoGeneration` | Auto-generate wind/snow loads |

### `ResultType`
| Value | Int | Description |
|---|---|---|
| `displacement` | 0 | Displacements |
| `forces` | 1 | Internal forces |
| `stresses` | 2 | Stresses |
| `resultantforces` | 4 | Resultant forces |

### `DiagnosticSeverity`
`Information` · `Warning` · `Error` · `Critical`

### `Language_Code`
`eLanguageFrench` · `eLanguageEnglish` · `eLanguageRomanian` · `eLanguageGerman` · `eLanguagePolish` · `eLanguageCzech` · `eLanguageDutch` · `eLanguageRussian` · `eLanguageSpanish` · `eLanguageEnglishUK` · `eLanguageHungarian` · `eLanguageChinese` · `eLanguageTaiwan` · `eLanguageJapanese` · `eLanguageKorean` · `eLanguageBulgarian` · `eLanguageGreek` · `eLanguageTurkish` · `eLanguageQuebec` · `eLanguageItalian` · `eLanguagePortuguese` · `eLanguagePortugueseBrazilian` · `eLanguageSlovak` · `eLanguageEndMark`

### `Localization_Code`
`LOCALIZATION_ALGERIA` · `LOCALIZATION_ASIA` · `LOCALIZATION_CANADA` · `LOCALIZATION_CZECH_REPUBLIC` · `LOCALIZATION_EUROPE` · `LOCALIZATION_FRANCE` · `LOCALIZATION_GERMANY` · `LOCALIZATION_MAROCCO` · `LOCALIZATION_POLAND` · `LOCALIZATION_ROMANIA` · `LOCALIZATION_TUNISIA` · `LOCALIZATION_UK` · `LOCALIZATION_US` · `LOCALIZATION_ITALIA` · `LOCALIZATION_SLOVAKIA` · `LOCALIZATION_BULGARIA` · `LOCALIZATION_SPAIN` · `LOCALIZATION_PORTUGAL`

### `LogVerboseLevel`
`Errors` · `ErrorsAndWarnings` · `AllDetails`

### `MaterialBehaviourType`
`isotropic_behavior` · `orthotropic_behavior`

### `LinearElementFEMType_API`
`eLinearElementFEMTypeGeneral` · `eLinearElementFEMTypeCompositeBeam`

### `GeneralBeamType_API`
`bar` · `beamWStandardBending` · `sbeam` · `variablebeam` · `tie` · `strut` · `cable` · `rigid` · `CompositeBeamSimpleBeam` · `CompositeBeamSbeam` · `CompositeBeamVariable`

### `PlanarElementType`
`membrane` · `plate` · `shell` · `deformation_plane` · `steeldeck` · `layeredShell` · `slabonsteeldeck` · `planar_no_of_types`

### `RigidSupportConstraint`
`fix` · `SupportHinge` · `other`

### `TractionOrCompressionBehaviour`
`compression` · `traction`

### `AdvancedRestraintType`
`eFree` · `eFixed` · `eElastic` · `eNLActiveForCompression` · `eNLActiveForTension` · `eNLGap` · `eNLHardening` · `eNLDiagram`

### `eCombinationType`
`eComboProjectSituationEluStrgeo` · `eComboProjectSituationEluEqu` · `eComboProjectSituationElsCharacteristic` · `eComboProjectSituationElsFrecvent` · `eComboProjectSituationElsQuasiPermanent` · `eComboProjectSituationELUA` · `eComboProjectSituationElsA` · `eComboProjectSituationNone`

### `ElementTypeEnum`
Used in `QueryElementsModel` to filter element queries:
`ElementBase` · `ElementLinear` · `ElementPlanar` · `ElementLoadArea` · `ElementMass` · `ElementLoadPunctual` · `ElementLoadLinear` · `ElementLoadPlanar` · `ElementImposedDisplacement` · `ElementRigidPunctualSupport` · `ElementElasticPunctualSupport` · `ElementTCPunctualSupport` · `ElementAdvancedPunctualSupport` · `ElementSinglePile` · `ElementRigidLinearSupport` · `ElementElasticLinearSupport` · `ElementTCLinearSupport` · `ElementAdvancedLinearSupport` · `ElementRigidPlanarSupport` · `ElementElasticPlanarSupport` · `ElementTCPlanarSupport` · `ElementAdvancedPlanarSupport`

### `InformationalElementTypeEnum`
Used in `QueryInfoModel` to filter informational element queries:
`InfoObjBase` · `LoadCase` · `LoadCaseFamily` · `LoadCaseFamily_Accidental` · `LoadCase_Accidental` · `LoadCaseFamily_DeadLoads` · `LoadCase_DeadLoads` · `LoadCaseFamily_DynamicTemporal` · `LoadCase_DynamicTemporal` · `LoadCaseFamily_LiveLoads` · `LoadCase_LiveLoads` · `LoadCaseFamily_Other` · `LoadCase_Other` · `LoadCaseFamily_PushOver` · `LoadCase_PushOver` · `LoadCaseFamily_Seismic` · `LoadCase_Seismic` · `LoadCaseFamily_SeismicCBN` · `LoadCase_SeismicCBN` · `LoadCaseFamily_SeismicEN_1998_1` · `LoadCase_SeismicEN_1998_1` · `LoadCaseFamily_SeismicIBC` · `LoadCase_SeismicIBC` · `LoadCaseFamily_Snow` · `LoadCase_Snow` · `LoadCaseFamily_Snow_1991_1_3` · `LoadCase_Snow_1991_1_3` · `LoadCaseFamily_SnowCBN` · `LoadCase_SnowCBN` · `LoadCaseFamily_SnowIBC` · `LoadCase_SnowIBC` · `LoadCaseFamily_Thermic` · `LoadCase_Thermic` · `LoadCaseFamily_Wind` · `LoadCase_Wind` · `LoadCaseFamily_Wind_1991_1_4` · `LoadCase_Wind_1991_1_4` · `LoadCaseFamily_WindCBN` · `LoadCase_WindCBN` · `LoadCaseFamily_WindIBC` · `LoadCase_WindIBC` · `Combination`

### `PlacementType_Wind_EN1991_1_4_API`
`TERRAIN_CATEGORY_0` · `TERRAIN_CATEGORY_I` · `TERRAIN_CATEGORY_II` · `TERRAIN_CATEGORY_III` · `TERRAIN_CATEGORY_IV`

### `WindDirection_Wind_EN1991_1_4_API`
`X_PLUS` · `X_MINUS` · `Y_PLUS` · `Y_MINUS`

### `InternalPressure_Wind_EN1991_1_4_API`
`POSITIVE` · `NEGATIVE`

### `SnowLoadCategory_API`
Category-based classification (Normal, Exceptional, etc.)

### `CalculMethod_API`
`SRSS` · `CQC` · `ABS`

### `Wind_Directions_type`
`DIRECTION_X_PLUS` · `DIRECTION_X_MINUS` · `DIRECTION_Y_PLUS` · `DIRECTION_Y_MINUS` · `GRCG_WIND_DIRECTIONS_NO`

### `eLoadCaseType`
`eADLoadCaseNone` · `eADLoadCaseGravity` · `eADLoadCaseExploitation` · `eADLoadCaseSnow` · `eADLoadCaseWind` · `eADLoadCaseThermic` · `eADLoadCaseSeismic` · `eADLoadCaseAccidental` · `eADLoadCaseTemporal` · `eADLoadCaseOther` · `eADLoadCaseEnvelope` · `eADLoadCaseBuckling` · `eADLoadCaseNonLinear` · `eADLoadCaseModal` · `eADLoadCaseCombination` · `eADLoadCaseTrafficLoads` · `eADLoadCaseSnowAcc` · `eADLoadCasePushOver` · `eADLoadCasePushOverAnalysis` · `eADLoadCaseCraneLoads` · `eADLoadCaseConstructionStagesAnalysis` · `eADLoadCaseWindAccidental`

---

## 14. Error Handling & Diagnostics

```csharp
try
{
    EIDApiResponse r = client.CreateElement(element);

    if (r.Details.HasErrors)
    {
        var errors = r.Details.Diagnostics
            .Where(d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Critical);

        foreach (var e in errors)
            Console.Error.WriteLine($"[{e.Code}] {e.Message} (Source: {e.Source})");

        throw new InvalidOperationException("Element creation failed.");
    }

    if (r.Details.HasWarnings)
    {
        foreach (var w in r.Details.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning))
            Console.WriteLine($"[WARN] {w.Message}");
    }

    long eid = r.Data.Value;
}
catch (ApiException ex)
{
    // HTTP-level error (non-200 status)
    Console.Error.WriteLine($"HTTP {ex.StatusCode}: {ex.Response}");
    // Inspect headers
    foreach (var h in ex.Headers)
        Console.Error.WriteLine($"  {h.Key}: {string.Join(", ", h.Value)}");
}
```

**Note:** `ApiException` is thrown only for non-200 HTTP responses. Business-level errors (invalid geometry, missing section, etc.) are returned as `Success = false` with populated `Diagnostics`, not as exceptions.

---

## 15. Complete C# Usage Examples

### Example 1 — Create a simple steel frame

```csharp
using var http = new HttpClient();
var client = new swaggerClient("http://localhost:5000", http);

// Open project
client.NewProject(@"C:\tmp\frame.fto", new Environments
{
    Language     = Language_Code.eLanguageEnglish,
    Localization = Localization_Code.LOCALIZATION_EUROPE,
    LogVerbosity = LogVerboseLevel.ErrorsAndWarnings
});

// Material
long matId = client.CreateMaterial(new MaterialSteel
{
    Name = "S235", E = 210000000, Ro = 7850, Nu = 0.3,
    Damping = 0.02, Alpha = 1.2e-5, SigmaE = 235000
}).Data.Value;

// Section
long secId = client.CreateSection("HEA200").Data.Value;

// Columns
long col1 = client.CreateElement(new ElementLinear
{
    GeomPtStart = new Pt3D { X = 0, Y = 0, Z = 0 },
    GeomPtEnd   = new Pt3D { X = 0, Y = 0, Z = 4 },
    Material    = new EID { Value = matId },
    Section     = new EID { Value = secId },
    LinearElementType = LinearElementFEMType_API.eLinearElementFEMTypeGeneral,
    GeneralBeamType   = GeneralBeamType_API.bar
}).Data.Value;

long col2 = client.CreateElement(new ElementLinear
{
    GeomPtStart = new Pt3D { X = 5, Y = 0, Z = 0 },
    GeomPtEnd   = new Pt3D { X = 5, Y = 0, Z = 4 },
    Material    = new EID { Value = matId },
    Section     = new EID { Value = secId },
    LinearElementType = LinearElementFEMType_API.eLinearElementFEMTypeGeneral,
    GeneralBeamType   = GeneralBeamType_API.bar
}).Data.Value;

// Beam
long beam = client.CreateElement(new ElementLinear
{
    GeomPtStart = new Pt3D { X = 0, Y = 0, Z = 4 },
    GeomPtEnd   = new Pt3D { X = 5, Y = 0, Z = 4 },
    Material    = new EID { Value = matId },
    Section     = new EID { Value = secId },
    LinearElementType = LinearElementFEMType_API.eLinearElementFEMTypeGeneral,
    GeneralBeamType   = GeneralBeamType_API.beamWStandardBending
}).Data.Value;

// Fixed supports at column bases
foreach (var pt in new[] { new Pt3D { X=0,Y=0,Z=0 }, new Pt3D { X=5,Y=0,Z=0 } })
{
    client.CreateElement(new ElementRigidPunctualSupport
    {
        GeomPt          = pt,
        Material        = new EID { Value = matId },
        ConstraintsType = RigidSupportConstraint.fix
    });
}

// Dead load family & case
long deadFamId = client.CreateInformationalElement(new LoadCaseFamily_DeadLoads
{
    Name = "G - Permanent loads"
}).Data.Value;

long deadId = client.CreateInformationalElement(new LoadCase_DeadLoads
{
    Name            = "G1 - Self weight",
    LoadCaseFamilyID = new EID { Value = deadFamId },
    Field           = new GravityFieldDefinition { X = 0, Y = 0, Z = -9.81 }
}).Data.Value;

// Live load family & case
long liveFamId = client.CreateInformationalElement(new LoadCaseFamily_LiveLoads
{
    Name = "Q - Live loads"
}).Data.Value;

long liveId = client.CreateInformationalElement(new LoadCase_LiveLoads
{
    Name            = "Q1 - Roof live",
    LoadCaseFamilyID = new EID { Value = liveFamId }
}).Data.Value;

// Point load on beam mid-span
client.CreateElement(new ElementLoadPunctual
{
    GeomPt    = new Pt3D { X = 2.5, Y = 0, Z = 4 },
    LoadCase  = new EID { Value = liveId },
    Fx = 0, Fy = 0, Fz = -10 // 10 kN downward
});

// ULS combination
client.AddCombination(new Combination
{
    ECombinationType = ECombinationType.eComboProjectSituationEluStrgeo,
    ListCasesCoeffs  = new List<EIDDoublePair>
    {
        new EIDDoublePair { Key = new EID { Value = deadId }, Value = 1.35 },
        new EIDDoublePair { Key = new EID { Value = liveId }, Value = 1.5  }
    }
});

// Analysis
var analysisResult = client.LaunchAnalysis();
if (!analysisResult.Details.Success)
    throw new Exception("Analysis failed!");

// Retrieve forces on beam
var results = client.GetResults(ResultType.Forces, liveId, new List<long> { beam });
if (results.Data.FirstOrDefault() is ResElementLinear linRes)
{
    Console.WriteLine("=== Beam internal forces ===");
    foreach (var node in linRes.ResNodes)
        Console.WriteLine($"  Fz={node.ResForces.Fz:F3} kN, My={node.ResForces.My:F3} kN·m");
}

// Save and close
client.CloseProject();
client.CloseSession();
```

---

### Example 2 — Wind load generation (EN 1991-1-4)

```csharp
// Create wind family
long windFamId = client.CreateInformationalElement(new LoadCaseFamily_Wind_1991_1_4
{
    Name = "WND - Wind EN1991",
    BasePressure = new BasePressureParameters_Wind_EN1991_1_4
    {
        Speed         = 28,
        CDir          = 1.0,
        CSeas         = 1.0,
        PlacementType = PlacementType_Wind_EN1991_1_4_API.TERRAIN_CATEGORY_II,
        CsCdCalcMode  = CsCdCalcMode_Wind_EN1991_1_4_API.CS_CD_AUTO,
        NAuto         = true,
        Delta         = 0.1
    },
    AutoGeneration = new AutoGenerationParameters_Wind_EN1991_1_4
    {
        WindWallStatus       = true,
        LoadGeneration       = true,
        AutoCalculationCpeCpi = true,
        UseCTICMCodeUpdates  = true
    }
}).Data.Value;

// Create individual wind cases
foreach (var (dir, ip) in new[]
{
    (WindDirection_Wind_EN1991_1_4_API.X_PLUS,  InternalPressure_Wind_EN1991_1_4_API.POSITIVE),
    (WindDirection_Wind_EN1991_1_4_API.X_MINUS, InternalPressure_Wind_EN1991_1_4_API.NEGATIVE),
    (WindDirection_Wind_EN1991_1_4_API.Y_PLUS,  InternalPressure_Wind_EN1991_1_4_API.POSITIVE),
    (WindDirection_Wind_EN1991_1_4_API.Y_MINUS, InternalPressure_Wind_EN1991_1_4_API.NEGATIVE),
})
{
    client.CreateInformationalElement(new LoadCase_Wind_1991_1_4
    {
        Name             = $"W_{dir}_{ip}",
        LoadCaseFamilyID  = new EID { Value = windFamId },
        EffectParameters = new WindEffectParameters_EN1991_1_4
        {
            WindDirection    = dir,
            InternalPressure = ip,
            IsAutomatic      = true,
            TypeOfLoadingsCase = LoadingsCaseType_Wind.GUST_WIND
        }
    });
}

// Trigger automatic climatic generation
client.ProcessAction(AD_API_ActionType.ClimaticAutoGeneration, new List<ActionArgumentsBase>());
```

---

### Example 3 — Query and retrieve elements

```csharp
// Get all linear element IDs
var ids = client.GetElementsID(new List<QueryBase>
{
    new QueryElementsModel { ElementType = ElementTypeEnum.ElementLinear }
}).Data;

Console.WriteLine($"Found {ids.Count} linear elements.");

// Retrieve full objects
var elements = client.GetElementsObject(ids.ToList()).Data;
foreach (var el in elements.OfType<ElementLinear>())
{
    Console.WriteLine(
        $"EID={el.UserName}  " +
        $"from ({el.GeomPtStart.X:F2},{el.GeomPtStart.Y:F2},{el.GeomPtStart.Z:F2}) " +
        $"to   ({el.GeomPtEnd.X:F2},{el.GeomPtEnd.Y:F2},{el.GeomPtEnd.Z:F2})");
}

// Get all load case families
var infoIds = client.GetElementsID(new List<QueryBase>
{
    new QueryInfoModel { InformationalElementType = InformationalElementTypeEnum.LoadCaseFamily }
}).Data;

var families = client.GetInformationalElementsObject(infoIds.ToList()).Data;
foreach (var f in families)
    Console.WriteLine($"Family: {f.GetType().Name} — {(f as LoadCaseFamily)?.Name}");
```

---

### Example 4 — Seismic analysis (EN 1998-1)

```csharp
long seismicFamId = client.CreateInformationalElement(new LoadCaseFamily_SeismicEN_1998_1
{
    Name = "SEIS - Seismic EN1998",
    Implantation = new ImplantationSeismicEN1998
    {
        SpectrumType = SpectrumType_API.Type1,
        AgRg         = 0.16,  // g
        SoilClass    = SoilClassEN1998_API.B,
        S            = 1.2,
        TbImposedValue = 0.15,
        TcImposedValue = 0.5,
        TdImposedValue = 2.0
    },
    Structure = new StructureSeismicEN1998
    {
        ImportanceCategory = ImportanceCategoryEN1998_API.II,
        CoefGammaI         = 1.0,
        CoefQHoriz         = 3.9,
        CoefQHorizY        = 3.9,
        CoefQVert          = 1.5,
        DuctilityClass     = DuctilityClassEN1998_API.DCM
    },
    Method = new CalculationMethodEN1998
    {
        CalculMethod = CalculMethod_API.CQC,
        ResidualMode = true
    }
}).Data.Value;

// Seismic cases in X and Y
foreach (var (dirChoice, sign) in new[]
{
    (DirectionChoiceEN1998_API.X_Direction, SignChoice_API.Positive),
    (DirectionChoiceEN1998_API.X_Direction, SignChoice_API.Negative),
    (DirectionChoiceEN1998_API.Y_Direction, SignChoice_API.Positive),
    (DirectionChoiceEN1998_API.Y_Direction, SignChoice_API.Negative),
})
{
    client.CreateInformationalElement(new LoadCase_SeismicEN_1998_1
    {
        Name             = $"E_{dirChoice}_{sign}",
        LoadCaseFamilyID  = new EID { Value = seismicFamId },
        Direction = new DirectionConfigEN1998
        {
            DirectionChoice = dirChoice,
            SignChoice      = sign,
            Mode            = 1
        },
        Ductility = new DuctilityConfigEN1998
        {
                    DuctilityDetermination = DuctilityDeterminationEN1998_API.Auto,
                        DuctilityCoefficient   = 1.0
                    }
                });
            }

            ---

            > 📝 **Note:** This document was generated with the assistance of AI.

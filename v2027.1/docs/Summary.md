# Advance Design API 1.27.1 — Summary

What the Advance Design API can do, on a few pages. The details of every call and every object are in
`HELP.md`; the section numbers below (for example *HELP 3.6*) point to it. The complete list of properties,
with their descriptions, is in `swagger.json`.

---

## 1. What the API is

The Advance Design API is a local HTTP (REST) service, `AD.API.Srv.exe`. It runs the Advance Design engine
without the user interface. Your program sends requests to it to build a structural model, calculate it and
read the results.

| You need | What it is |
|---|---|
| `AD.API.Srv.exe` | The server. Default address: `http://localhost:52000` |
| `AD.API.Client.dll` | The C# client (`AD_Client`) |
| `PythonClient.zip` | The Python client |
| `swagger.json` | The OpenAPI description, for any other language |

One server works on **one project at a time** and runs **one request at a time**. To work on several
projects in parallel, start several servers on different ports. *HELP 2.*

---

## 2. A typical session

| Step | Call |
|---|---|
| 1. Check that the server is ready | `GetVersion`, `GetStatus` |
| 2. Create or open a project, and choose the design standards | `NewProject` / `OpenProject` |
| 3. Create materials and sections | `CreateMaterial`, `CreateSection`, `CreateUserSection` |
| 4. Create elements and supports | `CreateElement` |
| 5. Create load case families and load cases | `CreateInformationalElement` |
| 6. Create loads, or generate the wind and snow loads | `CreateElement`, `ProcessAction` |
| 7. Create or generate the combinations | `AddCombination`, `GenerateCombinations` |
| 8. Adjust the model settings (optional) | `SetModelSettings` |
| 9. Run the analysis | `LaunchAnalysis` |
| 10. Read the results | `GetResults`, `GetResultsByQuery` |
| 11. Run a design calculation and read its results (optional) | `LaunchDesignCalculation`, `GetDesignResults` |
| 12. Save and close | `SaveProject`, `CloseProject`, `CloseSession` |

Complete C# examples: *HELP 15*.

---

## 3. All endpoints

All paths start with `/api/Model/` except the two service endpoints.

### Project — *HELP 3.1*

| Endpoint | Method | What it does |
|---|---|---|
| `management/NewProject` | `POST` | Creates a project (`.fto`) and opens it, with its language, localization and design standards |
| `management/OpenProject` | `POST` | Opens an existing project |
| `management/GetEnvironment` | `GET` | Returns the language, localization and design standards in force |
| `management/SaveProject` | `POST` | Saves the open project |
| `management/SaveProjectAs` | `POST` | Saves the project under a new name and continues on the new file |
| `management/CloseProject` | `POST` | Saves the project, then closes it |
| `management/CloseProjectWithoutSave` | `POST` | Closes the project and discards the unsaved changes |
| `management/CloseSession` | `POST` | Saves and closes the project, then stops the server |
| `management/CloseSessionWithoutSave` | `POST` | Stops the server without saving the project |

### Materials and sections — *HELP 3.2, 3.3*

| Endpoint | Method | What it does |
|---|---|---|
| `materials/CreateMaterial` | `POST` | Creates a material, or returns the one that already has that name |
| `materials/GetListMaterials` | `GET` | Returns the identifiers of all materials |
| `materials/GetMaterials` | `POST` | Returns materials by identifier |
| `sections/CreateSection` | `POST` | Adds a section by its name (`IPE300`, `HEA200`, `R20*30`) |
| `sections/CreateUserSection` | `POST` | Creates a section from its characteristics and, optionally, its outline |
| `sections/GetListSections` | `GET` | Returns the identifiers of all sections |
| `sections/GetSections` | `POST` | Returns sections by identifier |

### Elements, loads and other model objects — *HELP 3.4, 3.5, 3.10*

| Endpoint | Method | What it does |
|---|---|---|
| `elements/CreateElement` | `POST` | Creates an element, a support or a load |
| `elements/UpdateElement` | `POST` | Changes an element, a support or a load |
| `elements/DeleteElements` | `POST` | Deletes elements, loads, load cases, combinations, systems or templates |
| `elements/AssignDesignTemplateToElements` | `POST` | Assigns a design template to several elements |
| `elements/CreateInformationalElement` | `POST` | Creates a load case family, a load case, a combination, a structural system, a release template or a design template |
| `elements/UpdateInformationalElement` | `POST` | Changes one of these objects |
| `elements/GetElementsID` | `POST` | Finds objects by type and returns their identifiers |
| `elements/GetElementsObject` | `POST` | Returns elements, supports and loads by identifier |
| `elements/GetInformationalElementsObject` | `POST` | Returns load cases, combinations, systems and templates by identifier |
| `elements/ExportAdditionalEnums` | `POST` | Technical endpoint that publishes two enumerations; not needed in normal use |

### Combinations and settings — *HELP 3.6, 3.7*

| Endpoint | Method | What it does |
|---|---|---|
| `combinations/AddCombination` | `POST` | Creates one combination from load cases and coefficients |
| `combinations/GenerateCombinations` | `POST` | Generates all the combinations of the standard |
| `model/GetModelSettings` | `GET` | Reads one group of model settings |
| `model/SetModelSettings` | `POST` | Changes one group of model settings |
| `model/ResetModelSettings` | `POST` | Puts one group back to its defaults |

### Calculation and results — *HELP 3.8, 3.9*

| Endpoint | Method | What it does |
|---|---|---|
| `actions/ProcessAction` | `POST` | Runs an action on the model: the automatic generation of wind and snow loads |
| `analysis/LaunchAnalysis` | `POST` | Meshes the model and runs the finite-element analysis |
| `analysis/LaunchDesignCalculation` | `POST` | Runs the design calculation of one material family |
| `analysis/GetAnalysisModelStatus` | `GET` | Says what has been meshed and calculated |
| `analysis/GetResults` | `POST` | Returns the analysis results of one load case or combination |
| `analysis/GetResultsByQuery` | `POST` | Same, from a query object; also returns buckling modes |
| `analysis/GetDesignResults` | `POST` | Returns the steel design results |
| `analysis/GetMeshConnectivity` | `POST` | Returns the mesh of elements |
| `analysis/GetMeshNodesPosition` | `POST` | Returns the position of the mesh nodes |

### Service — *HELP 3.11*

| Endpoint | Method | What it does |
|---|---|---|
| `/api/Service/GetVersion` | `GET` | Returns the API version. Also the way to check that the server is running |
| `/api/Service/GetStatus` | `GET` | Says whether the server can work, and the state of its licence |

---

## 4. What you can put in a model

### Materials — *HELP 6*

`MaterialSteel`, `MaterialReinforcedConcrete`, `MaterialWood`, `MaterialWoodNorthAmerica`, `MaterialRigid`,
`MaterialOther`. A plain `Material` with only a name gets a standard material of the Advance Design
catalogs (`S235`, `C25/30`).

### Sections — *HELP 6.5*

- Catalog and parametric sections, by name.
- `UserSection`: you give the area, inertias, moduli and shear areas, and optionally the outline as polygons
  and circles.

### Elements — *HELP 7, 8, 9*

| Kind | Classes |
|---|---|
| Linear elements (beams, columns, bars, ties, cables) | `ElementLinear` |
| Planar elements (slabs, walls, shells), with openings and edge releases | `ElementPlanar` |
| Punctual, linear and planar supports | `ElementRigid...Support`, `ElementElastic...Support`, `ElementTC...Support` (tension or compression only), `ElementAdvanced...Support` (non-linear) |
| Other structural objects | `ElementSinglePile`, `ElementMass`, `ElementLoadArea` (surface that carries climatic and distributed loads) |
| Reference geometry | `ElementPoint`, `ElementPolyLine` |
| Loads | `ElementLoadPunctual`, `ElementLoadLinear`, `ElementLoadPlanar`, `ElementImposedDisplacement` |

### Load case families and load cases — *HELP 10.1, 10.2, 12*

| Kind of load | Family classes |
|---|---|
| Dead, live, accidental, thermal, other | `LoadCaseFamily_DeadLoads`, `LoadCaseFamily_LiveLoads`, `LoadCaseFamily_Accidental`, `LoadCaseFamily_Thermic`, `LoadCaseFamily_Other` |
| Wind | `LoadCaseFamily_Wind_1991_1_4` (EN 1991-1-4), `LoadCaseFamily_WindIBC` (ASCE 7), `LoadCaseFamily_WindCBN` (NBC Canada), `LoadCaseFamily_Wind` (other standards) |
| Snow | `LoadCaseFamily_Snow_1991_1_3` (EN 1991-1-3), `LoadCaseFamily_SnowIBC` (ASCE 7), `LoadCaseFamily_SnowCBN` (NBC Canada), `LoadCaseFamily_Snow` (other standards) |
| Seismic | `LoadCaseFamily_SeismicEN_1998_1` (EN 1998-1), `LoadCaseFamily_SeismicIBC` (IBC / ASCE 7), `LoadCaseFamily_SeismicCBN` (NBC Canada), `LoadCaseFamily_Seismic` (other standards) |
| Push-over, dynamic temporal | `LoadCaseFamily_PushOver`, `LoadCaseFamily_DynamicTemporal` |
| Buckling | `LoadCaseFamily_AnalysisBuckling` (can be read, not created) |

Each family has a matching `LoadCase_...` class. The family to use for wind, snow and seismic loads follows
the standard chosen for the project. The wind and snow loads of a building can be generated automatically
with `ProcessAction` (`ClimaticAutoGeneration`).

### Combinations — *HELP 3.6, 10.3*

- By hand: `AddCombination`, with the list of load cases and their coefficients.
- Automatically: `GenerateCombinations`, for the combination standard of the project (Eurocode, US, Canada,
  greenhouses).
- `DesignCalculations` on a combination says which design calculations use it.

### Organisation of the model — *HELP 10.4, 10.5, 10.6*

| Object | What it is for |
|---|---|
| `StructuralSystem` | Groups elements, and describes building levels. Elements list their systems in `SystemIDs` |
| `LinearReleaseTemplate` | How an edge of a planar element is connected (free, pinned, fixed, or elastic) |
| `EC3SteelLinearDesignTemplate`, `AISCSteelLinearDesignTemplate`, `CSASteelLinearDesignTemplate` | Steel design template: buckling, lateral-torsional buckling, deflection and other design parameters, defined once and assigned to elements |

### Model settings — *HELP 3.7*

| Group (`eModelSettingsCategory`) | What it holds |
|---|---|
| `CombinationsGeneration` | Options of the automatic combination generation |
| `SteelEc3Calculation` | Steel design settings for Eurocode 3 |
| `SteelAiscCalculation` | Steel design settings for AISC 360 and CAN/CSA S16 |
| `Tolerances` | Geometric and mesh tolerances |
| `Structure` | Work space (plane, grid or 3D), bending stiff or truss, reference temperature |

---

## 5. Calculations and results

### Analysis — *HELP 3.8, 11*

`LaunchAnalysis` meshes the model and runs the finite-element analysis. `GetResults` then returns, for one
load case or combination and a list of elements:

| `ResultType` | What you get |
|---|---|
| `displacement` | Displacements and rotations |
| `forces` | Internal forces and moments |
| `stresses` | Stresses |
| `resultantforces` | Support reactions |
| `generalizedBucklingRes` | Buckling modes (with `GetResultsByQuery`) |

Results come per element: at the nodes, as diagrams along linear elements, and on both faces of planar
elements. `GetMeshConnectivity` and `GetMeshNodesPosition` return the mesh.

### Design — *HELP 3.8, 11*

`LaunchDesignCalculation` runs the Expert design of one family: `Concrete`, `Steel`, `Timber` or
`Composite`. It needs the analysis results, and at least one combination that lists the design calculation.

`GetDesignResults` returns the design results of **steel linear elements**, for Eurocode 3, AISC 360 and
CAN/CSA S16: cross-section resistance, stability, deflections, buckling lengths, section class, maximum work
ratio, fire resistance (Eurocode 3), cold-formed verifications and profile optimisation. *HELP 3.8* lists
what is available for each standard.

---

## 6. Rules to remember

1. **Units are SI**: m, N, N·m, Pa, kg/m³, rad, s. A steel Young's modulus is `2.1e11`.
2. **The standards are chosen once**, by `NewProject` / `OpenProject`. Always send `Localization`. To change
   the standards, close the project and open it again. *HELP 3.1.1*
3. **Reopen a project with the same settings.** The project file does not store its localization or design
   standards.
4. **Check every answer.** `Details.Success` says whether the call worked; `Details.Diagnostics` says why
   not. For calls that return `true` / `false`, check `Data` too. *HELP 4, 14*
5. **Ask for the warnings.** Set `LogVerbosity` to `ErrorsAndWarnings`. In C# an `Environments` created
   without it sends `Errors`, and warnings are not returned.
6. **Polymorphic objects need `$type`**, the name of their class. The C# client writes it; in Python and in
   raw JSON you write it yourself. *HELP 5*
7. **Names matter for materials and sections.** A name that already exists in the project, or a catalog
   name, returns that object and ignores the properties you sent.
8. **Order of the calculation chain**: settings and combinations first, then `LaunchAnalysis`, then
   `LaunchDesignCalculation`, then the results. A setting or a combination changed after the analysis is
   not seen by the design until the analysis runs again.
9. **Combinations created by hand take part in no design** unless you fill `DesignCalculations`. Generated
   combinations list their design calculations; send each one back with `UpdateInformationalElement` before
   a steel design, so that the default design coefficients are written. *HELP 3.6.2*
10. **Calculations save the project.** `CloseProjectWithoutSave` only discards what changed since the last
    save, and the analysis, the design calculation, the combination generation and the model settings all
    save.
11. **`CloseSession` stops the server.** Do not send it further requests.

---

## 7. Limits

- Design results can be read for steel linear elements only. Concrete, timber and composite designs can be
  launched, but their results cannot be read through the API.
- Design templates exist for steel linear elements only.
- The 2nd generation Eurocodes cannot be selected.
- Pile design cannot be launched.
- The server accepts requests from the local machine only, over HTTP.

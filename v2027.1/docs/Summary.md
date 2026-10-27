# Advance Design 2027 API v1.27.1 – Capabilities Summary

> **Source:** `swagger.json` (OpenAPI 3.0.4, Advance Design 2027 v1.27.1)  
> **Main tag:** `Model`  
> **Base path:** `/api/Model/`  
> **Service tag:** `Service` — base path `/api/Service/`, version, status (licence) & liveness (section 12)  

---

## 1. Project Management

| Endpoint | Method | Description |
|---|---|---|
| `/api/Model/management/NewProject` | `POST` | Creates a new project at the specified path (a non-empty `.fto` file name, absolute or relative to the server working directory set with `/w`), with the environment settings of the session (language, localization, log verbosity, design standards). The `Environments` body is mandatory — send at least `new Environments()` |
| `/api/Model/management/OpenProject` | `POST` | Opens an existing `.fto` project (it must exist; a relative path is resolved as for `NewProject`), on the same environment terms as `NewProject` |
| `/api/Model/management/GetEnvironment` | `GET` | Reads back the environment in force: language, localization, log verbosity, the eight design standards, the national annexes that apply, and the AISC method |
| `/api/Model/management/SaveProject` | `POST` | Saves the open project to its own file. `Data` true with `PROJECT_SAVED` (information) when saved; otherwise `Data` false with `PROJECT_NOT_OPEN` (no project open), `PROJECT_SAVE_FAILED` (the engine could not save it; the project stays open, as it was) or `PROJECT_SAVE_EXCEPTION` (internal error) |
| `/api/Model/management/SaveProjectAs?filename=...` | `POST` | Saves the open project to a new `.fto` (the complete project: model, analysis model, results and project folder content); the session then works on the new file, and the file opened before keeps its last saved state. A relative name is resolved against the server working directory (`/w`); the target folder must exist. It is Advance Design's File > Save As: it also writes the documents, design and selected templates folders; spaces around the name and before the extension are dropped, a name with the extension twice is refused (`PROJECT_INVALID_FILENAME`), and the path must stay 28 characters shorter than the Windows limit (260). Refused, before anything is written, with `PROJECT_NOT_OPEN`, `PROJECT_INVALID_FILENAME`, `PROJECT_INVALID_EXTENSION`, `PROJECT_FOLDER_NOT_FOUND`, `PROJECT_TARGET_EXISTS` (the file or its project folder already exists) or `PROJECT_TARGET_INVALID` (one project folder inside the other). `Data` true with `PROJECT_SAVED_AS` (information) when saved; otherwise `Data` false with `PROJECT_SAVE_AS_FAILED` (or `PROJECT_SAVE_AS_EXCEPTION` for an internal error), whose message says which project the session is on: "The session's project is still ..." - nothing was written (also the code for the engine-side refusals: a path over the length limit, a folder that cannot be written to), the open project is unchanged; "The session's project is now ..." - the session is on a possibly partial new copy: do NOT call `SaveProject` or `CloseProject`, close it with `CloseProjectWithoutSave`, check both folders and reopen the original |
| `/api/Model/management/CloseProject` | `POST` | Closes the current project, saving it first. To close it without saving, use `CloseProjectWithoutSave` |
| `/api/Model/management/CloseProjectWithoutSave` | `POST` | Closes the current project without saving it: what changed since its last save is discarded (the calculations, the combinations generation, the model settings and the deletion of a structural system save the project on their own). No parameters |
| `/api/Model/management/CloseSession` | `POST` | Closes the current session and shuts the local API host down (useful for parallel automation sessions). Returns at once; the shutdown follows about 5 seconds later, once a running or queued operation has finished. Saves and closes the open project (no need to call `CloseProject` first, though it is still fine), releases the licence, then the process exits. To close the open project without saving it, use `CloseSessionWithoutSave` instead. From this call on every other `api/Model/*` request is refused with HTTP 503 and error code `SERVICE_STOPPING`, and `GET /api/Service/GetStatus` answers `Stopping` until the server has exited. Calling it again is harmless and changes nothing: the close already requested goes on as it was asked (the first close requested decides how the project is closed; the `SESSION_CLOSING` message of a later call says it joined that close and what it does) |
| `/api/Model/management/CloseSessionWithoutSave` | `POST` | The same as `CloseSession`, except that a project still open is closed without saving: what changed since its last save is discarded, as with `CloseProjectWithoutSave`. The first close requested (`CloseSession` or `CloseSessionWithoutSave`) decides how the project is closed; a later call, or a stop of the host (Ctrl+C), joins it and changes nothing; the `SESSION_CLOSING` message always says what the close in force does (a call that joined an earlier `CloseSession`, or a host stop, reports that the project is saved). No parameters |

**Environment settings (`Environments`):**
- UI Language (`Language_Code`): `eLanguageFrench`, `eLanguageEnglish`, `eLanguageRomanian`, `eLanguageGerman`, `eLanguagePolish`, `eLanguageCzech`, `eLanguageDutch`, `eLanguageRussian`, `eLanguageSpanish`, `eLanguageEnglishUK`, `eLanguageHungarian`, `eLanguageChinese`, `eLanguageTaiwan`, `eLanguageJapanese`, `eLanguageKorean`, `eLanguageBulgarian`, `eLanguageGreek`, `eLanguageTurkish`, `eLanguageQuebec`, `eLanguageItalian`, `eLanguagePortuguese`, `eLanguagePortugueseBrazilian`, `eLanguageSlovak`, `eLanguageEndMark`
- Localization/standard (`Localization_Code`): `LOCALIZATION_ALGERIA`, `LOCALIZATION_ASIA`, `LOCALIZATION_CANADA`, `LOCALIZATION_CZECH_REPUBLIC`, `LOCALIZATION_EUROPE`, `LOCALIZATION_FRANCE`, `LOCALIZATION_GERMANY`, `LOCALIZATION_MAROCCO`, `LOCALIZATION_POLAND`, `LOCALIZATION_ROMANIA`, `LOCALIZATION_TUNISIA`, `LOCALIZATION_UK`, `LOCALIZATION_US`, `LOCALIZATION_ITALIA`, `LOCALIZATION_SLOVAKIA`, `LOCALIZATION_BULGARIA`, `LOCALIZATION_SPAIN`, `LOCALIZATION_PORTUGAL`
- Log verbosity (`LogVerboseLevel`): `Errors` / `ErrorsAndWarnings` / `AllDetails`
- Design standards and national annexes (`DesignCodes`, nullable): the eight rows of the Advance Design
  *Localization* configuration, settable one by one — `Combinations` / `CombinationsAnnex`, `Seismic` /
  `SeismicAnnex`, `Climatic` / `ClimaticAnnex`, `Concrete` / `ConcreteAnnex`, `Steel` / `SteelAnnex`,
  `Timber` / `TimberAnnex`, `Masonry` / `MasonryAnnex`, `Composite` / `CompositeAnnex`

**A session is configured once.** The environment is applied by the `NewProject` / `OpenProject` call that
*opens* the session and lasts exactly as long as that project. A `NewProject` / `OpenProject` sent while a
project is still open is refused **whole** (`PROJECT_ENVIRONMENT_REFUSED`), and nothing is created or
opened - close the project first. A reopened project runs under the environment of the call that opened it,
not under the one it was created with.

**The language is narrower still: fixed for the life of the SERVER PROCESS.** The first environment settles
it. An explicit `Language` that disagrees with the one in force is refused, with a diagnostic telling the
caller to restart the server; a language merely *implied* by `Localization` is held back with a warning
while that localization's eight design standards are applied normally.

**Send `Localization`.** Null is not a neutral default - it means "keep the eight standards this machine is
configured for", so the same request produces a Eurocode model on one server and an ACI / AISC model on
another.

**The design standards (`DesignCodes`).** Every property is nullable and null means "keep what
`Localization` resolved to", so an object with every property null changes nothing. Fourteen new
enumerations name them - `SeismicDesignCode`, `SeismicEurocodeAnnex`, `ClimaticDesignCode`,
`ClimaticEurocodeAnnex`, `ConcreteDesignCode`, `ConcreteEurocodeAnnex`, `SteelEurocodeAnnex`,
`TimberDesignCode`, `TimberEurocodeAnnex`, `MasonryDesignCode`, `MasonryEurocodeAnnex`,
`CompositeDesignCode`, `CompositeEurocodeAnnex` - alongside the existing `SteelDesignCode`,
`eCombinationsStandard` and `eCombinationsEurocodeAnnex`, which are reused rather than published a second
time. The rules:

- An annex sent for a standard that has none is **refused**, not dropped: the whole call fails. EN 1998-1 is
  the only seismic standard that takes one, and Eurocode 3, 5, 6 and 4 the only ones of their families;
  EN 1992's annex is written for NTC 2008 / NTC 2018 as well (Italy only), and EN 1991's for prEN 13031-1
  (general and France only) and for the Romanian standard (Romania only).
- A request that is **wrong anywhere applies nothing** - validation is a complete pass that writes nothing,
  application a second pass that cannot fail.
- **Two standards pin their own annex**, asked for or not: NTC 2008 / NTC 2018 concrete pins the Italian
  annex, the Romanian climatic standard pins the Romanian one.
- **Steelwork and composite are written independently**, unlike the desktop dialog which slaves them to
  each other; a pair the dialog could not have produced is a **WARNING**, not an error.
- **`can_csa_s16_2024` is refused as an input** - it calculates exactly as `can_csa_s16_2019`, which is the
  value to send.
- The **greenhouse** standards (`Greenhouse_EN13031_1`, `pr_en13031_1`) need an installation carrying the
  greenhouse computing option.
- **Send enumerators by name, never by number** - three of the lists are deliberately not contiguous and
  the generated clients renumber their copy from 0.
- **No 2nd generation Eurocode can be named**: no enumeration of the contract has a member for one, by name
  or by number.
- These values are **read-only where they appear on the settings themselves**
  (`CombinationsGenerationSettings.Standard` / `.Annex`, `SteelCalculationSettings.DesignCode`) precisely
  because this is the one place they are written.

`GetEnvironment` reports what the session **holds**, not an echo of the request, so it is also how to
discover what a localization resolves to. It fails with `ENVIRONMENT_NO_SESSION` when no project is open,
and reports a standard the contract cannot name - a 2nd generation Eurocode, CM66, BS 8110 - as `null` for
that one property, with a warning.

See `HELP.md` §3.1 and §3.1.1.

---

## 2. Materials

| Endpoint | Method | Description |
|---|---|---|
| `/api/Model/materials/CreateMaterial` | `POST` | Creates a new material in the current project |
| `/api/Model/materials/GetListMaterials` | `GET` | Retrieves a list of all material IDs in the project |
| `/api/Model/materials/GetMaterials` | `POST` | Retrieves material objects by their internal IDs |

**Supported material types:**
| Type | Description |
|---|---|
| `Material` | Generic material (identified by name, e.g. `S235`, `C25/30`) |
| `MaterialSteel` | Steel – E, ρ (ro), ν (nu), α (alpha), σe (sigmaE), G |
| `MaterialReinforcedConcrete` | Reinforced concrete – E, fck, fcu, fyk, Es, ν (nu) |
| `MaterialWood` | European timber – fm,k, ft0k, fc0k, fvk, E0.05, G0.05, gammaM |
| `MaterialWoodNorthAmerica` | North American timber |
| `MaterialRigid` | Rigid material |
| `MaterialOther` | Custom material with manually defined properties |

**`MaterialBehaviourType`:** `isotropic_behavior` / `orthotropic_behavior`

---

## 3. Sections

| Endpoint | Method | Description |
|---|---|---|
| `/api/Model/sections/CreateSection` | `POST` | Creates a cross-section by name (e.g. `IPE400`, `HEA200`, `R20*30`), looked up in the section database |
| `/api/Model/sections/CreateUserSection` | `POST` | Creates a **user section** — one whose sectional characteristics (and optionally its outline) are supplied by the caller — in the model only, never in the catalog database |
| `/api/Model/sections/GetListSections` | `GET` | Retrieves a list of all section IDs in the project |
| `/api/Model/sections/GetSections` | `POST` | Retrieves section objects by their internal IDs |

`Section` is polymorphic: it requires the `$type` discriminator, `"Section"` for a catalog or parametric
section and `"UserSection"` for a user-defined one. `GetSections` returns whichever applies.

**`Section` object properties:**
| Property | Type | Description |
|---|---|---|
| `Name` | `string` | Section name |
| `Type` | `SectionType_API` | Section shape type (e.g. `st_I_SYMETRIC`, `st_RECTANGULAR`, `st_CIRCULAR_TUBE`, etc.) |
| `FamilyCode` | `string?` | Family code |
| `CatalogName` | `string?` | Catalog name |

### 3.1 User sections (`UserSection`)

A user section carries no dimensions: everything the analysis and the design engines need is the set of
sectional characteristics the caller supplies. Advance Design **never** derives them from the outline, so
they all have to be given, and they are never checked against it.

All values are in **SI units**, like the rest of the API. `CreateUserSection` requires a `UserSection`
(`"$type": "UserSection"`); a plain `Section` is refused.

**`UserSection` properties** (inherits `Section`; `Type` is forced to `st_USER`, `FamilyCode` is `"USECT"`
and `CatalogName` is empty — all three are outputs):

| Property | Unit | Description |
|---|---|---|
| `Area` | m² | Sectional area A — must be > 0 |
| `Iy`, `Iz` | m⁴ | Bending inertias about the local y and z axes — must be > 0 |
| `Iyz` | m⁴ | Product of inertia — 0 for any section with an axis of symmetry, **signed** for the others, so a negative value is accepted (−42.53 cm⁴ for an L 80×80×8). Only its magnitude is bounded: `Iy·Iz − Iyz²` must stay > 0 |
| `It` | m⁴ | Torsional constant (J) — must be > 0 |
| `Iw` | m⁶ | Warping constant (Cw) — may be 0; needed for lateral torsional buckling |
| `WelyInf`, `WelySup` | m³ | Elastic moduli about y, lower / upper fibre |
| `WelzInf`, `WelzSup` | m³ | Elastic moduli about z, lower / upper fibre |
| `Wply`, `Wplz` | m³ | Plastic moduli about y and z |
| `Wt` | m³ | Torsional modulus |
| `Sy`, `Sz` | m² | Shear areas (European notation) |
| `Avy`, `Avz` | m² | Shear areas (US / Canada notation) |
| `ProfileType` | — | `UserSectionProfileType_API` — which standard shape the section behaves like, used by the steel design engines |
| `RollingType` | — | `SectionRollingType_API` — lamination type, drives the buckling curve. Default `Welded` |
| `Shape` | — | `UserSectionShape?` — the outline. `null` keeps the default 50 × 50 mm square |

**Validation.** Rejected: any value that is not finite (`"NaN"` / `"Infinity"` are valid JSON here, so
they do reach the endpoint), any negative value **except `Iyz`**, `Area`/`Iy`/`Iz`/`It` ≤ 0,
`Iy·Iz − Iyz² ≤ 0`, and a `ProfileType` or `RollingType` that names no value of its enumeration.
Reported as warnings but accepted: a zero `Iw`, `Wel*`, `Wpl*`, `Wt`, `Sy`/`Sz` or `Avy`/`Avz`; a
`ProfileType` left `Unknown`; a plastic modulus smaller than the elastic one; a shear area larger than
`Area`.

**Name collisions.** A user section of the same name is **updated in place** and its ID returned. A name
already used by a section of another type (a catalog section) is refused, so nothing is overwritten.

### 3.2 The outline of a user section (`UserSectionShape`)

The outline — what Advance Design and its cross-section editor (Sec.exe) call the *épure* — is the
geometry the section is drawn with. It is optional: leaving `Shape` null keeps the default outline, a
50 × 50 mm square centred on the origin.

**Rules**

1. **2D points, in metres.** Every point is a `(Y, Z)` pair in the plane of the cross-section: y is the
   horizontal axis, z the vertical one and positive upwards. There is no third coordinate.
2. **The origin is the centre of gravity.** Advance Design does not compute the centroid of a user
   section from its outline — it reports it as `(0, 0)` and uses that as the reference point when it maps
   member offsets. Place the outline with its centroid on the origin. Nothing recentres an outline that
   arrives through the API.
3. **Which way up matters for an asymmetric section.** `WelyInf` / `WelzInf` belong to the fibres at the
   lowest z and y, `WelySup` / `WelzSup` to those at the highest. The characteristics have to agree with
   the outline.
4. **A contour closes itself.** List a polygon's vertices once, in order; do not repeat the first point
   at the end. Two consecutive identical points are refused.
5. **Several contours describe several areas.** A compound section needs one material contour per part: a
   double angle is two, a battened box of four angles is four. The parts are not merged.
6. **A hole removes material.** `IsHole = true` on a contour inside a material contour gives a tube, a box
   or a cell. Either kind of contour can be a hole, in any order.

**`UserSectionShape`:**
| Property | Type | Description |
|---|---|---|
| `Contours` | `UserSectionContour[]` | The contours, at least one of which must add material. Stored in the order given |

**`UserSectionContour`:**
| Property | Type | Description |
|---|---|---|
| `Type` | `UserSectionContourType_API` | `Polygon` (a vertex list) or `Circle` (a centre and a radius) |
| `IsHole` | `bool` | `false` adds material, `true` removes it. Default `false` |
| `Points` | `UserSectionContourPoint[]` | A polygon's vertices (≥ 3), or a circle's single centre point |
| `Radius` | `double` (m) | A circle's radius, which must be > 0. Ignored on a polygon |

**`UserSectionContourPoint`:**
| Property | Type | Description |
|---|---|---|
| `Y` | `double` (m) | Coordinate along the section local y axis |
| `Z` | `double` (m) | Coordinate along the section local z axis, positive upwards |
| `FilletRadius` | `double` (m) | Radius rounding the corner at this vertex; 0 keeps it sharp. Polygons only |

**Outline validation.** Rejected: an outline with no contour; an outline made only of holes; a polygon
with fewer than 3 points; a circle without exactly one point or without a positive radius; two
consecutive identical points (a polygon closed explicitly by repeating its first point falls into this);
a negative `FilletRadius`; and a `FilletRadius` too large for its corner — its tangent length,
`radius / tan(angle / 2)`, has to stay shorter than both adjacent edges, because Advance Design would
otherwise draw a corrupted outline without reporting it. A fillet on a vertex that forms no corner (one
lying on a straight edge) is accepted with a warning, since it has no effect. Every coordinate, every
`FilletRadius` and every `Radius` has to be a finite number.

**Update semantics.** Supplying a `Shape` for an existing user section replaces its outline entirely;
leaving `Shape` null leaves the outline it already had untouched.

**Limitations.** A user section edited in Sec.exe can carry thin-walled contours besides its drawn
geometry, which feed the profile segmentation the Advanced Stability solver uses. `UserSectionShape`
cannot express them: reading such a section returns only its drawn contours, and supplying an outline for
it drops the rest — reported as a warning. Advance Design also draws an arc, a segment and a cambered
polyline, which have no equivalent here: they are reported as polygons — exact for a straight polyline,
which is what the default square is, an approximation otherwise — and the read warns about them.

**`UserSectionContourType_API`:** `Polygon` · `Circle`

**`SectionRollingType_API`:** `Unknown` · `Rolled` · `HotRolled` · `ColdRolled` · `Welded` ·
`ThickWelded` · `RolledColdFormed` · `BendColdFormed`

**`UserSectionProfileType_API`:** `Unknown` · `Angle` · `UnequalAngle` · `DoubleAngle` ·
`DoubleAngleDiagonal` · `Circular` · `Rectangular` · `Square` · `ISymmetric` · `IDissymmetric` · `U` ·
`T` · `TDissymmetric` · `CircularTube` · `RectangularTube` · `SquareTube` · `OvalTube` · `TubeInTube` ·
`RectangularChamfer` · `SquareOpenTube`

---

## 4. Structural Elements (geometric)

| Endpoint | Method | Description |
|---|---|---|
| `/api/Model/elements/CreateElement` | `POST` | Creates any geometric or load element |
| `/api/Model/elements/GetElementsID` | `POST` | Returns element IDs filtered by query |
| `/api/Model/elements/GetElementsObject` | `POST` | Returns the full data of elements by ID |
| `/api/Model/elements/AssignDesignTemplateToElements` | `POST` | Assigns one [Expert design template](#66-expert-design-templates) to a set of elements — or detaches them (`designTemplateId = 0`) — changing nothing else on them |

### 4.1 Linear Elements
- **`ElementLinear`** – column, beam, truss, cable
  - FEM type (`LinearElementFEMType_API`): `eLinearElementFEMTypeGeneral`, `eLinearElementFEMTypeCompositeBeam`
  - General beam type (`GeneralBeamType_API`): `bar`, `beamWStandardBending`, `sbeam`, `variablebeam`, `tie`, `strut`, `cable`, `rigid`, `CompositeBeamSimpleBeam`, `CompositeBeamSbeam`, `CompositeBeamVariable`
  - Properties: section orientation angle, eccentricity, inertia properties, end releases, haunch start/end, clipping, load area transfer
  - **`DesignTemplateId`** *(new)*: the [Expert design template](#66-expert-design-templates) applied to the element. `null` = assignment unchanged, `0` = detached back to the built-in default template
  - **`SectionOrientationPointId`** *(new)*: orients the cross-section **towards a geometric point**
    ([`ElementPoint`](#46-other-elements)) instead of by an angle. It is the point's **user identifier**
    (`UserID`), not its `EID`; `0` = none, `SectionOrientationAngle` applies instead. Setting it makes the
    element an observer of the point — moving the point re-orients the element, deleting it resets the
    property to `0`; an identifier matching no point is rejected. It has **no default**: omitting it on an
    update leaves the current value unchanged
  - **`LoadAreaLoadTransferProperties.EffectiveWindArea`** *(new)*: ASCE 7-22 Chapter 30 effective wind area
    (m², `double?`) of the 2D climatic wind walls generated from the element; model default 0.929 m² (10 ft²),
    `null` = keep the default on create / unchanged on update

### 4.2 Planar Elements
- **`ElementPlanar`** – slab, wall, shell
  - Types (`PlanarElementType`): `membrane`, `plate`, `shell`, `deformation_plane`, `steeldeck`, `layeredShell`, `slabonsteeldeck`, `planar_no_of_types`
  - Variable thickness (X/Y slope), eccentricity, detailed mesh
  - **`Openings`**: optional list of interior polygon holes (`ICollection<ICollection<Pt3D>>`). Only fully interior openings are accepted.
  - **`SelectedReleaseName`** *(new)*: name of the [linear release template](#65-linear-release-templates-edge-connections) applied to **every** edge — the "Releases / Side" property of the AD user interface.
  - **`LinearReleases`** *(new)*: `ICollection<EID>` — the template applied to **each** edge, one entry per edge (the "Releases / Definition" dialog).

#### Edge releases (`SelectedReleaseName` / `LinearReleases`)

| | `SelectedReleaseName` | `LinearReleases` |
|---|---|---|
| Granularity | One template for all the edges | One template per edge, in edge order: `LinearReleases[i]` = edge `GeomPtsList[i]` → `GeomPtsList[i+1]`, the last one closing on `GeomPtsList[0]` |
| Create | Optional; when omitted the built-in **"Fixed"** template is applied to every edge (same default as an element created in AD). A name matching no template falls back to "Fixed" with a warning | Optional; the number of entries must equal the number of edges and every id must exist, otherwise the whole list is rejected and no edge is changed |
| Read | The stored value: a template name when all the edges share one, a localized **"Other"** when they differ, empty when the releases were never initialized | The element's complete edge list (authoritative per-edge state) |
| Update | `null`/empty = unchanged; a non-empty value must name an existing template, otherwise it is ignored and a warning is logged (no fallback, unlike create) | Same validation as on create; an invalid list leaves every edge untouched. An element whose releases were never initialized is initialized with the default template first |

> **`SelectedReleaseName` takes precedence over `LinearReleases`** when both are provided — on create as
> well as on update, and sending both logs a warning. Send `LinearReleases` alone for a per-edge mix.
> Since the built-in names are localized, the invariant keywords `"free"`, `"pinned"` and `"fixed"` are
> also accepted (case-insensitive).

> The number of releases always follows the **outer contour** only — openings do not add edges.

> ⚠️ Changing `GeomPtsList` so that the **number of edges** differs **resets every edge release to the
> built-in "Fixed"** template (with a warning) — the indices no longer designate the same sides. A contour
> change keeping the same number of edges leaves the releases untouched, like the interactive model.
> Send `SelectedReleaseName` or `LinearReleases` in the SAME update to keep/apply a specific assignment.

#### Planar contour validity

`GeomPtsList` of any object with a planar contour (planar element, planar support, load area, surface load)
must be a valid planar polygon: ≥ 3 points, no coincident points, **no three consecutive collinear points**
(never insert a point in the middle of a straight side), all points in one plane, no self-intersection.
Otherwise the create is refused, and an update modifies **nothing** at all.

### 4.3 Punctual Supports
| Type | Behavior |
|---|---|
| `ElementRigidPunctualSupport` | Rigid – `fix` / `SupportHinge` / `other` (custom 6 DOF via `DegreeOfFreedomRestraints`) |
| `ElementElasticPunctualSupport` | Elastic – stiffness per DOF + seismic damping |
| `ElementTCPunctualSupport` | Tension/compression only (`compression` / `traction`) |
| `ElementAdvancedPunctualSupport` | Advanced – nonlinear diagram per DOF (free, fixed, elastic, NL-compression, NL-tension, NL-gap, NL-hardening, NL-diagram) |

### 4.4 Linear Supports
- `ElementRigidLinearSupport`, `ElementElasticLinearSupport`, `ElementTCLinearSupport`, `ElementAdvancedLinearSupport`

### 4.5 Planar Supports
- `ElementRigidPlanarSupport`, `ElementElasticPlanarSupport`, `ElementTCPlanarSupport`, `ElementAdvancedPlanarSupport`

### 4.6 Other Elements
| Type | Description |
|---|---|
| `ElementSinglePile` | Single pile with bearing capacity (compression, tension, lateral) |
| `ElementMass` | Concentrated mass (Mx, My, Mz + inertias Ix, Iy, Iz) |
| `ElementImposedDisplacement` | Imposed displacement/rotation at a node (Dx, Dy, Dz, Rx, Ry, Rz) |
| `ElementLoadArea` | Load area surface for climatic and distributed load transfer; `ClimaticProperties.EffectiveWindArea` *(new)* = ASCE 7-22 Chapter 30 effective wind area (m², `double?`, model default 0.929 m² = 10 ft²) |
| `ElementPoint` | Geometric point (the *Point* object of Advance Design): no stiffness, no load — forces an FE node at its position, snaps other elements onto it, pivot of the steel imperfection force direction |
| `ElementPolyLine` *(new)* | Geometric poly line (the *Polyline* object of Advance Design, listed as *Line*): a chain of straight segments with no stiffness, no material, no section and no load — reference geometry that forces FE nodes along its segments and that other elements snap onto |

- **`ElementPoint`** — created, read, updated and deleted through the generic element endpoints
  (`CreateElement`, `UpdateElement`, `GetElementsObject`, `DeleteElements`); queried with
  `ElementTypeEnum.ElementPoint`
  - `GeomPt` *(required)* — position in the global coordinate system (m), default `(0, 0, 0)`
  - `MeshActive` (`bool?`, default `true`) — the mesher forces an FE node at the point; `false` = ignored
    by the analysis model
  - `Density` (default `0` m) — local mesh density around the point (target finite-element size); `0` = the
    global mesh settings apply. Only used when `MeshActive` is `true`
  - `MagnetismStatus` (default `false`) / `MagnetismTolerance` (`double?`, default `0.001` m) — other
    elements whose end lies within the tolerance are snapped onto the point
  - `ImperfectionForceDirectionPivot` (default `false`) — pivot for the direction of the steel imperfection
    forces; stored for every model but only meaningful under Eurocode / 2nd generation Eurocode
  - `MeshActive` and `MagnetismTolerance` are **nullable on purpose**: omitting them keeps the Advance
    Design default (create) or the current value (update), where a non-nullable `bool`/`double` would have
    reached the server as `false`/`0`

- **`ElementPolyLine`** *(new)* — created, read, updated and deleted through the generic element endpoints
  (`CreateElement`, `UpdateElement`, `GetElementsObject`, `DeleteElements`); queried with
  `ElementTypeEnum.ElementPolyLine`
  - `GeomPtsList` *(required)* — the vertices in drawing order, in the global coordinate system (m).
    N points give **N-1 segments**
  - `MeshProperties` (`PolyLineMeshProperties?`, default `null` = the whole mesh definition is left alone)
    - `MeshActive` (`bool?`, default `false`) — the mesher forces FE nodes along the segments; `false` =
      pure reference geometry, ignored by the analysis model
    - `DetailedMeshStylePerEdge` (`ICollection<DetailedMeshOnSegment>?`, default `null` = untouched) — the
      mesh definition of each **segment**, in segment order: entry `i` is the segment
      `GeomPtsList[i]` → `GeomPtsList[i+1]`, so there are `GeomPtsList.Count - 1` entries. Same sub-object
      as the detailed per-edge mesh of `ElementPlanar` (`NumberOfElements`, `SizeOfElements`,
      `SpacingOption`, `SpacingFactor`). Only used when `MeshActive` is `true`
  - `MeshActive` is **nullable on purpose**: omitting it — or omitting `MeshProperties` entirely — keeps
    the Advance Design default (create) or the current value (update), where a non-nullable `bool` would
    have reached the server as `false`

#### Poly line geometry validity

A poly line is **not** a planar contour. `GeomPtsList` is a point list like the planar one, but it follows
the rules of the interactive *Polyline* command, and nothing more:

| Point list | Result |
|---|---|
| Fewer than **2 points** | **Refused** — there is no segment at all |
| A point **coincident with the one before it** | **Dropped**, not refused — Advance Design skips the zero-length segment the same way; a warning reports how many went |
| Every point coincident with the previous one | **Refused** — no segment survives |
| **Aligned** points, any number of them | **Accepted** — they make collinear segments |
| **Non-planar** / **self-intersecting** / any number of points | **Accepted** — no planarity test, no self-intersection test, no maximum |

A refused list is rejected **before anything is written**, so the element is not created and an update
modifies **nothing** at all. After de-duplication, the geometry that is read back and
`DetailedMeshStylePerEdge` are both indexed on the segments that really exist.

> To close a poly line, **repeat the first point as the last one** — that is how Advance Design stores a
> closed poly line, and it is what you read back: N distinct vertices come back as N+1 points and N segments.

> ⚠️ Changing `GeomPtsList` so that the **number of segments** differs resizes `DetailedMeshStylePerEdge`:
> the segments that already existed keep their definition at the same index, the new ones get the Advance
> Design defaults and the removed ones are dropped. A list sent in the SAME update is applied on top of the
> resized vector; a list shorter or longer than the segment count is not an error (the extra entries are
> ignored, the segments it does not cover keep their current values, and a warning is logged).

### 4.7 System membership (`SystemIDs`)

Every structural element type — `ElementLinear`, `ElementPlanar`, `ElementLoadArea`, `ElementMass`,
`ElementSinglePile`, `ElementPoint`, `ElementPolyLine` and the three support families (so all 12 support
types) — carries
a `SystemIDs` list: the [structural systems](#64-structural-systems-containers--building-levels)
(containers / building levels) the element belongs to. An element can belong to several systems.

| Operation | Behaviour |
|---|---|
| Create | Created in the **first** listed system and added to the others; empty/omitted = the model root system ("Structure 0"). A listed ID that is not an existing system makes the creation **fail** |
| Read | Filled with the element's current systems |
| Update | A non-empty list sets the membership to exactly those systems (added to the new ones, removed from the others); an empty list leaves it unchanged; an invalid ID leaves the membership untouched and makes the update return `false` |

> **A structural element always belongs to at least one system** and can never be left with none — that
> is why an empty list means "unchanged" and not "clear". An element that would end up with no system
> (deleted systems, or a membership that could not be applied) is put back in the root system
> "Structure 0" with a warning.

> Loads (`ElementLoadPunctual`, `ElementLoadLinear`, `ElementLoadPlanar`) and
> `ElementImposedDisplacement` have **no** system membership and no `SystemIDs`.

---

## 5. Loads

Loads are created via `CreateElement` using the following types:

| Type | Description |
|---|---|
| `ElementLoadPunctual` | Punctual force/moment (Fx, Fy, Fz, Mx, My, Mz) |
| `ElementLoadLinear` | Distributed linear load with linear variation (coeff1/coeff2) |
| `ElementLoadPlanar` | Distributed planar load with variation at 3 points |

---

## 6. Informational Elements (Load Cases, Combinations, Structural Systems, Linear Release Templates, Design Templates)

| Endpoint | Method | Description |
|---|---|---|
| `/api/Model/elements/CreateInformationalElement` | `POST` | Creates load cases, load case families, combinations, structural systems, linear release templates, and Expert design templates |
| `/api/Model/elements/GetInformationalElementsObject` | `POST` | Returns the full data of informational elements |
| `/api/Model/elements/UpdateInformationalElement` | `POST` | Updates an existing informational element by internal ID |

### 6.1 Supported Load Case Families

| Family | Standard |
|---|---|
| `LoadCaseFamily_DeadLoads` | Self-weight (configurable gravity field) |
| `LoadCaseFamily_LiveLoads` | Live loads (configurable category) |
| `LoadCaseFamily_Accidental` | Accidental loads |
| `LoadCaseFamily_Thermic` | Thermal loads (dilatation, gradient) |
| `LoadCaseFamily_Other` | Other loads |
| `LoadCaseFamily_PushOver` | Push-over analysis (distribution, direction, control) |
| `LoadCaseFamily_DynamicTemporal` | Dynamic temporal analysis |
| **`LoadCaseFamily_Wind`** | Generic wind |
| **`LoadCaseFamily_WindIBC`** | Wind IBC / ASCE 7-22 (US) |
| **`LoadCaseFamily_WindCBN`** | Wind NBC Canada |
| **`LoadCaseFamily_Wind_1991_1_4`** | Wind EN 1991-1-4 (Eurocode) |
| **`LoadCaseFamily_Snow`** | Generic snow |
| **`LoadCaseFamily_SnowIBC`** | Snow ASCE 7-22 / IBC (US) |
| **`LoadCaseFamily_SnowCBN`** | Snow NBC Canada (Sn 2010) |
| **`LoadCaseFamily_Snow_1991_1_3`** | Snow EN 1991-1-3 / NF EC1 (Eurocode) |
| **`LoadCaseFamily_Seismic`** | Generic seismic |
| **`LoadCaseFamily_SeismicIBC`** | Seismic IBC / ASCE 7-22 (US) |
| **`LoadCaseFamily_SeismicCBN`** | Seismic NBC Canada |
| **`LoadCaseFamily_SeismicEN_1998_1`** | Seismic EN 1998-1 (Eurocode) |

### 6.2 Key Parameters for Climatic Generation

**Wind IBC (`LoadCaseFamily_WindIBC`):**
- Basic wind speed V, exposure category (B/C/D), risk category (I–IV)
- Factors: Kzt, Kd, Ke, D_Gust, Ri
- Design method: `windDesign` integer (`0` = Chapter 28 Envelope Low-Rise Building, `1` = Scaffolding), torsional load cases
- Auto-generation: wind walls, pressure coefficients, split wind walls, load generation

**Snow IBC (`LoadCaseFamily_SnowIBC`):**
- Pg (ground snow load), altitude, W2 (winter wind parameter)
- Terrain category: B/C/D, Exposure: fully/partially/sheltered
- Thermal factor Ct, risk category

**Seismic IBC (`LoadCaseFamily_SeismicIBC`):**
- Ss, S1, site class (A–F), occupancy category (I–IV)
- Factors Fa, Fv, transition periods T0, Ts, TL (with calculated spectral accelerations SaT0, SaTS, SaTL)
- Per-case: structural type (`StructureTypeIBC`), ductility factors R/Cd (`DuctilityFactorsIBC`)
- Combination methods: SRSS, CQC, ABS + residual mode

**Wind EN 1991-1-4:**
- Wind speed, pressure, cDir, cSeas, structure base height
- Terrain category (0–IV), z0, Kr, Cs×Cd (auto/imposed)
- Dynamic parameters: delta, N, phi, correlation coefficient Kdc, turbulence KL
- Greenhouse-specific parameters (EN 13031-1)

**Seismic EN 1998-1:**
- Spectrum type (`SPECTRE_ELASTIC_1`, `SPECTRE_DESIGN_1`, `SPECTRE_ELASTIC_2`, `SPECTRE_DESIGN_2`), ag×γI, soil class (A–E)
- Periods Tb, Tc, Td; correction factor β
- Ductility factor q (`Q_CALCULATED` or `Q_IMPOSE`), ductility class (`LOW`/`MEDIUM`/`HIGH`)
- Direction choice: `X`, `Y`, `Z` (`XY` is not available and is applied as `X`); sign: `UNSIGNED`, `SIGNED_PREPONDERANT_MODE`, `SIGNED_SELECTED_MODE`

### 6.3 Combinations

| Endpoint | Method | Description |
|---|---|---|
| `/api/Model/combinations/AddCombination` | `POST` | Creates a load combination with coefficients |
| `/api/Model/combinations/GenerateCombinations` | `POST` | Generates the whole combination set automatically, the way the Combinations dialog does |
| `/api/Model/model/GetModelSettings` | `GET` | Reads the options that generation runs with, and which standard the session resolved to (`category = CombinationsGeneration`) |
| `/api/Model/model/SetModelSettings` | `POST` | Writes those options — partially: what is not sent is left alone |
| `/api/Model/model/ResetModelSettings` | `POST` | Puts back the values the active standard and annex prescribe (`category = CombinationsGeneration`) |

**`eCombinationType` values:** `eComboProjectSituationEluStrgeo`, `eComboProjectSituationEluEqu`, `eComboProjectSituationElsCharacteristic`, `eComboProjectSituationElsFrecvent`, `eComboProjectSituationElsQuasiPermanent`, `eComboProjectSituationELUA`, `eComboProjectSituationElsA`, `eComboProjectSituationNone`

> **Note:** `Combination.idComb` is **read-only** (managed internally by Advance Design). It should only be used when reading combinations, not when creating them.

**Design (Expert) calculations.** `Combination.DesignCalculations` lists which design calculations use the combination — the API replacement of the `ExpertSelection` bit mask. A design engine ignores every combination that does not name its calculation, and refuses the run when none does.

**`eCombinationDesignCalculation` values:** `ConcreteDesign`, `SteelProfiles`, `SteelConnections`, `SteelDeflections`, `SteelFire`, `TimberDeflections`, `TimberProfiles`, `TimberFire`, `PreCompositeDeflections`, `PostCompositeDeflections`, `PreCompositeProfiles`, `PostCompositeProfiles`

The coefficients each engine applies are four optional groups on the combination — `ConcreteDesign`, `SteelDesign` (profiles / deflections / connections / fire), `TimberDesign` (deflections / profiles / fire) and `CompositeDesign` (pre- and post-composite profiles and deflections). Every property is optional: one left `null` keeps the stored value or takes the default the Advance Design combinations dialog would apply. Listing a calculation without supplying its group creates it with those defaults and reports a warning.

> **Create vs Update:** on Create, an absent or empty list means "no design calculation". On Update, an **absent** list leaves the stored selection untouched, while an **empty** one takes the combination out of every design calculation.

> **Order matters:** set `DesignCalculations` before `LaunchAnalysis` — the analysis model copies the selection while it is built, and the design engines read that copy.

See `HELP.md` §3.6.1 and §10.3 for the full rules, the per-group properties and the defaults.

**Automatic generation.** `GenerateCombinations` builds the whole set from the load cases, the same
way the Combinations dialog does - `Simplified` for the template work-flow, `DetailedMatrix` for the
concomitance matrix stored in the model. Nothing about the standard is a parameter: the coefficients
come from the load cases and the generator options from the model settings, so the request only says
which work-flow to run.

Two things worth knowing before the first call:

- **Leave `UpdateLoadCaseCodes` unset** (it means *yes*). The templates match load cases on their
  normative code, and a load case created through the API carries a neutral one - with the refresh off,
  the generation returns nothing.
- **The generated combinations already carry their design selection**, derived from each combination's
  normative code. `GenerateCombinations` -> `LaunchAnalysis` -> `LaunchDesignCalculation` therefore
  works with no further setup, unlike a combination created by hand.

Generation is additive, the set is trimmed to `ModelProperties.MaxNumberMatrixGeneratedCombinations`
when it exceeds it (`LimitExceeded` says so), and "nothing generated" is reported as a warning rather
than an error.

Three things to know before relying on the numbers:

- **Leave `CombinationsCoefficients` unset** on a load case and on its family, as the generated
  clients do unless you assign it. Advance Design then fills both from their own type - dead
  1.10 / 1.35, live 1.50 with psi 0.7 / 0.5 / 0.3, snow 1.50 with 0.5 / 0.2 / 0, wind 1.50 with
  0.6 / 0.2 / 0. Sending a block replaces every one of those values, including the ones you did not
  mean to touch.
- **Both work-flows reproduce the desktop exactly**, pinned against sets exported from Advance
  Design 2027.0: 12 combinations for `Simplified` and 47 for `DetailedMatrix` on a D + L + S + W
  model - same terms, coefficients, order and numbering.

**The generator options.** Which project situations are generated and the gamma / psi table of each
action family are model settings, not properties of any combination, so they are written once with
`SetModelSettings` before generating. They are polymorphic on `$type`, one shape per
standard - `EurocodeCombinationsGenerationSettings` (Eurocode with any annex, Eurocode Italia, NTC
2018), `Eurocode2ndGenCombinationsGenerationSettings`, `USCombinationsGenerationSettings`,
`CanadaCombinationsGenerationSettings` and `GreenhouseCombinationsGenerationSettings` - and which one
applies follows the combination standard of the **session**: `Environments.DesignCodes.Combinations` on the
call that opened the project, or what `Localization` resolved to when that was left null. `Standard`
reports it and is read-only there.

They are one **group of the general model settings** of section 7 —
`eModelSettingsCategory.CombinationsGeneration` — so they have no endpoints of their own, and the
five shapes derive from `CombinationsGenerationSettings`, which is the class of the group and
carries `Standard` and `Annex`.

The 2nd generation Eurocode (EN 1990:2023) shape stays published so that the contract does not change the
day the standard ships, but the standard is **not released and not reachable**: `eCombinationsStandard` has
no member for it, so it cannot be asked for by name and its raw number is refused, and no response can name
it. On an installation where the 2nd generation Eurocodes are not enabled, a model that already generates
to it is **refused** by `GetModelSettings`, `SetModelSettings`, `ResetModelSettings` and
`GenerateCombinations` alike, rather than answered under EN 1990:2002. Two things about that shape differ
from the 1st generation - its per-family coefficients are lists of rows matched by `Family` rather than
four fixed groups, and nothing recomputes a coefficient from its category, because that derivation still
lives in the desktop options dialog.

Three rules carry over from the dialog:

- **A category dictates its coefficients.** Setting `LiveLoads.Category` recomputes psi0/psi1/psi2
  from the annex's table; a psi sent alongside it is ignored with a warning. Each family has a *user
  defined* category that unlocks them.
- **The live load category list is annex-dependent.** France has thirteen categories and the Czech
  annex eight, so `LiveLoads.Category` may only name the ones that mean the same there (A..D and
  A..H); use `LiveLoads.CategoryIndex` beyond that. `UserDefinedCategory` on the read tells you the
  annex's range.
- **At least one project situation and one leading variable action must stay on** - the same two
  rules the dialog enforces by refusing the last uncheck.

See `HELP.md` §3.6.2, §3.6.3, §3.7 and §10.3.

### 6.4 Structural Systems (containers / building levels)

Created through `CreateInformationalElement` with `$type` = `StructuralSystem`. A system groups
structural elements and can also represent a building level; the model is a tree of systems rooted at
**"Structure 0"**, present in every model.

| Property | Description |
|---|---|
| `UserName` | System name (inherited from `InformationalElementBase`) |
| `UserID` | System number shown in the UI, assigned by Advance Design (read-only; `0` = the root system) |
| `ParentID` | System to nest this one under (`0` = directly under the model root). **Create-time only** |
| `BehaviourCoefficientX/Y/Z` | Seismic behaviour factors, default `1`, must be `> 0`. Inherited: cascade to the sub-systems |
| `ImposedDirectionX/Y/Z` | Behaviour coefficient imposed per direction |
| `IsLevel`, `LevelTop`, `LevelBottom`, `LevelNumber` | Building-level flag, elevations (m, `LevelTop >= LevelBottom`) and level number |
| `Color`, `IsWallGroup` | Packed RGB colour, wall-group flag |
| `SubSystemIDs` | **Read-only:** the direct sub-systems, filled on read |

- **Listing:** `GetElementsID` with `QueryInfoModel { InformationalElementType = StructuralSystem }`
  returns every system — root included, at any depth, empty systems included. Design groups are excluded.
- **Root system:** only the behaviour coefficients and `IsWallGroup` can be updated on it; anything else
  (renaming included) is rejected. Any other system, top-level ones included, is fully editable.
- **Delete** (`DeleteElements`) is refused on the root system; on any other system it removes it with its whole sub-tree of sub-systems. The elements
  inside are **not** deleted — they lose that membership, and those left without any system are
  re-assigned to the root system "Structure 0" (with a warning).
- **Element membership:** every structural element type carries `SystemIDs` — see section 4.7.

### 6.5 Linear Release Templates (edge connections)

Created through `CreateInformationalElement` with `$type` = `LinearReleaseTemplate` (the API projection of
the AD model object `som.LinearRelease`). A template describes how an **edge of a planar element** is
connected to the rest of the structure. All templates live in a single **model-level pool**; planar element
edges only reference them, so editing a template changes every edge that uses it.

| Property | Description |
|---|---|
| `LinearReleaseName` | Template name, **unique** in the model, max 50 characters. Required on Create and Update. Referenced by `ElementPlanar.SelectedReleaseName` (the base-class `UserName` is not used) |
| `LinearReleaseType` | **Read-only:** `default_release` (built-in) / `user_defined_release` |
| `Tx`, `Ty`, `Tz`, `Rx` | Degree-of-freedom flags — `true` = released (free), `false` = fixed. `Rx` is the rotation about the edge axis; there is **no `Ry` nor `Rz`** |
| `StiffnessTx/Ty/Tz` | Elastic stiffness of the released translation (kN/m/m); `0` = perfectly free |
| `StiffnessRx` | Elastic stiffness of the released rotation (kNm/rad/m); `0` = perfectly free |
| `TxNonlinearityType`, `TyNonlinearityType`, `TzNonlinearityType`, `RxNonlinearityType` | `LinearReleaseNonlinearityType`: `none` / `compression_only` / `tension_only` |

**Built-in templates** — every model always contains three of them, at fixed pool positions in this order:

| # | Template | Tx | Ty | Tz | Rx |
|---|---|---|---|---|---|
| 1 | "Free" | released | released | released | released |
| 2 | "Pinned" | fixed | fixed | fixed | released |
| 3 | "Fixed" *(default for a new planar element)* | fixed | fixed | fixed | fixed |

> Their names are **localized** — list the pool instead of hard-coding a name, and identify the built-ins
> by their degrees of freedom. The three built-ins **cannot be modified nor deleted** through the API.

- **Listing:** `GetElementsID` with `QueryInfoModel { InformationalElementType = LinearReleaseTemplate }`
  returns the pool in order: "Free", "Pinned", "Fixed", then the user-defined templates.
- **Create:** a name that already exists makes the creation **fail** — it is never silently turned into an update.
- **Update:** renaming a template also updates the `SelectedReleaseName` of every planar element that used the old name.
- **Delete** (`DeleteElements`) of a template **in use succeeds**: the affected planar element edges are reset
  to the built-in "Fixed" template — only the edges that referenced it, the other edges keep their own —
  and a warning reports how many elements were affected. Deleting a built-in template is refused.
- **Edge assignment:** planar elements reference templates through `SelectedReleaseName` (all edges) and
  `LinearReleases` (per edge) — see section 4.2.

**`LinearReleaseType`:** `default_release` / `user_defined_release`
**`LinearReleaseNonlinearityType`:** `none` / `compression_only` / `tension_only`

### 6.6 Expert Design Templates

Created through `CreateInformationalElement` with `$type` = one of the three steel leaves below (the API
projection of the AD model object `som.Role`). A design template is a named set of Expert design properties
defined once at model level — the "Design template description" dialog — and then **referenced** by
structural elements: assigning it, or editing it afterwards, copies its properties onto the expert
properties of every element that uses it.

Templates exist per **material family** × **element type**, and within that per **design code**. The API
exposes the **steel × linear** set, in the three shapes the steel design codes fall into; the hierarchy is
built so the other material families slot in without changing the contract:

```
InformationalElementBase
└── DesignTemplate                              MaterialType, ElementType, AssignmentNumber (output only)
    └── LinearDesignTemplate
        └── SteelLinearDesignTemplate           DesignCode                                  (output only)
            ├── EC3SteelLinearDesignTemplate            ← the Eurocode properties
            └── NorthAmericanSteelLinearDesignTemplate  ← the groups AISC and CAN/CSA share
                ├── AISCSteelLinearDesignTemplate       ← + the AISC section class
                └── CSASteelLinearDesignTemplate        ← + the Class 1-4 section class and
                                                          the effective Class 4 characteristics
```

`NorthAmericanSteelLinearDesignTemplate` is an **intermediate base, not a creatable type** — Create and
Update require one of the two leaves under it.

The template name is **`UserName`** (from `InformationalElementBase`): mandatory on create and update,
unique in the model, max 50 characters, trimmed.

**The steel design code is a MODEL-WIDE setting**, not a per-template one: Advance Design allocates the
expert property set of the code the model carries, and only that one. Create and Update **refuse a leaf
that does not match the active code**, and the diagnostic names the leaf the model does accept.

| Steel design code of the model | `$type` to send |
|---|---|
| `eurocode`, `ntc2008`, `ntc2018` | `EC3SteelLinearDesignTemplate` |
| `aisc`, `aisc_2016` | `AISCSteelLinearDesignTemplate` |
| `can_csa_s16_2009`, `can_csa_s16_2014`, `can_csa_s16_2019`, `can_csa_s16_2024` | `CSASteelLinearDesignTemplate` |

Both AISC editions share one class, and all four CAN/CSA editions share one class: Advance Design
normalizes them to a single "AISC" and a single "CSA" expert set, and neither the model nor the property
list distinguishes the editions. A model configured for a **2nd generation Eurocode** is refused outright
on any installation where the 2nd generation Eurocodes are not enabled - there is no leaf to send it.

#### The Eurocode leaf (`EC3SteelLinearDesignTemplate`)

**Flat properties:** `SectionClass` (`SteelSectionClass`), `ManufacturingQuality`, `ExecutionClassType` /
`ExecutionClassValue`, `FamilyOfElement`, plus the class-4 effective characteristics (`ImposedValues`,
`Sxeff`, `ENy`, `ENz`, `Weff1y/2y/1z/2z`).

**Property groups** — each one an optional sub-object; a group left `null` is **skipped** and keeps what the
model already holds, which makes both create and update partial operations:

| Group | Contents |
|---|---|
| `Deflection` | Element and super-element deflection limits (1/n), reference length, verification location |
| `Buckling` | Lfy/Lfz, μ and system-length modes per axis, buckling coefficients, fire-situation lengths, the two **nullable** braced/unbraced node types, Cmy0/Cmz0, buckling curves, torsional buckling length, and the CAN/CSA-only `TorsionalBucklingVerification` flag — which must be left `true` on this leaf |
| `LateralTorsionalBuckling` | Lds/Ldi, LTB curve, and the full parameter set of each flange (restraints, k/kw, C1/C2/C3, load application point and type, f and fM coefficients, the reduction-factor method `CurveMethod`, and how Mcr and Mcr,0 are obtained — `McrMode`/`McrValue` and `Mcr0Mode`/`Mcr0Value`, each with its **own** enumeration) |
| `GlobalSwayImperfections` | Column imperfections (φy, φz, φ0, h, m, bow flags, super-element intersection detection, 2nd-generation-only φ0-automatic flags) and beam imperfections (weak axis, k factor, force-direction pivot). `E0Ly`/`E0Lz` round-trip but are inert — e0/L is a solver result, not an input |
| `ColdFormedDesign` | Cold-formed options: `IgnoreFyMz`, `ExactClass4Calculation`, `BucklingResistance` — which does have an effect, it forces the buckling verification on — and `DesignInMainAxesUuVv`, **read-only in practice**: `true` is refused |
| `FireVerification` | Fire resistance, exposed faces, emissivity, and the fire-protection sub-object |
| `AngleVerification` | Angle-section leg, bolt/hole diameters, bolt count, net areas and tension flags, and the effective slenderness ratio with its own K factor (`EffectiveSlendernessKMode`/`EffectiveSlendernessKValue`) |
| `WebBuckling` | Transverse stiffeners (sides, spacing, **list** of locations) and longitudinal stiffeners |
| `NonLinearFEM` | 2nd-order analysis: iterations, annex C.2, and the parameters — **lists** of punctual supports and distributed springs, initial deformation with its **list** of segments, loads offset |

> The four collections are **replaced**, not merged, on every update.

> **`Buckling.BucklingLength.YNodesType` / `ZNodesType` are that group's only nullable properties**, and
> the schema gives them no default: the braced/unbraced nature of the nodes is the one buckling property
> whose default follows the design code — AD stamps `braced` on a new AISC or CAN/CSA template and
> `unbraced` on a Eurocode one. Leave either out and the stored value is **kept**, which on a fresh template
> is the value the model's code dictates; only a value actually sent is written, and both always read back
> populated. So an update that sends the `Buckling` group but omits them no longer resets them to
> `unbraced` — for a newly created Eurocode template the effective value is the same either way.

**The rules of the design-template dialogs** are applied too, so a template built through the API can never
hold a state the user interface could not produce. The rules below are written for the Eurocode leaf; those
concerning a group the North-American leaves also carry (`Buckling`, `NonLinearFEM`, the per-flange lengths)
hold there too, the others have no North-American counterpart.

Refused input — the whole update fails and nothing is written: a flange with `LengthMode = imposed` and
`LengthValue <= 0`; a `Buckling.BucklingLength.TorsionalBucklingVerification` set to `false`, which is a
**CAN/CSA-only row** Advance Design hides under every other steel code — only a
`CSASteelLinearDesignTemplate` may set it, and the diagnostic names the property and that leaf; a
`RestraintDefinitionValues` longer than the 50-character SOM field; two `NonLinearFEM` punctual supports at
the same abscissa; two distributed springs over the same span; a transverse stiffener whose `RelativeValue`
is not strictly between 0 and 1; and a `ColdFormedDesign.DesignInMainAxesUuVv` set to `true`, a row that is
**read-only in practice** — see below.

Derived fields — what you read back is computed, not what you sent:
- **`Lfy`/`Lfz` follow the buckling mode**: `Lfy = LfyValue` when the mode carries a length (`imposed` or
  either LTB-length mode), `0` otherwise; for `Lfz` only `imposed` carries a length. Sending a bare `Lfy`
  with the default mode switches the mode to `imposed`, like typing it in the property list; if a mode and
  a contradicting `Lfy` are both sent, the mode wins.
- **`PhiY`/`PhiZ` are computed** from `Phi0`, `HValue`, `MValue`:
  `alphaH = clamp(2/sqrt(h), 2/3, 1)`, `alphaM = sqrt(0.5*(1 + 1/m))`, `phi = alphaH*alphaM/phi0`. The sent
  value is kept only when the formula cannot be evaluated. Changing `GlobalBowImperfectionsY`/`Z` clears
  the phi of that direction, like ticking the box in the property list.
- **Transverse stiffeners come back sorted by `RelativeValue`, with `AbscissaValue` always 0** — a template
  has no geometry, so the relative position is its only stiffener coordinate.
- **`UpperFlangeRestraintSummary` / `LowerFlangeRestraintSummary` are output only** — localized strings AD
  rebuilds from the flange restraints; a value sent is ignored.
- **`Lds`/`Ldi` and the per-flange `LengthValue` are one quantity stored twice**, and AD derives the first
  from the second. Two equivalent idioms: send the flange length alone and the flange's `LengthMode`
  switches to `imposed` with it (the property list), or send the flange's own `LengthMode` and
  `LengthValue` and leave the flange length at `0` (the dialog). The `0` default is never taken literally
  — under the Eurocode the derived length is the flange's `LengthValue` when the mode is `imposed`, `0`
  otherwise — so **an update that sends the LTB length group without the flange-parameters sub-object
  keeps the stored length**. Sending that sub-object does reset the flange properties left unset, as
  sending any group does.
- Enabling **`NonLinearFEM.RunNonLinearFEM` disables the buckling and LTB checks**, exactly as in the UI.

Two flags of `ColdFormedDesign` need a word of their own:

- **`DesignInMainAxesUuVv` is effectively read-only** — only `false` is accepted. It stays in the contract
  so a GET reports the stored value and a GET-then-PUT round trip keeps working, but Advance Design
  withdrew the row from the design template and from the linear element alike, so a template can only ever
  hold `false`. A `true` would not merely be ignored: the option survives as the *slave* of the element's
  own *Product of inertia Iyz = 0* row, and assigning a template copies it onto the element **and**
  overwrites that row with its inverse — so it would silently clear `ProductOfInertiaIyz0` on every element
  using the template. The equivalent user-facing option is `InertiaProperties.ProductOfInertiaIyz0` on the
  linear element (section 4.1); set that instead.
- **`BucklingResistance` is not unused**, contrary to what the AD model schema comment and the previous
  revision of these docs said: the conversion to the analysis model still reads it, and a `true` forces the
  buckling verification on. Prefer `Buckling.ElementVerification`, the direct way to turn that check on.

New enumerations on this leaf: `SteelLtbCriticalMomentMode` (`auto_mcr` / `imposed_mcr` /
`from_advanced_stability`), `SteelLtbReferenceCriticalMomentMode` (`auto_mcr0` / `imposed_mcr0` /
`same_as_mcr`) and `SteelAngleEffectiveSlendernessKMode` (`auto_k` / `imposed_k`). The first two replace a
plain integer on `McrMode` and `Mcr0Mode`; the third replaces the `SteelAngleDiameterMode` that
`EffectiveSlendernessKMode` was borrowing — that enumeration is unchanged and still backs
`BoltNominalDiameterMode` and `BoltHoleDiameterMode`. The two Mcr modes are **two different**
enumerations on purpose, because the third option of the two native lists is not the same one: Mcr can be
taken from the **non-linear FEM analysis** (`from_advanced_stability`, which Advance Design accepts **only
for a cold-formed section under the 2nd-generation Eurocode**), whereas Mcr,0 can be made **equal to Mcr**
(`same_as_mcr`). Only `imposed_mcr` uses `McrValue`, only `imposed_mcr0` uses `Mcr0Value`.

> ⚠️ **`SteelLtbCurveMethod` gained a fifth member, `second_generation_most_favorable`, and its members do
> not mean the same thing under the two Eurocode generations.** They are named after the 1st-generation
> option list, but under the 2nd generation Eurocode 3 - which this contract does not name, and which only
> reaches a design template where the 2nd generation Eurocodes are enabled - the very same stored integer is
> read against a different, longer list that agrees with the 1st-generation one **only on `general_case`**.
> On a 2nd-generation model `rolled_welded_profiles` selects *doubly-symmetric I and H sections*,
> `least_favorable` selects *buckling of the compression flange* and `most_favorable` selects *the least
> favourable*; `second_generation_most_favorable` is 2nd-generation only. The full ordinal-by-ordinal table
> is in HELP.md, Section 13. And Advance
> Design stamps `least_favorable` on every newly created template whatever the generation, and never
> converts it when the model switches generation — so a 2nd-generation template starts out on *buckling of
> the compression flange*, not on that generation's own default. Check the model's `SteelDesignCode` before
> setting `LateralTorsionalBucklingLength.UpperFlangeParameters` / `LowerFlangeParameters`.`CurveMethod`.

#### The North-American leaves (`AISCSteelLinearDesignTemplate`, `CSASteelLinearDesignTemplate`)

Same endpoints, same `UserName` rule, same assignment rules — only the property set differs. Compared with
the Eurocode leaf they have **no manufacturing quality, no execution class, no family of element, no global
sway imperfections, no fire verification and no web-buckling group**: Advance Design does not allocate those
sub-objects outside the Eurocode. They gain a `SteelSpecificProperties` group that has no Eurocode
counterpart at all.

**Flat properties**, the only thing that separates the two leaves:

| Leaf | Flat properties |
|---|---|
| `AISCSteelLinearDesignTemplate` | `SectionClass` typed **`SteelSectionClassAISC`** (`auto_class` / `compact` / `noncompact` / `slender_element`) — AISC 360 clause B4 classifies sections that way. No imposed effective characteristics: there is nothing for the class to enable |
| `CSASteelLinearDesignTemplate` | `SectionClass` typed **`SteelSectionClass`** (CAN/CSA S16 keeps the Class 1 to 4 list), plus the effective Class 4 characteristics `ImposedValues`, `Sxeff`, `ENy`, `ENz`, `Weff1y/2y/1z/2z` |

**Property groups** — the seven carried by `NorthAmericanSteelLinearDesignTemplate`, identical on both
leaves, and nullable exactly like the Eurocode ones:

| Group | Type | Contents |
|---|---|---|
| `NonLinearFEM` | `SteelNonLinearFEM` | Same object as the Eurocode group |
| `Deflection` | `SteelLinearDeflection` | Same object as the Eurocode group |
| `Buckling` | `SteelLinearBuckling` | Same object as the Eurocode group, but `BucklingLength.YNodesType`/`ZNodesType` default to **`braced`** here rather than to `unbraced` — see the rules below. `BucklingLength.TorsionalBucklingVerification` is a CAN/CSA-only row — the AISC property list hides it, so sending it `false` on an `AISCSteelLinearDesignTemplate` is **refused**; only the CAN/CSA leaf may set it |
| `LateralTorsionalBuckling` | `NorthAmericanSteelLinearLateralTorsionalBuckling` | **Different object.** Per flange: `UpperFlangeLength`/`LowerFlangeLength` (Lds/Ldi), the two restraint summaries, and a parameter set per flange (`IsProfile`, start/end restraint, length mode and value, and the **Cb** moment modification factor `CbMode`/`CbValue`). **No LTB curve** — AISC and CAN/CSA derive the resistance from the unbraced length and Cb, not from a tabulated curve |
| `SteelSpecificProperties` | `NorthAmericanSteelSpecificProperties` | **North-American only.** HSS design thickness (choice + derived value + stress-relieved flag), combined torsion and shear, web stiffeners (spacing, tension field), the shear lag factor Kt (`KtFactorCheck`/`KtFactorValue`), and the double-angle `ConnectorsSpacing` |
| `ColdFormedDesign` | `NorthAmericanSteelColdFormedDesign` | **Different object** (AISI S100, not EC3). Exact-calculation flag, overlap and overhang at the support, `KPhiE`, `BearingLength`, and the web-crippling conditions at the `ExteriorSupport` and `InteriorSupport` — three enum rows each, the entries of the AISI S100 clause G5 table |
| `AngleVerification` | `SteelAngleVerification` | Same object as the Eurocode group |

New enumerations: `SteelSectionClassAISC`, `SteelLtbFactorCbMode`, `SteelHssDesignThickness`,
`SteelCFDSupportCondition`, `SteelCFDFlangeCondition`, `SteelCFDFlangeLoadingType`.

**Rules specific to these two leaves:**

- **Two rows are CAN/CSA-only.** Sending a non-default `SteelSpecificProperties.StiffenersSpacing` or
  `SteelSpecificProperties.CheckForTorsionAndShear` on an **AISC** template is **refused** — the AISC
  property list hides those rows, so a payload can never store a value the desktop could not produce.
  Use a `CSASteelLinearDesignTemplate` for them.
- **A third row is CAN/CSA-only, and that one is shared with the Eurocode.** Sending
  `Buckling.BucklingLength.TorsionalBucklingVerification = false` on an **AISC** template is **refused**
  too — AD hides that row under every steel code except CAN/CSA S16. Only a
  `CSASteelLinearDesignTemplate` may set it to `false`.
- **The braced/unbraced node types default to `braced` under these two codes.**
  `Buckling.BucklingLength.YNodesType`/`ZNodesType` are **nullable and carry no schema default** — they are
  the one property of that shared group whose default follows the design code (`braced` on a new AISC or
  CAN/CSA template, `unbraced` on a Eurocode one). Leave either out and the stored value is kept; only a
  value actually sent is written, and both always read back populated. Sending the `Buckling` group without
  naming these two used to flip them to `unbraced` — a different structural assumption, hence a different
  design result.
- **`HssDesignThicknessValue` is output only** — derived from `HssDesignThicknessChoice` *and* from the
  active code: **0.93 / 1.00 under AISC, 0.90 / 1.00 under CAN/CSA**. Set the choice, read the value. The
  schema advertises **no default** for it — the number depends on the code, so there is none to state.
- **`UpperFlangeRestraintSummary` / `LowerFlangeRestraintSummary` are output only** and localized, exactly
  as on the EC3 leaf — AD rebuilds them from the flange restraints, so a value sent is ignored.
- **`UpperFlangeLength`/`LowerFlangeLength` are derived from the flange's `LengthValue`** — one quantity
  stored twice, as on the EC3 leaf, with the same two idioms (the flange length alone, or the flange's own
  `LengthMode` + `LengthValue` with the length left at `0`). Under the North-American codes the derived
  length follows the flange's `LengthValue` **whatever the mode**, so it carries a length for `imposed` and
  a **coefficient** for the computed modes. An update that sends the LTB length group without the
  flange-parameters sub-object therefore keeps the stored length; sending that sub-object does reset the
  flange properties left unset.
- A flange whose `LengthMode` is `imposed` must have `LengthValue > 0`, as on the EC3 leaf.
- ⚠️ **`ColdFormedDesign` is allocated under both codes but only listed by the AISC dialog** — the CAN/CSA
  template is missing its resource entry. Values sent for a CAN/CSA template do reach the design engine and
  the element; they just do not show in the desktop property list.

**Assignment** — `ElementLinear.DesignTemplateId` for one element,
`/api/Model/elements/AssignDesignTemplateToElements` for a whole selection. A template is applied only when
**all three** conditions hold:

1. it is a template of the **same kind of element** (linear template on a linear element);
2. it is of the **same material family** as the element — a steel element modelled as a **composite beam**
   belongs to the composite family, so a steel template cannot be assigned to it;
3. it carries the property set of the **design code active at the moment of the assignment** — a template
   only holds the properties of the code that was active when it was created.

| `DesignTemplateId` | Effect |
|---|---|
| `null` | Assignment unchanged |
| `0` | Element detached back to the built-in default template ("None") |
| an id | That template is assigned and copied onto the element |

- **Detaching is not a reset:** AD keeps the element's current expert values.
- ⚠️ **Changing the material of an element drops its design template** — AD recreates the expert object of
  the new material, and switching back does not restore the assignment. Re-assign after a material change.
- `AssignmentNumber` counts the referencing elements and follows every (re)assignment.
- **Listing:** `GetElementsID` with `QueryInfoModel { InformationalElementType = DesignTemplate }` returns the
  pool in order; the built-in default template is **not** part of it. Identify a template by EID or name,
  never by index.
- **Delete** of a template in use succeeds: the elements fall back to the default template and keep their
  values. Deleting the default template is refused.
- **Partial success:** in a mixed selection the elements that break a rule are reported individually in
  `Details.Diagnostics` and left untouched; the others are still applied and the returned value is `false`.

---

## 7. General Model Settings

The options that apply to the model **as a whole** rather than to one of its objects — what
the option dialogs of Advance Design edit, stored on the model itself. **Three endpoints for every
group of settings there is, and every group there will be:**

| Endpoint | Method | Description |
|---|---|---|
| `/api/Model/model/GetModelSettings` | `GET` | Reads one group of settings, named by `category` |
| `/api/Model/model/SetModelSettings` | `POST` | Writes one group — which one follows from the `$type` of the body |
| `/api/Model/model/ResetModelSettings` | `POST` | Puts back what the model's own standard prescribes for that group |

`ModelSettingsBase` is polymorphic on `$type`: one class per group of settings, and under that
class as many classes as the group has shapes — *which settings* (level 2), *in which variant*
(level 3), the same two-level arrangement as `InformationalElementBase` → `DesignTemplate`.

| `eModelSettingsCategory` | Class of the group | Shapes | Described in |
|---|---|---|---|
| `CombinationsGeneration` | `CombinationsGenerationSettings` | 5, one per combination standard | section 6.3 |
| `SteelEc3Calculation` | `SteelCalculationSettings` | 1, `Ec3SteelCalculationSettings`, for the Eurocode 3 family | section 7.1 |
| `SteelAiscCalculation` | `AiscSteelCalculationSettings` | 1, covering AISC 360, AISC 360-16 and the four CAN/CSA S16 editions | section 7.2 |

- **The object names the group on a write**, so there is no category parameter and no endpoint per
  group. `ModelSettingsBase` itself carries nothing and is refused; a new group of settings adds a
  value to `eModelSettingsCategory` and a class under `ModelSettingsBase`, and no endpoint.
- **Every property is optional** and what is not sent keeps the value the model has, which is what
  makes a `Get` → edit → `Set` round trip safe.
- **A property the model derives from another one cannot be imposed** — it is ignored with a
  warning rather than refused, so handing back what a `Get` returned never fails.
- **`SettingsCategory` on a read** says which group answered, so the object identifies itself
  without its `$type` having to be parsed. Read-only: a value sent in a write is ignored. Named
  `SettingsCategory` because a group's shapes have categories of their own (`LiveLoads.Category`).
- `eModelSettingsCategory.None`, which is what an omitted parameter binds to, is refused rather
  than answered for some default group.

See `HELP.md` §3.7.

### 7.1 Steel (Eurocode 3) calculation settings

The general options of the **Expert STEEL design calculation** — what the *Calculation settings* dialog
edits, tab by tab: which verifications are performed, the partial safety factors, the imperfection and
buckling options, the optimisation criteria, the profile sort order, the angle-verification bolt factors
and the calculation sequence. They belong to the model, not to any element — what a member contributes on
its own lives on that element or on its design template (section 6.6).

One group of the general model settings above, `eModelSettingsCategory.SteelEc3Calculation`, so **no new
endpoint**: `GetModelSettings`, `SetModelSettings` and `ResetModelSettings` as they already are.

**One shape, and a second generation behind a gate.** `Ec3SteelCalculationSettings` is the only shape: the
two generations share every page of the dialog and differ by exactly three stored values, which live in
`SecondGeneration` sub-objects of the pages they belong to. EN 1993-1-1:2023 is **not released**:
`SteelDesignCode` has no member for it, and on an installation where the 2nd generation Eurocodes are not
enabled a model configured with it is **refused** by this group rather than answered under the 1st
generation. Those sub-objects are therefore inert on an ordinary installation - a value sent in one is
ignored with a warning - and stay published so the shape does not change when the standard ships.

**Seven optional sub-objects, one per tab.** A `null` page keeps that whole tab; an unset property inside a
page keeps that property.

| Sub-object | Tab | Carries |
|---|---|---|
| `Verification` | Verifications | strength / stability, the calculation type, the fire verification and its time, the three imperfections, 2nd order analysis, the two buckling-length options, the imperfection generation, the plate buckling method |
| `SafetyFactors` | Partial safety factors | the Auto / Imposed option and γM0, γM1, γM2, γM,fi (+ η_v for the 2nd generation) |
| `Optimization` | Optimisation | the grouping mode, the work-ratio and deflection triggers with their percentage limits, the search range and its iteration ceiling |
| `ProfileSorting` | Sort profiles | the sort and ordering criteria, and the four section height / width limits |
| `Buckling` | Buckling | the buckling-length method |
| `AngleVerification` | Angle verification | e₁, e₂ and p₁, as multiples of the bolt hole diameter |
| `CalculationSequence` | Calculation sequence | which steps the run performs, chained optimisation and its iteration count |

- **The Eurocode family only** — EUROCODE, NTC 2008, NTC 2018. Under any other steel code all three calls
  are refused, naming the code; a North-American model is told to ask for `SteelAiscCalculation`
  (section 7.2) instead. `DesignCode` is read-only **here**: the steel code is a session setting, chosen
  with `Environments.DesignCodes.Steel` on the call that opened the project or inherited from
  `Localization` (section 1).
- **Call it before `LaunchAnalysis`.** The values are written to the descriptive model and copied into the
  analysis model — which is what the design engine runs on — while that model is built.
- **The four γ factors are derived unless imposed.** While the option is `eSafetyFactorsAuto` the code and
  its national annex decide them and a value sent for one is ignored with a warning; set the option to
  `eSafetyFactorsImposed` in the same request to have them stored.
- **A field whose control the dialog greys out is stored and reported, not refused** — the fire time, the
  optimisation limits, the iteration ceiling, the profile limits and the complete-calculation count each
  follow another switch. Send the pair together.
- **Units are SI**: the fire resistance time is in **seconds** (the dialog shows minutes) and the profile
  limits in **metres**.
- **The enumerations carry Advance Design's own member names** (`eTypeDeCalculTractionCompression`,
  `eMethodBucklingN1_N2`, …) because each is declared from one list of values shared with the desktop
  application, so the contract cannot drift from the dialog.
- **Five of the seven sub-objects are shared with the North-American group** and carry no `Ec3` prefix
  for that reason: `SteelOptimizationSettings`, `SteelProfileSortingSettings`, `SteelBucklingSettings`,
  `SteelAngleVerificationSettings`, `SteelCalculationSequenceSettings`.
- **`ResetModelSettings` is the dialog's *Load defaults*** — it restores what a new model with this steel
  code and annex would carry, so a German project comes back with γM1 = 1.10.

See `HELP.md` §3.7.1.

### 7.2 Steel (AISC / CAN-CSA) calculation settings

The same *Calculation settings* dialog, for a model designed to a **North-American** steel code, as that
family sees it. One group of the general model settings above,
`eModelSettingsCategory.SteelAiscCalculation`, so again **no new endpoint**.

**Six steel codes, one shape.** `aisc`, `aisc_2016`, `can_csa_s16_2009`, `can_csa_s16_2014`,
`can_csa_s16_2019` and `can_csa_s16_2024` share `AiscSteelCalculationSettings` — every per-code branch in
the dialog lists exactly those six together and treats them identically. `DesignCode` says which one the
model carries; it is read-only **here** and follows the steel code of the session -
`Environments.DesignCodes.Steel` on the call that opened the project, which takes five of the six
(`can_csa_s16_2024` is refused as an input), or `Localization` when that was left null (`US` →
`aisc_2016`, `CANADA` → `can_csa_s16_2019`, `ASIA` → `aisc`).

**Six optional sub-objects, one per tab.** Five of them are literally the classes the Eurocode group uses —
those dialog pages carry no steel-code branch — so their rules, ranges, units and warnings are the ones
section 7.1 describes.

| Sub-object | Tab | Carries |
|---|---|---|
| `Verification` | Verifications | `AiscSteelVerificationSettings`: strength, stability, the calculation type — and nothing else |
| `Optimization` | Optimisation | shared with 7.1 |
| `ProfileSorting` | Sort profiles | shared with 7.1 |
| `Buckling` | Buckling | shared with 7.1; `eMethodBucklingGa_Gb` is the only accepted method |
| `AngleVerification` | Angle verification | shared with 7.1 |
| `CalculationSequence` | Calculation sequence | shared with 7.1 |

- **No `SafetyFactors` page.** The *Partial safety factors* tab is added only for the four Eurocode codes:
  a North-American verification works with φ (LRFD) or Ω (ASD) taken from the code, not with an editable
  γ_M. The γ fields still exist on the model and this group never touches them.
- **A Verification page of three controls.** The dialog hides the fire group, the three imperfections, the
  2nd order analysis, both buckling-length groups, the imperfection generation, the plate buckling method
  and the 2nd-generation groups for these codes. The model stores them all and a write here carries them
  across untouched.
- **`Buckling.Method` is `eMethodBucklingGa_Gb` and nothing else** — the tab enables the GA-GB radio
  alone. A new model is created with it, and `ResetModelSettings` restores it. Echoing back whatever the
  model already holds is always allowed.
- **`ChainedOptimization` raises nothing here** — the warning the Eurocode group returns needs a global
  sway imperfection, which this family has no control for.
- **The Kt / Kz lateral-torsional-buckling factors are NOT exposed**, although they are this family's own
  LTB input: the page that would edit them is never added to the dialog for any steel code, so exposing
  them would let the API produce a state the desktop application cannot. They are round-tripped untouched.
- **Design templates are a separate axis** — `SteelLinearDesignTemplate` covers this family too, through
  `AISCSteelLinearDesignTemplate` and `CSASteelLinearDesignTemplate` (section 6.6). These settings still
  apply to the whole model regardless of what any element carries.

See `HELP.md` §3.7.2.

---

## 8. Actions / Processing

| Endpoint | Method | Description |
|---|---|---|
| `/api/Model/actions/ProcessAction` | `POST` | Triggers actions on the model |
| `/api/Model/analysis/LaunchAnalysis` | `POST` | Launches the FEM calculation of the model |
| `/api/Model/analysis/LaunchDesignCalculation` | `POST` | Launches the Expert design calculation of one material family, on the whole model or on a set of elements |
| `/api/Model/analysis/GetAnalysisModelStatus` | `GET` | Reads the state of the analysis model: mesh, FEM, the four design experts, construction stages, and a single `OverallState` summary |

**Available actions (`AD_API_ActionType`):**
- `ClimaticAutoGeneration` – automatic generation of climatic loads (wind + snow) for existing families

**Design families (`DesignCalculationType`):**
| Value | Design engine |
|---|---|
| `Concrete` | Per the concrete design code of the model. BS 8110 and STAS 10107 are known to the engine but no longer offered by Advance Design, and `ConcreteDesignCode` does not carry them |
| `Steel` | EC3, CM66, AISC or CSA, per the steel design code of the model |
| `Timber` | EC5 or the North-American codes (O86 / NDS), per the timber design code of the model |
| `Composite` | EC4, AISC 360 or CSA S16, per the composite design code of the model |

Pile foundations have **no** entry here: the API cannot launch a pile foundation design. Advance Design does
track that fifth expert internally, but since it cannot be launched through the API it is not reported by
`GetAnalysisModelStatus` either - the status and the launch verb list exactly the same four families.

**Design calculation notes:**
- **`LaunchAnalysis` is a precondition:** the design engines verify their criteria against the finite-element
  results, so a model that was never calculated is refused with an explicit diagnostic. The API never runs the
  analysis implicitly.
- **At least one combination must list the calculation:** an engine only verifies combinations whose
  `DesignCalculations` names it, and refuses the run when none does (*"No case selected for the Steelwork
  Design calculation!"*). Since the property is optional, a combination created by hand without it takes part
  in nothing - combinations produced by `GenerateCombinations` already carry their selection, derived from
  each combination's normative code. Set it **before** `LaunchAnalysis` — the analysis model copies the selection while it is built,
  so a later change has no effect until the analysis is run again. See section 6.3.
- The **element list is optional** — omit it, or send an empty array, to calculate every element of the model.
  It is ignored for `Composite`, whose engine reads its own element set from the model (a warning says so).
- The **design code and the element kind are not parameters**: the design modules decide them from the code
  configured on the model and from what each element actually is.
- `Data` mirrors the engine's own per-family flag (`CalculationSteelDone`, `CalculationReinforcementDone`,
  `CalculationTimberDone`, `CalculationCompositeDone`), the same one the user interface reads; the flag name and
  value appear in `Details.Diagnostics`.
- Engine warnings are reported on the error channel, so a successful calculation can return `Data = true`
  together with `Details.HasErrors = true` — read the diagnostics. Use `LogVerbosity = AllDetails` to also get
  the informational summary.
- FEM results are not modified by the design calculation and stay retrievable afterwards. If the model changed
  since the last analysis, a warning reports it and the design continues on the existing results.

**Analysis model status (`GetAnalysisModelStatus`):**

A pure read - no element of the model is visited, and nothing is created, opened or calculated - so it can be
called freely to branch on the model's state instead of launching a calculation to find out. It fails only
when no model is open; a model with no analysis model is a *successful* answer with `NotCreated`.

`AnalysisModelStatus` is flat: `HasAnalysisModel`, `MeshState`, `FemIsCalculated`, `FemIsUpToDate`,
`FemCalculatedCasesCount`, `HasConstructionStages`, `AllStagesCalculated`, `OverallState`, plus `Design` -
one `DesignExpertStatus` (`Family`, `IsCalculated`, `SettingsChangedSinceCalculation`) per family, always all
four, in enum order. The families are named by the same `DesignCalculationType` this section's table lists,
so a caller reads a state and launches a calculation with one vocabulary.

| Enum | Values |
|---|---|
| `EMeshState` | `Mesh_no`, `Mesh_available` (complete), `Mesh_incomplete` (the calculation is still allowed to run on it) |
| `EAnalysisModelState` | `NotCreated`, `Created`, `Meshed`, `Calculated`, `OutOfDate` |

`eMeshState` shares its definition with Advance Design's own native mesh enumeration, which is why its members
are `mesh_*`; the generated clients render them `Mesh_no` / `mesh_no`.

`OverallState`, first match wins: `NotCreated` if there is no analysis model -> `OutOfDate` if calculated but
not up to date -> `Calculated` if calculated -> `Meshed` if a mesh exists -> `Created`. The design experts are
deliberately not part of the rule (`Design` answers that), and neither is the mesh tri-state
(`Mesh_incomplete` + `Calculated` is a legitimate state).

Two limitations to know before using it as a gate:
- **`FemIsUpToDate` is maintained by the desktop application only**, so a headless session never reports that
  *your* changes invalidated the results - and `OverallState` is never `OutOfDate` there. `false` is
  trustworthy; `true` only means "nothing reported an invalidation". Re-run `LaunchAnalysis` rather than
  asking this flag.
- **`IsCalculated` is a stored flag, not proof of results on disk.** The desktop application can set
  `CalculationSteelDone` on a path that calculated nothing, so a project saved from the user interface may
  report a family calculated with no results. Read `GetDesignResults` when it has to be certain.

`SettingsChangedSinceCalculation` is `null` when the model tracks nothing for that family - always for
`Concrete`, which has no such flag in the model schema. See `HELP.md` section 3.8.

---

## 9. FEM Analysis Results

| Endpoint | Method | Description |
|---|---|---|
| `/api/Model/analysis/GetResults` | `POST` | Returns FEM results for specified elements |
| `/api/Model/analysis/GetMeshConnectivity` | `POST` | Returns mesh connectivity (nodes per element) |
| `/api/Model/analysis/GetMeshNodesPosition` | `POST` | Returns the 3D positions of mesh nodes |

**Result types (`ResultType`):**
| Value | Description |
|---|---|
| `displacement` | Node displacements (Dx, Dy, Dz, Rx, Ry, Rz) |
| `forces` | Internal forces/efforts (Fx, Fy, Fz, Mx, My, Mz) |
| `stresses` | Stresses (Sxx, Sxy, Sxz, Von Mises) |
| `resultantforces` | Resultant forces (support torsors) |

**Result element types:**
- `ResNode` – results at node (displacements, forces, stresses)
- `ResElementLinear` – linear element results (nodes + diagrams along the element)
- `ResElementPlanar` – planar element results (nodes + top/bottom face torsors)
- `ResLinearSupport`, `ResPlanarSupport`, `ResPunctualSupport` – support reactions

---

## 10. Design Results

| Endpoint | Method | Description |
|---|---|---|
| `/api/Model/analysis/GetDesignResults` | `POST` | Returns Expert **design** results, per element and per mesh cell, for the families the design engine produced |

The read side of `LaunchDesignCalculation`. Scope today: **steel × linear elements × Eurocode 3**, both
generations. The request is polymorphic on `ResultsQueryDesign`; send
**`ResultsQueryDesignSteelLinear`** with `$type` set — anything else is refused with
`DESIGN_RESULTS_QUERY_INVALID`.

```jsonc
{
  "$type": "ResultsQueryDesignSteelLinear",
  "listElementsIDs": [ 12, 13 ],   // DESCRIPTIVE element ids, as everywhere else in the API
  "eResType":  "CrossSectionResistance",
  "eResultSet": "Standard"
}
```

> **The 2nd generation payloads are published, but the standard is not released.** `ResEc3G2*` and the
> "2nd gen" column below stay in the contract so that nothing changes the day EN 1993-1-1:2023 ships. On an
> installation where the 2nd generation Eurocodes are not enabled, a model carrying that code is refused by
> `GetDesignResults` rather than answered under the 1st generation, so nothing there produces them.

**Result sets (`SteelDesignResultSet`)** — not a filter over the types below; each selects a *different
engine* with its own families:

| Value | Families |
|---|---|
| `Standard` | the ambient EN 1993-1-1 verifications |
| `ColdFormed` | EN 1993-1-3, for cold-formed members |
| `AdvancedStability` | the non-linear FEM result set |

**Result types (`SteelLinearDesignResultType`)** — `All` expands to everything readable for the model's
generation and result set:

| Value | 1st gen | 2nd gen | Payload |
|---|---|---|---|
| `CrossSectionResistance` | yes | yes | `ResEc3Resistance` / `ResEc3G2Resistance` |
| `Stability` | yes | yes | `ResEc3Stability` / `ResEc3G2Stability` |
| `FireResistance` | yes | yes | `ResEc3FireResistance` / `ResEc3G2FireResistance` |
| `Deflection1`, `Deflection2` | yes | yes | `ResEc3Deflection` / `ResEc3G2Deflection` |
| `WebShearBuckling` | yes | yes | `ResEc3WebShearBuckling` |
| `BucklingLengths` | yes | yes | `ResEc3BucklingLengths` |
| `LateralTorsionalBucklingLengths` | yes | yes | `ResEc3LateralTorsionalBucklingLengths` |
| `CrossSectionClass`, `FireCrossSectionClass` | yes | yes | `ResEc3CrossSectionClass` / `ResEc3FireCrossSectionClass` |
| `FireTemperature` | yes | yes | `ResEc3FireTemperature` |
| `AdvancedStabilityResistance` | yes | yes | `ResEc3AdvancedStabilityResistance` |
| `MaxWorkRatio` | yes | **no** | `ResEc3MaxWorkRatio` |
| `CrossSectionInfo` | **no** | yes | `ResEc3G2CrossSectionInfo`; `ResAiscSectionInfo` under a North American code |
| `ColdFormedResistance` | yes | yes | `ResEc3ColdFormedResistance` |
| `ColdFormedDeflection1`, `ColdFormedDeflection2` | yes | yes | `ResEc3Deflection` / `ResEc3G2Deflection` |
| `ColdFormedSectionInfo` | yes | yes | `ResSteelColdFormedSectionInfo` |
| `ProfileOptimisation` | yes | yes | `ResSteelProfileOptimisation` |
| `HaunchSectionProperties` | yes | **no** | `ResEc3HaunchSectionProperties` |
| `BucklingLengthCoefficients` | yes | yes | `ResEc3BucklingLengthCoefficients` |
| `BucklingLengthNodeRestraints` | yes | yes | `ResEc3BucklingLengthNodeRestraints` |
| `GlobalSwayImperfectionForces` | yes | yes | `ResEc3GlobalSwayImperfectionForces` |
| `LocalBowImperfectionForces` | yes | yes | `ResEc3LocalBowImperfectionForces` |

The three per-generation gaps are limitations of the engine, not of the API: the 2nd-generation maximum
work-ratio accessor is a stub that returns zero, the 1st generation has no cross-section-info result, and
the 2nd writes no haunch file - it reports the same quantities as the haunch sections of
`ResEc3G2CrossSectionInfo` instead.
Requesting one for the wrong generation returns an empty list plus a diagnostic saying so.

The two imperfection-force rows have a further condition that is neither a code nor a generation: they
exist only for a model whose imperfections were actually generated. That takes the `GlobalSwayImperfections`
/ `LocalBowImperfections` switches on the steel calculation settings, the matching per-element switches on
the design template, and - for the sway - a **derived** non-zero phi, which comes from `Phi0Y`, `HValueY`
and `MValueY` rather than from the `PhiY` field, since that one cannot be imposed. A model without them
reports no element rather than an error, because "no imperfection was generated" is a legitimate state.

> **A 2nd-generation model currently produces fewer families than the table promises.** The rows marked
> readable for both generations share one struct *and one file*, and a 2nd-generation run writes only its own
> `EC3g2_*` files — so `BucklingLengths`, `LateralTorsionalBucklingLengths`, `CrossSectionClass`,
> `FireCrossSectionClass` and `FireTemperature` come back empty on such a model, with a diagnostic. Those
> quantities are not lost: for the 2nd generation they arrive as parameters of the stability and resistance
> equations instead. This reflects EN 1993-1-1:2023 still being under development in the design engine, so the
> API keeps advertising the families rather than hiding them — the day the engine writes those files they
> start working with no contract change.

**The last seven rows answer on a different type, and not all on the same one.**

`ColdFormedSectionInfo`, `ProfileOptimisation`, `HaunchSectionProperties`, `BucklingLengthCoefficients`
and `BucklingLengthNodeRestraints` are stored PER ELEMENT rather than per mesh cell, so they come back as
`ResDesignElementLinearSteelPerElement` - one `data` payload, no `resMeshes`.

`GlobalSwayImperfectionForces` and `LocalBowImperfectionForces` are stored PER COMBINATION, because they
are loads the engine ADDED to the model rather than verifications it performed on it - so there is nothing
for it to reduce over and no governing combination to keep. They come back as
`ResDesignElementLinearSteelPerCase`, whose `resCases` list pairs a `caseNumber` with a `data` payload
exactly as `resMeshes` pairs a `meshIndex` with one.

Same call and same query for both; a client switches on `$type` as it already must. Elements the engine
produced nothing for are absent rather than present and empty, which is how `HaunchSectionProperties`
reports only haunched elements, `ColdFormedSectionInfo` only cold-formed ones and
`GlobalSwayImperfectionForces` only columns that were in compression. Under AISC 360 and CSA S16
`ProfileOptimisation` likewise reports nothing when the model asked for no optimisation, because the
record the engine leaves behind in that case is entirely zeroed.

**Response shape** — one `ResDesignElementLinearSteel` per *element and family*, carrying `resMeshes`, one
entry per mesh cell, whose `data` is the polymorphic payload:

```
ResDesignElementLinearSteel { id, userID, designFamily, resultType, resMeshes[] }
  └── ResDesignMesh { meshIndex, data }
        └── ResEc3Resistance { checks[12], my, mny, mz, mnz, a, b, ... }
```

**Notes:**
- **`LaunchDesignCalculation` is a precondition.** The model-wide `CalculationSteelDone` flag is checked and
  a missing design is refused explicitly — no result is ever read from a stale or absent file.
- **Granularity is the mesh cell, and no envelope is computed.** Each payload also carries its own
  `meshPoint` / `pointNumber` — the abscissa index inside the cell, 0 to 4 — because every verification
  reaches its worst point independently. Compute your own maxima; `GetMaxResultsFor*` is deliberately not used.
- **Units are SI throughout** — N, N·m, m, m², N/m² — while the Advance Design reports print kN, kN·m and cm.
- **Values can be infinite or negative, and both are genuine.** A fully restrained member gives an infinite
  buckling length or critical moment, and interaction terms can go negative; the server emits the JSON named
  literals (`Infinity`, `-Infinity`) rather than clamping, so a client must not assume a finite non-negative
  number. Advance Design's own tables print `inf` and negative percentages for the same cells.
- **Test `isCalculated` before reading a check's numbers.** It reports whether the engine performed that
  check, and it is *not* the same as `verification` being non-zero — that mask records which code equations
  *apply*, and the engine fills it in either way. A check that was not performed carries zeros. The 2nd
  generation says the same thing with `exists`, and a cold-formed lateral-torsional buckling check that
  clause 6.3.1.2(4) allowed to be skipped says it with `isIgnored` — so a zero work ratio always has a flag
  next to it distinguishing "not verified" from "verified, no utilisation".
- **All three buckling curves use `SteelBucklingCurve`**, the six-member vocabulary with an a0 curve —
  including the lateral-torsional one. Advance Design does have a five-member lateral-torsional set, but it
  is the vocabulary of the design template *setting*; the engine converts to the six-member set before
  computing and stores every result in it. `auto_curve` on the lateral-torsional curve means the general case
  of clause 6.3.2.2, where alpha,LT is computed rather than read off a named curve ("Curve -" in the reports).
- **Cold-formed checks report `usedEquation`**, the EN 1993-1-3 formula actually applied — several clauses
  offer alternatives and the choice is not derivable from the reported quantities.
- **`ResEc3CrossSectionClass.sectionClass` is the class SETTING** (`Auto` unless the user imposed one). The
  classes the engine determined are in `classPerLoadingPattern`, one per pattern, and the class a given
  verification used is on that verification.
- `Deflection1` / `Deflection2` mean the first and second allowable-deflection criterion in **both**
  generations. The 1st generation stores one file per criterion; the 2nd keeps both in one and the API filters.
- **The element list is required.** Unlike `LaunchDesignCalculation`, an empty list is *not* "the whole model"
  — results are read per element, so an empty list is refused. Ids are **descriptive**; the response echoes
  the element's user-visible `userID`, which is a different numbering.
- Elements that are not linear, unknown ids, and elements whose user identifier cannot be read are each
  reported individually in `Details.Diagnostics` and skipped; the others are still returned.
- A family that was asked for **by name** and produced nothing is reported as an error rather than answered
  with a silent empty list. Requesting `All` does not do this.
- **Not covered yet:** super elements, planar elements, profile optimisation, cold-formed section info, and
  the concrete / timber / composite engines — even though `LaunchDesignCalculation` can run all four.

---

## 11. Auxiliary

| Endpoint | Method | Description |
|---|---|---|
| `/api/Model/elements/ExportAdditionalEnums` | `POST` | Exports auxiliary enums not directly used in model classes (`Wind_Directions_type`, `eLoadCaseType`) |

**`Wind_Directions_type`:** `DIRECTION_X_PLUS`, `DIRECTION_X_MINUS`, `DIRECTION_Y_PLUS`, `DIRECTION_Y_MINUS`, `GRCG_WIND_DIRECTIONS_NO`

**`eLoadCaseType`:** `eADLoadCaseNone`, `eADLoadCaseGravity`, `eADLoadCaseExploitation`, `eADLoadCaseSnow`, `eADLoadCaseWind`, `eADLoadCaseThermic`, `eADLoadCaseSeismic`, `eADLoadCaseAccidental`, `eADLoadCaseTemporal`, `eADLoadCaseOther`, `eADLoadCaseEnvelope`, `eADLoadCaseBuckling`, `eADLoadCaseNonLinear`, `eADLoadCaseModal`, `eADLoadCaseCombination`, `eADLoadCaseTrafficLoads`, `eADLoadCaseSnowAcc`, `eADLoadCasePushOver`, `eADLoadCasePushOverAnalysis`, `eADLoadCaseCraneLoads`, `eADLoadCaseConstructionStagesAnalysis`, `eADLoadCaseWindAccidental`

---

## 12. Service (version, status & liveness)

| Endpoint | Method | Description |
|---|---|---|
| `/api/Service/GetVersion` | `GET` | Returns the API version as a plain JSON string |
| `/api/Service/GetStatus` | `GET` | Returns whether the service can run model operations, and if not, why — today: whether it holds its licence, and what that licence allows |

The only two endpoints outside `/api/Model/`. Both need **no project, no model and no session**, and
neither is queued behind a running model operation or analysis.

**`GetVersion`**
- An HTTP 200 is at the same time the liveness check ("ping") of the service.
- The value is the version of the API **contract** (`"1.27.1"`) — the same as `info.version` in
  `swagger.json`, not the build number of `AD.API.Srv.exe`.
- No `ApiResponse` envelope and no diagnostics: the body is the string itself, e.g. `"1.27.1"`.

**`GetStatus`**
- Returns a bare **`ServiceStatus`** object — **no** `ApiResponse` envelope — and **always HTTP 200** while
  the service answers: the verdict is in the body.
- `ServiceStatus`: `schemaVersion` (`int`, currently `1`; rises when a section or a status value is added —
  do not fail on a higher one), `serviceVersion` (same string as `GetVersion`), `status`, `canRun`,
  `message` (`null` when `Ok`), `licence` (`LicenceStatus`, `null` = not known yet; with `Stopping`, the
  last snapshot taken).
- **Decide with `canRun`.** `status` is a **string, not an enumeration, on purpose**: `Ok`, `Starting`,
  `Licence_Unavailable`, `Initialization_Failed`, `Stopping` today — treat any other value as "not Ok".
  `Starting`: ask again shortly. `Licence_Unavailable`: fix the licence and restart the server (a missing
  licence is never checked out again). `Initialization_Failed`: counts only while there is no session — a
  later model request may recover it (it retries the creation), otherwise restart the server.
  `Stopping`: `CloseSession` was called; send nothing more (`api/Model/*` answers HTTP 503 `SERVICE_STOPPING`).
- **After `CloseSession`**: `Stopping` from the call until the process exits — never `Licence_Unavailable`.
  The licence section switches from the held snapshot to the released one (only `checkedAt` +
  `errorMessage`) once the queued close has run (after any running operation, the save of the open project and a 5 s wait); no refresh while stopping.
- **Forward compatibility**: only `status` and `canRun` are guaranteed never to break a client; do not fail
  on a higher `schemaVersion`, and ignore unknown properties. The enumerations inside the licence section
  (`DesignCalculationType`, `LicenceProtectionType`) follow the usual enum versioning — a new member is a
  contract change.
- `LicenceStatus` (a **snapshot**, `checkedAt` says when): `isAvailable` (the mandatory licence — the one
  value that decides; checked out once at startup and never re-checked, so a lost network lease is not
  detected), `errorMessage` (also set after release, and with `isAvailable` true when the licence details
  could not be read), `protectionType` (`LicenceProtectionType`: `NoLicence`, `Demo`, `Evaluation`,
  `Education`, `Commercial`, `Reseller`, `Internal`, `HomeUse`, `Unknown`), `isPermanent`, `daysToExpiry`,
  `designCalculations` (one `DesignCalculationLicence` entry — `type` + `isAvailable` — per design family
  the server knows; available = not refused for licence reasons at `checkedAt`, no promise about a later
  `LaunchDesignCalculation`, which does its own checkout and has other gates, e.g. the 2nd-generation EC3
  one).
- **`errorMessage` is user-relevant only**: the licence component's message cut at its first line starting
  with `Feature:`, `Server name:`, `License path:`, `FlexNet Licensing error` or another FlexNet context label
  such as `Filename:` / `Hostname:` (they expose licence server names, hosts and licence paths); the lines kept are joined with a space, and a generic description replaces an
  empty result. FlexNet text is raw English; Cryptlex / Nalpeiron text is short and localized. The full raw
  text and the back-end error code go to the **server log** only.
- **No serial number, licence server or licence path** is ever returned.
- Timing: the server checks its licence out at startup, as the first item of its work queue; while that
  runs, `GetStatus` waits **at most 30 s** and then answers `Starting`. Afterwards it never waits behind
  model work. A licence refresh is due when the licence is held, the queue is idle and the later of
  `checkedAt` and the last refresh attempt is at least 30 s old (a failing refresh is retried at most every
  30 s); it is waited for **2 s at most**, and since a full refresh takes about 2 s, the triggering call
  usually answers from the previous snapshot and the next call gets the new one.

---
> 📝 **Note:** This document was generated with the assistance of AI. Last updated: 2026-10-15 (swagger.json OpenAPI 3.0.4, Advance Design 2027 v1.27.1).

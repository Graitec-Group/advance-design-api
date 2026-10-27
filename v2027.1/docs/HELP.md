# Advance Design 2027 API v1.27.1 — Developer Reference (C#)

---

## Table of Contents

1. [Overview](#1-overview)
2. [Getting Started](#2-getting-started)
3. [API Endpoints Reference](#3-api-endpoints-reference)
   - 3.1 [Project Management](#31-project-management)
     - 3.1.1 [The environment of a session](#311-the-environment-of-a-session)
   - 3.2 [Model — Materials](#32-model--materials)
   - 3.3 [Model — Sections](#33-model--sections)
   - 3.4 [Model — Elements (geometric)](#34-model--elements-geometric)
   - 3.5 [Model — Informational Elements](#35-model--informational-elements)
   - 3.6 [Model — Combinations](#36-model--combinations)
     - 3.6.2 [Automatic generation of combinations](#362-automatic-generation-of-combinations)
     - 3.6.3 [Settings of the automatic generation](#363-settings-of-the-automatic-generation)
   - 3.7 [Model — General settings](#37-model--general-settings)
     - 3.7.1 [Steel (Eurocode 3) calculation settings](#371-steel-eurocode-3-calculation-settings)
     - 3.7.2 [Steel (AISC / CAN-CSA) calculation settings](#372-steel-aisc--can-csa-calculation-settings)
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

> 📝 **Last updated:** 2026-10-15 (swagger.json OpenAPI 3.0.4, Advance Design 2027 v1.27.1)

> ✨ **New in this update — saving a project:**
> - **`POST api/Model/management/SaveProject`** saves the open project to its own file; **`POST api/Model/management/SaveProjectAs?filename=...`**
>   writes the complete project to a new `.fto`, which from then on is the session's project.
> - **`POST api/Model/management/CloseProjectWithoutSave`** and **`POST api/Model/management/CloseSessionWithoutSave`** (no parameters):
>   close the project without saving it, discarding what changed since its last save. **`CloseProject`** and **`CloseSession`** are
>   unchanged (no parameters; they save first, then close). See [3.1](#31-project-management).

> ✨ **Earlier update — service status & licence:**

> - **`GET /api/Service/GetStatus`**: whether the service can run model operations, and if not, why —
>   today, whether it holds its licence and what that licence allows (licence kind, base-module expiry,
>   the design families it can run). No serial number, licence server or licence path is returned. Like `GetVersion` it needs no project, no
>   model and no session, and it is **not queued behind a running model operation or analysis**. It returns
>   a **bare `ServiceStatus` object, not an envelope**, always with HTTP 200 — decide with **`canRun`** —
>   see [Section 3.11](#311-service--version-status--liveness).
> - **`ServiceStatus.status` is a string, not an enumeration, on purpose**: `Ok`, `Starting`,
>   `Licence_Unavailable`, `Initialization_Failed` or `Stopping` (from `CloseSession` until the process
>   exits) today, more later. Treat any other value as "not Ok".
> - **Three new schemas**: `ServiceStatus`, `LicenceStatus`, `DesignCalculationLicence`, and one new
>   enumeration, `LicenceProtectionType` — see [Section 13](#13-enumerations-reference).
> - **The server now checks its licence out at startup**, as the first item of its work queue, instead of on
>   the first model request; a missing licence is visible through `GetStatus` before any model call.
> - **`Ec3ColdFormedEquation.eq8_59`**: Equation (8.59) of EN 1993-1-3:2024 (2nd generation) — total shear
>   stress checked against the shear buckling strength fbv instead of fya/√3.

> ✨ **Previous update — design standards and national annexes:**
> - **`DesignCodes.AiscMethod`** (`eAiscDesignMethod`: `AllowableStrengthDesign` / `LoadAndResistanceFactorDesign`):
>   the *AISC Method* of the same dialog — whether the AISC 360 Experts verify in ASD or LRFD. Accepted when
>   `Combinations` = `US` / `US2018` or `Steel` = `aisc` / `aisc_2016`; **ASD whenever `Localization` is sent**;
>   read back by `GetEnvironment` under the same condition — see
>   [Section 3.1.1](#311-the-environment-of-a-session).
> - **`Environments.DesignCodes`**: the eight design standards a session runs under — Combinations,
>   Seismic, Climatic, Reinforced Concrete, Steelwork, Timber, Masonry, Composite — and the national
>   annex beside each are now the **caller's** choice, sent on the `NewProject` / `OpenProject` call that
>   opens the session. Until now they could only be inherited from `Localization`; they still are, property
>   by property, wherever the request says nothing — see
>   [Section 3.1.1](#311-the-environment-of-a-session).
> - **Fourteen new enumerations** name them — `SeismicDesignCode`, `SeismicEurocodeAnnex`,
>   `ClimaticDesignCode`, `ClimaticEurocodeAnnex`, `ConcreteDesignCode`, `ConcreteEurocodeAnnex`,
>   `SteelEurocodeAnnex`, `TimberDesignCode`, `TimberEurocodeAnnex`, `MasonryDesignCode`,
>   `MasonryEurocodeAnnex`, `CompositeDesignCode`, `CompositeEurocodeAnnex` — alongside the existing
>   `SteelDesignCode`, `eCombinationsStandard` and `eCombinationsEurocodeAnnex`, which are reused rather
>   than published a second time under another spelling — see
>   [Section 13](#design-standards-and-national-annexes).
> - **`GET /api/Model/management/GetEnvironment`**: reads back what the session actually holds — the
>   language, the localization, the log verbosity and all sixteen standards and annexes — see
>   [Section 3.1](#31-project-management).
> - **The environment is configured once per session.** It is applied by the `NewProject` /
>   `OpenProject` call that opens the session and lasts exactly as long as that project; a further
>   `NewProject` / `OpenProject` sent while a project is still open is now **refused whole**
>   (`PROJECT_ENVIRONMENT_REFUSED`), and no project is created or opened.
> - **The language is fixed for the life of the server process.** An explicit `Language` that disagrees
>   with the one in force is refused and the caller told to restart the server; a language merely
>   *implied* by `Localization` is held back with a warning, while that localization's eight design
>   standards are applied normally.
> - **Send `Localization`.** `null` is not a neutral default — it means *keep the eight standards this
>   machine is configured for*, so the same request produces a different model on a different server.
> - **⚠️ The 2nd generation Eurocodes are not released, and no longer appear in the contract.**
>   `SteelDesignCode.second_generation_eurocode` and the 2nd generation member of `eCombinationsStandard`
>   are **gone** from their enumerations, and none of the fourteen new ones carries a member for a 2nd
>   generation Eurocode: one cannot be sent by name, and its raw number is refused. The DTO shapes stay
>   published — `Eurocode2ndGenCombinationsGenerationSettings` and its rows, `Ec3G2VerificationOptions`,
>   `Ec3G2SafetyFactorOptions`, the `ResEc3G2*` result payloads — but on installations where the 2nd
>   generation Eurocodes are not enabled a model that carries one of those codes is **refused** rather
>   than answered under the previous generation of the standard: by the combination settings, the steel
>   settings group, the design templates, `GenerateCombinations`, `LaunchDesignCalculation` and
>   `GetDesignResults` alike — see [Section 13](#design-standards-and-national-annexes).

> ✨ **Previous update — geometric poly line element:**
> - **`ElementPolyLine`**: the *Polyline* object of Advance Design (`som.EFFacadeCurve`, listed as
>   *Line* in the model tree) — a chain of straight segments with **no stiffness, no material, no
>   section and no load**, used as reference geometry: to drive the mesh (it forces FE nodes along its
>   segments) and to snap or construct other elements onto it — see
>   [Section 7](#7-data-models--elements-geometric). It is created, read, updated and deleted through the
>   **same generic element endpoints** as every other element (`CreateElement`, `UpdateElement`,
>   `GetElementsObject`, `DeleteElements`). **No new endpoint.**
> - **`ElementTypeEnum.ElementPolyLine`**: new query value listing every poly line of the model, for
>   `GetElementsID` — see [Section 13](#13-enumerations-reference).
> - **A poly line is not a planar contour.** Its `GeomPtsList` is a point list like
>   [`ElementPlanar`](#elementplanar)'s, but it follows the rules of the interactive *Polyline* command
>   instead: it is **open**, it is **not required to be planar**, it **may cross itself** and it may run
>   through **any number of aligned points**. To close it, repeat the first point as the last one.
> - **One mesh definition per SEGMENT.** `MeshProperties.DetailedMeshStylePerEdge` has
>   `GeomPtsList.Count - 1` entries, and the vector is resized automatically when a geometry update
>   changes the number of segments.

> ✨ **Earlier update — geometric point element:**
> - **`ElementPoint`**: the *Point* object of Advance Design (`som.EFFacadePoint`) — a positioned model
>   object with **no stiffness and no load**, used to drive the mesh (it forces an FE node at its
>   location), to snap other elements onto it, and — under Eurocode — to act as the pivot of the
>   direction of the steel imperfection forces — see [Section 7](#7-data-models--elements-geometric).
>   It is created, read, updated and deleted through the **same generic element endpoints** as every other
>   element (`CreateElement`, `UpdateElement`, `GetElementsObject`, `DeleteElements`). **No new endpoint.**
> - **`ElementTypeEnum.ElementPoint`**: new query value listing every point of the model, for
>   `GetElementsID` — see [Section 13](#13-enumerations-reference).
> - **`ElementLinear.SectionOrientationPointId`**: orients the cross-section **towards a point**
>   instead of by a fixed angle. It carries the point's **user identifier** (`UserID`), not its `EID`;
>   `0` = no orientation point, and `SectionOrientationAngle` applies instead — see
>   [`ElementLinear`](#elementlinear).
> - **`MeshActive` and `MagnetismTolerance` are nullable on purpose.** A non-nullable `bool` would reach
>   the server as `false` — and a non-nullable `double` as `0` — whenever the client omits it, so leaving
>   either **null** means "keep the Advance Design default" on create and "leave the current value
>   untouched" on update.

> ✨ **Previous update — service version & liveness:**
> - **`GET /api/Service/GetVersion`**: the first endpoint outside the `Model` tag. It returns the API
>   version as a plain JSON string and needs no project, no model and no session, so it also serves as
>   the liveness check ("ping") for the service — see [Section 3.11](#311-service--version-status--liveness).

> ✨ **Earlier update — steel design templates:**
> - **`AISCSteelLinearDesignTemplate`** and **`CSASteelLinearDesignTemplate`**: the Expert design template
>   for a linear steel element, for the two **North-American** families — AISC 360 / 360-16 and CAN/CSA S16
>   (2009, 2014, 2019, 2024). Same endpoints, same `UserName` and assignment rules as the Eurocode leaf;
>   only the property set differs — see [Section 10.6](#106-expert-design-templates). **No new endpoint.**
> - **`NorthAmericanSteelLinearDesignTemplate`**: the intermediate base carrying the seven groups the two
>   codes share. It is **not creatable on its own** — Create and Update require one of the two leaves.
> - **Six new sub-objects**: `NorthAmericanSteelLinearLateralTorsionalBuckling`,
>   `NorthAmericanSteelLateralTorsionalBucklingLength`,
>   `NorthAmericanSteelLateralTorsionalBucklingParameters` (the **Cb** moment modification factor in place
>   of the Eurocode C1/C2/C3), `NorthAmericanSteelSpecificProperties` (HSS design thickness, Kt factor, web
>   stiffeners, double-angle connector spacing), `NorthAmericanSteelColdFormedDesign` (AISI S100) and
>   `NorthAmericanSteelWebCripplingInfo`.
> - **Six new enumerations** — `SteelSectionClassAISC`, `SteelLtbFactorCbMode`, `SteelHssDesignThickness`,
>   `SteelCFDSupportCondition`, `SteelCFDFlangeCondition`, `SteelCFDFlangeLoadingType` — see
>   [Section 13](#13-enumerations-reference).
> - **Three more new enumerations, these on the Eurocode leaf** — `SteelLtbCriticalMomentMode`
>   (`auto_mcr` / `imposed_mcr` / `from_advanced_stability`), `SteelLtbReferenceCriticalMomentMode`
>   (`auto_mcr0` / `imposed_mcr0` / `same_as_mcr`) and `SteelAngleEffectiveSlendernessKMode`
>   (`auto_k` / `imposed_k`). The first two replace the plain integer that
>   `EC3SteelLateralTorsionalBucklingParameters.McrMode` and `.Mcr0Mode` used to be, the third replaces
>   the `SteelAngleDiameterMode` that `SteelAngleVerification.EffectiveSlendernessKMode` was borrowing.
>   The two Mcr enumerations are deliberately **not the same one**, because the third option of the two
>   native lists differs — Mcr can be taken from the advanced stability analysis, whereas Mcr,0 can be
>   made equal to Mcr. `SteelAngleDiameterMode` itself is unchanged and still backs
>   `BoltNominalDiameterMode` and `BoltHoleDiameterMode` — see
>   [Section 13](#13-enumerations-reference).
> - **⚠️ `SteelLtbCurveMethod` gained a fifth member, `second_generation_most_favorable`, and its members
>   do not mean the same thing under the two Eurocode generations.** The members are named after the
>   **1st-generation** option list, but under the 2nd generation Eurocode 3 the very same
>   stored integer is read against a different and longer list, which agrees with the 1st-generation one
>   only on `general_case`. On a 2nd-generation model `least_favorable` therefore selects *buckling of the
>   compression flange* and `most_favorable` selects *the least favourable*, while
>   `second_generation_most_favorable` is 2nd-generation only and does not exist under the 1st. Read the
>   note in [Section 13](#13-enumerations-reference) before setting it on a 2nd-generation model.
> - **⚠️ `ColdFormedDesign.DesignInMainAxesUuVv` is now *refused* when set to `true`.** It stays in the
>   contract — a GET reports the stored value and a GET-then-PUT round trip keeps working — but only
>   `false` is accepted, which makes it **effectively read-only**. Advance Design withdrew the row from
>   the design template and from the linear element alike; the equivalent user-facing option is
>   `ProductOfInertiaIyz0` on the linear element — see [Section 10.6](#106-expert-design-templates).
> - **`ColdFormedDesign.BucklingResistance` is NOT unused, contrary to what this document said.** The AD
>   model schema still marks the field as unused, but that comment is stale: the conversion to the
>   analysis model reads it, and a `true` forces the buckling verification on. Prefer
>   `Buckling.ElementVerification` for that — see [Section 10.6](#106-expert-design-templates).
> - **The design code is model-wide.** Create and Update **refuse a leaf that does not match the model's
>   active steel code**, and the diagnostic names the leaf the model *does* accept.
> - **⚠️ `Buckling.BucklingLength.TorsionalBucklingVerification` is CAN/CSA-only, and is now
>   *refused* elsewhere.** Advance Design hides that property-list row under every steel code except
>   CAN/CSA S16 — the Eurocode and AISC included — so sending it `false` on an
>   `EC3SteelLinearDesignTemplate` or on an `AISCSteelLinearDesignTemplate` now fails the whole create or
>   update, and the diagnostic names the property and `CSASteelLinearDesignTemplate` as the only leaf that
>   may set it. **This one also changes the Eurocode leaf**, whose payloads used to carry the field freely.
> - **⚠️ `Buckling.BucklingLength.YNodesType` / `ZNodesType` are now *nullable*, because their
>   default depends on the steel design code.** `CRoleManager::CreateLinearMetalExpert` stamps `braced` on
>   a new AISC or CAN/CSA template and `unbraced` on a Eurocode one, so no single default could have been
>   stated honestly — and the one that was stated was wrong for the North-American family: sending the
>   `Buckling` group on an AISC or CAN/CSA template without naming these two silently flipped them from
>   braced to unbraced, a different structural assumption and a different design result. Leaving either
>   **null** now means *keep what the template already holds*; only a value actually sent is written, and
>   both always read back populated. **This one also touches the Eurocode leaf**: an update that sends the
>   `Buckling` group but omits them no longer resets them to `unbraced`, it keeps their stored value — the
>   effective value on a freshly created Eurocode template is unchanged.
> - **`SteelSpecificProperties.HssDesignThicknessValue` no longer advertises a default value.** It is
>   output only and what it resolves to depends on the design code — 0.93 / 1.00 under AISC, 0.90 / 1.00
>   under CAN/CSA — so there is no single number the contract could honestly state.
> - **The per-flange lateral-torsional buckling length is derived, on both families.** The flange length
>   (`Lds`/`Ldi`) and the flange's own `LengthValue` are one physical quantity stored twice, so an update
>   that sends the LTB length group **without** the flange-parameters sub-object keeps the stored
>   length — see [Section 10.6](#106-expert-design-templates).
> - Hierarchy fix: **`SteelLinearDesignTemplate` now derives from `LinearDesignTemplate`** instead of
>   straight from `DesignTemplate`. No property and no `$type` value changed — the wire format is
>   identical.

> ✨ **Previous update — North-American steel calculation settings:**
> - **`AiscSteelCalculationSettings`**: the same *Calculation settings* dialog, for a model designed to
>   **AISC 360, AISC 360-16 or CAN/CSA S16** (2009, 2014, 2019, 2024). Six tabs instead of seven — that
>   family has no *Partial safety factors* page, and its *Verifications* page carries three controls
>   instead of thirteen — see [Section 3.7.2](#372-steel-aisc--can-csa-calculation-settings). Still **no
>   new endpoint**.
> - **`eModelSettingsCategory.SteelAiscCalculation`**: the new group name, for `GetModelSettings` and
>   `ResetModelSettings`. The two steel groups **refuse each other's models** and name the one to ask for.
> - **`SteelDesignCode`** now also carries `aisc`, `aisc_2016`, `can_csa_s16_2009`, `can_csa_s16_2014`,
>   `can_csa_s16_2019` and `can_csa_s16_2024`. The design *templates* of
>   [10.6](#106-expert-design-templates) accept those six codes too since the update above.
> - **Five sub-objects are now shared by the two steel groups and were renamed accordingly**:
>   `Ec3SteelOptimizationSettings` → **`SteelOptimizationSettings`**, `Ec3SteelProfileSortingSettings` →
>   **`SteelProfileSortingSettings`**, `Ec3SteelBucklingSettings` → **`SteelBucklingSettings`**,
>   `Ec3SteelAngleVerificationSettings` → **`SteelAngleVerificationSettings`** and
>   `Ec3SteelCalculationSequenceSettings` → **`SteelCalculationSequenceSettings`**. Their properties are
>   unchanged; the dialog pages behind them carry no steel-code branch at all.
> - **`AiscSteelVerificationSettings`**: the reduced *Verifications* page — `CrossSectionStrength`,
>   `ElementStability`, `CalculationType`.

> ✨ **Previous update — steel calculation settings:**
> - **`SteelCalculationSettings` / `Ec3SteelCalculationSettings`**: the general options of the Expert
>   STEEL design calculation — the *Calculation settings* dialog of Advance Design, tab by tab — as the
>   second group of the general model settings. Read, written and reset through the three endpoints that
>   were already there; **no new endpoint** — see [Section 3.7.1](#371-steel-eurocode-3-calculation-settings).
> - **`eModelSettingsCategory.SteelEc3Calculation`**: the new group name, for `GetModelSettings` and
>   `ResetModelSettings`.
> - **Seven sub-objects, one per tab of the dialog** — `Ec3SteelVerificationSettings`,
>   `Ec3SteelSafetyFactorSettings`, `SteelOptimizationSettings`, `SteelProfileSortingSettings`,
>   `SteelBucklingSettings`, `SteelAngleVerificationSettings` and
>   `SteelCalculationSequenceSettings` — plus `Ec3G2VerificationOptions` and
>   `Ec3G2SafetyFactorOptions` for the three fields EN 1993-1-1:2023 adds.
> - Eleven new enumerations (`eSteelCalculationType`, `eSteelBucklingLengthOption`,
>   `eSteelPlateBucklingMethod`, `eSteelSafetyFactorsOption`, `eSteelOptimizationMode`, …). Their members
>   carry **Advance Design's own native spellings**, because each enumeration is now declared from a single
>   list shared with the desktop application rather than mirrored — see
>   [Section 3.7.1](#371-steel-eurocode-3-calculation-settings).

> ✨ **Earlier update — user sections:**
> - **`POST /api/Model/sections/CreateUserSection`**: creates a cross-section whose sectional
>   characteristics are supplied by the caller — the API equivalent of the *User* tab of the cross-section
>   manager. The section is created in the model only; the user section catalog database is never
>   modified — see [Section 3.3](#33-model--sections).
> - **`UserSection`**: `Section` is now polymorphic (`$type`), and a user section carries its 17 sectional
>   characteristics, its `ProfileType` and its `RollingType` — see
>   [Data Models — Sections](#65-data-models--sections). `GetSections` returns it for every user section of
>   the model.
> - **`UserSection.Shape`** (`UserSectionShape`): the optional outline (the *épure*) — a list of polygon
>   and circle contours, material or hole, which describes any shape including compound ones (double
>   angles, double channels, battened boxes) and hollow ones. 2D points in the section local y and z axes,
>   in metres, with the origin on the centre of gravity. Left `null`, the section keeps the default
>   50 × 50 mm square.
> - Three new enumerations — **`UserSectionContourType_API`**, **`SectionRollingType_API`** and
>   **`UserSectionProfileType_API`** — see [Section 13](#13-enumerations-reference).

> ✨ **Earlier update — Expert design calculation:**
> - **`POST /api/Model/analysis/LaunchDesignCalculation`**: runs the Expert design engine of one material family (concrete, steel, timber or composite) on the whole model or on a chosen set of elements — see [Section 3.8](#38-model--analysis). This is the calculation that produces the design verifications (work ratios, buckling, lateral-torsional buckling, deflections, fire, …) on top of the finite-element results.
> - **`DesignCalculationType`** enumeration: the material family whose design engine is launched — see [Section 13](#13-enumerations-reference).
> - The finite-element analysis is a **precondition**: the design engines verify criteria against the FEM results, so `LaunchAnalysis` must have been run first. A model that was never calculated is refused with an explicit diagnostic.

> ✨ **Earlier update — Expert design templates:**
> - **`EC3SteelLinearDesignTemplate`** informational element: create / read / update / delete the Expert design templates of the model (the "Design template description" dialog of Advance Design), for the steel linear × Eurocode property set — see [Section 10.6](#106-expert-design-templates).
> - **`ElementLinear.DesignTemplateId`**: the template assigned to a linear element; `null` leaves the assignment unchanged, `0` detaches the element back to the built-in default template — see [`ElementLinear`](#elementlinear).
> - **`POST /api/Model/elements/AssignDesignTemplateToElements`**: assigns one template to a whole set of elements — or detaches them — in a single call, without touching any other property — see [Section 3.4](#34-model--elements-geometric).
> - **`InformationalElementTypeEnum.DesignTemplate`**: new query value listing every design template of the model.
> - 40 new enumerations describing the steel design parameters (`SteelSectionClass`, `SteelBucklingCurve`, `SteelLtbLoadType`, `SteelFireProtectionMode`, …) — see [Section 13](#13-enumerations-reference).

> ✨ **Earlier update — linear releases of planar elements:**
> - **`LinearReleaseTemplate`** informational element: create / read / update / delete the templates of the model release pool, which describe how an edge of a planar element is connected to the rest of the structure — see [Section 10.5](#105-linear-release-templates).
> - **`ElementPlanar.SelectedReleaseName`**: the template applied to **every** edge of the element (the "Releases / Side" property of the AD user interface) — see [`ElementPlanar`](#elementplanar).
> - **`ElementPlanar.LinearReleases`**: the per-edge assignment, one template EID per edge in edge order (the AD "Releases / Definition" dialog) — see [`ElementPlanar`](#elementplanar).
> - **`InformationalElementTypeEnum.LinearReleaseTemplate`**: new query value listing the whole release pool of the model, built-in templates included.
> - New enumerations **`LinearReleaseType`** and **`LinearReleaseNonlinearityType`** — see [Section 13](#13-enumerations-reference).

> ✨ **Earlier update — structural systems:**
> - **`StructuralSystem`** informational element: create / read / update / delete the Advance Design system containers and building levels — see [Section 10.4](#104-structural-systems).
> - **`SystemIDs`** on every structural element type (linear and planar elements, all 12 support types, single piles, masses, load areas): the systems the element belongs to, settable on create and on update — see [System membership](#system-membership--systemids). Loads and imposed displacements are not concerned.
> - **`InformationalElementTypeEnum.StructuralSystem`**: new query value listing every system of the model, empty ones included.

> ⚠️ **Breaking changes since last release:**
> - **`UserName` renamed to `UserID`** in `ElementBase` (and `InformationalElementBase`). Any serialized JSON using `"userName"` must be updated to `"userID"`.
> - **`WindDesign_WindIBC_API` enum removed**: the `windDesign` field of `LoadCaseFamily_WindIBC` is now an `int` (`0` = Ch28 Envelope Low-Rise Building, `1` = Scaffolding).
> - **`MaterialBase` removed**: `Material` is now the direct top-level base class for all material objects.

---

## 1. Overview

The **Advance Design API** exposes a REST/HTTP interface that allows external C# applications (add-ins, automation scripts, test harnesses) to:

- Open, create and close Advance Design projects.
- Populate the structural model with materials, sections, linear/planar elements and supports.
- Define load cases, load case families, climatic loads (wind, snow, seismic) and combinations.
- Trigger analysis and post-process finite-element results (displacements, forces, stresses).
- Configure Expert design templates and run the Expert design calculation (concrete, steel, timber, composite).
- Query element IDs and retrieve fully deserialised element objects.

All model responses are wrapped in a **typed envelope** (`BooleanApiResponse`, `EIDApiResponse`, `ResBaseListApiResponse`, …) that carries both the payload and structured diagnostics; only the two `Service` endpoints, `GetVersion` and `GetStatus`, return a bare value.

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

> ℹ️ **Server.** `AD.API.Srv.exe [/p <port>] [/w <folder>] [/console] [/h]` listens on **`http://localhost:52000`** by
> default (HTTP only, local machine only); `/p` changes the port, and `/w <folder>` sets the working directory of the server (see the *Working directory* bullet below).
>
> - **`AD_Client`** is the generated `swaggerClient` with `HttpClient.Timeout` set to *infinite*. A plain
>   `new HttpClient()` gives up after 100 s, which a long `LaunchAnalysis` or `LaunchDesignCalculation`
>   exceeds — so build the client on `AD_Client`, or set the timeout yourself.
> - **One request at a time.** Every `/api/Model/*` request runs on a single worker thread: parallel calls
>   are queued, and one server holds one session and one open project. For parallel work, start one server
>   per port. `GetVersion`, `GetStatus` and the `CloseSession` call itself bypass the queue (the close it starts is
>   queued behind every request already accepted; `GetStatus` may add a short licence refresh to it, only
>   when it is idle, and waits for that refresh 2 s at most).
> - **JSON conventions.** Property names are camelCase and enumerators travel **as names**; `NaN`,
>   `Infinity` and `-Infinity` are accepted and written as named literals everywhere. A missing body,
>   malformed JSON, an unknown `$type` or an unknown enumerator is answered with **HTTP 400** — an
>   `ApiException` in the generated client, not an envelope.
> - **OpenAPI.** `/swagger/v1/swagger.json` (and the UI at `/swagger`) is served only when the server runs with
>   `ASPNETCORE_ENVIRONMENT=Development`; the committed copy is
>   `Applications/API/AD.API.ClientHost/OpenAPIs/swagger.json`.
> - **C# enumerator names.** NSwag upper-cases the first character of every enumerator: the wire value
>   `eLanguageFrench` is `Language_Code.ELanguageFrench`, `bar` is `GeneralBeamType_API.Bar`, `fix` is
>   `RigidSupportConstraint.Fix`, `shell` is `PlanarElementType.Shell`. JSON bodies and the Python client keep
>   the wire spelling, which is the one listed in [Section 13](#13-enumerations-reference).
> - **Working directory (`/w`).** The folder must exist and be writable, else the server does not start (exit code `2`); the server does not create it, and there is no fallback. A relative `filename` of `NewProject` and `OpenProject` is resolved against it. With `/w`, `Log.log` is written in it, and temporary files go to its `Temp` subfolder. Without `/w`, the working directory is the folder the server was started from, and temporary files use the system TEMP. The calculation logs (`LogVerify.txt`, `LogExpertise.txt`, `Log.txt`) do not depend on `/w`: every host of the API engine writes them to `<Advance Design log folder>\API` (`AD.API.Srv`, and `AD.EC.Host` / `AD.EC.Gui` of the Engineering Component), one folder shared by every process, apart from the desktop's log folder; the folder is not deleted automatically. Give each parallel server its own `/w` and `/p`. Inside quotes, do not end the path with a backslash.
> - **SI units everywhere.** Lengths in m, forces in N, moments in N·m, stresses and moduli in Pa, mass density
>   in kg/m³, angles in rad (unless a property says otherwise).

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

### 2.4 Check that the service is up, and which version it is

`GET /api/Service/GetVersion` is the cheapest call in the API. It needs no project, no model and no
session, and it does not queue behind a running analysis, so a successful answer means the service is
up and able to reply — which makes it the natural "ping" before anything else.

```csharp
string version = client.GetVersion();    // "1.27.1"
```

```text
curl http://localhost:52000/api/Service/GetVersion
"1.27.1"
```

`GetVersion` answers "is the service up?". To know whether it can actually **run model operations** — in
particular, whether it holds its licence — ask `GET /api/Service/GetStatus` and decide with `canRun`:

```csharp
ServiceStatus status = await client.GetStatusAsync();
if (!status.CanRun)
    throw new InvalidOperationException($"Advance Design API not ready: {status.Status} - {status.Message}");
```

See [Section 3.11](#311-service--version-status--liveness) for what the values mean.

> ℹ️ **Note:** All code examples in this document are written in **C#** using the auto-generated `swaggerClient` class produced by NSwag. The underlying HTTP calls and JSON payloads are identical regardless of the language used — refer to the [Raw JSON examples in Section 5](#5-polymorphism--the-type-discriminator) when implementing a client in another language.

---

## 3. API Endpoints Reference

### 3.1 Project Management

#### `POST /api/Model/management/NewProject`

Creates a new Advance Design project file.

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| `filename` | query | `string` | Yes | Path of the `.fto` file to create, absolute or relative to the server working directory (`/w`; forward slashes are accepted). Its folder must already exist, or the call fails with `PROJECT_FOLDER_NOT_FOUND`: the server does not create it. Must be non-empty and free of characters that Windows does not allow in a file name (`PROJECT_INVALID_FILENAME`) and end in `.fto` (`PROJECT_INVALID_EXTENSION`), and have a name before the extension: `.fto`, `..fto` or `sub\.fto` are refused (`PROJECT_INVALID_FILENAME`), because the project folder would be the folder that contains the file |
| *(body)* | body | `Environments` | Yes | Language, localization, log verbosity and the design standards the session runs under. Send at least `new Environments()` |

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

> ⚠️ **This is the call that configures the session, and the only one.** The environment takes effect
> here, before the model is created, and lasts exactly as long as the project. A `NewProject` or
> `OpenProject` sent while a project is still open is **refused whole** — `PROJECT_ENVIRONMENT_REFUSED`,
> and the project is not created either. Call `CloseProject` first; the next project takes a new
> environment. See [3.1.1](#311-the-environment-of-a-session).

---

#### `POST /api/Model/management/OpenProject`

Opens an existing Advance Design project file.

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| `filename` | query | `string` | Yes | Path of the `.fto` file to open, absolute or relative to the server working directory as for `NewProject`. The file must exist (`PROJECT_FILE_NOT_FOUND`, which names the resolved full path), its name must end in `.fto` (`PROJECT_INVALID_EXTENSION`), and it must not contain characters that Windows does not allow in a file name, nor be only an extension (`.fto`, `..fto`, `sub\.fto`: no name before the extension) (`PROJECT_INVALID_FILENAME`) |
| *(body)* | body | `Environments` | Yes | Environment settings — on exactly the same terms as `NewProject` |

**Returns:** `BooleanApiResponse`

```csharp
BooleanApiResponse r = client.OpenProject(@"C:\Projects\existing.fto", new Environments());
```

> ⚠️ **A reopened project runs under the environment of the call that opens it**, not under the one it
> was created with — a project file does not carry its localization or its design standards back into the
> session. Send the same `Environments` you created it with, or read what you are actually getting with
> [`GetEnvironment`](#get-apimodelmanagementgetenvironment). The once-per-session rule is the same here as
> on `NewProject`: sent while a project is still open, the whole call is refused with
> `PROJECT_ENVIRONMENT_REFUSED`. See [3.1.1](#311-the-environment-of-a-session).

---

#### `GET /api/Model/management/GetEnvironment`

Reads back the environment **in force**: the language, the localization, the log verbosity and all sixteen
design standards and national annexes.

**No parameters.**

**Returns:** `EnvironmentsApiResponse` · `Data` is an [`Environments`](#311-the-environment-of-a-session).

Every standard and every annex comes back set, whether the `NewProject` / `OpenProject` request named it or
the localization resolved it. This is what the session **holds**, not an echo of what was asked for — which
makes it both the way to discover what a localization resolves to, and the way to confirm what an override
actually changed.

```csharp
var env = client.GetEnvironment();

Console.WriteLine(env.Data.Localization);                 // LOCALIZATION_FRANCE
Console.WriteLine(env.Data.DesignCodes.Concrete);         // eurocode
Console.WriteLine(env.Data.DesignCodes.ConcreteAnnex);    // france
Console.WriteLine(env.Data.DesignCodes.Steel);            // eurocode
Console.WriteLine(env.Data.DesignCodes.SteelAnnex);       // germany, if the request asked for it
```

**It fails when no project is open**, with `ENVIRONMENT_NO_SESSION`. The environment is a property of the
session and the session lasts exactly as long as the project, so outside one there is nothing to report —
and answering from the machine's own Advance Design configuration instead would describe the server's
installation rather than this session.

> ℹ️ **A standard the session carries but this API cannot name comes back as `null`** for that one
> property, with a warning naming the property; everything else in the response is unaffected. That is what
> a project the desktop application configured for a 2nd generation Eurocode, CM66 or BS 8110 reads as. It
> is deliberately not reported as a number: the generated clients renumber their copy of each enumeration,
> so a number would be read back as a **different** standard. The warning needs
> `LogVerbosity = ErrorsAndWarnings` or higher.

---

#### `POST /api/Model/management/SaveProject`

Saves the open project to its own file. `Data` is `true` and the diagnostics carry `PROJECT_SAVED` (information) when the project was saved. Otherwise `Data` is `false` with, as error: `PROJECT_NOT_OPEN` when no project is open, `PROJECT_SAVE_FAILED` when the engine could not save it (its own messages say why; the project stays open, as it was), `PROJECT_SAVE_EXCEPTION` for an internal error.

**Returns:** `BooleanApiResponse`

```csharp
client.SaveProject();
```

---

#### `POST /api/Model/management/SaveProjectAs`

Saves the open project to a new project file, which from then on is the session's project. It is the same Save As as Advance Design's (**File > Save As**): the model and the analysis model, the results, and the documents, design and selected templates folders of the project are written to the new project, so that it opens in Advance Design in the same state. The file opened before is left as it was at its last save. From this call on the session works on the new file: `SaveProject` and `CloseProject` write to it.

As in Advance Design, the spaces around the name and before the extension are dropped, a name holding the extension twice is refused (`PROJECT_INVALID_FILENAME`), and the path must stay 28 characters shorter than the Windows path limit (260).

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| `filename` | query | `string` | Yes | The name of the new project file, with the extension `.fto`. A relative name is resolved against the working directory of the server (the `/w` startup option, else the folder the server was started from); an absolute path is used as given. The folder that is to contain the file must already exist |

**Returns:** `BooleanApiResponse`

```csharp
BooleanApiResponse r = client.SaveProjectAs(@"C:\Projects\copy.fto");
```

The call is refused **before anything is written** with the following codes (the last one is raised by the engine side, as `PROJECT_SAVE_AS_FAILED`):

| Code | When |
|---|---|
| `PROJECT_NOT_OPEN` | No project is open |
| `PROJECT_INVALID_FILENAME`, `PROJECT_INVALID_EXTENSION` | As for `NewProject`, and `PROJECT_INVALID_FILENAME` for a name holding the extension twice |
| `PROJECT_FOLDER_NOT_FOUND` | The folder that is to contain the file does not exist (it is not created) |
| `PROJECT_TARGET_EXISTS` | The file or its project folder (the file's path without `.fto`) already exists — saving there would first empty it |
| `PROJECT_TARGET_INVALID` | The new project folder and the open project's folder would lie one inside the other |
| `PROJECT_SAVE_AS_FAILED` (message "The session's project is still ...") | The refusals the engine side makes before writing: a path longer than the limit above, a folder that cannot be written to |

When the project was saved to the new file, `Data` is `true` and the diagnostics carry `PROJECT_SAVED_AS` (information). When it was not, `Data` is `false` with `PROJECT_SAVE_AS_FAILED` (or `PROJECT_SAVE_AS_EXCEPTION` for an internal error), and the message says which project the session is on:

- **"The session's project is still ..."** — nothing was written. This is also the code for the engine-side refusals above (a path over the length limit, a folder that cannot be written to). The open project is unchanged and can still be saved and closed as usual.
- **"The session's project is now ..."** — the engine had begun: the session is on the new, possibly partial, copy. **Do NOT save it** (no `SaveProject`, no `CloseProject`): close it with `CloseProjectWithoutSave`, check both folders, and open the original again.

---

#### `POST /api/Model/management/CloseProject`

Closes the currently open project (does not shut down the API host). The project is **saved, then closed**; to close it without saving, call [`CloseProjectWithoutSave`](#post-apimodelmanagementcloseprojectwithoutsave). It returns `data = false` with the error `PROJECT_CLOSE_FAILED` when the project could not be saved or closed cleanly: it is closed all the same, the other diagnostics say why, and the changes made since the last save may be lost.

**Returns:** `BooleanApiResponse`

```csharp
client.CloseProject();  // saves, then closes
```

---

#### `POST /api/Model/management/CloseProjectWithoutSave`

Closes the currently open project **without saving it** (does not shut down the API host): what changed since its last save is discarded. "Its last save" means `SaveProject`, `SaveProjectAs`, and the operations that save the project on their own — the calculations, the combinations generation, the model settings and the deletion of a structural system. To save the project before closing it, call `CloseProject`. Like `CloseProject`, it returns `data = false` with the error `PROJECT_CLOSE_FAILED` when the project could not be closed cleanly.

**Returns:** `BooleanApiResponse`

```csharp
client.CloseProjectWithoutSave();  // closes, discarding what changed since the last save
```

---

#### `POST /api/Model/management/CloseSession`

Closes the current session **and requests the local API host to shut down**. Use this when running multiple parallel automation sessions and you want to release resources. The call **returns at once**; the shutdown follows about 5 seconds later. It saves and closes the open project — there is no need to call `CloseProject` first (it is still fine to) — releases the licence, and the process exits (exit code `0` when the close succeeded, `3` when it failed: the error is in the server log). To close the open project **without saving** it, call [`CloseSessionWithoutSave`](#post-apimodelmanagementclosesessionwithoutsave) instead. The first close requested decides how the project is closed; the `SESSION_CLOSING` message of a later call says it joined that close and what the close in force does.

**Returns:** `BooleanApiResponse`

```csharp
client.CloseSession();  // saves the open project, then shuts down
```

> ⚠️ **Important:** After calling `CloseSession`, the HTTP server process will terminate. Do not make further calls on this client instance. The call answers immediately (`SESSION_CLOSING`) and the shutdown follows about **5 seconds** later. The close itself is queued behind every request already accepted, so a running or queued operation or analysis **finishes first** (no time limit); then the open project is saved and closed (usage analytics are flushed and the server waits 5 s), the licence is released and the process exits (exit code `0` when the close succeeded, `3` when it failed: the error is in the server log, and in the Windows Application event log under source `.NET Runtime`). From this call on, every other `api/Model/*` request is refused at once with **HTTP 503** and the error code **`SERVICE_STOPPING`** (`details.diagnostics[0].code`), and [`GET /api/Service/GetStatus`](#get-apiservicegetstatus) answers **`Stopping`** (`canRun` false) until the server has exited. Calling `CloseSession` again is harmless, and changes nothing: the close already requested goes on as it was asked (the first close requested, `CloseSession` or `CloseSessionWithoutSave`, decides how the project is closed, and the `SESSION_CLOSING` message of the later call says it joined that close and what it does). Ctrl+C (or any host stop) closes the session the same way: it waits up to about 25 s only while the close is still queued behind a running operation (past that the process ends without saving, exit code `3`); once the close has started it is awaited to the end (project save, licence release), however long it takes.

---

#### `POST /api/Model/management/CloseSessionWithoutSave`

The same as `CloseSession` (it closes the current session and requests the local API host to shut down), except that a project still open is closed **without saving**: what changed since its last save is discarded, as with `CloseProjectWithoutSave`. The first close requested decides how the project is closed: a `CloseSession` or `CloseSessionWithoutSave` called afterwards — or a stop of the host (Ctrl+C) — joins the close already requested and changes nothing. The `SESSION_CLOSING` message always says what the close in force does: a call that joined an earlier `CloseSession` (or a stop of the host) reports that the project is saved. All the rest of `CloseSession` applies (the call returns at once, the shutdown follows about 5 seconds later, `SESSION_CLOSING`, `SERVICE_STOPPING`).

**Returns:** `BooleanApiResponse`

```csharp
client.CloseSessionWithoutSave();  // shuts down, discarding what changed since the last save
```

> ⚠️ **Important:** As for `CloseSession`, the HTTP server process will terminate: do not make further calls on this client instance.

---

##### 3.1.1 The environment of a session

`Environments` is the body of `NewProject` and `OpenProject`. It says which language the server talks,
which country's rules the model is built and calculated under, how much of the engine log comes back —
and, since this release, which design standard and which national annex each of the eight material
families uses.

| Property | Type | Description |
|---|---|---|
| `Language` | `Language_Code?` | The language of the prompts, the catalogues and the messages. **Fixed for the life of the server process**, not of the session — see below. `null` means "whatever the server is already running in", which is always accepted |
| `Localization` | `Localization_Code?` | The country configuration. It resolves **all eight standards and all eight annexes in one sweep**, exactly as the installation does for the desktop application. `null` does *not* mean neutral — see below |
| `LogVerbosity` | `LogVerboseLevel` | `Errors`, `ErrorsAndWarnings` (the default) or `AllDetails`. Every warning this document mentions is filtered out of the response below `ErrorsAndWarnings`. Unlike the other fields it is a property of the SERVER, not of the session: the last accepted value stays in force for every request until another one changes it, and `GetEnvironment` reports that rather than a per-session setting |
| `DesignCodes` | `DesignCodes?` | The standards and annexes to impose on top of what `Localization` resolved to. `null` — the default — means "take all sixteen from the localization" |

**A session is configured once.** The environment takes effect on the call that *opens* the session, before
the model is created or read, and lasts exactly as long as that project. The localization and the eight
standards are read while the model is built, and read again by the combination generation and by the design
Experts, so a second project run on them would be calculated under rules it was never described with. A
`NewProject` / `OpenProject` sent while a project is still open is therefore refused **whole** —
`PROJECT_ENVIRONMENT_REFUSED`, and nothing is created or opened. Call `CloseProject` first; the next project
takes a new environment. Read back what is in force with
[`GetEnvironment`](#get-apimodelmanagementgetenvironment).

**Send `Localization`.** `null` is not a neutral default: it means *keep the eight standards this machine is
configured for*, read from the server's own Advance Design settings. The same request then produces a
Eurocode model on one server and an ACI / AISC model on another — and a `DesignCodes` object accepted on one
can be refused on the other, because the annex rules are checked against whatever standard is in force. It is
the one property whose absence makes the result depend on the server's installation.

**The language is narrower still — it is fixed for the life of the SERVER PROCESS.** The first environment
settles it, and the two ways of asking for another one afterwards are treated differently:

| What the request does | What happens |
|---|---|
| sends an explicit `Language` that differs from the one in force | the whole call is **refused**; the diagnostic names both languages and tells the caller to restart the server |
| sends a `Localization` that merely *implies* another language | the language is **held back with a warning**, and that localization's eight design standards are applied normally |

The restriction is what the desktop application achieves by restarting itself: reloading the prompt database
is not enough, because the catalogues, the units and the resources already loaded keep the language they
were read in. Refusing the second case as well would tie a whole server to one localization family — sixteen
of the eighteen localizations imply a language other than English.

**Nothing chosen here is written back to the machine.** Every standard, annex, localization and language of a
session lives in the server's memory for the life of that session.

###### The eight standards and their annexes — `DesignCodes`

Sixteen properties, every one of them nullable: one pair per row of the desktop application's *Localization*
configuration, a standard and the "NAD used for Eurocodes" combo beside it. **Null means "keep what
`Localization` resolved to"**, property by property — so an object with every property null changes nothing,
and there is no "no standard" state to fall into.

| Family | Standard | National annex | The annex is accepted with |
|---|---|---|---|
| Combinations | `Combinations` · `eCombinationsStandard` | `CombinationsAnnex` · `eCombinationsEurocodeAnnex` | `Eurocode` only — `EurocodeItalia` and `NTC2018` differ from it by *standard*, and the US and Canadian standards have no annex at all |
| Seismic | `Seismic` · `SeismicDesignCode` | `SeismicAnnex` · `SeismicEurocodeAnnex` | `en1998_1` only |
| Climatic | `Climatic` · `ClimaticDesignCode` | `ClimaticAnnex` · `ClimaticEurocodeAnnex` | `eurocode` (all ten), `pr_en13031_1` (`general` or `france`), `romania` (`romania`, which it pins anyway) |
| Reinforced concrete | `Concrete` · `ConcreteDesignCode` | `ConcreteAnnex` · `ConcreteEurocodeAnnex` | `eurocode` (every value except `italy`), `ntc2008` / `ntc2018` (`italy` only, which they pin anyway) |
| Steelwork | `Steel` · `SteelDesignCode` | `SteelAnnex` · `SteelEurocodeAnnex` | `eurocode` only, and never `italy` — Italy is a steel standard here, not an annex |
| Timber | `Timber` · `TimberDesignCode` | `TimberAnnex` · `TimberEurocodeAnnex` | `eurocode` only, and never `italy`, for the same reason |
| Masonry | `Masonry` · `MasonryDesignCode` | `MasonryAnnex` · `MasonryEurocodeAnnex` | `eurocode` only |
| Composite | `Composite` · `CompositeDesignCode` | `CompositeAnnex` · `CompositeEurocodeAnnex` | `eurocode` only |

A seventeenth property, **`AiscMethod`** · `eAiscDesignMethod` (`AllowableStrengthDesign` or
`LoadAndResistanceFactorDesign`), is the *AISC Method* combo of the same dialog: whether the AISC 360
Experts verify in ASD or LRFD. It is accepted when a family that reads it is in force: `Combinations` =
`US` / `US2018` (the dialog's own rule — the combo is on that row) **or** `Steel` = `aisc` / `aisc_2016`. With
neither — Eurocode or Canadian combinations together with a Eurocode or CSA S16 steel standard — it is refused
like an annex sent for a standard that has none, and `GetEnvironment` reports it under exactly the same
condition. Left null it keeps
what the session holds: **ASD whenever `Localization` is sent**, exactly as the dialog resets it when a
localization is applied, and the machine's own configuration otherwise. The Experts read it from the
session; the US combination generation reads the project's copy, which `GenerateCombinations` rewrites with
the session's method first — as the desktop's combination dialog does — so both always use the same method.

The members of all sixteen enumerations are listed in
[Section 13](#design-standards-and-national-annexes).

```csharp
// A French model, but verified to Eurocode 3 with the German national annex and to EN 1998-1 with the
// general one. The thirteen values the request does not name keep what LOCALIZATION_FRANCE resolved to.
var env = new Environments
{
    Localization = Localization_Code.LOCALIZATION_FRANCE,
    LogVerbosity = LogVerboseLevel.ErrorsAndWarnings,
    DesignCodes  = new DesignCodes
    {
        Steel        = SteelDesignCode.Eurocode,
        SteelAnnex   = SteelEurocodeAnnex.Germany,
        Seismic      = SeismicDesignCode.En1998_1,
        SeismicAnnex = SeismicEurocodeAnnex.General
    }
};
client.NewProject(@"C:\Projects\mixed.fto", env);

// ... and what the session really ended up with, the thirteen it never mentioned included.
var inForce = client.GetEnvironment();
Console.WriteLine(inForce.Data.DesignCodes.Concrete);        // eurocode, from LOCALIZATION_FRANCE
Console.WriteLine(inForce.Data.DesignCodes.ConcreteAnnex);   // france
```

**Notes**

- **An annex sent for a standard that has none is refused, not dropped.** The desktop dialog hides the NAD
  combo whenever the standard is not a Eurocode one and then writes no annex at all; this API fails the call
  instead, so a caller is never left believing a national annex is in force when it is not. `Concrete` =
  `aci` together with *any* `ConcreteAnnex` fails the whole `NewProject` / `OpenProject`.
- **A request that is wrong anywhere applies nothing.** Validation is one complete pass that writes nothing,
  application a second pass that cannot fail — so a session never carries half a request, and there is
  nothing for the caller to roll back. The whole call fails with `PROJECT_ENVIRONMENT_REFUSED` and the
  diagnostics carry one message per problem.
- **Two standards pin their own annex, asked for or not.** `Concrete` = `ntc2008` / `ntc2018` pins
  `ConcreteAnnex` = `italy`, and `Climatic` = `romania` pins `ClimaticAnnex` = `romania` — the snow and wind
  parameters read it. Both are what the desktop dialog does.
- **Moving a standard without its annex can be refused.** A request that names a standard and leaves the
  annex to the localization keeps the annex the localization put there, and that pair may be one this API
  refuses when it is stated explicitly: `LOCALIZATION_ITALIA` resolves concrete to NTC 2018 and pins the
  Italian annex, so asking for plain `eurocode` and nothing else would leave Italy behind it — which
  Eurocode 2 proper does not offer. Send the annex as well; the diagnostic lists what the new standard
  accepts.
- **Steelwork and composite are written independently, and a mismatched pair is a WARNING, not an error.**
  The desktop dialog slaves the two combos to each other — the Eurocode and Italian steel standards give
  EC4, the North-American ones give AISC 360-16 — so it cannot produce `Steel` = `aisc_2016` with
  `Composite` = `eurocode`. This API applies both exactly as asked and says so in a warning, rather than
  changing a family the request never named; the composite Expert may then be unable to calculate the model.
- **`can_csa_s16_2024` is refused as an input.** `SteelDesignCode` is shared with the read-only
  `SteelCalculationSettings.DesignCode`, so it lists one value this API will not take: CAN/CSA S16-24 is no
  longer offered by Advance Design and calculates exactly as `can_csa_s16_2019`, which is the value to send.
- **The greenhouse standards need the greenhouse licence.** `Combinations` = `Greenhouse_EN13031_1` and
  `Climatic` = `pr_en13031_1` are accepted only on an installation carrying the greenhouse computing option
  — the same condition under which the desktop dialog offers them.
- **Send enumerators by name, never by number.** The numbers behind these members are Advance Design's own
  storage values and three of the lists are deliberately not contiguous, while the generated clients
  renumber their copy from 0 — so a number copied out of the documentation and cast into a client
  enumeration lands on a **different** standard. A number that is not a member at all is refused with an
  error rather than accepted.
- **A 2nd generation Eurocode cannot be named here.** No enumeration of this contract has a member for one;
  see [Section 13](#design-standards-and-national-annexes).
- **These values are not read back from the settings they govern.** `CombinationsGenerationSettings.Standard`
  and `.Annex`, and `SteelCalculationSettings.DesignCode`, report what the session holds and ignore a value
  sent in a `SetModelSettings` request — they are read-only *there* precisely because they are settable
  *here*, and only here.
- Open the project with `LogVerbosity = ErrorsAndWarnings` or higher, or the warnings above are filtered out
  of the response before they reach you.

---

### 3.2 Model — Materials

#### `POST /api/Model/materials/CreateMaterial`

Creates a new material in the current project.

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| *(body)* | body | `Material` *(polymorphic)* | Yes | The material to create. Must include `$type` discriminator. |

**Returns:** `EIDApiResponse` · `Data.Value` contains the new material's internal ID (`long`).

---

#### `GET /api/Model/materials/GetListMaterials`

Retrieves a list of all material IDs in the current project.

**Returns:** `Int64ListApiResponse` · `Data` contains the list of material internal IDs.

```csharp
Int64ListApiResponse matIds = client.GetListMaterials();
foreach (long id in matIds.Data)
    Console.WriteLine($"Material ID: {id}");
```

---

#### `POST /api/Model/materials/GetMaterials`

Retrieves fully populated material objects by their internal IDs.

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| *(body)* | body | `long[]` | No | Array of material internal IDs |

**Returns:** `MaterialListApiResponse` · `Data` contains polymorphic `Material` objects (may be `MaterialSteel`, `MaterialReinforcedConcrete`, etc.).

```csharp
var matIds = new List<long> { 1, 2, 3 };
MaterialListApiResponse mats = client.GetMaterials(matIds);
foreach (var mat in mats.Data)
{
    if (mat is MaterialSteel steel)
        Console.WriteLine($"{steel.Name}: E={steel.E}");
}
```

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

#### `POST /api/Model/sections/CreateUserSection`

Creates a **user section** — a cross-section whose sectional characteristics are supplied by the caller
instead of being looked up in a catalog or derived from parametric dimensions. This is the API equivalent
of the *User* tab of the Advance Design cross-section manager. The section is created **in the model
only**: the user section catalog database (`.mdb`) is never modified.

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| *(body)* | body | `UserSection` | ✓ | Must carry `"$type": "UserSection"`. A plain `Section` is refused |

**Returns:** `EIDApiResponse` · `Data.Value` contains the section's internal ID, or `0` on failure — read
the diagnostics to see which value was rejected.

If a **user section of the same name** already exists in the model it is **updated in place** and its ID
returned. A name already used by a section of another type is refused, so a catalog section is never
overwritten.

See [Data Models — Sections](#65-data-models--sections) for every property, the validation rules, and the
rules for the optional outline.

```csharp
// a 200 x 300 mm solid rectangular user section, outline included
var section = new UserSection
{
    Name        = "MyUserSection",
    Area        = 0.06,        // m²   - all values are SI
    Iy          = 4.5e-4,      // m⁴
    Iz          = 2.0e-4,      // m⁴
    It          = 5.11e-7,     // m⁴
    Iw          = 4.90e-7,     // m⁶
    WelyInf     = 3.0e-3,      // m³
    WelySup     = 3.0e-3,
    WelzInf     = 2.0e-3,
    WelzSup     = 2.0e-3,
    Wply        = 4.5e-3,
    Wplz        = 3.0e-3,
    Wt          = 1.0e-4,
    Sy          = 0.04,        // m²
    Sz          = 0.04,
    Avy         = 0.04,
    Avz         = 0.04,
    ProfileType = UserSectionProfileType_API.Rectangular,
    RollingType = SectionRollingType_API.HotRolled,

    // the outline: one solid rectangle, centred on the centre of gravity
    Shape = new UserSectionShape
    {
        Contours =
        [
            new UserSectionContour
            {
                Points =
                [
                    new UserSectionContourPoint { Y = -0.100, Z =  0.150 },
                    new UserSectionContourPoint { Y =  0.100, Z =  0.150 },
                    new UserSectionContourPoint { Y =  0.100, Z = -0.150 },
                    new UserSectionContourPoint { Y = -0.100, Z = -0.150 },
                ]
            }
        ]
    }
};

EIDApiResponse resp = client.CreateUserSection(section);
if (!resp.Details.Success)
    foreach (var d in resp.Details.Diagnostics)
        Console.WriteLine($"{d.Severity}: {d.Message}");

long sectionId = resp.Data.Value;
```

---

#### `GET /api/Model/sections/GetListSections`

Retrieves a list of all section IDs in the current project.

**Returns:** `Int64ListApiResponse` · `Data` contains the list of section internal IDs.

```csharp
Int64ListApiResponse secIds = client.GetListSections();
```

---

#### `POST /api/Model/sections/GetSections`

Retrieves fully populated section objects by their internal IDs.

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| *(body)* | body | `long[]` | No | Array of section internal IDs |

**Returns:** `SectionListApiResponse` · `Data` contains `Section` objects with `Name`, `Type` (`SectionType_API`), `FamilyCode`, and `CatalogName`. `Section` is polymorphic: a user section comes back as a `UserSection`, carrying its sectional characteristics and its outline as well — cast or pattern-match on it. See [Data Models — Sections](#65-data-models--sections).

```csharp
var secIds = new List<long> { 1, 2 };
SectionListApiResponse secs = client.GetSections(secIds);
foreach (var s in secs.Data)
    Console.WriteLine($"{s.Name} (type: {s.Type})");
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

#### `POST /api/Model/elements/AssignDesignTemplateToElements`

> ℹ️ **Enabled in this build.** This endpoint is compiled into the server behind a build switch
> that is currently **on**, so it is declared in `swagger.json` and present in every generated
> client. A build made with that switch off does not expose it, and calling it there returns **404**.

Assigns one [Expert design template](#106-expert-design-templates) to several elements at once, or detaches
them from the template they carry. **Only the design template of each element changes** — every other
property is left exactly as it is, which is what distinguishes this call from `UpdateElement` (which needs
the complete element payload and therefore a read-modify-write round trip per element).

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| `designTemplateId` | query | `long` | Yes | The template to assign, or **`0` to detach** the elements back to the built-in default template ("None") |
| *(body)* | body | `long[]` | Yes | The elements the template is applied to |

**Returns:** `BooleanApiResponse` — `true` only when **every** requested element was assigned.

A template is applied to an element only when all three [assignment conditions](#assigning-a-template-to-an-element)
hold. A mixed selection is a normal partial success: the elements that break a rule are reported
individually in `Details.Diagnostics` and left untouched, the others are still applied, and `Data` is
`false`. A `designTemplateId` that is not a template of the model changes **nothing at all**. A duplicated
element id is applied once.

```csharp
// Apply one template to a whole selection
BooleanApiResponse ok = client.AssignDesignTemplateToElements(templateId, new List<long> { beam1, beam2, beam3 });

// Detach the same elements (they keep their current expert values - this is not a reset)
client.AssignDesignTemplateToElements(0, new List<long> { beam1, beam2, beam3 });
```

---

### 3.5 Model — Informational Elements

#### `POST /api/Model/elements/CreateInformationalElement`

Creates a new non-geometric informational element: load cases, load case families, combinations, structural systems, linear release templates, or Expert design templates.

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| *(body)* | body | `InformationalElementBase` *(polymorphic)* | Yes | Load case / family / combination / structural system / linear release template / design template definition. Must include `$type`. |

**Returns:** `EIDApiResponse`

**Supported concrete types:** see [Section 10](#10-data-models--informational-elements-load-cases--families) — including `StructuralSystem`, the container/level that groups structural elements ([Section 10.4](#104-structural-systems)), `LinearReleaseTemplate`, the edge-connection template referenced by planar elements ([Section 10.5](#105-linear-release-templates)), and the Expert design templates referenced by linear elements — `EC3SteelLinearDesignTemplate`, `AISCSteelLinearDesignTemplate` and `CSASteelLinearDesignTemplate`, one per steel design code family ([Section 10.6](#106-expert-design-templates)).

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
    },

    // Which design (Expert) calculations use this combination. Without it the combination takes
    // part in none, and the steel engine refuses with "No case selected for the Steelwork Design
    // calculation!". The coefficients are optional - see 3.6.1.
    DesignCalculations = [ECombinationDesignCalculation.SteelProfiles]
};
client.AddCombination(combo);
```

##### 3.6.1 Design (Expert) calculations of a combination

A design engine only verifies the combinations that list its calculation. In the Advance Design model
this is the `ExpertSelection` bit mask on the combination; the API replaces it with the plain list
`DesignCalculations`, so no bit arithmetic is needed. The coefficients each engine applies live in
four optional groups — `ConcreteDesign`, `SteelDesign`, `TimberDesign`, `CompositeDesign` — described
in [10.3](#103-combinations).

Rules, applied per design calculation:

| Listed in `DesignCalculations`? | Group supplied? | Result |
|---|---|---|
| yes | yes | the supplied properties are written; the ones left `null` keep the stored value, or take the default when nothing is stored |
| yes | no | defaults are written when the combination does not carry the values yet, and a **warning** is reported in `Details.Diagnostics`; already-stored values are left untouched |
| no | yes | the values are stored anyway (AD keeps the sub-objects independently of the mask, so they survive a later change of selection) and a **warning** says no design calculation will use them |
| no | no | nothing is written |

Nothing is re-written when the resulting value equals the stored one.

**`DesignCalculations` on Create vs Update** — the two differ on purpose:

| Value | Create | Update |
|---|---|---|
| absent / `null` | no design calculation at all | **the stored selection is left untouched** — a caller that only wants to change a load coefficient does not have to restate it |
| `[]` | no design calculation at all | the combination is taken out of every design calculation |
| `[…]` | exactly those | exactly those, replacing whatever was selected before |

> **Warnings are filtered by verbosity.** The "default design properties were applied" warnings only
> reach `Details.Diagnostics` when the session was opened with
> `LogVerbosity = LogVerboseLevel.ErrorsAndWarnings` or `AllDetails`. `LogVerboseLevel.Errors` maps to
> a quiet diagnostic level that strips every warning from the response.

> **Set it before `LaunchAnalysis`.** The design engines read the *analysis* model, which takes its own
> copy of the selection while it is built. Changing `DesignCalculations` on an already-analysed model
> has no effect on a design calculation until `LaunchAnalysis` is run again.

Defaults are not constants: they are read from the model exactly like the Advance Design combinations
dialogs do — the partial safety factors from the model properties (with the national-annex variants),
the composite γM0/γM1/γM2 from the composite design settings, and the concrete limit state and load
duration derived from the project situation of the combination.

```csharp
// A combination checked for steel strength and steel deflection, with explicit factors on the
// deflection side and the model defaults on the strength side.
var combo = new Combination
{
    ECombinationType = ECombinationType.eComboProjectSituationElsCharacteristic,
    ListCasesCoeffs  = new List<EIDDoublePair>
    {
        new EIDDoublePair { Key = new EID { Value = deadLoadId }, Value = 1.0 },
        new EIDDoublePair { Key = new EID { Value = liveLoadId }, Value = 1.0 }
    },
    DesignCalculations =
    [
        ECombinationDesignCalculation.SteelProfiles,
        ECombinationDesignCalculation.SteelDeflections
    ],
    SteelDesign = new SteelCombinationDesign
    {
        Deflections = new CombinationDeflectionVerification
        {
            Criterion    = EDeflectionCriterion.FirstCriterion,
            SafetyFactor = 1.0
        }
        // Profiles left null -> created with the model defaults, reported as a warning
    }
};
client.AddCombination(combo);
```

---

##### 3.6.2 Automatic generation of combinations

`GenerateCombinations` runs the same generation the **Combinations** dialog of the desktop
application runs: it reads the standard and the generator options from the model, builds the
combination set from the load cases, and writes it into the project. It is the API equivalent of
the *Simplified Comb.* and *Detailed Comb.* buttons.

Nothing about the standard is a parameter. The permanent actions are factored with the γ values on
their own load cases; every variable action — exploitation, snow, wind, temperature — is factored with
the γ and ψ of the model's combination-generator options, along with which project situations are
generated and the equation option. The request only says *which work-flow to run*; those options are
set separately, beforehand, with
[`SetModelSettings`](#363-settings-of-the-automatic-generation).

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| *(body)* | body | `CombinationsGenerationRequest` | Yes | Which work-flow to run |

**Returns:** `CombinationsGenerationResultApiResponse`

```csharp
// Everything else is read from the model - this is the whole call.
var result = client.GenerateCombinations(new CombinationsGenerationRequest
{
    Mode = ECombinationsGenerationMode.Simplified
});

Console.WriteLine($"{result.Data.CreatedCombinations.Count} combination(s), "
                + $"No. {result.Data.FirstIdentifier} to {result.Data.LastIdentifier}");

// The generated combinations are ordinary combinations - read them back like any other.
var objects = client.GetInformationalElementsObject(
    result.Data.CreatedCombinations.Select(e => e.Value).ToList());
```

On a model with one permanent (`LoadCase_DeadLoads`) and one variable (`LoadCase_LiveLoads`) load
case, the Eurocode set is the six combinations the dialog produces, numbered from 101:

| No. | Project situation | Terms |
|---|---|---|
| 101 | ULS StrGeo | 1.35 × D |
| 102 | ULS StrGeo | 1.35 × D + 1.50 × L |
| 103 | SLS characteristic | 1.00 × D |
| 104 | SLS characteristic | 1.00 × D + 1.00 × L |
| 105 | SLS quasi-permanent | 1.00 × D |
| 106 | SLS quasi-permanent | 1.00 × D + 0.30 × L |

**Notes**

- **The generated combinations already carry their design (Expert) selection.** The Eurocode
  work-flows derive it from the normative code of each combination, so an ULS combination comes
  out selected for the concrete, steel and timber *strength* checks and an SLS one for the
  *deflection* checks — see [3.6.1](#361-design-expert-calculations-of-a-combination). This means
  `GenerateCombinations` → `LaunchAnalysis` → `LaunchDesignCalculation` works with no further
  setup, which is not true of a combination created by hand through `AddCombination`.
- **Leave `UpdateLoadCaseCodes` unset**, which means *yes*, for any model built through this API.
  The combination templates match load cases on their normative code (ECG, ECQ, ECN, …), and a load
  case created through the API carries a neutral default one — with the refresh turned off, the
  generation returns **no combination at all** on such a model. Send `false` only for a model opened
  from a project file whose codes were set deliberately and must not be touched.
- **The result matches the desktop.** Both work-flows are the same code the *Combinations* dialog
  runs, and both are pinned against sets exported from Advance Design 2027.0: `Simplified` reproduces
  all 12 combinations of a D + L + S + W model and `DetailedMatrix` all 47 — same terms, same
  coefficients, same order, same numbering.
- **Generation is additive.** Existing combinations are kept and the new ones are numbered after
  them, so a second call on the same model doubles the set.
- **`DetailedMatrix` uses the concomitance rules already stored in the model.** There is no
  concomitance editor in the API, so this mode is meaningful for a model opened from a project file
  whose matrix its author set up. `ApplyDefaultConcomitanceRules` re-initialises that matrix from the
  default rules before generating — leave it `false` to keep what the model carries.
- **No combination generated is not an error.** A model whose load cases yield nothing comes back
  with `Success = true`, an empty `CreatedCombinations` and a warning saying so. Open the project with
  `LogVerbosity = ErrorsAndWarnings` or higher, or the warning is filtered out of the response.
- **Leave `CombinationsCoefficients` unset on a load case and on its family.** Sent as `null` — which
  is what the generated clients do unless you assign it — Advance Design fills the family and the case
  from their own type: dead 1.10 / 1.35, live 1.50 with psi 0.7 / 0.5 / 0.3, snow 1.50 with
  0.5 / 0.2 / 0 (French annex, at or below 1000 m), wind 1.50 with 0.6 / 0.2 / 0. Sending a block
  **replaces all of them**, including the ones you did not mean to touch, because every field of the
  block has a value. Assign it only to override a coefficient deliberately, and then state the whole
  block.
- The desktop has a third work-flow, generation from a custom `.cbn` template picked in a file
  dialog. It is deliberately **not** exposed: it would mean an absolute path in the *server's* file
  system inside the request, and there is no way for a caller to discover which templates that
  machine has. The project `filename` of `NewProject` and `OpenProject` is a different case: it may be relative, and is then taken from the server working directory set with `/w` (see [2.1](#21-instantiate-the-client)).
- The combination standard is **session state**, not a model property: it is chosen once, on the
  `NewProject` / `OpenProject` call that opens the session, with `Environments.DesignCodes.Combinations`,
  and falls back to what the session `Localization` resolves to — the same way the desktop resolves it
  from its installation. Two localizations therefore generate differently for the same model — through
  the API exactly as in the user interface. [3.1.1](#311-the-environment-of-a-session) is where it is
  set; [3.6.3](#363-settings-of-the-automatic-generation) reports which standard the session ended up
  with.

---

##### 3.6.3 Settings of the automatic generation

The general options the *Combinations options* dialog edits: which project situations are generated,
and the γ / ψ table of each action family. They belong to the **model**, not to any combination, and
are read by `GenerateCombinations` — so they are written once, before generating.

They are therefore one **group of the general model settings** of
[3.7](#37-model--general-settings) — `eModelSettingsCategory.CombinationsGeneration` — and have
no endpoints of their own:

| Call | Parameter / Body | Returns |
|---|---|---|
| `GET /api/Model/model/GetModelSettings` | `category = CombinationsGeneration` | `ModelSettingsBaseApiResponse` — one of the shapes below |
| `POST /api/Model/model/SetModelSettings` | one of the shapes below | `BooleanApiResponse` |
| `POST /api/Model/model/ResetModelSettings` | `category = CombinationsGeneration` | `BooleanApiResponse` |

**Four standards, four shapes.** Eurocode, US, Canada and EN 13031-1 (greenhouses) share nothing:
each has its own fields on the model and its own dialog page, so the settings are polymorphic on
`$type` (see [5](#5-polymorphism--the-type-discriminator)). Which one applies follows the combination
standard of the **session** — `Environments.DesignCodes.Combinations` on the call that opened the
project, or what `Localization` resolved to when that was left null
([3.1.1](#311-the-environment-of-a-session)) — and `Standard` reports it.

The shapes derive from `CombinationsGenerationSettings`, the class of this group, which carries
what they all have — `Standard` and `Annex`, both read-only **here**, because they are session
settings and a session is configured once — and which itself derives from `ModelSettingsBase`. So the
`$type` on the wire is always the shape, never the group.

| Derived type | Standards it covers |
|---|---|
| `EurocodeCombinationsGenerationSettings` | Eurocode with any national annex, Eurocode Italia, NTC 2018 |
| `Eurocode2ndGenCombinationsGenerationSettings` | EN 1990:2023 — shape published, standard not released and not reachable |
| `USCombinationsGenerationSettings` | USA, USA 2018 |
| `CanadaCombinationsGenerationSettings` | Canada NBC 2010 / NBC 2015 / CNB 2020 |
| `GreenhouseCombinationsGenerationSettings` | PR EN 13031-1 |

```csharp
// Read what the model generates with, and which standard that is.
var settings = client.GetModelSettings(EModelSettingsCategory.CombinationsGeneration);
var eurocode = (EurocodeCombinationsGenerationSettings)settings.Data;
Console.WriteLine($"{eurocode.Standard} / {eurocode.Annex}");

// A write is partial: this turns SLS frequent on and touches nothing else.
// No category on the write — the object is what names the group of settings.
client.SetModelSettings(new EurocodeCombinationsGenerationSettings
{
    ProjectSituations = new EurocodeProjectSituations { SlsFrequent = true }
});

client.GenerateCombinations(new CombinationsGenerationRequest
{
    Mode = ECombinationsGenerationMode.Simplified
});
```

**Notes**

- **Every property is optional, and what you do not send is left alone.** A request carrying one
  group changes that group only, and a group carrying one property changes that property only. This
  is what makes a `Get` → edit → `Set` round trip safe.
- **A category dictates its coefficients.** Wherever the standard tabulates ψ (or γ) against a
  category, the category decides and the coefficient is read-only — exactly as the cell is read-only
  in the dialog's grid. Setting `LiveLoads.Category` recomputes ψ0/ψ1/ψ2 from the annex's table;
  sending a ψ alongside it is **ignored and reported as a warning**, not refused, so that handing
  back what a `Get` returned never fails. Each family has one *user defined* category which unlocks
  its coefficients — that is the only way to impose your own.
- **The live load category list is annex-dependent, and two annexes renumber it.** France has
  thirteen categories (A, B, C, D, E1, E2a, E2b, E2c, F, G, H, K, K1) and the Czech annex eight
  (A…H); everywhere else it is the ten of `eExploitationCategory` (A…J). Because of that,
  `LiveLoads.Category` may only name a category the active annex reads the same way — A…D under the
  French annex, A…H under the Czech one — and anything past that is **refused** rather than written
  under a name that would mean a different action. Use `LiveLoads.CategoryIndex` there; it takes
  precedence when both are sent, and `Get` always fills it.
- **`AvailableCategoryCount` and `UserDefinedCategory` are read-only, and they are how you discover
  the annex's list.** They are equal — the real categories are numbered from zero, so the count is
  also the index of the user defined one: 13 under the French annex, 8 under the Czech one, 10
  elsewhere; 3 for snow, 1 for wind and for temperature.
- **Two rules are enforced, the same two the dialog enforces by refusing the last uncheck:** at
  least one project situation, and at least one variable action family as a leading action, must stay
  on. A request that would leave neither is refused — a generation with none produces nothing.
- **A field the active standard or annex does not offer is stored but reported.** `UlsGeotechnical`
  exists only under the Italian standards, `Equation` only under the General, Poland, Czech and
  Slovakia annexes, `SlsCharacteristicIncludesPermanent` only under the UK one. They are written, so
  a round trip does not lose them, and a warning says the generation will not read them.
- **`ResetModelSettings(CombinationsGeneration)` is the *Load defaults* button** — it puts back the values
  the active standard and annex prescribe.
- **The US f1 and f2 values are read-only**, because there is no user-defined option there: the two
  choices *are* the two values (`PublicAssembly` → 1.0, `Others` → 0.5; `RoofNotShedding` → 0.7,
  `RoofOthers` → 0.2). `AiscDesignMethod` is read-only here too — it is the project's steel design
  configuration, chosen with `Environments.DesignCodes.AiscMethod` on the call that opens the project.
- **The greenhouse consequence factors are dictated by the class** (C0 → 0.80 / 0.60, C1 → 0.90 /
  0.80, C2 → 1.00 / 1.00) and writable only for `Other`. Note also that no localization *resolves* to
  PR EN 13031-1: it has to be asked for, with `Environments.DesignCodes.Combinations` =
  `Greenhouse_EN13031_1` on the call that opens the session, and only an installation carrying the
  greenhouse computing option accepts it. Elsewhere those settings can still be written and read back on
  any model, but no generation uses them.
- **The 2nd generation Eurocode is not released, and is not reachable.** EN 1990:2023 has the same
  shape here as the other four — `Eurocode2ndGenCombinationsGenerationSettings`, still published so
  that the contract does not change the day the standard ships — but `eCombinationsStandard` has **no
  member** for it: it cannot be asked for by name, its raw number is refused, and no response can name
  it. On an installation where the 2nd generation Eurocodes are not enabled, a model that already
  generates to that standard — one the desktop application configured that way — is **refused** by
  `GetModelSettings`, `SetModelSettings`, `ResetModelSettings` and `GenerateCombinations` alike, rather
  than answered under EN 1990:2002, which would be a wrong answer that looks like a right one. Three
  things about that shape are worth knowing where it does run:
  - its per-family coefficients are **lists of rows** matched by `Family`, not four fixed groups.
    `Family` is required on every row — a row that omits it is refused, and so is a family the
    model has no row for, because the model vector cannot grow through this API;
  - **no coefficient is recomputed from its category** — the 2nd generation category tables still
    live in the desktop options dialog, so what you send is what gets stored;
  - the same two dialog rules are enforced: at least one verification case or SLS combination, and
    at least one variable family as a leading action, must stay on.
  <br/>Note also that `GammaVC2b` of a **permanent** row is written but never read back — the model
  reader has never read that column, a pre-existing asymmetry shared with the desktop dialog, so a
  `Get` reports the default 1.0 there. The `GammaVC2b` of a *variable* row reads normally.
- Open the project with `LogVerbosity = ErrorsAndWarnings` or higher, or every warning above is
  filtered out of the response before it reaches you.

---

### 3.7 Model — General settings

The options that apply to the model **as a whole** rather than to one of its objects — what the
option dialogs of the desktop application edit, stored on the model itself. They are not properties
of an element, a load case or a combination.

**Three endpoints for every group of settings there is, and every group there will be.** The group
is named by `eModelSettingsCategory` on the two that read and reset, and by the `$type` of the body
on the one that writes — never by the endpoint.

| Endpoint | Parameter / Body | Returns |
|---|---|---|
| `GET /api/Model/model/GetModelSettings` | `category` (query) | `ModelSettingsBaseApiResponse` |
| `POST /api/Model/model/SetModelSettings` | `ModelSettingsBase` | `BooleanApiResponse` |
| `POST /api/Model/model/ResetModelSettings` | `category` (query) | `BooleanApiResponse` |

**One class per group, and the group's shapes under it.** `ModelSettingsBase` is polymorphic on
`$type` (see [5](#5-polymorphism--the-type-discriminator)): one derived class per group of settings,
and under that class as many classes as the group has shapes. The hierarchy therefore reads *which
settings* (level 2), *in which variant* (level 3) — the same two-level arrangement as
`InformationalElementBase` → `DesignTemplate` → `LinearDesignTemplate`.

| `eModelSettingsCategory` | Class of the group | Shapes | Documented in |
|---|---|---|---|
| `CombinationsGeneration` | `CombinationsGenerationSettings` | 5, one per combination standard | [3.6.3](#363-settings-of-the-automatic-generation) |
| `SteelEc3Calculation` | `SteelCalculationSettings` | 1, `Ec3SteelCalculationSettings`, for the Eurocode 3 family | [3.7.1](#371-steel-eurocode-3-calculation-settings) |
| `SteelAiscCalculation` | `AiscSteelCalculationSettings` | 1, covering AISC 360, AISC 360-16 and the four CAN/CSA S16 editions | [3.7.2](#372-steel-aisc--can-csa-calculation-settings) |

**The two steel groups are siblings, not a group and its shape.** They mirror the same dialog, but not the
same set of tabs: a North-American model has no *Partial safety factors* page at all and a *Verifications*
page of three controls. Which group a model belongs to follows from its steel design code — a **session**
setting, chosen with `Environments.DesignCodes.Steel` on the call that opened the project, or inherited
from `Localization` when that was left null ([3.1.1](#311-the-environment-of-a-session)). It is the
caller's choice *there*, and fixed for the life of the project everywhere else. Ask for the wrong one and
the answer is an error naming the right one.

```csharp
// The category says WHICH settings; the $type of what comes back says which shape they are in.
var settings = client.GetModelSettings(EModelSettingsCategory.CombinationsGeneration);
var eurocode = (EurocodeCombinationsGenerationSettings)settings.Data;

// No category on the write — the object is what names the group.
client.SetModelSettings(new EurocodeCombinationsGenerationSettings
{
    ProjectSituations = new EurocodeProjectSituations { SlsFrequent = true }
});

// ... and back to what the model's own standard prescribes.
client.ResetModelSettings(EModelSettingsCategory.CombinationsGeneration);
```

**Notes**

- **`SetModelSettings` takes no category on purpose** — the runtime type of the body says which group
  is written and in which shape, exactly as `CreateInformationalElement` lets the object decide what
  is created. `ModelSettingsBase` itself carries nothing, so an instance of it is refused with a
  diagnostic rather than silently writing nothing.
- **Every property is optional, and what you do not send is left alone.** A request carrying one
  group of properties changes that group only. This is what makes a `Get` → edit → `Set` round trip
  safe.
- **A property the model derives from another one cannot be imposed.** The coefficients a standard
  tabulates against a category are the case that matters today: the value is **ignored and reported
  as a warning**, not refused, so that handing back what a `Get` returned never fails. What each
  group derives, and what it refuses outright, is documented on that group's own section.
- **`ResetModelSettings` is the group's *Load defaults* button** — it puts back the values the
  model's own standard and national annex prescribe for that group.
- **`SettingsCategory` on a read says which group answered**, so a returned object identifies
  itself without its `$type` having to be parsed. It is read-only: a value sent in a `Set` is
  ignored, because the group follows from the type. It is `SettingsCategory` and not `Category`
  because a group's shapes carry categories of their own — `LiveLoads.Category` and its
  siblings — and one request body must not hold two properties of the same name at two levels.
- **An unnamed category is refused.** `eModelSettingsCategory.None` is what an omitted parameter
  binds to, so a `Get` or a `Reset` carrying it reports an error instead of answering for some
  default group.
- **A new group of settings changes no endpoint.** It is a new `eModelSettingsCategory` value and a
  new class under `ModelSettingsBase`; these three endpoints, their parameters and their envelopes
  stay as they are. A client written against them keeps working when the next group arrives.
- Open the project with `LogVerbosity = ErrorsAndWarnings` or higher, or every warning above is
  filtered out of the response before it reaches you.

#### 3.7.1 Steel (Eurocode 3) calculation settings

The general options of the **Expert STEEL design calculation** — what the *Calculation settings* dialog
of Advance Design edits, tab by tab: which verifications are performed, the partial safety factors, the
imperfection and buckling options, the optimisation criteria, the profile sort order, the
angle-verification bolt factors and the calculation sequence.

They belong to the **model**, not to any element. What an individual member contributes on its own — its
buckling lengths, its lateral-torsional buckling data, its deflection limits — lives on that element or on
the [design template](#106-expert-design-templates) assigned to it.

They are one group of the general model settings of [3.7](#37-model--general-settings) —
`eModelSettingsCategory.SteelEc3Calculation` — and have no endpoints of their own:

| Call | Parameter / Body | Returns |
|---|---|---|
| `GET /api/Model/model/GetModelSettings` | `category = SteelEc3Calculation` | `ModelSettingsBaseApiResponse` — an `Ec3SteelCalculationSettings` |
| `POST /api/Model/model/SetModelSettings` | `Ec3SteelCalculationSettings` | `BooleanApiResponse` |
| `POST /api/Model/model/ResetModelSettings` | `category = SteelEc3Calculation` | `BooleanApiResponse` |

**The Eurocode family only.** These settings exist for the codes that use the EC3 expert property set —
Eurocode 3, NTC 2008 and NTC 2018. The active steel design code is not part of them: it is a **session**
setting, chosen with `Environments.DesignCodes.Steel` on the call that opened the project or inherited from
`Localization` ([3.1.1](#311-the-environment-of-a-session)), and fixed for the life of the project. Under
any other steel code all three calls are **refused**, naming the code, rather than answering with values no
engine would read — and a North-American model is told to ask for
[`SteelAiscCalculation`](#372-steel-aisc--can-csa-calculation-settings) instead. `DesignCode` reports which
one the model has, and is read-only **here**.

**One shape, and a second generation behind a gate.** The two generations of Eurocode 3 share every page of
the dialog and differ by exactly three stored values, so there is a single derived class,
`Ec3SteelCalculationSettings`, and those three values live in `SecondGeneration` sub-objects of the pages
they belong to rather than in a shape of their own. EN 1993-1-1:2023 is **not released**: `SteelDesignCode`
has no member for it, and on an installation where the 2nd generation Eurocodes are not enabled a model
configured with it is **refused** by this group rather than answered under the 1st generation. The
`SecondGeneration` sub-objects are therefore inert on an ordinary installation — a value sent in one is
**ignored and reported as a warning** — and stay published so that the shape does not change under existing
callers the day the standard does ship.

**Seven sub-objects, one per tab of the dialog.** Each is optional: a `null` page is left exactly as the
model has it, so a request may carry one tab and change nothing else.

| Sub-object | Tab | What it carries |
|---|---|---|
| `Verification` | *Verifications* | `CrossSectionStrength`, `ElementStability`, `CalculationType`, the fire verification and its `FireResistanceTime`, the three imperfections, `SecondOrderAnalysis`, `ColumnsBucklingLength`, `BeamsBucklingLength`, `ImperfectionGeneration`, `PlateBucklingMethod` (+ `SecondGeneration`) |
| `SafetyFactors` | *Partial safety factors* | `Option`, `GammaM0`, `GammaM1`, `GammaM2`, `GammaMfi` (+ `SecondGeneration`) |
| `Optimization` | *Optimisation* | `Mode`, the three work-ratio / deflection switches with their percentage limits, `SearchRange`, `MaximumIterationCount` |
| `ProfileSorting` | *Sort profiles* | `SortCriterion`, `OrderingCriterion`, and the four height / width limits with their switches |
| `Buckling` | *Buckling* | `Method` |
| `AngleVerification` | *Angle verification* | `EdgeDistanceFactorE1`, `EdgeDistanceFactorE2`, `BoltSpacingFactorP1` — all multiples of the bolt hole diameter d₀ |
| `CalculationSequence` | *Calculation sequence* | `Verify`, `CalculateBuckling`, `VerifyDeflections`, `CalculateShapes`, `ChainedOptimization`, `CompleteCalculationCount` |

**Units are SI, as everywhere in this API — and two fields differ from the dialog.**
`Verification.FireResistanceTime` is in **seconds** (the dialog shows minutes), and the four profile
height / width limits of `ProfileSorting` are in **metres** (the dialog shows the section unit). Everything
else is a ratio, a percentage as an integer, a count or an enumeration.

**The enumerations carry Advance Design's own member names.** Each one is declared from a single list of
values shared with the desktop application and the design engines, so the contract cannot drift from the
dialog; the price is that the members are spelled as the application spells them. The generated C# client
upper-cases the first character (`eTypeDeCalculTractionCompression` →
`ETypeDeCalculTractionCompression`, `imposed` → `Imposed`).

| Enumeration | Values (wire spelling) |
|---|---|
| `eSteelCalculationType` | `eTypeDeCalculTractionCompression` · `eTypeDeCalculComposedBending` · `eTypeDeCalculBiaxialBending` |
| `eSteelBucklingLengthOption` | `elementLength` · `imposed` (buckling length from the element properties) · `neglected` |
| `eSteelImperfectionGeneration` | `eImperfectionGenerationForGoverningComb` · `eImperfectionGenerationForAllComb` · `eImperfectionGenerationUndefined` |
| `eSteelPlateBucklingMethod` | `eMethodWebBucklingEffectiveCrossSections` · `eMethodWebBucklingReducedStresses` · `eMethodWebBucklingUndefined` |
| `eSteelSafetyFactorsOption` | `eSafetyFactorsAuto` · `eSafetyFactorsImposed` |
| `eSteelOptimizationMode` | `eOptimizationParElements` · `eOptimizationParSections` · `eOptimizationParRole` · `eOptimizationParName` · `eOptimizationParSystems` · `eOptimizationNonExistant` |
| `eSteelOptimizationSearchRange` | `eProfileOptimizationSearchRangeAuto` · `eProfileOptimizationSearchRangeAutoWithMaxLimit` |
| `eSteelProfileOrderingCriterion` | `eProfileOptimizationOrderedByHeight` · `…ByWidth` · `…ByIyInertia` · `…ByIzInertia` · `…ByWeight` |
| `eSteelProfileSortCriterion` | `eTriCriteriaDeflections` · `eTriCriteriaWorkRatio` · `eTriCriteriaEnveloppe` |
| `eSteelBucklingLengthMethod` | `eMethodBucklingRoA_RoB` · `eMethodBucklingKa_Kb` · `eMethodBucklingN1_N2` · `eMethodBucklingGa_Gb` · `eMethodBucklingGu_Gl` · `eMethodBucklingNone` |
| `eSteelInteractionFactorMethod` | `eInteractionFactor_kij_Method_EN1993` · `eInteractionFactor_kij_Method_CEN_TS_1993` |

```csharp
// Read what the steel design engine will run with, and which generation of EC3 that is.
var settings = client.GetModelSettings(EModelSettingsCategory.SteelEc3Calculation);
var steel = (Ec3SteelCalculationSettings)settings.Data;
Console.WriteLine(steel.DesignCode);          // eurocode | ntc2008 | ntc2018

// A write is partial: this changes the calculation sequence and touches no other tab.
// No category on the write - the object is what names the group of settings.
client.SetModelSettings(new Ec3SteelCalculationSettings
{
    CalculationSequence = new SteelCalculationSequenceSettings
    {
        Verify             = true,
        CalculateBuckling  = false,   // skip the buckling families of this run
        VerifyDeflections  = true
    }
});

// Imposing the partial safety factors takes the option and the values together.
client.SetModelSettings(new Ec3SteelCalculationSettings
{
    SafetyFactors = new Ec3SteelSafetyFactorSettings
    {
        Option   = ESteelSafetyFactorsOption.ESafetyFactorsImposed,
        GammaM0  = 1.00,
        GammaM1  = 1.10
    }
});

// ... then run the analysis and the design calculation, in that order.
client.LaunchAnalysis();
client.LaunchDesignCalculation(DesignCalculationType.Steel, new List<long>());

// And back to what the model's steel code and national annex prescribe.
client.ResetModelSettings(EModelSettingsCategory.SteelEc3Calculation);
```

**Notes**

- **Call it before `LaunchAnalysis`.** These values are written to the descriptive model, and the analysis
  model — which is what the design engine runs on — copies them while it is built. Writing them after the
  analysis model exists leaves that copy behind until the model is rebuilt. This is the same ordering rule
  the design properties of a combination follow.
- **Every property is optional, and what you do not send is left alone**, at both levels: a `null` page
  keeps that whole tab, and an unset property inside a page keeps that property. This is what makes a
  `Get` → edit → `Set` round trip safe. Prefer sending the page you actually changed over handing back a
  whole `Get`: an echo re-sends values that are currently read-only and collects their warnings.
- **The four gamma factors are derived unless imposed.** While `SafetyFactors.Option` is
  `eSafetyFactorsAuto` the design code and its national annex decide them, and a value sent for one is
  **ignored and reported as a warning** — exactly as the dialog greys the field out. Set `Option` to
  `eSafetyFactorsImposed` in the same request to have them stored as sent.
- **A field whose control the dialog greys out is stored and reported, not refused.** The fire time, the
  three optimisation limits, the iteration ceiling, the four profile limits and the complete-calculation
  count each follow another switch; sent without it they are still written — as the dialog writes them —
  and a warning says nothing will read them. Send the pair together to keep the response silent.
- **The imperfection options are read only while an imperfection is enabled**, and the two halves of the
  gate differ: `SecondOrderAnalysis`, `ColumnsBucklingLength` and `ImperfectionGeneration` need *any* of
  the three imperfections (global sway, local bow, crown); `BeamsBucklingLength` needs the local bow
  alone. A request that turns the last imperfection off also **clears `SecondOrderAnalysis`**, because
  that is what the dialog does rather than merely disabling the box.
- **`ChainedOptimization` together with `GlobalSwayImperfections` is reported, not refused.** The
  imperfection forces are not regenerated between chained iterations, so the two do not belong
  together. The dialog looks as though it forbids the pair — ticking one clears the other — but that
  handler acts on a checkbox of the *Calculation sequence* tab which is permanently hidden and which
  the dialog never saves; the stored flag comes from the *Verifications* tab, which has no such
  handler. A model with both on is therefore reachable from the desktop application too, so the API
  stores what you sent and returns a warning naming the pair rather than refusing a state that
  really exists.
- **Three values are read-only in practice.** `eMethodWebBucklingUndefined`,
  `eImperfectionGenerationUndefined` and `eMethodBucklingNone` mean *"no option selected"* — a state only
  a model created under a steel code without that option carries. A `Get` returns them, and sending back
  the value a model already holds is a no-op, but a request cannot move a model **to** one: the dialog's
  radio group cannot be cleared once it holds a real value. `eOptimizationNonExistant` is refused
  outright; it is a sentinel of the shared enumeration that no model ever stores.
- **`Buckling.Method` is restricted by the active code.** Under the Eurocode the dialog offers
  `eMethodBucklingKa_Kb` and `eMethodBucklingN1_N2` only, and anything else is **refused**.
- **Ranges are the dialog's own.** The gamma factors accept 0…100 and η_v 0…1; the work-ratio and
  deflection limits are percentages 0…100; `MaximumIterationCount` is 1…100 and
  `CompleteCalculationCount` 1…20; e₁ and e₂ are at least 1.2 and p₁ at least 2.2, all at most 100. An
  upper work-ratio limit at or below the lower one is refused while both criteria are on. `NaN` and
  `Infinity` are refused for every number.
- **`ResetModelSettings` is the dialog's *Load defaults* button** — it restores what a newly created model
  with this steel code and national annex would carry, which is why a German project comes back with
  γM1 = 1.10 rather than 1.00.
- **Five of the seven sub-objects are shared with the North-American group** and are named without the
  `Ec3` prefix for that reason: `SteelOptimizationSettings`, `SteelProfileSortingSettings`,
  `SteelBucklingSettings`, `SteelAngleVerificationSettings` and `SteelCalculationSequenceSettings`. Their
  dialog pages carry no steel-code branch, so the two families really do edit the same thing.
- Open the project with `LogVerbosity = ErrorsAndWarnings` or higher, or every warning above is filtered
  out of the response before it reaches you.

---

#### 3.7.2 Steel (AISC / CAN-CSA) calculation settings

The general options of the **Expert STEEL design calculation of a model designed to a North-American
code** — the same *Calculation settings* dialog of Advance Design, as AISC 360, AISC 360-16 and the four
CAN/CSA S16 editions see it.

They are one group of the general model settings of [3.7](#37-model--general-settings) —
`eModelSettingsCategory.SteelAiscCalculation` — and have no endpoints of their own:

| Call | Parameter / Body | Returns |
|---|---|---|
| `GET /api/Model/model/GetModelSettings` | `category = SteelAiscCalculation` | `ModelSettingsBaseApiResponse` — an `AiscSteelCalculationSettings` |
| `POST /api/Model/model/SetModelSettings` | `AiscSteelCalculationSettings` | `BooleanApiResponse` |
| `POST /api/Model/model/ResetModelSettings` | `category = SteelAiscCalculation` | `BooleanApiResponse` |

**Six steel codes, one shape.** `aisc`, `aisc_2016`, `can_csa_s16_2009`, `can_csa_s16_2014`,
`can_csa_s16_2019` and `can_csa_s16_2024` share it — the dialog draws no distinction between them
anywhere. `DesignCode` reports which of the six the model carries, and is read-only **here**: the steel
code is a session setting, chosen with `Environments.DesignCodes.Steel` on the call that opened the
project — which takes five of the six, `can_csa_s16_2024` being the one it refuses — or inherited from
`Localization` when that was left null (`US` → `aisc_2016`, `CANADA` → `can_csa_s16_2019`, `ASIA` →
`aisc`), and fixed for the life of the project ([3.1.1](#311-the-environment-of-a-session)). Under any
other steel code all three calls are **refused**, and a Eurocode model is told to ask for
[`SteelEc3Calculation`](#371-steel-eurocode-3-calculation-settings) instead.

**Six sub-objects, one per tab of the dialog.** Each is optional: a `null` page is left exactly as the
model has it, so a request may carry one tab and change nothing else.

| Sub-object | Tab | What it carries |
|---|---|---|
| `Verification` | *Verifications* | `CrossSectionStrength`, `ElementStability`, `CalculationType` |
| `Optimization` | *Optimisation* | `Mode`, the three work-ratio / deflection switches with their percentage limits, `SearchRange`, `MaximumIterationCount` |
| `ProfileSorting` | *Sort profiles* | `SortCriterion`, `OrderingCriterion`, and the four height / width limits with their switches |
| `Buckling` | *Buckling* | `Method` — `eMethodBucklingGa_Gb` only |
| `AngleVerification` | *Angle verification* | `EdgeDistanceFactorE1`, `EdgeDistanceFactorE2`, `BoltSpacingFactorP1` — all multiples of the bolt hole diameter d₀ |
| `CalculationSequence` | *Calculation sequence* | `Verify`, `CalculateBuckling`, `VerifyDeflections`, `CalculateShapes`, `ChainedOptimization`, `CompleteCalculationCount` |

Five of the six are **the very same classes** the Eurocode group uses: those dialog pages carry no
steel-code branch, so the two families edit the same thing with the same rules, the same ranges and the
same warnings. Read them in [3.7.1](#371-steel-eurocode-3-calculation-settings) — everything said there
about `Optimization`, `ProfileSorting`, `AngleVerification` and `CalculationSequence` holds here
unchanged, including the SI units (the four profile limits are in **metres**).

**What this family does NOT have, and why.** The differences are the dialog's, not the API's:

| Not here | Why |
|---|---|
| the whole `SafetyFactors` page | the *Partial safety factors* tab is added only for the four Eurocode codes. A North-American verification works with the resistance factor φ (LRFD) or the safety factor Ω (ASD) taken from the code itself, not with a user-editable γ_M |
| `FireVerification`, `FireResistanceTime` | hidden for these six codes — Advance Design has no fire verification for them |
| the three imperfections, `SecondOrderAnalysis`, `ColumnsBucklingLength`, `BeamsBucklingLength`, `ImperfectionGeneration` | hidden outside the Eurocode family; they are EN 1993-1-1 §5.3 constructs |
| `PlateBucklingMethod` | hidden outside CM66 and the Eurocode family; it is EN 1993-1-5 |
| the `SecondGeneration` sub-objects | EN 1993-1-1:2023 only |
| the Kt / Kz lateral-torsional-buckling factors | the model stores them and they *are* this family's own LTB input, but the page that would edit them is never added to the dialog for any steel code. Exposing them would let the API produce a state the desktop application cannot; they are round-tripped untouched |

**The model still stores every one of those fields, and this group never touches them.** A write here is a
read-modify-write of the six pages above only, so a γ factor or an imperfection flag left behind by an
older file or a code conversion is carried across unchanged. `ResetModelSettings` leaves them alone for the
same reason.

**`Buckling.Method` is `eMethodBucklingGa_Gb`, and nothing else.** The *Buckling* tab enables the GA-GB
radio alone for these six codes, so ρ_A/ρ_B, K_a/K_b, η1/η2 and G_u/G_l are all **refused**. Writing back
the value the model already carries is always allowed, as everywhere in these settings, so echoing a `Get`
never fails.

```csharp
// Read what the steel design engine will run with, and which North-American code that is.
var settings = client.GetModelSettings(EModelSettingsCategory.SteelAiscCalculation);
var steel = (AiscSteelCalculationSettings)settings.Data;
Console.WriteLine(steel.DesignCode);          // aisc | aisc_2016 | can_csa_s16_2009 | … | can_csa_s16_2024
Console.WriteLine(steel.Buckling.Method);     // always eMethodBucklingGa_Gb

// A write is partial: this changes the verification tab and touches no other.
// No category on the write - the object is what names the group of settings.
client.SetModelSettings(new AiscSteelCalculationSettings
{
    Verification = new AiscSteelVerificationSettings
    {
        CrossSectionStrength = true,
        ElementStability     = true,
        CalculationType      = ESteelCalculationType.ETypeDeCalculBiaxialBending
    }
});

// The shared pages are written with the shared classes - the same ones the Eurocode group takes.
client.SetModelSettings(new AiscSteelCalculationSettings
{
    CalculationSequence = new SteelCalculationSequenceSettings
    {
        Verify            = true,
        CalculateBuckling = false,   // skip the buckling families of this run
        VerifyDeflections = true
    }
});

// ... then run the analysis and the design calculation, in that order.
client.LaunchAnalysis();
client.LaunchDesignCalculation(DesignCalculationType.Steel, new List<long>());

// And back to what the model's steel code prescribes.
client.ResetModelSettings(EModelSettingsCategory.SteelAiscCalculation);
```

**Notes**

- **Call it before `LaunchAnalysis`**, for the reason [3.7.1](#371-steel-eurocode-3-calculation-settings)
  gives: these values are written to the descriptive model, and the analysis model copies them while it is
  built.
- **`ChainedOptimization` raises nothing here.** The warning the Eurocode group returns when it meets
  `GlobalSwayImperfections` needs a control this family's *Verifications* page does not have.
- **Design templates are a separate axis.** The [Expert design templates](#106-expert-design-templates)
  of `SteelLinearDesignTemplate` cover this family too, through `AISCSteelLinearDesignTemplate` and
  `CSASteelLinearDesignTemplate`. They are still a *different* axis: these settings apply to the whole
  model and are read by the design engine whether or not any element carries a template.
- Open the project with `LogVerbosity = ErrorsAndWarnings` or higher, or every warning above is filtered
  out of the response before it reaches you.

---

### 3.8 Model — Analysis

#### `POST /api/Model/analysis/LaunchAnalysis`

Launches the finite-element calculation of the current model.

**Returns:** `BooleanApiResponse` · `Data = true` when analysis completed successfully.

```csharp
BooleanApiResponse r = client.LaunchAnalysis();
if (!r.Details.Success)
    Console.WriteLine(string.Join("\n", r.Details.Diagnostics.Select(d => d.Message)));
```

---

#### `POST /api/Model/analysis/LaunchDesignCalculation`

> ℹ️ **Enabled in this build.** This endpoint is compiled into the server behind a build switch
> that is currently **on**, so it is declared in `swagger.json` and present in every generated
> client. A build made with that switch off does not expose it, and calling it there returns **404**.

Runs the **Expert design calculation** of one material family: the design engine verifies its code criteria
(work ratios, buckling, lateral-torsional buckling, deflections, fire, reinforcement, …) against the
finite-element results and writes the design results into the model and the results folder.

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| `designType` | query | `DesignCalculationType` | No | Material family whose design engine is launched |
| *(body)* | body | `long[]` | No | Element EIDs to calculate. Omit, send `null` or send an empty array to calculate **every** element of the model |

**Returns:** `BooleanApiResponse` · `Data = true` when the expert actually calculated.

> ⚠️ **The FEM analysis is a precondition.** The design engines verify against the finite-element results, so
> [`LaunchAnalysis`](#post-apimodelanalysislaunchanalysis) must have been run first. A model that was never
> calculated is refused with an explicit diagnostic — the API never silently runs the analysis for you.

> ⚠️ **At least one combination must list the design calculation.** An engine only verifies the combinations
> whose [`DesignCalculations`](#361-design-expert-calculations-of-a-combination) name its calculation, and it
> refuses the whole run when it finds none — the steel engine reports *"No case selected for the Steelwork
> Design calculation!"*. Since `DesignCalculations` is optional, a combination created by hand without it
> takes part in nothing, which is the easiest way to get this refusal. Combinations produced by
> [`GenerateCombinations`](#362-automatic-generation-of-combinations) already carry their selection, derived
> from each combination's normative code, so generate-then-calculate needs no extra step.
>
> The selection has to be in place **before** `LaunchAnalysis`: the design engines read the *analysis* model,
> which takes its own copy of the selection while it is built. Adding the calculation to a combination after
> the analysis has run changes nothing until `LaunchAnalysis` is called again.

**How the outcome is decided.** The design engine itself reports whether it produced results, through the
per-family model flag the Advance Design user interface reads as well (`CalculationSteelDone`,
`CalculationReinforcementDone`, `CalculationTimberDone`, `CalculationCompositeDone`). `Data` is that flag, and
the flag name and value are echoed in `Details.Diagnostics` so the outcome is always traceable.

The same flags are readable at any time, without launching anything, through
[`GetAnalysisModelStatus`](#get-apimodelanalysisgetanalysismodelstatus) — which reports **five** families
rather than the four this verb can launch: pile foundations have no entry in `DesignCalculationType` and are
read-only through the API.

**Notes on behaviour:**

- The **design code** (EC2 / EC3 / EC5 / EC4, AISC, CM66, BS, ACI, CSA, …) and the **kind of each element**
  are *not* parameters: the design modules decide them from the code configured on the model and from what
  each element actually is. Elements not concerned by the requested family are simply not calculated.
- Everything the design engine reports while it calculates — errors and warnings alike — comes back in
  `Details.Diagnostics`. Note that engine warnings are reported on the error channel, so a *successful*
  calculation can return `Data = true` together with `Details.HasErrors = true`; read the messages.
- Set `LogVerbosity` to `AllDetails` in the project environment to also receive the informational summary
  (scope, family, flag value, message counts).
- Elements are addressed by their **descriptive-model** EID, exactly as everywhere else in the API.
- **Composite:** the composite engine reads its own element set from the model, so an element list sent with
  `Composite` is ignored — a warning says so and every composite element is calculated.
- The design calculation drops the cached FEM results before starting (as the user interface does) but does
  **not** change them: FEM results stay retrievable and identical afterwards.
- If the model was modified since the last analysis, a warning reports it and the calculation continues on
  the existing results — call `LaunchAnalysis` again for an up-to-date design.
- **A model configured for a 2nd generation Eurocode is refused**, on any installation where the 2nd
  generation Eurocodes are not enabled, rather than calculated under the previous generation of the
  standard — see [Section 13](#design-standards-and-national-annexes).

```csharp
// whole model, steel design
BooleanApiResponse design = client.LaunchDesignCalculation(DesignCalculationType.Steel, new List<long>());

if (!design.Data)
    Console.WriteLine("Design failed: " +
        string.Join("\n", design.Details.Diagnostics.Select(d => d.Message)));

// only two elements
var selected = new List<long> { beamEid, columnEid };
client.LaunchDesignCalculation(DesignCalculationType.Steel, selected);
```

---

#### `GET /api/Model/analysis/GetAnalysisModelStatus`

Reads **where the model stands in the mesh → finite element → design chain**: whether an analysis model
exists, the state of the mesh, whether the finite-element analysis was run and how many analysis cases carry
results, which of the four design experts have calculated, and how construction stages stand — plus a single
`OverallState` summarizing all of it.

**No parameters.**

**Returns:** `AnalysisModelStatusApiResponse` · `Data` is an [`AnalysisModelStatus`](#analysismodelstatus).

It is a **pure read, and a cheap one**: every value is a flag or a counter the model already carries, no
element of the model is visited, and nothing is created, opened or calculated. Call it as often as you like —
before `LaunchAnalysis` to see that there is nothing yet, after it to check what was produced, after
`LaunchDesignCalculation` to see which family ran. It fails only when no model is open at all
(`ANALYSIS_MODEL_STATUS_UNAVAILABLE`); a model that simply has no analysis model is a *successful* answer with
`OverallState = NotCreated`.

##### `AnalysisModelStatus`

| Property | Type | Meaning |
|---|---|---|
| `HasAnalysisModel` | `bool` | The project holds an analysis model. A model created through the API has none until `LaunchAnalysis` is called; a project opened from a file has one when it was ever calculated. An Advance Design project holds at most **one**, so this is a plain boolean — there is nothing to name or select |
| `MeshState` | `EMeshState` | `Mesh_no`, `Mesh_available` (complete) or `Mesh_incomplete` |
| `FemIsCalculated` | `bool` | The finite-element analysis has been run and produced results — the precondition `LaunchDesignCalculation` refuses to run without |
| `FemIsUpToDate` | `bool` | The analysis model still matches the results on disk |
| `FemCalculatedCasesCount` | `int` | How many analysis cases (load cases, combinations, envelopes) carry results — exactly the set Advance Design offers in a result table |
| `Design` | `DesignExpertStatus[]` | One entry per material family, always all four of `DesignCalculationType`, in that order |
| `HasConstructionStages` | `bool` | The model defines construction stages |
| `AllStagesCalculated` | `bool?` | Every stage has been calculated. `null` when the model defines no stage, or when the analysis model could not be read |
| `OverallState` | `EAnalysisModelState` | The one-value summary — see the rule below |

##### `DesignExpertStatus`

| Property | Type | Meaning |
|---|---|---|
| `Family` | `DesignCalculationType` | Which material family this entry describes — the **same** enumeration `LaunchDesignCalculation` takes, so reading a state and launching a calculation use one vocabulary |
| `IsCalculated` | `bool` | This family's design engine has produced design results — the engine's own model flag, the same one `LaunchDesignCalculation` returns as its verdict |
| `SettingsChangedSinceCalculation` | `bool?` | The family's calculation settings were changed after the design was last calculated. `null` when the model tracks no such flag — **always null for `Concrete`** |

##### Enumerations

| `EMeshState` | Meaning |
|---|---|
| `Mesh_no` | No mesh: never meshed, or the mesh was dropped |
| `Mesh_available` | A complete mesh — the only state in which FE results are meaningful everywhere |
| `Mesh_incomplete` | A mesh exists but the mesher could not cover part of the model. Advance Design **still lets the calculation run on it**, so results may exist while whole parts of the structure have no finite elements. Treat it as a warning, not a success |

The values are Advance Design's own: `eMeshState` shares its definition with the native mesh enumeration the
mesher and the analysis model use (`Components/Common/EnumContents/MeshState.h`, included by both), so the two
cannot drift. That is why the members are `mesh_*` rather than API-shaped names — NSwag renders them
`Mesh_no` / `Mesh_available` / `Mesh_incomplete` in the C# client, and Kiota as `mesh_no` … in Python.

| `EAnalysisModelState` | Meaning |
|---|---|
| `NotCreated` | No analysis model at all; every other property is at its default and says nothing |
| `Created` | An analysis model exists but carries neither a mesh nor results — also what is reported in the one case where nothing more could be read, an analysis model the session has not opened |
| `Meshed` | A mesh (complete or incomplete) but no FE results |
| `Calculated` | The FE analysis has been run and its results are current as far as the model can tell |
| `OutOfDate` | FE results exist but the model changed since they were produced. **Never reported in a headless API session** — see the warning below |

For the design families see [`DesignCalculationType`](#post-apimodelanalysislaunchdesigncalculation). Advance
Design does track a fifth expert — pile foundations — but `LaunchDesignCalculation` cannot run one, so the API
neither launches nor reports it.

##### How `OverallState` is derived

First match wins:

| # | Condition | `OverallState` |
|---|---|---|
| 1 | `!HasAnalysisModel` | `NotCreated` |
| 2 | `FemIsCalculated` **and** `!FemIsUpToDate` | `OutOfDate` |
| 3 | `FemIsCalculated` | `Calculated` |
| 4 | `MeshState != Mesh_no` | `Meshed` |
| 5 | otherwise | `Created` |

Two things deliberately **not** in the rule. The **design experts**: a model can be `Calculated` with every
family uncalculated, which is the normal state right after `LaunchAnalysis` — `Design` is the answer to that
question, and no single summary can carry four independent booleans. And the **mesh tri-state**: rules 2–4 only
ask whether a mesh *exists*, because Advance Design allows a calculation on an incomplete one, so
`Mesh_incomplete` + `Calculated` is a real and legitimate state. Read `MeshState` when it matters.

> ⚠️ **`FemIsUpToDate` is not a freshness guarantee in an API session, and `OverallState` is therefore never
> `OutOfDate` there.** The flag is maintained by the desktop application: a successful solve sets it to `true`,
> but the thing that sets it back to `false` is the user-interface listener that reacts to a model change
> (`InvalidateAnalysisModel`, raised by the Advance Design navigator and snap-ins). An API session installs no
> such listener, so once `LaunchAnalysis` has succeeded it stays `true` no matter what you create, update or
> delete afterwards.
>
> So `false` is trustworthy — something really did invalidate the model, which in practice means a project
> saved stale by Advance Design, or one converted on open — while `true` only means *"nothing reported an
> invalidation"*. **If you changed the model through the API and need results that match it, call
> `LaunchAnalysis` again; do not ask this flag.**

> ⚠️ **`IsCalculated` is not proof that design results are on disk.** It is a boolean the model stores, and the
> desktop application writes it on one path that never calculated anything: closing the *Advanced stability*
> dialog of a linear element re-writes `CalculationSteelDone` as "nothing relevant changed". A project saved
> from the user interface can therefore report the steel family calculated without a single steel result. That
> path does not exist in an API session, so a model calculated entirely through the API is not affected — but a
> model opened from a project file may be. When it has to be certain, read the results with
> [`GetDesignResults`](#post-apimodelanalysisgetdesignresults) rather than the flag.

> ⚠️ **An analysis model the session has not opened stops the read at `Created`.** This call will not open one:
> opening loads the whole mesh and instantiates the finite-element model, which is work and state, not a status
> read. The mesh, FE, design and construction-stage values then stay at their defaults and a warning in the
> diagnostics says so. It takes a project the desktop application left in that state — `LaunchAnalysis` and
> `OpenProject` both leave the model open.

```csharp
AnalysisModelStatusApiResponse s = client.GetAnalysisModelStatus();

// branch on the state instead of launching a calculation to find out
if (s.Data.OverallState != EAnalysisModelState.Calculated)
    client.LaunchAnalysis();

if (s.Data.MeshState == EMeshState.Mesh_incomplete)
    Console.WriteLine("WARNING: parts of the model have no finite elements.");

bool steelDone = s.Data.Design.Single(d => d.Family == DesignCalculationType.Steel).IsCalculated;
Console.WriteLine($"{s.Data.OverallState}, mesh {s.Data.MeshState}, "
                + $"{s.Data.FemCalculatedCasesCount} calculated case(s), "
                + $"steel design {(steelDone ? "done" : "not run")}");
```

---

#### `POST /api/Model/analysis/GetDesignResults`

> ℹ️ **Enabled in this build.** This endpoint is compiled into the server behind a build switch
> that is currently **on**, so it is declared in `swagger.json` and present in every generated
> client. A build made with that switch off does not expose it, and calling it there returns **404**.

Retrieves **Expert design** results — the read side of `LaunchDesignCalculation`. Scope today:
**steel × linear elements × Eurocode 3**.

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| *(body)* | body | `ResultsQueryDesign` | Yes | Polymorphic query; send `ResultsQueryDesignSteelLinear` |

`ResultsQueryDesignSteelLinear`:

| Property | Type | Description |
|---|---|---|
| `listElementsIDs` | `long[]` | **Descriptive** element EIDs. Required — an empty list is refused, it does *not* mean "the whole model" |
| `eResType` | `SteelLinearDesignResultType` | Which family, or `All` for everything readable |
| `eResultSet` | `SteelDesignResultSet` | `Standard`, `ColdFormed` or `AdvancedStability` |

**Returns:** `ResBaseListApiResponse` — one `ResDesignElementLinearSteel` per element *and* family. See
[Section 11](#11-data-models--analysis-results) for the payload shapes.

**Preconditions.** `LaunchAnalysis` then `LaunchDesignCalculation(Steel)` must both have succeeded: the
model-wide `CalculationSteelDone` flag is checked and a missing design is refused explicitly, so no value is
ever read from a stale or absent result file. A non-Eurocode steel code is refused by name — and so is a
model configured for a 2nd generation Eurocode, on any installation where the 2nd generation Eurocodes are
not enabled. The `ResEc3G2*` payloads below stay published, but nothing on such an installation may produce
them.

**Response codes:** `DESIGN_RESULTS_RETRIEVED`, `DESIGN_RESULTS_EMPTY_INPUT`, `DESIGN_RESULTS_QUERY_INVALID`,
`DESIGN_RESULTS_RETRIEVAL_FAILED`.

```csharp
client.LaunchAnalysis();
client.LaunchDesignCalculation(DesignCalculationType.Steel, new List<long>());

ResBaseListApiResponse res = client.GetDesignResults(new ResultsQueryDesignSteelLinear
{
    EResType = SteelLinearDesignResultType.CrossSectionResistance,
    EResultSet = SteelDesignResultSet.Standard,
    ListElementsIDs = new List<long> { beamEid, columnEid }
});

foreach (var element in res.Data.OfType<ResDesignElementLinearSteel>())
{
    foreach (var mesh in element.ResMeshes)
    {
        var payload = (ResEc3Resistance)mesh.Data;

        foreach (var check in payload.Checks)
        {
            // ALWAYS test IsCalculated first - a check the engine skipped carries zeros, and the
            // Verification mask is non-zero either way.
            if (!check.IsCalculated)
                continue;

            Console.WriteLine($"elem {element.UserID} mesh {mesh.MeshIndex} {check.Check}: " +
                              $"{check.Action} / {check.Limit} = {check.WorkRatio}");
        }
    }
}
```

> **Values may be infinite or negative, and both are genuine.** A fully restrained member yields an infinite
> buckling length or critical moment, and interaction terms can go negative — Advance Design's own tables
> print `inf` and negative percentages for the same cells. The server emits the JSON named literals
> (`Infinity`, `-Infinity`) rather than clamping, so do not assume a finite non-negative number.

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

### 3.9 Model — Actions

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

### 3.10 Model — Query & Retrieval

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
| `QueryInfoModel` | `"QueryInfoModel"` | Filter informational elements by `InformationalElementTypeEnum` (use `StructuralSystem` to list every [structural system](#104-structural-systems) of the model, `LinearReleaseTemplate` to list the whole [release pool](#105-linear-release-templates), or `DesignTemplate` to list every [Expert design template](#106-expert-design-templates)) |
| `QueryInfoLoadCase` | `"QueryInfoLoadCase"` | Returns all loads that belong to a specific load case (`CaseFamilyId`) |

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

Returns fully populated `InformationalElementBase` objects (load cases, families, combinations, structural systems, linear release templates) for the given EIDs.

| Parameter | Location | Type | Required |
|---|---|---|---|
| *(body)* | body | `long[]` | No |

**Returns:** `InformationalElementBaseListApiResponse`

---

### 3.11 Service — version, status & liveness

The only endpoints that are not about a model. They are answered straight off the request thread: they
do not wait in the queue that serializes every model and analysis operation, so they reply even
while a long analysis is running. (The one bounded exception: when that queue is **idle**, `GetStatus` may
add a short licence refresh to it and waits for it **2 s at most** — it never waits behind work.)

#### `GET /api/Service/GetVersion`

Returns the version of the Advance Design API service.

| Parameter | Location | Type | Required |
|---|---|---|---|
| *(none)* | — | — | — |

**Returns:** a plain JSON string — **not** an `ApiResponse` envelope. The call cannot fail halfway, so
it carries no diagnostics; the HTTP status is the whole outcome.

```csharp
string version = client.GetVersion();    // "1.27.1"
```

```text
curl http://localhost:52000/api/Service/GetVersion
"1.27.1"
```

**What the value is.** The version of the API **contract** — the same string the OpenAPI document
declares in `info.version`, read from a single constant on the server, so the document and the service
can never advertise two different versions. It is *not* the build number of `AD.API.Srv.exe`.

**Using it as a ping.** Any HTTP 200 means the service is up: no project has to be open, no session is
created by the call, and an analysis in progress does not delay the answer.

#### `GET /api/Service/GetStatus`

Returns whether the Advance Design API service can run model operations, and if not, why — today:
whether it holds its licence, and what that licence allows.

| Parameter | Location | Type | Required |
|---|---|---|---|
| *(none)* | — | — | — |

**Returns:** a bare [`ServiceStatus`](#servicestatus) object — **not** an `ApiResponse` envelope, and no
diagnostics. **Always HTTP 200 while the service answers**: the verdict is in the body. No serial number,
licence server name or licence path is ever returned; the technical details of a licence failure go to the
server log only.

```csharp
ServiceStatus status = await client.GetStatusAsync();
if (!status.CanRun)                       // decide on CanRun, never on the Status string alone
{
    Console.WriteLine($"{status.Status}: {status.Message}");
    return;
}
LicenceStatus licence = status.Licence;   // may be null: "not known yet"
Console.WriteLine(licence?.IsPermanent == true ? "permanent licence"
                                              : $"{licence?.DaysToExpiry} day(s) to expiry");
```

```text
curl http://localhost:52000/api/Service/GetStatus
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

*(Illustrative: the licence kind, the days to expiry and the families available depend on the licence.)*

**The status values.** `status` is a **string, not an enumeration, on purpose** — more values may come.

| `status` | `canRun` | Meaning | What to do |
|---|---|---|---|
| `Ok` | `true` | The service is up and holds everything it needs to run model operations. `message` is `null`. | Go on. |
| `Starting` | `false` | The service is still creating its engine session and checking out its licence; nothing is known about the licence yet (`licence` is `null`). | Ask again shortly. |
| `Licence_Unavailable` | `false` | The service runs but does not hold its mandatory licence: model operations cannot be relied upon. `message` and `licence.errorMessage` say why, in words a user can act on (the technical details are in the server log). | Fix the licence, then **restart the server**: a missing licence is not checked out again by later calls. |
| `Initialization_Failed` | `false` | The engine session could not be created, or no licence state was recorded while creating it. The server log has the details. A startup failure counts only **while there is no session**: every model request retries the creation, and once one succeeds the status comes from the licence snapshot again. | A later model request may recover it; otherwise read the server log and **restart the server**. |
| `Stopping` | `false` | The service is shutting down: `CloseSession` was called, the licence is being — or has been — released, and the process is about to exit. When the close failed, `message` reads "The session close failed: ... See the server log." | Send no more model operations. |
| *anything else* | — | A value added by a later version. | Treat it as "not Ok"; decide with `canRun`. |

**After `CloseSession`.** From the moment `CloseSession` is requested until the process exits, the answer
is **`Stopping`** — never `Licence_Unavailable`, even though the licence is released on the way out. Its
`licence` section is the last snapshot taken: first the one of the licence still held, then, once the
session is closed and the licence released (the close runs once the requests already accepted are done, then saves the open project and waits 5 s), the released one — only
`checkedAt` and `errorMessage` set, nothing of the released licence reported any more. No licence refresh
is attempted while stopping. When the close failed, `message` reads "The session close failed: ... See the server log.", and the process exits with code `3`.

**Timing — it does not queue behind model work.**

- **At startup** the server creates its engine session — and checks its licence out — as the **first item
  of its work queue**, before any model request. While that is still running, `GetStatus` waits for it
  **at most 30 s** and then answers `Starting`.
- **Afterwards** `licence` is a **snapshot**, not a live read: taken when the session was created, and
  refreshed now and then. `licence.checkedAt` says when. A refresh is due only when the licence is
  **held**, the work queue is **idle**, and the **later of `checkedAt` and the last refresh attempt** is at
  least **30 s** old — so a refresh that keeps failing is retried at most every 30 s too. None is attempted
  while the service is `Stopping`, or while a startup failure is current.
- A refresh re-reads the licence kind, the days to expiry of the base module and the design families (a test
  checkout of each family's modules); it never checks the mandatory licence out again.
- The call that triggers a refresh waits for it **at most 2 s**. A full refresh takes about 2 s, so that
  call usually answers from the **previous** snapshot and the **next** call gets the new one (the refresh
  completes on the work queue either way). Polling `GetStatus` more often than every 30 s returns the same
  snapshot, and never turns into polling the licence server.
- A running model operation or analysis never delays the answer.

**Forward compatibility.** What a newer server promises not to break:

- **`status` and `canRun` are the two values guaranteed never to break a client.** `status` is a string so
  that a new value cannot fail deserialization: treat any value other than `Ok` as "not Ok", and decide
  with **`canRun`**, which keeps its meaning whatever the `status`.
- **`schemaVersion`** rises whenever a section or a status value is added. Compare it with the one you
  were written against if you wish, but do **not** fail on a higher one.
- A section that is **`null`** (for example `licence`) means "not known yet" or "does not apply to this
  server" — never "failed". Ignore properties you do not know.
- **The enumerations inside the licence section** — `DesignCalculationType` and `LicenceProtectionType` —
  are **not** covered by this promise: they follow the usual versioning of the API's enumerations, where a
  new member is a change of the contract.

##### `ServiceStatus`

The state of the Advance Design API service: whether it can run model operations, and if not, why.

| Property | Type | Required | Description |
|---|---|---|---|
| `schemaVersion` | `int` | Yes | Version of the shape of this object; rises when a section or a status value is added. Default `1` (the current value) |
| `serviceVersion` | `string` | Yes | Version of the API service — the same string `GET api/Service/GetVersion` returns |
| `status` | `string` | Yes | The verdict: `Ok`, `Starting`, `Licence_Unavailable`, `Initialization_Failed` or `Stopping` (after `CloseSession`, until the process exits) today. A string, not an enumeration, on purpose: more values may come; treat an unknown one as "not Ok" and decide with `canRun`. Default `"Starting"` |
| `canRun` | `bool` | Yes | True when model operations can be sent to this service now. The one value to decide on, whatever `status` says. Default `false` |
| `message` | `string?` | No | What a user should be told when `status` is not `Ok`. `null` when it is |
| `licence` | [`LicenceStatus?`](#licencestatus) | No | The licence this service holds. `null` while it is not known yet (status `Starting`) or when no licence state could be recorded. With `Stopping`, the last snapshot taken — once the licence is released it reports nothing of it any more |

##### `LicenceStatus`

The licence section of `GetStatus`: whether this server holds the licence it needs to run, and what that
licence allows. **A snapshot, not a live read** (see *Timing* above). `isAvailable` is the one value that
decides whether the server can work. Everything else describes the licence it holds, and is at its default
when it holds none — including once the licence has been **released** because the server is shutting
down: then only `checkedAt` and `errorMessage` are set.

| Property | Type | Required | Description |
|---|---|---|---|
| `checkedAt` | `DateTimeOffset` | Yes | When the licence was read for this snapshot |
| `isAvailable` | `bool` | Yes | True when the server holds its **mandatory** licence — the base module and the user seat, checked out together when the server started. Without it no model operation can be relied upon, and `errorMessage` says why. The mandatory licence is checked out **once, at startup, and never checked out again** afterwards: a network licence whose lease is lost later is **not** detected here. Default `false` |
| `errorMessage` | `string?` | No | Why the mandatory licence is not held, **in words a user can act on**: the licence component's own message without its technical lines (licence server names, licence file paths, raw error codes — those go to the **server log only**), or, when nothing is left of it, a description of what failed. Also set when the licence was released because the server is shutting down, and — with `isAvailable` **true** — when the licence is held but its details could not be read. `null` when there is nothing to report. See *What `errorMessage` contains* below |
| `protectionType` | [`LicenceProtectionType`](#licenceprotectiontype) | Yes | The kind of licence the server runs under. Default `NoLicence` |
| `isPermanent` | `bool` | Yes | True when the base module's licence never expires. `daysToExpiry` is `null` then. Default `false` |
| `daysToExpiry` | `int?` | No | Days left before the base module's licence expires, counted when the snapshot was taken. `null` when the licence is permanent, when none is held, or when the licence component could not tell |
| `designCalculations` | [`DesignCalculationLicence[]`](#designcalculationlicence) | Yes | Whether each design family's licence is available. The server always fills in **every `DesignCalculationType` it knows**, in the order of that enumeration — four today: `Concrete`, `Steel`, `Timber`, `Composite` |

**What `errorMessage` contains.** Only the part of the licence component's message a user can act on:

- The message is cut at its **first line that starts with** `Feature:`, `Server name:`, `License path:`,
  `FlexNet Licensing error` or another FlexNet context label (`Filename:`, `Hostname:`, `Hostid:` …;
  case-insensitive) — those lines name licence servers, hosts and licence file paths.
  Everything from that line on is dropped.
- The lines kept are trimmed, empty ones skipped, and **joined with a single space** (a FlexNet reason is one
  paragraph hard-wrapped over several lines).
- When **nothing is left** (the message was empty, or began with a technical line), a generic description
  takes its place, naming the mandatory licence features that could not be checked out.
- The language depends on the licence back end: **FlexNet** text is its raw **English**; **Cryptlex** and
  **Nalpeiron** return short, **localized** sentences, which pass through whole.
- The full raw text and the back-end error code are written to the **server log** only, once, at startup.

##### `DesignCalculationLicence`

Whether the licence of **one** design family is available. The server returns one entry for every
`DesignCalculationType` it knows. The answer comes from the **same licence mapping `LaunchDesignCalculation`
refuses on**, so a family reported available here was **not refused for licence reasons at the time of
`LicenceStatus.checkedAt`**. It is no promise about a later call: `LaunchDesignCalculation` does its own
full checkout, and other gates (for example the 2nd-generation EC3 one) can still refuse it.

| Property | Type | Required | Description |
|---|---|---|---|
| `type` | `DesignCalculationType` | Yes | Which design family this entry describes — the same enumeration `LaunchDesignCalculation` takes. Default `Concrete` |
| `isAvailable` | `bool` | Yes | True when the licence module(s) this family needs could be checked out when the licence was last read (`LicenceStatus.checkedAt`). `Composite` needs both the concrete and the steelwork modules. Always `false` when the mandatory licence is not held (`LicenceStatus.isAvailable` false): nothing was asked of the licence component then. Default `false` |

---

## 4. Response Envelope Pattern

Every API call — except `GetVersion`, which returns a bare string, and `GetStatus`, which returns a bare `ServiceStatus` object — returns a typed response envelope. All envelopes follow the same pattern:

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
  "userID": 1,
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

All material classes inherit from `Material` (the direct base class). `MaterialBase` was removed — `Material` is now the top-level base.

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

### 6.5 Data Models — Sections

`Section` is polymorphic and requires the `$type` discriminator: `"Section"` for a catalog or parametric
section, `"UserSection"` for one the caller defines.

#### `Section` (base)

| Property | Type | Required | Description |
|---|---|---|---|
| `Name` | `string` | ✓ | Section name |
| `Type` | `SectionType_API` | ✓ | Section shape type |
| `FamilyCode` | `string?` | — | Family code (`"USECT"` for a user section) |
| `CatalogName` | `string?` | — | Catalog the section came from; empty for a user section, which lives only in the model |

#### `UserSection`

Inherits `Section`. A user section carries no dimensions: everything the analysis and the design engines
need is the set of sectional characteristics below. **Advance Design never derives them from the
outline**, so they all have to be supplied, and they are never checked against it. All values are in SI
units.

`Type` is forced to `st_USER`, `FamilyCode` is `"USECT"` and `CatalogName` is empty — the three are
outputs, and any value sent for them is ignored.

| Property | Type | Unit | Description |
|---|---|---|---|
| `Area` | `double` | m² | Sectional area A. **Must be > 0** — it drives E·A and the self weight |
| `Iy` | `double` | m⁴ | Bending inertia about the local y axis (strong axis). **Must be > 0** |
| `Iz` | `double` | m⁴ | Bending inertia about the local z axis (weak axis). **Must be > 0** |
| `Iyz` | `double` | m⁴ | Product of inertia. 0 for any section with an axis of symmetry; **signed** for the others, so a negative value is accepted (Advance Design reports −42.53 cm⁴ for an L 80×80×8). Only its magnitude is bounded: `Iy·Iz − Iyz²` must stay > 0 |
| `It` | `double` | m⁴ | Torsional constant (J). **Must be > 0** — it drives G·It |
| `Iw` | `double` | m⁶ | Warping constant (Cw). May be 0 for solid sections and closed tubes; needed for lateral torsional buckling |
| `WelyInf` | `double` | m³ | Elastic modulus about y, lower fibre (Syinf) |
| `WelySup` | `double` | m³ | Elastic modulus about y, upper fibre (Sysup) |
| `WelzInf` | `double` | m³ | Elastic modulus about z, lower fibre (Szinf) |
| `WelzSup` | `double` | m³ | Elastic modulus about z, upper fibre (Szsup) |
| `Wply` | `double` | m³ | Plastic modulus about y (Zy) |
| `Wplz` | `double` | m³ | Plastic modulus about z (Zz) |
| `Wt` | `double` | m³ | Torsional modulus |
| `Sy` | `double` | m² | Shear area for a shear force along y (European notation) |
| `Sz` | `double` | m² | Shear area for a shear force along z (European notation) |
| `Avy` | `double` | m² | Shear area for a shear force along y (US / Canada notation) |
| `Avz` | `double` | m² | Shear area for a shear force along z (US / Canada notation) |
| `ProfileType` | `UserSectionProfileType_API` | — | Which standard shape the section behaves like. Left `Unknown`, the steel design engines skip the shape-dependent verifications |
| `RollingType` | `SectionRollingType_API` | — | Lamination type, drives the buckling curve. Default `Welded`, which is what Advance Design assigns to a user section |
| `Shape` | `UserSectionShape?` | — | The outline. `null` keeps the default 50 × 50 mm square |

**Validation.** The call is **refused** — and nothing is written in the model — for any value that is
not a finite number, for any negative value **except `Iyz`**, for `Area`, `Iy`, `Iz` or `It` ≤ 0, for
`Iy·Iz − Iyz² ≤ 0` (an inertia tensor that is not positive definite), and for a `ProfileType` or a
`RollingType` that names no value of its enumeration. `"NaN"`, `"Infinity"` and `"-Infinity"` are valid
JSON for this service — the design results carry genuine infinities — so they can reach this endpoint;
they are refused rather than stored, because no comparison against them would ever be true.
Reported as **warnings** but accepted: a zero `Iw`, `Wel*`, `Wpl*`, `Wt`, `Sy`/`Sz` or
`Avy`/`Avz`; a `ProfileType` left `Unknown`; a plastic modulus smaller than the elastic one; a shear area
larger than `Area`. Warnings only reach the response when the session was opened with
`LogVerbosity = LogVerboseLevel.ErrorsAndWarnings`.

#### `UserSectionShape` — the outline (the epure)

The outline is the geometry the section is drawn with, and what the application derives its bounding box
and its section shape from. It is a list of contours, so a section made of several disjoint parts, or one
with holes, needs nothing special.

**How to define an outline**

1. **The points are 2D, in the section local axes, in metres.** Every point is a `(Y, Z)` pair in the
   plane of the cross-section: y is the horizontal axis, z the vertical one and positive upwards. There
   is no third coordinate — the outline is the constant cross-section of the member, which Advance Design
   extrudes along its length.
2. **The origin is the centre of gravity of the section.** Advance Design does not compute the centre of
   gravity of a user section from its outline: it reports it as `(0, 0)`, and takes that as the reference
   point when it maps the offsets of the members the section is assigned to. So place the outline with
   its centroid on the origin. That is also what the application does to the outlines it produces itself
   — after computing the characteristics it translates the outline so the computed centroid falls on
   `(0, 0)`. **Nothing recentres an outline that arrives through the API**: an off-centre outline is
   stored and drawn exactly where it was put, and the member then looks shifted with respect to its own
   axis.
3. **Which way up matters for an asymmetric section.** `WelyInf` and `WelzInf` belong to the fibres at
   the lowest z and y, `WelySup` and `WelzSup` to those at the highest. Drawing the flange of a T at
   positive z therefore means "Sup" is the flange side; the characteristics have to agree with the
   outline.
4. **A contour is closed automatically.** List a polygon's vertices once, in order around the contour, and
   do not repeat the first point at the end. The order is stored exactly as given; Advance Design writes
   its own outlines clockwise in the (y, z) plane.
5. **Several contours describe several areas.** A compound section needs one material contour per part —
   a double angle is two contours, a double channel is two, a battened box of four angles is four. The
   parts do not have to touch and they are not merged.
6. **A hole removes material.** Set `IsHole` on a contour lying inside a material contour to describe a
   tube, a box or a cell. Holes and material contours mix freely, in any order, and either kind can be a
   polygon or a circle — a circular tube is one circle plus one circular hole.
7. **The outline is geometry only.** It changes no sectional characteristic.

| Property | Type | Description |
|---|---|---|
| `Contours` | `UserSectionContour[]` | The contours the outline is made of, at least one of which must add material. Stored and drawn in the order given |

#### `UserSectionContour`

| Property | Type | Default | Description |
|---|---|---|---|
| `Type` | `UserSectionContourType_API` | `Polygon` | `Polygon` (a vertex list) or `Circle` (a centre and a radius) |
| `IsHole` | `bool` | `false` | `false` adds material, `true` removes it |
| `Points` | `UserSectionContourPoint[]` | — | A polygon's vertices, at least 3, listed once in order; or a circle's single centre point |
| `Radius` | `double` | `0` | A circle's radius in m, which must be > 0. Ignored on a polygon |

#### `UserSectionContourPoint`

| Property | Type | Default | Description |
|---|---|---|---|
| `Y` | `double` | `0` | Coordinate along the section local y axis, in m |
| `Z` | `double` | `0` | Coordinate along the section local z axis, in m, positive upwards |
| `FilletRadius` | `double` | `0` | Radius of the fillet rounding the corner at this vertex, in m; 0 keeps it sharp. Polygons only — a circle's radius is `UserSectionContour.Radius` |

**Outline validation.** The call is **refused** for: an outline with no contour (leave `Shape` null to get
the default one); an outline made only of holes; a polygon with fewer than 3 points; a circle without
exactly one point or without a positive radius; two consecutive identical points — which is also what a
polygon closed explicitly by repeating its first point produces; a negative `FilletRadius`; and a
`FilletRadius` too large for its corner. That last rule uses the same geometry as the application: the
fillet's tangent length, `radius / tan(angle / 2)` where *angle* is the interior angle at the vertex, has
to stay shorter than both adjacent edges — Advance Design would otherwise draw a corrupted outline without
reporting anything. A fillet on a vertex that forms no corner (one sitting on a straight edge between its
neighbours) is accepted with a **warning**, because it has no effect. Every coordinate, every
`FilletRadius` and every circle `Radius` also has to be a finite number.

**Update semantics.** Supplying a `Shape` for an existing user section replaces its outline entirely;
leaving `Shape` null leaves the outline it already had untouched — it does **not** reset it to the default
square.

**Limitations.** A user section edited in the Advance Design cross-section editor (Sec.exe) can carry
thin-walled contours in addition to its drawn geometry, which feed the profile segmentation the Advanced
Stability solver uses. `UserSectionShape` cannot express them: reading such a section returns only its
drawn contours, and supplying an outline for it drops the rest, which is reported as a warning.

Advance Design also draws two contour kinds this object has no equivalent for — an arc and a segment —
and a polyline, whose points carry a **camber** rather than a fillet radius. Reading a section that uses
them reports those contours as polygons, which is exact for a straight polyline (that is what the default
square is) but only an approximation for the rest. Such a read raises a warning naming the section, and
that outline must not be sent back unchanged.

**Example — a compound section: two angles back to back with a 10 mm gap.**

```csharp
static UserSectionContourPoint P(double y, double z) => new() { Y = y, Z = z };

// an 80 x 80 x 8 angle, heel at the origin, legs along +y and +z, clockwise
static UserSectionContourPoint[] Angle(double leg, double thk) =>
[
    P(0, 0), P(0, leg), P(thk, leg), P(thk, thk), P(leg, thk), P(leg, 0)
];

var leg = 0.080; var thk = 0.008; var halfGap = 0.005;

var left  = Angle(leg, thk).Select(p => P(-p.Y - halfGap, p.Z - leg / 2)).Reverse().ToList(); // mirrored
var right = Angle(leg, thk).Select(p => P( p.Y + halfGap, p.Z - leg / 2)).ToList();

var doubleAngle = new UserSectionShape
{
    Contours =
    [
        new UserSectionContour { Points = left  },
        new UserSectionContour { Points = right }
    ]
};
```

Mirroring reverses the direction the vertices run in, so the sequence is reversed as well to keep both
contours turning the same way.

---

## 7. Data Models — Elements (geometric)

All geometric elements inherit from `ElementBase`.

### `ElementBase` (base)

| Property | Type | Required | Description |
|---|---|---|---|
| `UserID` | `int` | — | User-facing ID displayed in the UI for element identification. Differs from the internal EID — managed by the application; treat as read-only from the client side. |

### System membership — `SystemIDs`

Every **structural** element type carries a `SystemIDs` list (`ICollection<EID>`): the
[structural systems](#104-structural-systems) — containers / building levels — the element belongs to.
An element can belong to several systems at once.

It is declared on the concrete structural types, not on `ElementBase`: `ElementLinear`,
`ElementPlanar`, `ElementLoadArea`, `ElementMass`, `ElementSinglePile`, `ElementPoint`,
`ElementPolyLine` and the three support families (`FamilySupportPunctual`, `FamilySupportLinear`,
`FamilySupportPlanar`, hence all 12 support types).
Loads and imposed displacements (`ElementLoadPunctual`, `ElementLoadLinear`, `ElementLoadPlanar`,
`ElementImposedDisplacement`) have **no** system membership and no `SystemIDs`.

| Operation | Behaviour |
|---|---|
| **Create** | The element is created in the **first** listed system and added to the others. When the list is empty or omitted it is placed in the model root system ("Structure 0"). If **any** listed EID is not an existing system the element is **not created** — it is never silently placed elsewhere. |
| **Read** | Filled with the element's current systems. |
| **Update** | A non-empty list sets the membership to **exactly** those systems (the element is added to the new ones and removed from the ones no longer listed). An **empty** list means "membership not specified" and leaves it unchanged. If any listed EID is invalid, the membership is left untouched and the update **returns false** (the other property changes already applied stay applied). |

> **A structural element always belongs to at least one system** — it can never be left with none. This
> is why an empty `SystemIDs` list is "unchanged" and not "clear the membership". If an element would end
> up with no system at all (its systems were deleted, or a requested membership could not be applied),
> the API puts it back in the root system "Structure 0" and logs a warning.

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
| `SectionOrientationPointId` | `int?` | — | User identifier (`UserID`) of the [`ElementPoint`](#elementpoint) the section is oriented towards. `0` = none, the section is then oriented by `SectionOrientationAngle` — see [Orienting a section towards a point](#orienting-a-section-towards-a-point) |
| `SectionExcentration` | `SectionOffsetStyle` | — | Section eccentricity offset |
| `InertiaProperties` | `LinearElementInnertia` | — | Cracked inertia settings |
| `InitialConstraintProperties` | `InitialConstraint` | — | Pre-stress / initial stress |
| `RelaxationTotale` | `ConnectionTotale` | — | Total (rigid) end releases |
| `RelaxationElastique` | `ConnectionElastique` | — | Elastic end-spring releases |
| `HaunchStart` | `HaunchProperties` | — | Start haunch |
| `HaunchEnd` | `HaunchProperties` | — | End haunch |
| `Clipping` | `ClippingProperties` | — | Clipping planes (XY, XZ) |
| `LoadAreaLoadTransferProperties` | `LinearLoadAreaLoadTransfer` | — | Load area transfer settings |
| `SystemIDs` | `ICollection<EID>` | — | Systems the element belongs to — see [System membership](#system-membership--systemids) |
| `DesignTemplateId` | `EID?` | — | [Expert design template](#106-expert-design-templates) assigned to the element. `null` = assignment unchanged, `0` = detached (built-in default template) — see [Assigning a template to an element](#assigning-a-template-to-an-element) |

#### Orienting a section towards a point

`SectionOrientationPointId` is the **user identifier** of an [`ElementPoint`](#elementpoint) — its `UserID`,
the number shown in the interface, **not** its internal `EID`. When it is set, the element's local axes are
rebuilt so that the cross-section faces that point, in place of the fixed `SectionOrientationAngle`.

| Value | Behaviour |
|---|---|
| `0` | No orientation point — the element is oriented by `SectionOrientationAngle` alone |
| `UserID` of an existing point | The section is oriented towards that point; `SectionOrientationAngle` no longer drives the orientation |
| `UserID` matching no point | **Rejected** — the value is not stored |
| omitted (`null`) | **Unchanged** — this property deliberately has no default, so an update that does not send it keeps the current value |

> Setting it registers the element as an **observer** of the point: moving the point re-orients the element,
> and deleting the point resets `SectionOrientationPointId` back to `0` (the element falls back to
> `SectionOrientationAngle`).

### `LinearLoadAreaLoadTransfer`

Controls how loads from wind walls, scaffolding, snow, and climatic surfaces are transferred to this linear element.

| Property | Type | Default | Description |
|---|---|---|---|
| `IsWindWallSupport` | `bool` | `true` | Acts as a wind wall load transfer support |
| `IsScaffoldingSupport` | `bool` | `false` | Acts as a scaffolding load transfer support |
| `ClimaticLE2D` | `bool` | `false` | Participates in 2D climatic (wind/snow) distribution |
| `LoadSpanFront` | `double` | `2.5` | Tributary span on front side (m) |
| `LoadSpanBehind` | `double` | `2.5` | Tributary span on back side (m) |
| `ContinuityCoeff` | `double` | `1.0` | Continuity coefficient (1.0 = full continuity) |
| `OpeningType` | `LoadArea_Opening_type` | `CG_WINDWALL_OPENING_TYPE_CLOSED_NO_OPENINGS` | Opening type for wind pressure calculations |
| `OpeningsValue` | `double` | `0.0` | Total area or percentage of openings (m² or %) |
| `SnowGuards2D` | `bool` | `false` | Snow guards present on this 2D element |
| `ClimaticType` | `Load_Area_Type` | `CG_LOADAREA_BUILDING` | Climatic type for 2D wind/snow load distribution |
| `EffectiveWindArea` | `double?` | — (model: `0.929`) | ASCE 7-22 effective wind area (m²) of the 2D climatic wind walls generated from this element; `null` = keep the default on create, unchanged on update; must be > 0 |

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
| `Openings` | `ICollection<ICollection<Pt3D>>` | — | List of interior openings (holes). Each opening is a closed polygon defined by 3D points in global coordinates. Only fully interior openings are accepted — edge openings will be rejected. |
| `SelectedReleaseName` | `string?` | — | Name of the [linear release template](#105-linear-release-templates) applied to **every** edge — see [Edge releases](#edge-releases--selectedreleasename-and-linearreleases) |
| `LinearReleases` | `ICollection<EID>` | — | Template EID applied to **each** edge, one entry per edge in edge order — see [Edge releases](#edge-releases--selectedreleasename-and-linearreleases) |
| `SystemIDs` | `ICollection<EID>` | — | Systems the element belongs to — see [System membership](#system-membership--systemids) |

### Edge releases — `SelectedReleaseName` and `LinearReleases`

The edges (sides) of a planar element are connected to the rest of the structure through
[linear release templates](#105-linear-release-templates) taken from the model release pool. A planar
element only **references** the templates — it never owns a copy — so editing a template changes every
edge that uses it. Two complementary properties expose that assignment:

| Property | Type | Granularity | AD user interface |
|---|---|---|---|
| `SelectedReleaseName` | `string?` | One template for **all** the edges | "Releases / Side" property |
| `LinearReleases` | `ICollection<EID>` | One template **per edge** | "Releases / Definition" dialog |

**Edge order.** `LinearReleases[i]` is the edge going from `GeomPtsList[i]` to `GeomPtsList[i+1]`, the
last entry closing back on `GeomPtsList[0]` — so there are exactly as many releases as points.

| Operation | `SelectedReleaseName` | `LinearReleases` |
|---|---|---|
| **Create** | Optional. When omitted, the built-in **"Fixed"** template is applied to every edge — exactly like an element created in AD. A name that matches no template falls back to **"Fixed"** with a warning; the element is still created. | Optional. When supplied, its length must equal the number of edges of the element and every EID must be an existing template, otherwise the **whole list is rejected**, an error is logged and no edge is changed. |
| **Read** | The value stored on the element: the name of a template when all the edges share the same one, a localized **"Other"** when they differ, and empty for an element whose releases were never initialized. | The complete edge list of the element (`null` only when the element has no releases at all). This is the authoritative per-edge state. |
| **Update** | `null` or an empty string leaves the current assignment unchanged. A non-empty value must name a template that exists in the model, otherwise it is **ignored and a warning is logged** — unlike on create, nothing falls back. | Same validation as on create — an invalid list leaves **every** edge untouched rather than assigning part of it. An element whose releases were never initialized is initialized with the default template first, so the per-edge assignment still applies. |

> **`SelectedReleaseName` wins.** Setting it applies that template to **all** the edges and therefore
> takes precedence over `LinearReleases` when both are provided in the same request — on create as well
> as on update, and sending both logs a warning. Send `LinearReleases` alone to define a per-edge
> assignment; send `SelectedReleaseName` alone to make every edge the same.

> Because the names of the three built-in templates are **localized**, the invariant keywords `"free"`,
> `"pinned"` and `"fixed"` are also accepted by `SelectedReleaseName` (case-insensitive) and resolved to
> the corresponding built-in template. For any other template, list the pool and read the name from it
> instead of hard-coding it.

> **Outer contour only.** The number of releases always follows the **outer contour** of the element —
> `Openings` never add edges — and the element always holds exactly one release per edge.

> ⚠️ **Changing the number of edges resets the edge releases.** When an update changes `GeomPtsList` so that
> the element ends up with a different number of edges, every edge is reset to the built-in **"Fixed"**
> template and a warning is returned: with edges added or removed the list indices no longer designate the
> same physical sides, so keeping the releases by index would silently move a release onto another edge.
> A contour change that keeps the **same** number of edges leaves the releases untouched — they stay
> attached to the same edge index, exactly like the interactive model. To keep or apply a specific
> assignment across an edge-count change, send `SelectedReleaseName` or `LinearReleases` **in the same
> update**: an explicit request always wins over the reset, and the reset still runs if what you sent had
> to be refused.

### Planar contour validity

Every object built on a planar contour — planar elements, planar supports, load areas and surface loads —
has its `GeomPtsList` validated with the same rule as the interactive model. The contour must:

- have at least **3** points;
- contain **no coincident (duplicated)** points;
- contain **no three consecutive collinear** points — an intermediate point in the middle of a straight
  side produces a degenerate zero-length edge, which the model does not accept;
- be **planar** — every point in the same plane;
- **not intersect itself**.

A contour that breaks any of these is refused: on create the element is **not created** — it leaves no
trace in the model, so a rejected create never has to be cleaned up — and on update **nothing at all is
modified** (the element keeps its previous geometry, its releases and its name). The error message names
the exact reason, so fix the point list rather than retrying the same points.

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
| `SystemIDs` | `ICollection<EID>` | — | Systems the pile belongs to — see [System membership](#system-membership--systemids) |

### `ElementLoadArea`

Defines a load area (tributary area) that distributes climatic or mechanical loads onto structural elements.

| Property | Type | Required | Description |
|---|---|---|---|
| `GeomPtsList` | `ICollection<Pt3D>` | ✓ | Polygon vertices |
| `LoadTransferProperties` | `LoadAreaLoadTransfer` | — | Transfer method and direction |
| `MechanicalProperties` | `LoadAreaMechanicalProperties` | — | Self-weight, material, thickness |
| `ClimaticProperties` | `LoadAreaClimaticProperties` | — | Wind/snow availability, gutters, exposure |
| `SystemIDs` | `ICollection<EID>` | — | Systems the load area belongs to — see [System membership](#system-membership--systemids) |

`LoadAreaClimaticProperties.EffectiveWindArea` (`double?`, m²) is the ASCE 7-22 effective wind area
of the components carried by the load area (span × effective width, the width not less than a third of the
span). The model default is **0.929 m² (10 ft²)**; `null` keeps it on create and leaves the current value on
update; a value ≤ 0 is refused. Only the ASCE 7-22 climatic code with the future Chapter 30 wind design method implementation will read it.

### `ElementPoint`

The *Point* object of Advance Design (`som.EFFacadePoint`): a positioned model object with **no stiffness
and no load**. It drives the mesh (it forces an FE node at its location), it snaps other elements onto it,
and — under Eurocode — it acts as the pivot of the direction of the steel imperfection forces.

Points are created, read, updated, queried and deleted through the same generic element endpoints as every
other element; query them with [`ElementTypeEnum.ElementPoint`](#elementtypeenum).

| Property | Type | Required | Description |
|---|---|---|---|
| `GeomPt` | `Pt3D` | ✓ | Position of the point in the global coordinate system (m). Default `(0, 0, 0)` |
| `MeshActive` | `bool?` | — | Whether the mesh generator takes the point into account: `true` forces an FE node at its position, `false` makes the analysis model ignore it. Default `true` |
| `Density` | `double` | — | Local mesh density imposed around the point — the target size of the finite elements in its neighbourhood (m). `0` = no local density, the global mesh settings apply. Only used when `MeshActive` is `true`. Default `0` |
| `MagnetismStatus` | `bool` | — | Enables magnetism on the point: other elements whose end lies within `MagnetismTolerance` of it are snapped onto it. Default `false` |
| `MagnetismTolerance` | `double?` | — | Radius within which magnetism attracts other elements (m). Only used when `MagnetismStatus` is `true`. Default `0.001` |
| `ImperfectionForceDirectionPivot` | `bool` | — | Marks the point as a pivot for the direction of the steel imperfection forces. Read by the steel design when the element design template sets `ForceDirectionPivotOnSuperElement` to `use_points`. Default `false` |
| `SystemIDs` | `ICollection<EID>` | — | Systems the point belongs to — see [System membership](#system-membership--systemids) |

> **`MeshActive` and `MagnetismTolerance` are nullable on purpose.** A non-nullable `bool` would reach the
> server as `false` — and a non-nullable `double` as `0` — whenever the client omits it. Leaving either
> **null** means "keep the Advance Design default" on create and "leave the current value untouched" on
> update; both always read back populated.

> `ImperfectionForceDirectionPivot` is stored and returned for **every** model, but it is only meaningful
> under **Eurocode** and **2nd generation Eurocode** — the desktop hides the property for the other design
> codes.

```csharp
long pointId = client.CreateElement(new ElementPoint
{
    GeomPt             = new Pt3D { X = 2.5, Y = 0, Z = 4 },
    MeshActive         = true,
    Density            = 0.25,   // 25 cm finite elements around the point
    MagnetismStatus    = true,
    MagnetismTolerance = 0.01    // snap element ends closer than 1 cm
}).Data.Value;
```

### `ElementPolyLine`

The *Polyline* object of Advance Design (`som.EFFacadeCurve`, listed as *Line* in the model tree): a chain
of straight segments with **no stiffness, no material, no section and no load**. It is reference geometry —
it drives the mesh (it forces FE nodes along its segments when `MeshActive` is set), and other elements are
snapped or constructed onto it.

Poly lines are created, read, updated, queried and deleted through the same generic element endpoints as
every other element; query them with [`ElementTypeEnum.ElementPolyLine`](#elementtypeenum).

| Property | Type | Required | Description |
|---|---|---|---|
| `GeomPtsList` | `ICollection<Pt3D>` | ✓ | The vertices, in drawing order, in the global coordinate system (m). N points give **N-1 segments** |
| `MeshProperties` | `PolyLineMeshProperties` | — | Mesh definition. `null` = leave the whole mesh definition alone |
| `SystemIDs` | `ICollection<EID>` | — | Systems the poly line belongs to — see [System membership](#system-membership--systemids) |

#### `PolyLineMeshProperties`

| Property | Type | Required | Description |
|---|---|---|---|
| `MeshActive` | `bool?` | — | Whether the mesh generator takes the poly line into account: `true` forces FE nodes along its segments, `false` makes it pure reference geometry. Default `false` |
| `DetailedMeshStylePerEdge` | `ICollection<DetailedMeshOnSegment>` | — | The mesh definition of each **segment**, in segment order: entry `i` is the segment from `GeomPtsList[i]` to `GeomPtsList[i+1]`, so there are `GeomPtsList.Count - 1` entries. Only used when `MeshActive` is `true`. Default `null` (untouched) |

Each `DetailedMeshOnSegment` carries `NumberOfElements`, `SizeOfElements` (m), `SpacingOption`
([`DetailedMeshSpacingOption`](#detailedmeshspacingoption)) and `SpacingFactor` — the same sub-object the
detailed per-edge mesh of [`ElementPlanar`](#elementplanar) uses.

> **A poly line is not a planar contour.** `GeomPtsList` follows the rules of the interactive *Polyline*
> command, and nothing more:
>
> | Point list | Result |
> |---|---|
> | Fewer than **2 points** | **Refused** — there is no segment at all |
> | A point **coincident with the one before it** | **Dropped**, not refused — Advance Design skips the zero-length segment the same way. The dropped points are reported as a warning |
> | Every point coincident with the previous one | **Refused** — no segment survives |
> | **Aligned** points, any number of them | **Accepted** — they make collinear segments |
> | **Non-planar** | **Accepted** |
> | **Self-intersecting** | **Accepted** |
> | Any number of points | **Accepted** — there is no maximum |
>
> A refused list is rejected **before anything is written**, so the element is not created and an update
> modifies nothing at all. After de-duplication, the geometry that is read back and
> `DetailedMeshStylePerEdge` are both indexed on the segments that really exist.

> **To close a poly line, repeat the first point as the last one.** That is how Advance Design itself
> stores a closed poly line, and it is what you read back: a closed poly line of N distinct vertices comes
> back with N+1 points (the closing one repeated) and N segments.

> **`MeshActive` is nullable on purpose.** A non-nullable `bool` would reach the server as `false`
> whenever the client omits it. Leaving it — or `MeshProperties` itself — **null** means "keep the Advance
> Design default" on create and "leave the current value untouched" on update; `MeshProperties` always
> reads back populated.

> **Changing the geometry resizes the mesh vector.** An update that changes the number of segments resizes
> `DetailedMeshStylePerEdge` first — segments that already existed keep their definition at the same index,
> new ones get the Advance Design defaults, removed ones are dropped — and only then applies the
> definitions sent in the same update. A `DetailedMeshStylePerEdge` shorter or longer than the segment
> count is not an error: the extra entries are ignored, the segments it does not cover keep their current
> values, and a warning is logged.

```csharp
long polyLineId = client.CreateElement(new ElementPolyLine
{
    GeomPtsList = new List<Pt3D>
    {
        new() { X = 0, Y = 0, Z = 0 },
        new() { X = 3, Y = 0, Z = 0 },
        new() { X = 3, Y = 4, Z = 0 },   // 3 points -> 2 segments
    },
    MeshProperties = new PolyLineMeshProperties
    {
        MeshActive = true,
        DetailedMeshStylePerEdge = new List<DetailedMeshOnSegment>
        {
            new() { NumberOfElements = 4, SizeOfElements = 0.25, SpacingOption = DetailedMeshSpacingOption.Beginning, SpacingFactor = 1.5 },
            new() { NumberOfElements = 6, SizeOfElements = 0.50, SpacingOption = DetailedMeshSpacingOption.Uniform },
        }
    }
}).Data.Value;
```

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
| `SystemIDs` | `ICollection<EID>` | — |

#### `FamilySupportPlanar`

| Property | Type | Required |
|---|---|---|
| `GeomPtsList` | `ICollection<Pt3D>` | ✓ |
| `ElementActive` | `bool?` | — |
| `SystemIDs` | `ICollection<EID>` | — |

#### `FamilySupportPunctual`

| Property | Type | Required |
|---|---|---|
| `GeomPt` | `Pt3D` | ✓ |
| `Material` | `EID` | ✓ |
| `Footing` | `FootingDimensions` | — |
| `SuppElement` | `SupportingElementDimensions` | — |
| `Punching` | `PunchingProperties` | — |
| `ElementActive` | `bool?` | — |
| `SystemIDs` | `ICollection<EID>` | — |

> The three support families carry `SystemIDs` (the systems the support belongs to), so every one of the
> 12 support types below inherits it — see [System membership](#system-membership--systemids).

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
| `UserComment` | `string` | — | User comment (max 250 characters) |

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
| `UserComment` | `string` | — | User comment (max 250 characters) |

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
| `UserComment` | `string` | — | User comment (max 250 characters) |

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
| `SystemIDs` | `ICollection<EID>` | — | Systems the mass belongs to — see [System membership](#system-membership--systemids) |

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
        DirectionChoice = DirectionChoiceEN1998_API.X,
        SignChoice      = SignChoice_API.SIGNED_PREPONDERANT_MODE,
        Mode            = 1
    },
    Ductility = new DuctilityConfigEN1998
    {
        DuctilityDetermination = DuctilityDeterminationEN1998_API.Q_IMPOSE,
        DuctilityCoefficient   = 1.0
    }
};
```

#### `LoadCase_SeismicCBN` (key properties)

| Property | Type | Description |
|---|---|---|
| `Direction` | `DirectionSeismicCBN` | Direction parameters (choice, sign, mode) |
| `StructureType` | `StructureTypeSeismicCBN` | Structural system type and SFRS configuration |
| `DirectionVector` | `DirectionVectorSeismicCBN` | Direction vector components |
| `DuctilityFactors` | `DuctilityFactorsCBN` | Ductility factors (Rd, Ro, calibration) |

#### `LoadCase_SeismicIBC` (key properties)

| Property | Type | Description |
|---|---|---|
| `Direction` | `DirectionSeismicIBC` | Direction parameters |
| `StructureType` | `StructureTypeIBC` | Structural system type and SFRS configuration |
| `DirectionVector` | `DirectionVectorIBC` | Direction vector components |
| `DuctilityFactors` | `DuctilityFactorsIBC` | Ductility factors (R, Cd, calibration) |

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
| `IdComb` | `int` | — | User combination ID (read-only) |
| `ECombinationType` | `ECombinationType` | ✓ | Combination type (ULS, SLS, —) |
| `ListCasesCoeffs` | `ICollection<EIDDoublePair>` | — | Load case EIDs with their coefficients |
| `DesignCalculations` | `ICollection<ECombinationDesignCalculation>` | — | The design (Expert) calculations this combination takes part in. Replaces the `ExpertSelection` bit mask of the AD model. See [3.6.1](#361-design-expert-calculations-of-a-combination) for the Create/Update semantics |
| `ConcreteDesign` | `ConcreteCombinationDesign` | — | Properties the reinforced concrete engine applies |
| `SteelDesign` | `SteelCombinationDesign` | — | Properties the steelwork engine applies |
| `TimberDesign` | `TimberCombinationDesign` | — | Properties the timber engine applies |
| `CompositeDesign` | `CompositeCombinationDesign` | — | Properties the composite beam engine applies |

Every property of every group below is optional: one left `null` keeps the value already stored, or
takes the default the Advance Design combinations dialog would apply.

#### `ConcreteCombinationDesign`

Maps to `som.BetonFields`. Relevant when `ConcreteDesign` is listed.

| Property | Type | Description |
|---|---|---|
| `Type` | `EConcreteCombinationType?` | Limit state. Default: derived from the project situation — ULS→`Uls`, SLS→`Sls`, accidental→`UlsAccidental` |
| `Duration` | `EConcreteLoadDuration?` | Load duration. Default: `ShortTerm` for an accidental combination, `LongTerm` otherwise |

#### `SteelCombinationDesign`

| Property | Type | Design calculation |
|---|---|---|
| `Profiles` | `CombinationProfileVerification` | `SteelProfiles` |
| `Deflections` | `CombinationDeflectionVerification` | `SteelDeflections` |
| `Connections` | `CombinationConnectionVerification` | `SteelConnections` |
| `Fire` | `CombinationFireVerification` | `SteelFire` |

#### `TimberCombinationDesign`

| Property | Type | Design calculation |
|---|---|---|
| `Deflections` | `CombinationTimberDeflectionVerification` | `TimberDeflections` |
| `Profiles` | `CombinationTimberVerification` | `TimberProfiles` |
| `Fire` | `CombinationTimberVerification` | `TimberFire` |

> Advance Design stores the three timber groups in a **single** object (`som.TimberFields`), so writing
> any one of them creates the other two with zeroed values. Their existence therefore says nothing
> about what is configured, and on read a timber group is returned **only when its design calculation
> is listed**. Steel and composite groups each own their sub-object and do not behave this way.

#### `CompositeCombinationDesign`

A composite beam is verified twice: at the construction stage, when the steel section acts alone
("pre-composite"), and at the final stage, when the composite section acts ("post-composite").

| Property | Type | Design calculation |
|---|---|---|
| `PreCompositeProfiles` | `CombinationProfileVerification` | `PreCompositeProfiles` |
| `PostCompositeProfiles` | `CombinationProfileVerification` | `PostCompositeProfiles` |
| `PreCompositeDeflections` | `CombinationDeflectionVerification` | `PreCompositeDeflections` |
| `PostCompositeDeflections` | `CombinationDeflectionVerification` | `PostCompositeDeflections` |

#### Verification groups

`CombinationProfileVerification` — maps to `som.MetalProfileFields`, shared by steel and both
composite stages:

| Property | Type | Description |
|---|---|---|
| `State` | `EDesignVerificationState?` | Whether the verification is evaluated for this combination. Default `Active` |
| `ResistanceSafetyFactor` | `double?` | γM0. Default: model property `ResistanceSecurityCoeff` (steel) / γM0 of the composite settings |
| `StabilitySafetyFactor` | `double?` | γM1. Default: model property `StabilitySecurityCoeff` (steel) / γM1 of the composite settings |

`CombinationDeflectionVerification` — maps to `som.MetalDeflectionFields`, shared by steel and both
composite stages:

| Property | Type | Description |
|---|---|---|
| `Criterion` | `EDeflectionCriterion?` | Deflection criterion. Default `FirstCriterion`, except under the 2nd generation Eurocode where an SLS **characteristic** combination defaults to `BothCriteria` (Wmax & W3) |
| `SafetyFactor` | `double?` | Default: model property `DeflectionsSecurityCoeff` (steel) / γM2 of the composite settings |

`CombinationConnectionVerification` — maps to `som.MetalConnectionFields`:

| Property | Type | Description |
|---|---|---|
| `State` | `EDesignVerificationState?` | Default `Active` |
| `SafetyFactor` | `double?` | Default: model property `ConnectionsSecurityCoeff` |

`CombinationFireVerification` — maps to `som.MetalFireFields`:

| Property | Type | Description |
|---|---|---|
| `State` | `EDesignVerificationState?` | Default `Active` |
| `SafetyFactor` | `double?` | γMfi. Default: model property `FireSecurityCoeff` |
| `LoadLevel` | `double?` | Loading level of the fire situation. Default `1.0` |

`CombinationTimberVerification` (profiles, fire) and `CombinationTimberDeflectionVerification`
(deflections) — map to `som.TimberFields`:

| Property | Type | Description |
|---|---|---|
| `Type` | `ETimberCombinationType?` | Limit state. Default `Uls` |
| `Duration` | `ETimberLoadDuration?` | Load-duration class, drives kmod and kdef. Default `Permanent` |
| `Criterion` | `ETimberDeflectionCriterion?` | Deflection group only. Default `FirstCriterion` |

---

#### `CombinationsGenerationRequest`

Input of `GenerateCombinations` — see [3.6.2](#362-automatic-generation-of-combinations). Deliberately
narrow: it selects the work-flow, it does not parameterize the standard.

| Property | Type | Required | Description |
|---|---|---|---|
| `Mode` | `ECombinationsGenerationMode` | — | Which work-flow to run. Default `Simplified` |
| `UpdateLoadCaseCodes` | `bool?` | — | Refresh the normative code of every load case from the combination template before generating. **Unset means `true`** — leave it unset for a model built through the API, or the generation matches no template and returns nothing. Send `false` only for a project file whose codes must not be touched |
| `ApplyDefaultConcomitanceRules` | `bool` | — | `DetailedMatrix` only: re-initialise the concomitance matrix from the default rules before generating. Default `false`, which uses the rules set stored in the model |

**`ECombinationsGenerationMode` values:**

| Value | Work-flow | Desktop equivalent |
|---|---|---|
| `Simplified` | Template based: the `.cbt` of the model's combination standard, together with the coefficients on the load cases and on the model settings | *Simplified Comb.* |
| `DetailedMatrix` | Generated from the concomitance rules set stored in the model | *Detailed Comb.* |

> `UpdateLoadCaseCodes` is nullable on purpose. A generated client mirrors the property *type*, not a
> C# initializer, so a non-nullable `bool` would reach the server as an explicit `false` whenever the
> caller did not set it — making the useful default unreachable. Omit the property and the server
> applies `true`.

#### `CombinationsGenerationResult`

| Property | Type | Description |
|---|---|---|
| `CreatedCombinations` | `ICollection<EID>` | Internal identifiers of the combinations that were created, in generation order. Read them back with `GetInformationalElementsObject`. Empty when nothing was generated — the reason is then in `Details.Diagnostics` |
| `FirstIdentifier` | `int` | User visible number ("No.") of the first generated combination, 0 when nothing was generated. Same value as `Combination.IdComb`; this pair is here so that reporting the range costs no extra read |
| `LastIdentifier` | `int` | User visible number of the last generated combination |
| `LimitExceeded` | `bool` | `true` when the generator produced more combinations than `ModelProperties.MaxNumberMatrixGeneratedCombinations` and the set was trimmed to it. `CreatedCombinations` then holds what was kept; the rest do not exist |

The set is always trimmed to the model limit rather than discarded, and the limit itself is not part
of this API — `LimitExceeded` is how a caller learns that trimming happened.


### 10.4 Structural Systems

#### `StructuralSystem`

A **system** is the Advance Design container that groups structural elements; it can also represent a
building level/storey. The model is a tree of systems whose root is the system displayed as
**"Structure 0"** and which exists in every model. Systems are created, read, updated and deleted
through the informational-element endpoints (`$type` = `"StructuralSystem"`):

| Operation | Endpoint |
|---|---|
| Create | `POST /api/Model/elements/CreateInformationalElement` |
| Read | `POST /api/Model/elements/GetInformationalElementsObject` |
| Update | `POST /api/Model/elements/UpdateInformationalElement` |
| Delete | `POST /api/Model/elements/DeleteElements` |
| List all | `POST /api/Model/elements/GetElementsID` with `QueryInfoModel { InformationalElementType = StructuralSystem }` |

Inherited from `InformationalElementBase`: `UserName` is the **system name** (set it on create) and
`UserID` is the **system number** shown in the UI, assigned by Advance Design (read-only; the root
system is the only one whose `UserID` is `0`).

| Property | Type | Required | Description |
|---|---|---|---|
| `ParentID` | `EID` | — | Existing system this one is nested under; `0` (default) attaches it directly under the model root. **Create-time only** — an update never re-parents a system. |
| `BehaviourCoefficientX/Y/Z` | `double` | — | Seismic behaviour factors, default `1`. Must be **strictly positive**. Inherited properties: changing them on a system cascades to its sub-systems. |
| `ImposedDirectionX/Y/Z` | `bool` | — | Behaviour coefficient imposed per direction |
| `IsLevel` | `bool` | — | Marks the system as a building level; then `LevelTop`/`LevelBottom`/`LevelNumber` apply |
| `LevelTop` | `double` | — | Top elevation (m). When `IsLevel` is true it must be **≥ `LevelBottom`** |
| `LevelBottom` | `double` | — | Bottom elevation (m) |
| `LevelNumber` | `int` | — | Level number, default `1` |
| `Color` | `int` | — | Packed RGB colour |
| `IsWallGroup` | `bool` | — | Marks the system as a wall group |
| `SubSystemIDs` | `ICollection<EID>` | — | **Read-only**, filled on read: the EIDs of the direct sub-systems. Build a hierarchy with `ParentID` on create, never through this list. |

**Rules**

- **Validation (create and update):** the behaviour coefficients must be `> 0` and, for a level,
  `LevelTop >= LevelBottom`. An invalid system is rejected without touching the model.
- **Root system ("Structure 0"):** only the behaviour coefficients and `IsWallGroup` may be changed on
  it; any other difference (name, level flags/elevations, colour, imposed directions) is rejected. Every
  other system — including a top-level one, which also reports `ParentID = 0` — is fully editable.
- **Update is a full-state operation:** every exposed property is compared with the model and the
  differences are written, so a property left at its DTO default (`Color = 0`, `LevelNumber = 1`, …)
  counts as a change. Read the system first and re-send its current values with only your change applied.
- **The root system cannot be deleted** — every element falls back to it, so a delete request on it is
  refused.
- **Delete** removes the system **and its whole sub-tree of sub-systems**. The structural elements that
  were inside are **not** deleted — they only lose that membership; those left without any system are
  re-assigned to the root system "Structure 0" (with a warning), since an element must always belong to
  at least one system.
- **Listing** returns every system of the model — the root plus all sub-systems at any depth — including
  systems that contain nothing at all. Design groups share the same native class but are **not** returned.

```csharp
// Level "Floor 1" under the model root, with a nested sub-system
var level = client.CreateInformationalElement(new StructuralSystem
{
    UserName = "Floor 1",
    IsLevel = true,
    LevelTop = 6.0,
    LevelBottom = 3.0,
    LevelNumber = 2
});

var zone = client.CreateInformationalElement(new StructuralSystem
{
    UserName = "Zone A",
    ParentID = level.Data
});

// Every system in the model (empty ones included)
var systemIds = client.GetElementsID(new List<QueryBase>
{
    new QueryInfoModel { InformationalElementType = InformationalElementTypeEnum.StructuralSystem }
});
```

---

### 10.5 Linear Release Templates

#### `LinearReleaseTemplate`

A **linear release template** describes how an edge of a planar element is connected to the rest of the
structure. Templates live in a single **model-level pool**: they are defined once and then referenced by
the edges of any number of planar elements through
[`ElementPlanar.SelectedReleaseName` and `ElementPlanar.LinearReleases`](#edge-releases--selectedreleasename-and-linearreleases)
— so editing a template changes **every** edge that uses it. Templates are created, read, updated and
deleted through the informational-element endpoints (`$type` = `"LinearReleaseTemplate"`):

| Operation | Endpoint |
|---|---|
| Create | `POST /api/Model/elements/CreateInformationalElement` |
| Read | `POST /api/Model/elements/GetInformationalElementsObject` |
| Update | `POST /api/Model/elements/UpdateInformationalElement` |
| Delete | `POST /api/Model/elements/DeleteElements` |
| List all | `POST /api/Model/elements/GetElementsID` with `QueryInfoModel { InformationalElementType = LinearReleaseTemplate }` |

The template name is `LinearReleaseName` and it must be **unique in the model**; the base-class member
`InformationalElementBase.UserName` is **not** used by this type. Available for the descriptive model only.

| Property | Type | Required | Description |
|---|---|---|---|
| `LinearReleaseName` | `string` | ✓ | Template name, unique in the model, max **50** characters. Required on create **and** on update. This is the value referenced by `ElementPlanar.SelectedReleaseName`. |
| `LinearReleaseType` | `LinearReleaseType` | — | **Output only:** `default_release` for one of the three built-ins, `user_defined_release` for anything created through the API. |
| `Tx` | `bool` | — | Release of the translation along the local **x** axis of the edge. `true` = released (free), `false` = fixed. Default `false`. |
| `StiffnessTx` | `double` | — | Elastic stiffness of the `Tx` release (used when `Tx` is `true`). Unit **kN/m/m**; `0` = perfectly free. Default `0`. |
| `TxNonlinearityType` | `LinearReleaseNonlinearityType` | — | Non-linear behaviour of the `Tx` release. Default `none`. |
| `Ty` | `bool` | — | Release of the translation along the local **y** axis. Default `false`. |
| `StiffnessTy` | `double` | — | Elastic stiffness of the `Ty` release. Unit **kN/m/m**; `0` = perfectly free. Default `0`. |
| `TyNonlinearityType` | `LinearReleaseNonlinearityType` | — | Non-linear behaviour of the `Ty` release. Default `none`. |
| `Tz` | `bool` | — | Release of the translation along the local **z** axis. Default `false`. |
| `StiffnessTz` | `double` | — | Elastic stiffness of the `Tz` release. Unit **kN/m/m**; `0` = perfectly free. Default `0`. |
| `TzNonlinearityType` | `LinearReleaseNonlinearityType` | — | Non-linear behaviour of the `Tz` release. Default `none`. |
| `Rx` | `bool` | — | Release of the rotation **about the edge axis**. Default `false`. |
| `StiffnessRx` | `double` | — | Elastic stiffness of the `Rx` release. Unit **kNm/rad/m**; `0` = perfectly free. Default `0`. |
| `RxNonlinearityType` | `LinearReleaseNonlinearityType` | — | Non-linear behaviour of the `Rx` release. Default `none`. |

> **Four degrees of freedom only.** A linear release exposes `Tx`, `Ty`, `Tz` and `Rx` — there is **no
> `Ry` nor `Rz`**. `Rx` is the rotation about the edge axis, the only rotational release of an edge.

**Built-in templates**

Every model always contains three built-in templates, at fixed positions at the head of the pool, in this
order:

| # | Template | `Tx` | `Ty` | `Tz` | `Rx` | Usage |
|---|---|---|---|---|---|---|
| 1 | "Free" | released | released | released | released | Edge fully disconnected |
| 2 | "Pinned" | fixed | fixed | fixed | released | Hinged edge |
| 3 | "Fixed" | fixed | fixed | fixed | fixed | **Default for a new planar element** |

> ⚠️ Their names are **localized**. Never hard-code a name: list the pool and identify the built-ins by
> their released degrees of freedom (or by their fixed position in the returned list). The three
> built-ins **cannot be modified nor deleted** through the API — both attempts fail with an error.

**Rules**

- **Create:** `LinearReleaseName` is mandatory and must not already exist in the model. Creating a
  template with an existing name **fails** — it is never silently turned into an update. A template
  created through the API is always `user_defined_release`.
- **Update:** `LinearReleaseName` is mandatory here too. **Renaming** a template also updates the
  `SelectedReleaseName` of every planar element that used the old name, so the elements keep pointing at
  the template. Updating a built-in template is refused.
- **Delete:** deleting a template that is **in use succeeds** — and the reset is **per edge**: only the
  edges that actually referenced the deleted template fall back to the built-in **"Fixed"** template,
  every other edge keeps its own. A warning reports how many elements were affected, and
  `SelectedReleaseName` is re-derived on each of them (it becomes "Fixed" if the reset made all the edges
  uniform again, and stays "Other" if they still differ). Elements that did not use the template are not
  touched at all. Deleting a built-in template is **refused**; in a batch that mixes a built-in with
  user templates the built-in is refused while the others are still deleted, and the call reports failure
  for the batch.
- **Listing** returns the pool in order: "Free", "Pinned", "Fixed", then the user-defined templates.

```csharp
// 1. List the pool and identify the built-ins by their degrees of freedom (the names are localized)
var poolIds = client.GetElementsID(new List<QueryBase>
{
    new QueryInfoModel { InformationalElementType = InformationalElementTypeEnum.LinearReleaseTemplate }
}).Data;

var pool = client.GetInformationalElementsObject(poolIds.ToList())
                 .Data.OfType<LinearReleaseTemplate>().ToList();

LinearReleaseTemplate free   = pool.First(r =>  r.Tx &&  r.Ty &&  r.Tz &&  r.Rx);
LinearReleaseTemplate pinned = pool.First(r => !r.Tx && !r.Ty && !r.Tz &&  r.Rx);
LinearReleaseTemplate fixedT = pool.First(r => !r.Tx && !r.Ty && !r.Tz && !r.Rx);

// The three built-ins are always the first three ids of the pool, in this order
long freeId = poolIds.ElementAt(0), pinnedId = poolIds.ElementAt(1), fixedId = poolIds.ElementAt(2);
```

```csharp
// 2. Create a user-defined template: hinged edge with an elastic, compression-only vertical spring
EIDApiResponse created = client.CreateInformationalElement(new LinearReleaseTemplate
{
    LinearReleaseName = "Elastic bearing",
    Tx = false,                                     // fixed
    Ty = false,                                     // fixed
    Tz = true,  StiffnessTz = 15000.0,              // released, elastic — kN/m/m
    TzNonlinearityType = LinearReleaseNonlinearityType.Compression_only,
    Rx = true,  StiffnessRx = 0.0,                  // released — kNm/rad/m, 0 = perfectly free
    RxNonlinearityType = LinearReleaseNonlinearityType.None
});
long bearingId = created.Data.Value;
```

```csharp
// 3. Apply one template to EVERY edge of a slab (the "Releases / Side" property)
var slab = new ElementPlanar
{
    GeomPtsList = new List<Pt3D>
    {
        new Pt3D { X = 0, Y = 0, Z = 0 }, new Pt3D { X = 5, Y = 0, Z = 0 },
        new Pt3D { X = 5, Y = 4, Z = 0 }, new Pt3D { X = 0, Y = 4, Z = 0 }
    },
    Material             = new EID { Value = materialId },
    ElementType          = PlanarElementType.shell,
    ThicknessIn1stVertex = 0.20,
    SelectedReleaseName  = pinned.LinearReleaseName   // localized name read from the pool
};
long slabId = client.CreateElement(slab).Data.Value;
```

```csharp
// 4. Assign a different template to a SINGLE edge (the "Releases / Definition" dialog)
var stored = (ElementPlanar)client.GetElementsObject(new List<long> { slabId }).Data.First();

var perEdge = stored.LinearReleases.ToList();   // one entry per edge, in edge order
perEdge[0] = new EID { Value = bearingId };     // edge GeomPtsList[0] -> GeomPtsList[1]

BooleanApiResponse upd = client.UpdateElement(slabId, new ElementPlanar
{
    GeomPtsList          = stored.GeomPtsList,
    Material             = stored.Material,
    ElementType          = stored.ElementType,
    ThicknessIn1stVertex = stored.ThicknessIn1stVertex,
    LinearReleases       = perEdge,             // must hold exactly one entry per point
    SelectedReleaseName  = null                 // leave it null, otherwise it overrides LinearReleases
});
```

> The per-edge list is validated as a whole: a wrong number of entries or a single unknown EID rejects
> the **complete** list, logs an error and leaves every edge unchanged.

---

### 10.6 Expert Design Templates

An **Expert design template** is a named set of design (Expert) properties defined once at model level and
then referenced by structural elements — the "Design template description" dialog of Advance Design
(`som.Role` in the model). Assigning a template, or editing it afterwards, **copies** its properties onto
the expert properties of every element that references it.

Templates exist per **material family** × **element type**, and within that per **design code**; the API
exposes the **steel × linear** set, in the three shapes the steel design codes of Advance Design fall
into — one Eurocode leaf and two North-American ones. The class hierarchy is built so the other material
families slot in without changing the contract:

```
InformationalElementBase
└── DesignTemplate                              MaterialType, ElementType, AssignmentNumber
    └── LinearDesignTemplate                    (element type anchor)
        └── SteelLinearDesignTemplate           DesignCode
            ├── EC3SteelLinearDesignTemplate            ← the Eurocode properties
            └── NorthAmericanSteelLinearDesignTemplate  ← the groups AISC and CAN/CSA share
                ├── AISCSteelLinearDesignTemplate       ← + the AISC section class
                └── CSASteelLinearDesignTemplate        ← + the Class 1-4 section class and
                                                          the effective Class 4 characteristics
```

`NorthAmericanSteelLinearDesignTemplate` is an **intermediate base, not a creatable type**: a template
always belongs to one code, so Create and Update require one of the two leaves under it. The two codes
share a base because their two SOM objects differ by exactly two things — the meaning of the cross-section
class, and the Class 4 effective characteristics, which only CAN/CSA carries.

#### Which leaf for which design code

The design code is a **model-wide** setting, not a per-template one: Advance Design allocates the expert
property set of the code the model is configured with, and only that one.

| Steel design code of the model | `$type` to send | Section class typed with |
|---|---|---|
| `eurocode`, `ntc2008`, `ntc2018` | `EC3SteelLinearDesignTemplate` | `SteelSectionClass` |
| `aisc`, `aisc_2016` | `AISCSteelLinearDesignTemplate` | `SteelSectionClassAISC` |
| `can_csa_s16_2009`, `can_csa_s16_2014`, `can_csa_s16_2019`, `can_csa_s16_2024` | `CSASteelLinearDesignTemplate` | `SteelSectionClass` |

> ⚠️ **Sending a leaf that does not match the active code is refused**, on create and on update alike, and
> the diagnostic names the leaf the model *does* accept. The engine would never allocate the other code's
> property set, so there is nothing to write it into.

> ⚠️ **A model configured for a 2nd generation Eurocode is refused outright**, on any installation where
> the 2nd generation Eurocodes are not enabled — there is no leaf to send it, and the standard is not
> released. See [Section 13](#design-standards-and-national-annexes).

**Both AISC editions share one class, and all four CAN/CSA editions share one class.** They share one SOM
object: Advance Design normalizes them to a single "AISC" and a single "CSA" expert set, and neither the
model nor the property list distinguishes the editions.

| Operation | Endpoint |
|---|---|
| Create | `POST /api/Model/elements/CreateInformationalElement` with `$type` = one of the three leaves above |
| Read | `POST /api/Model/elements/GetInformationalElementsObject` |
| Update | `POST /api/Model/elements/UpdateInformationalElement` |
| Delete | `POST /api/Model/elements/DeleteElements` |
| List all | `POST /api/Model/elements/GetElementsID` with `QueryInfoModel { InformationalElementType = DesignTemplate }` |
| Assign to elements | `ElementLinear.DesignTemplateId`. (`POST /api/Model/elements/AssignDesignTemplateToElements` does the same for a whole selection, but needs the build switch above.) |

> The template **name is `UserName`** (inherited from `InformationalElementBase`). It is mandatory on
> create and on update, must be **unique** in the model and at most **50** characters; leading and trailing
> spaces are trimmed. A name that already exists makes the call fail — it is never silently turned into an
> update.

#### Header properties

| Property | Type | Description |
|---|---|---|
| `UserName` | `string` | Template name — unique, max 50 characters, required |
| `MaterialType` | `DesignTemplateMaterialType` | **Output only:** `steel` for this type |
| `ElementType` | `DesignTemplateElementType` | **Output only:** `linear` for this type |
| `AssignmentNumber` | `int` | **Output only:** how many elements currently reference the template |
| `DesignCode` | `SteelDesignCode` | **Output only:** the design code the property set belongs to |

#### The Eurocode leaf — general properties

| Property | Type | Description |
|---|---|---|
| `SectionClass` | `SteelSectionClass` | Imposed section class, or `automatic` |
| `ManufacturingQuality` | `SteelManufacturingQuality` | Manufacturing quality class |
| `ExecutionClassType` / `ExecutionClassValue` | `SteelExecutionClassType` / `SteelExecutionClass` | Global or imposed execution class (EXC1–EXC4) |
| `FamilyOfElement` | `SteelFamilyOfElement` | Family of the element used by the fire and imperfection rules |
| `ImposedValues` | `bool` | Impose the class-4 effective characteristics below instead of computing them |
| `Sxeff`, `ENy`, `ENz`, `Weff1y`, `Weff2y`, `Weff1z`, `Weff2z` | `double` | Class-4 effective area, shifts of the neutral axis and effective section moduli |

#### The Eurocode leaf — property groups

Each group is an **optional sub-object**. A group left `null` is **skipped**: it keeps whatever the model
already holds, which is what makes both create and update partial operations.

| Property | Type | Contents |
|---|---|---|
| `Deflection` | `SteelLinearDeflection` | Element and super-element deflection checks: `AllowableDeflection1/2` (the 1/n limits), reference-length option and value, verification location |
| `Buckling` | `SteelLinearBuckling` | `BucklingLength`: `Lfy`/`Lfz`, the μ and system-length modes and values per axis, the buckling-length coefficients, the **fire-situation** lengths, the two **nullable** braced/unbraced node types, `Cmy0`/`Cmz0`, the buckling curves in both planes, and the torsional buckling length. Its `TorsionalBucklingVerification` flag is a **CAN/CSA-only row** that must be left `true` on this leaf — see the rules below |
| `LateralTorsionalBuckling` | `EC3SteelLinearLateralTorsionalBuckling` | `Lds`/`Ldi` (upper/lower flange lengths), the LTB curve, and the full parameter set of **each flange**: end and intermediate restraints, the restraint definition string, k/kw, C1/C2/C3, load application point and type, the f and fM reduction coefficients, the reduction-factor method (`CurveMethod` — read its note in [Section 13](#13-enumerations-reference) before using it on a 2nd-generation model), and how the elastic critical moment and its reference value are obtained (`McrMode`/`McrValue` and `Mcr0Mode`/`Mcr0Value`, each with its **own** enumeration) |
| `GlobalSwayImperfections` | `SteelGlobalSwayImperfections` | Column imperfections (φy, φz, φ0, h, m, local/global bow flags, super-element intersection detection, and the 2nd-generation-only `Phi0AutoY`/`Phi0AutoZ`) and beam imperfections (weak-axis direction, k factor, force-direction pivot). `E0Ly`/`E0Lz` are present but **inert** — see the note below the table |

> ⚠️ **`GlobalSwayImperfections.E0Ly` / `E0Lz` round-trip but influence nothing.** e0/L is a *result* in
> Advance Design, not an input: the steel design run computes it and stores it on the ELEMENT under the
> different names `e0LColumnY` / `e0LColumnZ` / `e0LBeam`, from where the report reads it back. No dialog,
> accessor or solver reads the design-template field, and because the element uses other names the
> template-to-element copy cannot carry it either. The two properties are kept so a GET-then-PUT round trip
> preserves what a model already holds.
| `ColdFormedDesign` | `EC3SteelColdFormedDesign` | Cold-formed options: `IgnoreFyMz`, `ExactClass4Calculation`, `BucklingResistance` — which does have an effect, it forces the buckling verification on — and `DesignInMainAxesUuVv`, which is **read-only in practice**: `true` is refused, see the rules below |
| `FireVerification` | `EC3SteelFireVerification` | Fire resistance (minutes), exposed faces, emissivity, and the `FireProtection` sub-object (mode, thickness, density, conductivity, specific heat) |
| `AngleVerification` | `SteelAngleVerification` | Angle-section checks: leg, bolt and bolt-hole diameters, number of bolts, net areas and tension flags at each end, design in principal axes, and the effective slenderness ratio with its own K factor (`EffectiveSlendernessKMode`/`EffectiveSlendernessKValue`) |
| `WebBuckling` | `EC3SteelWebBuckling` | Transverse stiffeners (sides, spacing, the **list** of stiffener locations) and longitudinal stiffeners (number, thickness, width, relative positions) |
| `NonLinearFEM` | `SteelNonLinearFEM` | 2nd-order / Advance stability analysis: `RunNonLinearFEM`, iteration count, the annex C.2 flag, and `Parameters` — the **lists** of punctual supports and distributed springs, the initial deformation with its **list** of segments, and the loads offset |

> **Lists are replaced, not merged.** The four collections (`PunctualSupports`, `DistributedSprings`,
> `InitialDeformation.Segments`, `TransverseStiffeners.StiffenerLocations`) are rebuilt from the payload on
> every update, so sending two entries where three existed leaves exactly two.

> **The braced/unbraced node types have no fixed default — theirs follows the design code.**
> `Buckling.BucklingLength.YNodesType` and `ZNodesType` are the only two **nullable** properties of that
> group, and the schema deliberately gives them no default: `CRoleManager::CreateLinearMetalExpert` stamps
> `braced` on a new AISC or CAN/CSA template and `unbraced` on a Eurocode one, so a single documented
> default would have been wrong for one of the two families. Leaving either **null** — simply not sending
> it — means *keep what the template already holds*, which on a fresh template is the value the model's
> steel design code dictates; **only a value you actually send is written**. Both always **read back
> populated**, so a read-then-update round-trip preserves them. On this leaf the effective value of a newly
> created template is still `unbraced`; what changed is that an update sending the `Buckling` group without
> naming them no longer resets them.

#### The rules of the design-template dialogs

The rules below are written for the Eurocode leaf. Those that concern a group the North-American leaves
also carry — `Buckling`, `NonLinearFEM`, the per-flange lengths — hold there too; those that concern a
Eurocode-only group (the imperfections, the web stiffeners, the restraint-definition string) simply have
no North-American counterpart. The rules that are specific to the two North-American leaves are listed
[further down](#the-rules-specific-to-the-north-american-leaves).

Several groups are not edited in the AD property list but in a modal dialog, and those dialogs do more
than store what the user typed: they refuse some input, and they derive some stored fields from others.
The API applies the same rules, so a template built through it can never hold a state the user interface
could not produce. Concretely:

**Input that is refused** (the whole update fails and *nothing* is written):

| Rule | Why |
|---|---|
| `Buckling.BucklingLength.TorsionalBucklingVerification` set to `false` | **CAN/CSA-only row.** Advance Design hides it under every other steel code, the Eurocode included, and those design engines ignore the field — a hidden row cannot be edited in the desktop, so the value would describe a state no user could produce. Only a `CSASteelLinearDesignTemplate` may set it to `false`, and the diagnostic names both the property and that leaf |
| `ColdFormedDesign.DesignInMainAxesUuVv` set to `true` | **Read-only in practice.** Advance Design withdrew this row from the design template and from the linear element alike — it is hidden unconditionally in both property lists — so a template can only ever hold `false`, which is what the engine stamps when it creates one. And a `true` would do harm rather than nothing: the option survives as the *slave* of the element's own "Product of inertia Iyz = 0" row, and assigning a template copies it onto the element **and** overwrites that row with its inverse, so it would silently clear `ProductOfInertiaIyz0` on every element using the template — a state no dialog can display or undo. Set the element property instead |
| A flange whose `LengthMode` is `imposed` must have `LengthValue > 0` | The LTB dialog refuses to close on a null imposed length |
| `RestraintDefinitionValues` is limited to 50 characters | The SOM field is `character[50]` |
| Two `NonLinearFEM` punctual supports must not share an `Abscissa` | The stability dialog keys them by abscissa, so colliding entries silently merge |
| Two `NonLinearFEM` distributed springs must not share the same `AbscissaStart`/`AbscissaEnd` span | Same, keyed by span |
| A transverse stiffener's `RelativeValue` must be strictly between 0 and 1 | The stiffener manager refuses a position outside the beam, and the extremities are not stiffener positions |

**Fields that are derived** (what you read back is computed, not what you sent):

- **`Buckling.BucklingLength.Lfy` / `Lfz` follow the mode.** `Lfy` is the value the property list shows and
  the design engine reads; `LfyMode`/`LfyValue` are what the dialog stores. After an update,
  `Lfy = LfyValue` when the mode carries a length (`imposed`, or either of the two LTB-length modes) and
  `0` otherwise. `Lfz` is the same but only `imposed` carries a length — the z axis has no LTB variant.
  Two ways to set it, both supported: send a bare `Lfy` and leave `LfyMode` at its default, which switches
  the mode to `imposed` exactly as typing the value in the property list does; or name a mode and set
  `LfyValue`, which is the dialog idiom. **If you do both and they contradict, the mode wins.**
- **`GlobalSwayImperfections.ColumnImperfections.PhiY` / `PhiZ` are computed** from `Phi0`, `HValue` and
  `MValue`, with the formula of the imperfections dialog:
  `alphaH = clamp(2/sqrt(h), 2/3, 1)`, `alphaM = sqrt(0.5 * (1 + 1/m))`, `phi = alphaH * alphaM / phi0`.
  The value you send for `Phi` is kept only when the formula cannot be evaluated (`Phi0`, `HValue` or
  `MValue` not strictly positive). Note that **changing `GlobalBowImperfectionsY`/`Z` clears the `Phi` of
  that direction**, exactly as ticking the box does in the property list — an update that leaves the flag
  as it is does not touch `Phi`.
- **`UpperFlangeRestraintSummary` / `LowerFlangeRestraintSummary` are output only.** Advance Design
  rebuilds these two strings from the restraints of the corresponding flange parameters, so a value sent
  is ignored. They are localized — read the restraints themselves rather than parsing the summary.
- **Transverse stiffeners are stored sorted by `RelativeValue`, and their `AbscissaValue` is always 0.**
  A design template has no geometry, so the stiffener manager cannot convert a relative position into an
  abscissa and refuses one typed by the user — `RelativeValue` is a template's only stiffener coordinate.
  Whatever order you send the list in, it comes back sorted.
- **`Lds` / `Ldi` and the per-flange `LengthValue` are one physical quantity, stored twice**, and Advance
  Design derives the first from the second. There are two equivalent ways to set it, and both work: the
  **property-list idiom**, where you send the flange length alone and the flange's `LengthMode` switches to
  `imposed` with it, exactly as typing the value in the property list does; and the **dialog idiom**, where
  you send the flange's own `LengthMode` and `LengthValue` and leave the flange length at `0`. The `0`
  default is never taken literally — under the Eurocode the derived length is the flange's `LengthValue`
  when its mode is `imposed`, and `0` otherwise. Consequently **an update that sends the
  `LateralTorsionalBucklingLength` group without the flange-parameters sub-object keeps the stored
  length**: patching the LTB curve alone cannot wipe `Lds`. Do note that *sending* a flange-parameters
  sub-object does reset the flange properties you leave unset, as sending any group does.
- **Enabling `NonLinearFEM.RunNonLinearFEM` disables the buckling and lateral-torsional-buckling checks**
  (`Buckling.ElementVerification`, `LateralTorsionalBuckling.ElementVerification` become `false`), exactly
  as in the user interface — the 2nd-order analysis replaces them.

> **`ColdFormedDesign.DesignInMainAxesUuVv` is there to be read, and its user-facing equivalent lives on
> the element.** It is the same option inverted as the linear element's *Product of inertia Iyz = 0* row —
> `InertiaProperties.ProductOfInertiaIyz0` of `LinearElementInnertia`, see
> [Section 7](#7-data-models--elements-geometric) — and that is the one to set. The template field is kept
> in the contract only so that a GET reports what is stored and a GET-then-PUT round trip keeps working;
> `true` is refused, per the rule above.

> **`ColdFormedDesign.BucklingResistance` does have an effect — this document used to say it did not.**
> The AD model schema marks the field as unused and the previous revision of this reference repeated that,
> but the comment is stale: the conversion to the analysis model still reads the flag, and when it is
> `true` it writes `BucklingChecking` = `true` on the element's buckling group, forcing the buckling
> verification on. Setting it therefore works, but indirectly — prefer `Buckling.ElementVerification`,
> which is the direct way to turn that check on.

#### The North-American leaves — general properties

Compared with the Eurocode leaf, the two North-American leaves have **no manufacturing quality, no
execution class, no family of element, no global sway imperfections, no fire verification and no
web-buckling group** — Advance Design does not allocate those sub-objects outside the Eurocode. What they
carry instead is the `SteelSpecificProperties` group below.

| Property | On which leaf | Type | Description |
|---|---|---|---|
| `SectionClass` | `AISCSteelLinearDesignTemplate` | `SteelSectionClassAISC` | Imposed cross-section class from the **AISC** list — `compact`, `noncompact`, `slender_element` — or `auto_class`. The same stored integer as the other two leaves, read against a different list |
| `SectionClass` | `CSASteelLinearDesignTemplate` | `SteelSectionClass` | CAN/CSA S16 keeps the **Class 1 to Class 4** list, so this leaf shares the Eurocode enumeration. Only `class_4` makes the characteristics below meaningful |
| `ImposedValues` | `CSASteelLinearDesignTemplate` | `bool` | Impose the Class 4 effective characteristics below instead of computing them |
| `Sxeff`, `ENy`, `ENz`, `Weff1y`, `Weff2y`, `Weff1z`, `Weff2z` | `CSASteelLinearDesignTemplate` | `double` | Class-4 effective area (m²), shifts of the centroid of the effective area along y and z (m), and effective section moduli (m³) |

> **AISC has no imposed effective characteristics.** It classifies a section as compact, noncompact or
> slender-element, and there is nothing for the class to enable — which is exactly why the two codes need
> two leaves rather than one.

#### The North-American leaves — property groups

The seven groups below are carried by `NorthAmericanSteelLinearDesignTemplate` and are therefore identical
on both leaves. As on the Eurocode leaf, each one is an **optional sub-object**: a group left `null` is
skipped and keeps whatever the model already holds, which makes both create and update partial operations.

| Property | Type | Same as Eurocode? | Contents |
|---|---|---|---|
| `NonLinearFEM` | `SteelNonLinearFEM` | **Yes** | 2nd-order / Advance stability analysis — the same object and the same rules as the Eurocode group |
| `Deflection` | `SteelLinearDeflection` | **Yes** | Element and super-element deflection checks |
| `Buckling` | `SteelLinearBuckling` | **Yes** | Flexural and torsional buckling lengths — with `BucklingLength.YNodesType`/`ZNodesType` defaulting to **`braced`** here rather than to `unbraced`, see the note below. Note that `BucklingLength.TorsionalBucklingVerification` is a **CAN/CSA-only row**: the AISC property list hides it, so sending it `false` on an `AISCSteelLinearDesignTemplate` is **refused** — only a `CSASteelLinearDesignTemplate` may set it to `false` |
| `LateralTorsionalBuckling` | `NorthAmericanSteelLinearLateralTorsionalBuckling` | **No** | Per flange, built around the **Cb** moment modification factor instead of the Eurocode C1/C2/C3 set |
| `SteelSpecificProperties` | `NorthAmericanSteelSpecificProperties` | **North-American only** | HSS design thickness, combined torsion and shear, web stiffeners, Kt factor, double-angle connector spacing |
| `ColdFormedDesign` | `NorthAmericanSteelColdFormedDesign` | **No** | The AISI S100 options and the web crippling conditions at the two kinds of support |
| `AngleVerification` | `SteelAngleVerification` | **Yes** | Net-area verification of angle sections connected through one leg |

> ⚠️ **`Buckling.BucklingLength.YNodesType` / `ZNodesType` default to `braced` here, not to
> `unbraced`.** The braced/unbraced nature of the nodes is the one property of this shared group whose
> default depends on the steel design code: `CRoleManager::CreateLinearMetalExpert` stamps `braced` on a new
> AISC or CAN/CSA template and `unbraced` on a Eurocode one. Both properties are therefore **nullable and
> carry no default in the schema** — leave either one out and the stored value is kept, which on a fresh
> North-American template is `braced`; only a value you actually send is written, and both always read back
> populated, so a read-then-update round-trip preserves them. Sending the `Buckling` group without naming
> these two used to flip them to `unbraced`, which is a different structural assumption and changes the
> design result.

#### The North-American sub-objects

**`NorthAmericanSteelSpecificProperties`** — the group that has no Eurocode counterpart at all. Several
rows only apply to one family of sections; the design engine ignores the others, and the property list
shows them all, so they are all writable.

| Property | Type | Description |
|---|---|---|
| `HssDesignThicknessChoice` | `SteelHssDesignThickness` | Fraction of the nominal wall thickness used as the design wall thickness — `reduced` or `nominal`. Circular and rectangular tubes only |
| `HssDesignThicknessValue` | `double` | **Output only.** The numeric factor the choice resolves to, derived from `HssDesignThicknessChoice` **and** from the active code — **0.93 / 1.00 under AISC, 0.90 / 1.00 under CAN/CSA**. The property list does not expose it and a value sent is overwritten. The schema advertises **no default** for it on purpose: the number depends on the code, so it never reads back as 0 and no single default could be stated honestly |
| `HssDesignAxialStressRelieved` | `bool` | Whether the HSS is axially stress-relieved. Tubes only |
| `CheckForTorsionAndShear` | `bool` | Combined torsion and shear verification. **CAN/CSA only** — see the rules below |
| `StiffenersSpacing` | `double` (m) | Spacing of the transverse web stiffeners. **CAN/CSA only** — see the rules below. I, U and T profiles only |
| `StiffenersTensionField` | `bool` | Take the tension field action of the stiffened web into account. I, U and T profiles only |
| `KtFactorCheck` | `bool` | Verify the shear lag factor Kt (default `true`) |
| `KtFactorValue` | `double` | Imposed Kt, read only when `KtFactorCheck` is true (default `0.8`, adimensional) |
| `ConnectorsSpacing` | `double` (m) | Spacing of the connectors between the two angles of a double-angle section (default `1.0`). Double angles only |

**`NorthAmericanSteelLinearLateralTorsionalBuckling`** — the lateral-torsional buckling group. The two
flanges are treated independently, because the compressed flange is the one that needs restraining and
which flange is compressed depends on the sign of the moment.

| Property | Type | Description |
|---|---|---|
| `ElementVerification` | `bool` | Whether lateral-torsional buckling is verified (default `true`). When `false`, the whole length group below is ignored by the design engine |
| `LateralTorsionalBucklingLength` | `NorthAmericanSteelLateralTorsionalBucklingLength` | The lengths and the two parameter sets |

`NorthAmericanSteelLateralTorsionalBucklingLength`:

| Property | Type | Description |
|---|---|---|
| `UpperFlangeLength` / `LowerFlangeLength` | `double` (m) | LTB length of the upper / lower flange (the model fields `Lds` / `Ldi`). The same physical quantity as the `LengthValue` of the matching flange parameters, stored twice and **derived** from it — see the rules below |
| `UpperFlangeRestraintSummary` / `LowerFlangeRestraintSummary` | `string` | **Output only and localized** — Advance Design rebuilds them from the restraints of the corresponding parameter set, so a value sent is overwritten. Read the restraints; do not parse these |
| `UpperFlangeParameters` / `LowerFlangeParameters` | `NorthAmericanSteelLateralTorsionalBucklingParameters` | The parameter set of each flange |

`NorthAmericanSteelLateralTorsionalBucklingParameters` — the same class used twice, once per flange:

| Property | Type | Description |
|---|---|---|
| `IsProfile` | `bool` | Whether the element is a catalogue profile (default `true`) |
| `StartRestraint` / `EndRestraint` | `SteelLtbEndRestraint` | Lateral restraint at each end of the flange |
| `LengthMode` | `SteelBucklingLengthMode` | How the unbraced length of this flange is obtained |
| `LengthValue` | `double` | A **length in metres** when `LengthMode` is `imposed`, a dimensionless **coefficient** when the mode derives the length from the element, the mesh size or the super-element (default `1.0`) |
| `CbMode` | `SteelLtbFactorCbMode` | How the **Cb** moment modification factor is obtained — `auto_cb` or `imposed_cb` |
| `CbValue` | `double` | Imposed Cb, read only when `CbMode` is `imposed_cb` (adimensional) |

> **No buckling curve here.** Unlike `EC3SteelLateralTorsionalBucklingLength`, this group carries no LTB
> curve: AISC 360 and CAN/CSA S16 derive the resistance from the unbraced length and Cb rather than from a
> tabulated curve. Cb is AISC 360 clause F1; CAN/CSA S16 clause 13.6 calls the same quantity ω₂.

> **Four fields of the underlying object are deliberately not exposed.** The SOM class is shared with the
> French CM66 code and carries its B, C, D and load-application-point coefficients as well. The
> AISC / CAN-CSA lateral-torsional-buckling dialog writes none of them and the North-American design engine
> reads none of them, so exposing them would only offer a way to store values that nothing consumes.

**`NorthAmericanSteelColdFormedDesign`** — the AISI S100 options. Most of the group describes **web
crippling** (AISI S100 clause G5): the bearing length and the support conditions decide which coefficient
row of the standard's table the design engine picks.

| Property | Type | Description |
|---|---|---|
| `ExactCFDCalculation` | `bool` | Use the exact cold-formed design calculation instead of the simplified one |
| `OverlapSectionAtSupport` | `bool` | Whether the sections overlap at the support |
| `OverhangLengthAtSupport` | `double` (m) | Length of the overhang at the support |
| `KPhiE` | `double` | Rotational stiffness k of the elastic restraint provided by the sheeting (adimensional) |
| `BearingLength` | `double` (m) | Bearing length over which the concentrated load or reaction is applied |
| `ExteriorSupport` / `InteriorSupport` | `NorthAmericanSteelWebCripplingInfo` | Web crippling conditions at an **exterior (end)** and at an **interior** support |

`NorthAmericanSteelWebCripplingInfo` — the three entries of the AISI S100 clause G5 coefficient table,
used twice, once per kind of support:

| Property | Type | Values |
|---|---|---|
| `SupportCondition` | `SteelCFDSupportCondition` | `fastened` · `unfastened` |
| `FlangeCondition` | `SteelCFDFlangeCondition` | `stiffened_or_partially_stiffened` · `unstiffened` |
| `FlangeLoadingType` | `SteelCFDFlangeLoadingType` | `one_flange_loading_or_reaction` · `two_flange_loading_or_reaction` |

> ⚠️ **The cold-formed group is allocated under both codes, but only listed by the AISC dialog.** The model
> creates it and propagates it to the element for AISC *and* CAN/CSA, yet the "Design template description"
> dialog is missing its resource entry on the CAN/CSA side. Values sent here for a CAN/CSA template do
> reach the design engine and the element — they simply do not show in the desktop property list.

#### The rules specific to the North-American leaves

**Input that is refused** (the whole create or update fails and *nothing* is written):

| Rule | Why |
|---|---|
| The `$type` must match the model's active steel design code | The engine only ever allocates one code's property set; the diagnostic names the leaf the model does accept |
| `NorthAmericanSteelLinearDesignTemplate` itself is not accepted | It is an intermediate base — send `AISCSteelLinearDesignTemplate` or `CSASteelLinearDesignTemplate` |
| `SteelSpecificProperties.StiffenersSpacing` non-zero on an **AISC** template | CAN/CSA-only row: the AISC property list hides it, so a payload can never store a value the desktop could not have produced |
| `SteelSpecificProperties.CheckForTorsionAndShear` true on an **AISC** template | Same — use a `CSASteelLinearDesignTemplate` for either of these two |
| `Buckling.BucklingLength.TorsionalBucklingVerification` set to `false` on an **AISC** template | Same again, on a group shared with the Eurocode: the row is CAN/CSA-only and the AISC property list hides it. Only a `CSASteelLinearDesignTemplate` may set it to `false` |
| A flange whose `LengthMode` is `imposed` must have `LengthValue > 0` | The LTB dialog refuses to close on a null imposed length |

**Fields that are derived** (what you read back is computed, not what you sent):

- **`SteelSpecificProperties.HssDesignThicknessValue` is output only.** It is derived from
  `HssDesignThicknessChoice` **and** from the model's steel code — `reduced` resolves to **0.93 under
  AISC** and to **0.90 under CAN/CSA**, `nominal` to 1.00 under both. Set the choice, read the value. The
  schema states **no default** for it: with the number depending on the code, there is none to state.
- **`UpperFlangeRestraintSummary` / `LowerFlangeRestraintSummary` are output only**, exactly as on the
  Eurocode leaf: localized strings that Advance Design rebuilds from the flange restraints, so a value
  sent is ignored.
- **The flange LTB length follows the flange's `LengthValue`.** `UpperFlangeLength` / `LowerFlangeLength`
  (`Lds`/`Ldi`) and the `LengthValue` of the matching flange parameters are one physical quantity stored
  twice, and the first is derived from the second. Both idioms work, exactly as on the Eurocode leaf: send
  the flange length alone and the flange's `LengthMode` switches to `imposed` with it (the property-list
  idiom), or send the flange's own `LengthMode` and `LengthValue` and leave the flange length at `0` (the
  dialog idiom). Under the North-American codes the derived length simply **follows the flange's
  `LengthValue` whatever the mode** — so it carries a length for `imposed` and a **coefficient** for the
  computed modes, which is exactly what the AISC / CAN-CSA dialog stores there. The `0` default is never
  taken literally, so **an update that sends the `LateralTorsionalBucklingLength` group without the
  flange-parameters sub-object keeps the stored length** — patching a flange's Cb factor alone cannot wipe
  `Lds`. Sending a flange-parameters sub-object does reset the flange properties you leave unset, as
  sending any group does.

#### Assigning a template to an element

An element holds a **reference**, not a copy. A template is applied only when **all three** conditions hold:

| # | Condition |
|---|---|
| 1 | The template is a template of the **same kind of element** (a linear template on a linear element) |
| 2 | The template is of the **same material family** as the element. A steel element modelled as a **composite beam** (`LinearElementType = eLinearElementFEMTypeCompositeBeam`) belongs to the composite family, so a steel template cannot be assigned to it |
| 3 | The template carries the property set of the **design code the model is configured with at the moment of the assignment**. A template only ever holds the properties of the code that was active when it was created |

If a condition fails the assignment is refused with a diagnostic and the element is left untouched — the
element itself is still created or updated.

| Value of `DesignTemplateId` | Effect |
|---|---|
| `null` | The current assignment is left unchanged |
| `0` | The element is detached back to the built-in default template ("None") |
| an id | That template is assigned and its properties are copied onto the element |

> **Detaching is not a reset.** Advance Design keeps the expert property values the element currently has
> when the default template is assigned, so hand-made changes survive.

> ⚠️ **Changing the material of an element drops its design template.** Advance Design recreates the expert
> object of the new material, so the assignment is lost — and switching the material back does *not* bring
> it back. Re-assign the template after any material change. `AssignmentNumber` follows correctly: the
> template stops counting that element.

- **`AssignmentNumber`** follows the assignments: it is incremented on the new template and decremented on
  the previous one.
- **Deleting a template that is in use succeeds:** the elements that referenced it fall back to the default
  template and keep their values.
- **Editing a template updates every element that references it** — the same propagation the design-template
  dialog performs when it closes.

```csharp
// 1. Create a template - only the groups you care about need to be filled
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
    // every other group left null -> untouched
});
long templateId = created.Data.Value;

// 2. Assign it to a whole selection in one call
client.AssignDesignTemplateToElements(templateId, new List<long> { beam1, beam2, beam3 });

// 3. Edit the template - every element referencing it follows
var stored = (EC3SteelLinearDesignTemplate)client
    .GetInformationalElementsObject(new List<long> { templateId }).Data.First();
stored.Deflection.Deflections.AllowableDeflection1 = 300;
client.UpdateInformationalElement(templateId, stored);

// 4. Detach one element (it keeps its current expert values)
client.AssignDesignTemplateToElements(0, new List<long> { beam2 });
```

The North-American leaves are used exactly the same way — only the `$type` and the property set change:

```csharp
// The model must already be configured with an AISC steel design code, otherwise this is refused
// and the diagnostic names the leaf the model does accept.
EIDApiResponse aisc = client.CreateInformationalElement(new AISCSteelLinearDesignTemplate
{
    UserName     = "HSS columns - AISC",
    SectionClass = SteelSectionClassAISC.Compact,

    SteelSpecificProperties = new NorthAmericanSteelSpecificProperties
    {
        // reduced -> 0.93 under AISC (0.90 under CAN/CSA); read it back in HssDesignThicknessValue
        HssDesignThicknessChoice = SteelHssDesignThickness.Reduced,
        KtFactorCheck            = true,
        KtFactorValue            = 0.85
        // StiffenersSpacing and CheckForTorsionAndShear are CAN/CSA-only:
        // a non-default value for either one would make this call fail
    },

    LateralTorsionalBuckling = new NorthAmericanSteelLinearLateralTorsionalBuckling
    {
        ElementVerification = true,
        LateralTorsionalBucklingLength = new NorthAmericanSteelLateralTorsionalBucklingLength
        {
            UpperFlangeLength     = 3.0,             // same quantity as the flange LengthValue below
            UpperFlangeParameters = new NorthAmericanSteelLateralTorsionalBucklingParameters
            {
                StartRestraint = SteelLtbEndRestraint.Pinned,
                EndRestraint   = SteelLtbEndRestraint.Pinned,
                LengthMode     = SteelBucklingLengthMode.Imposed,
                LengthValue    = 3.0,                        // imposed -> must be > 0
                CbMode         = SteelLtbFactorCbMode.Imposed_cb,
                CbValue        = 1.14                        // Cb, not the Eurocode C1/C2/C3
            }
        }
    }
    // every other group left null -> untouched
});

// Against a CAN/CSA model, send a CSASteelLinearDesignTemplate instead: same seven groups,
// plus the Class 1-4 section class and the effective Class 4 characteristics.
var csa = new CSASteelLinearDesignTemplate
{
    UserName      = "Girders - CSA",
    SectionClass  = SteelSectionClass.Class_4,
    ImposedValues = true,
    Sxeff         = 0.0085,          // m2
    Weff1y        = 0.00072          // m3
};
```

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

> On the nodes of a **linear element** the rotations are read in the frame the post-processing is
> configured with, and `R` is their norm (the engine exposes no resultant rotation for a linear
> element). The diagram counterpart, `ResDiagramDisplacements`, still carries translations only.

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

### Design results

Returned by [`GetDesignResults`](#post-apimodelanalysisgetdesignresults). A separate hierarchy from the FEM
results above — same `ResBase` root, different payloads, addressed per **mesh cell** rather than per node.

> ⚠️ These payload shapes are part of the contract in this release, but the endpoint that produces
> them is not — see [`GetDesignResults`](#post-apimodelanalysisgetdesignresults).

> ℹ️ **The `ResEc3G2*` payloads are the 2nd generation ones.** They stay published so that the contract
> does not change the day EN 1993-1-1:2023 ships, but the standard is not released: on an installation
> where the 2nd generation Eurocodes are not enabled, a model carrying that code is refused by
> `GetDesignResults` rather than answered under the 1st generation, so nothing there can produce them —
> see [Section 13](#design-standards-and-national-annexes).

```
ResDesignElementLinearSteel : ResBase
  ├── userID        the element's user-visible identifier (NOT the descriptive EID that was requested)
  ├── designFamily  DesignCalculationType.Steel
  ├── resultType    which family this object carries
  └── resMeshes[]   one ResDesignMesh per mesh cell
        ├── meshIndex   0-based, restarts at 0 on every element
        └── data        the polymorphic payload (ResDesignData)
```

**Addressing.** Granularity is the mesh cell and **no envelope is computed** — compute your own maxima. Each
payload also carries the abscissa index *inside* the cell (`meshPoint`, or `pointNumber` in the 2nd
generation), 0 to 4, because every verification reaches its worst point independently. `caseNumber` is the
governing combination as the **raw engine index**, not an EID.

**Units are SI throughout** — N, N·m, m, m², N/m². The Advance Design reports print kN, kN·m and cm.

| Payload `$type` | Family | Shape |
|---|---|---|
| `ResEc3Resistance` | `CrossSectionResistance`, 1st gen | 12 `ResEc3ResistanceCheck` blocks + the clause 6.2.9 reduced resistances |
| `ResEc3Stability` | `Stability`, 1st gen | ~90 named quantities: χ, λ̄, φ, α, Ncr, Mcr, C1–C3, k<sub>yy</sub>…k<sub>zz</sub>, and both interaction sums (6.61 / 6.62) split into their N / My / Mz terms |
| `ResEc3FireResistance` | `FireResistance`, 1st gen | 6 check blocks + temperature-dependent material factors |
| `ResEc3Deflection` | `Deflection1/2`, 1st gen | per direction (y, z), each with the five location families and an envelope |
| `ResEc3WebShearBuckling` | `WebShearBuckling`, both gens | up to 3 EN 1993-1-5 checks, each nullable — present only when performed |
| `ResEc3G2Resistance` / `…Stability` / `…FireResistance` | 2nd gen | `verifications[]`, each a list of code **equations** with their symbol/value parameters and messages |
| `ResEc3G2Deflection` | `Deflection1/2`, 2nd gen | `verifications[]`, each carrying its four-digit id **and its decoded** entity / location / direction / criterion |
| `ResEc3G2CrossSectionInfo` | `CrossSectionInfo`, 2nd gen only | start / end / haunch sections, each a name + material + symbol map |
| `ResEc3BucklingLengths`, `ResEc3LateralTorsionalBucklingLengths` | both gens | L<sub>fy</sub>/L<sub>fz</sub>, L<sub>di</sub>/L<sub>ds</sub>, their slendernesses and ratios |
| `ResEc3CrossSectionClass`, `ResEc3FireCrossSectionClass` | both gens | the class setting, the classes per loading pattern, and the effective characteristics |
| `ResEc3FireTemperature`, `ResEc3AdvancedStabilityResistance`, `ResEc3MaxWorkRatio` | see the endpoint table | |
| `ResEc3ColdFormedResistance` | `ColdFormedResistance` | one block per EN 1993-1-3 clause (6.1.2 … 6.3), each also reporting `usedEquation` — which formula of that clause was applied |
| `ResSteelProfileOptimisation` | `ProfileOptimisation`, every code | **per element** - current section + proposed section, with the strength and deflection work ratios of both |
| `ResSteelColdFormedSectionInfo` | `ColdFormedSectionInfo`, every code | **per element** - the cold-formed start and end sections: gross and effective area, inertias, moduli, shear centre, fy / fu / fya |
| `ResEc3HaunchSectionProperties` | `HaunchSectionProperties`, 1st gen only | **per element** - the haunch at each end, or null where there is none |
| `ResEc3BucklingLengthCoefficients` | `BucklingLengthCoefficients`, both gens | **per element** - the end-restraint coefficients k behind the automatic buckling lengths, null per direction when the length was imposed |
| `ResEc3BucklingLengthNodeRestraints` | `BucklingLengthNodeRestraints`, both gens | **per element** - what frames into each end of the element, per direction: the connected member identifiers split into superior / inferior / horizontal, and the stiffness sum of each group |
| `ResAiscSectionInfo` | `CrossSectionInfo`, North American only | per mesh cell - the gross section: dimensions, areas, moduli, inertias, radii of gyration, shear centre. **Always SI**, whichever code |
| `ResEc3GlobalSwayImperfectionForces` | `GlobalSwayImperfectionForces`, both gens | **per combination** - V<sub>Ed</sub> and the equivalent forces φ<sub>y</sub>·V<sub>Ed</sub> / φ<sub>z</sub>·V<sub>Ed</sub> |
| `ResEc3LocalBowImperfectionForces` | `LocalBowImperfectionForces`, both gens | **per combination** - the punctual and linear force per direction, the length, e<sub>0</sub>/L, and `isColumn` |

#### Six traps worth reading before using these

**0. Seven families do not come back on `ResDesignElementLinearSteel`,** and they split into two
groups.

*Five are PER ELEMENT*: `ProfileOptimisation`, `ColdFormedSectionInfo`, `HaunchSectionProperties`,
`BucklingLengthCoefficients` and `BucklingLengthNodeRestraints`. The engine writes one identical
record per mesh cell, or two records describing the element's two ends, so there is one answer for
the whole element - and it arrives as **`ResDesignElementLinearSteelPerElement`**, which carries a
single `data` payload and no `resMeshes` list.

*Two are PER COMBINATION*: `GlobalSwayImperfectionForces` and `LocalBowImperfectionForces`. These are
not verifications the engine performed on the model but **loads it added to it** - one set per
(element, unfavourable combination), all of them kept - so they arrive as
**`ResDesignElementLinearSteelPerCase`**, which carries a `resCases` list whose entries pair a
`caseNumber` with a `data` payload, exactly as `resMeshes` pairs a `meshIndex` with one.

Same call, same query, two more `$type`s to switch on. An element the engine produced nothing for is
absent from the response rather than present with an empty payload, so `HaunchSectionProperties`
answers only for elements that have a haunch, `ColdFormedSectionInfo` only for cold-formed ones, and
`GlobalSwayImperfectionForces` only for columns that were in compression.

**1. `isCalculated`, not `verification`.** Every resistance and fire check carries both. `verification` is a
bit mask of which code equations *apply* to that section and loading, and the engine fills it in whether or
not it went on to verify anything — a check with no result still has a non-zero mask. `isCalculated` is the
one that says the check ran; when it is false, `action`, `limit` and `workRatio` are all zero and mean
nothing. Advance Design's shape sheet prints those rows as *"not done"*.

**2. `ResEc3CrossSectionClass.sectionClass` is the class SETTING, not the class.** It is `Auto` on almost
every element, because that is what "let the engine classify" is stored as; it only holds a fixed class when
`isImposed` is true. The classes the engine **determined** are in `classPerLoadingPattern` — one entry per
pattern, since a section classifies differently in compression, in oblique bending, for flexural buckling and
for lateral-torsional buckling. The class a given verification used is on that verification, at
`sectionClassDetails.classDetails.sectionClass`.

**3. `Lfy` is the buckling length in the y direction, which governs buckling about z-z.** That is Advance
Design's own convention, mirrored here: its shape sheet pairs "Buckling on Y-Y" with L<sub>fz</sub> and
"Buckling on Z-Z" with L<sub>fy</sub>. Likewise `slendernessLfy` pairs with `lambda_bar_z`.

**4. A zero work ratio does not mean "verified and unstressed".** Three different situations produce one, and
each has its own flag saying which: a 1st-generation resistance or fire check that never ran reports
`isCalculated` false (trap 1); a 2nd-generation verification that was not computed reports `exists` false;
and a cold-formed lateral-torsional buckling check that clause 6.3.1.2(4) allowed to be skipped reports
`isIgnored` true. Read the flag before reading the number.

**5. The imperfection e<sub>0</sub>/L is a RATIO, and for a beam it excludes k.** Advance Design's two
bow-imperfection report tables print `1/ratio` rounded to an integer - the familiar "L/250" - whereas
`bowImperfectionRatioY` / `…Z` carry the stored ratio itself, so an IPE300 column comes back as `0.005`
and `0.004` rather than `200` and `250`. For a **beam** the engine has already divided the k
coefficient out of the value it stored, so the ratio is e<sub>0</sub>/L without k; k itself is a design
input, not a result, and `GetDesignTemplate` returns it at
`globalSwayImperfections.beamImperfections.kValue` - which is where the beam report table reads its own
extra k column from.

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
| `CsCdCalcMode` | `CsCdCalcMode_Wind_EN1991_1_4_API` | `CS_CD_AUTO` | — | Cs·Cd calculation mode: `CS_CD_AUTO`, `CS_CD_IMPOSED`, `CS_CD_NO` (no structural factor) |
| `CsCdVal` | `double` | 1.0 | — | Cs·Cd value |
| `Delta` | `double` | 0.1 | — | Logarithmic decrement |
| `N` | `double` | 0 | Hz | Fundamental frequency |
| `NAuto` | `bool` | true | — | Auto-calculate N |
| `PhiScaffolding` | `double` | 0.4 | — | Scaffolding factor φ |
| `CorrelationCoefficientType` | `CorrelationCoefficientType_Wind_EN1991_1_4_API` | `CORREL_AUTO` | — | Correlation coefficient type: `CORREL_AUTO`, `CORREL_IMPOSED`, `CORREL_NO` (no correlation coefficient) |
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
| `ImposedCpeType` | `e_CPE_MODE_default` | Imposed Cpe type: `e_CPE_MODE_default`, `e_CPE_MODE_FORCE` (Cpe,10), `e_CPE_MODE_1` (Cpe,1), `e_CPE_MODE_1_10` (Cpe,1 and Cpe,10) |
| `PitchType` | `PITCH_TYPE_POSITIVE_SLOPE` | Roof pitch type: `PITCH_TYPE_POSITIVE_SLOPE`, `PITCH_TYPE_NEGATIVE_SLOPE`, `PITCH_TYPE_MAX_CPE` (per zone the minimum Cpe of both pitch sets). `PITCH_TYPE_MONO` / `PITCH_TYPE_DUO` are obsolete aliases of positive / negative slope; a read returns the new names |

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
| `WindDesign` | 0 | — | Wind design method: `0` = Chapter 28 Envelope Low-Rise Building; `1` = Scaffolding;. The `WindDesign_WindIBC_API` enum was removed; use integer values directly. |
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

| Property | Type | Default | Description |
|---|---|---|---|
| `ImportanceCategory` | `ImportanceCategoryEN1998_API` | `CAT_II` | Importance class (`CAT_I`…`CAT_IV`) |
| `CoefGammaI` | `double` | 1.0 | Importance factor γI |
| `CoefQHoriz` | `double` | 1.5 | Behaviour factor q (horizontal X) |
| `CoefQHorizY` | `double` | 1.5 | Behaviour factor q (horizontal Y) |
| `CoefQVert` | `double` | 1.5 | Behaviour factor q (vertical) |
| `Correction` | `bool` | `false` | Apply lower-bound correction (factor β) |
| `CoefBeta` | `double` | 0.2 | Lower-bound factor β |
| `DuctilityClass` | `DuctilityClassEN1998_API` | `MEDIUM` | Ductility class (`LOW` / `MEDIUM` / `HIGH`) |

#### `CalculationMethodEN1998`

| Property | Type | Description |
|---|---|---|
| `CalculMethod` | `CalculMethod_API` | SRSS, CQC, ABS |
| `ResidualMode` | `bool` | Include residual modes in analysis |

---

### 12.8 Seismic CBN

#### `ImplantationSeismicCBN`

Key properties: `Sa02`, `Sa05`, `Sa10`, `Sa20`, `Sa50` *(CBN 2015/2020 only)*, `Sa100` *(CBN 2015/2020 only)*, `Pga` *(CBN 2015/2020 only)*, `Sa02_X450` *(CBN 2020 only)*, `Sa05_X450` *(CBN 2020 only)*, `Sa10_X450` *(CBN 2020 only)*, `Sa20_X450` *(CBN 2020 only)*, `IeS10` *(CBN 2020 only)*, `SiteClass`, `Fa`, `Fv`, `IeFaSa02`, `SeismicCategory` *(CBN 2020 only)*, `ImportanceFactorIE`.

#### `StructureSeismicCBN`

| Property | Type | Default | Description |
|---|---|---|---|
| `TotalHeight` | `double` | 0.0 | Total structure height (m) |
| `FloorsNo` | `int` | 0 | Number of floors |
| `RegularStructure` | `bool` | `true` | Regularity flag |
| `SfsrCheck` | `bool` | `false` | SFSR check flag |
| `AccidentalTorsion` | `double` | 0.0 | Accidental torsion (dimensionless) |
| `ApplyDuctilityEffects` | `bool` | `true` | Enable ductility effects |

#### `StructureTypeSeismicCBN`

Defines the structural system type and SFRS material for the seismic CBN load case.

| Property | Type | Default | Description |
|---|---|---|---|
| `TypeStructure` | `TypeStructure_API` | `StructureResistMomentFrame` | Structural system type |
| `TypeSRFS` | `MaterialSRFS_API` | `Concrete` | SFRS material type |
| `SfrsConcrete` | `SFRSConcrete_API` | `DuctileMomentResistingFrame` | Concrete SFRS type |
| `SfrsSteel` | `SFRSSteel_API` | `DuctileMomentResistingFrame` | Steel SFRS type |
| `SfrsMasonry` | `SFRSMasonry_API` | `DuctileShearWall` | Masonry SFRS type |
| `SfrsTimber` | `SFRSTimber_API` | `DuctileShearWall` | Timber SFRS type |

#### `DuctilityFactorsCBN`

| Property | Type | Default | Description |
|---|---|---|---|
| `Calibration` | `bool` | `true` | Calibration flag |
| `Torsion` | `bool` | `false` | Torsion consideration flag |
| `Rd` | `double` | 1.0 | Ductility-related force modification factor Rd |
| `Ro` | `double` | 1.0 | Overstrength-related force modification factor Ro |
| `L` | `double` | 0.0 | L parameter — CBN 2015/2020 only |

---

### 12.9 Seismic IBC

#### `ImplantationSeismicIBC`

Key properties: `Ss`, `S1`, `T0`, `SaT0` *(calculated)*, `Ts`, `SaTS` *(calculated)*, `Tl`, `SaTL` *(calculated)*, `SiteClass`, `Fa`, `Fv`, `OccupancyCategory`.

#### `StructureSeismicIBC`

| Property | Type | Default | Description |
|---|---|---|---|
| `TotalHeight` | `double` | 0.0 | Total structure height (m) |
| `FloorsNo` | `int` | 0 | Number of floors |
| `RegularStructure` | `bool` | `true` | Regularity flag |
| `SfsrCheck` | `bool` | `false` | SFRS check flag |
| `AccidentalTorsion` | `double` | 0.0 | Accidental torsion (dimensionless) |

#### `StructureTypeIBC`

| Property | Type | Default | Description |
|---|---|---|---|
| `TypeStructure` | `TypeStructureIBC_API` | `StructureResistMomentFrame` | Structural system type |
| `TypeSRFS` | `int` | 0 | SFRS material type index (material-based) |

#### `DuctilityFactorsIBC`

| Property | Type | Default | Description |
|---|---|---|---|
| `Calibration` | `bool` | `true` | Calibration flag |
| `Torsion` | `bool` | `true` | Torsion consideration flag |
| `R` | `double` | 0 *(calc. from SFRS)* | Response modification coefficient |
| `Cd` | `double` | 0 *(calc. from SFRS)* | Deflection amplification factor |

---

## 13. Enumerations Reference

### `AD_API_ActionType`
| Value | Description |
|---|---|
| `ClimaticAutoGeneration` | Auto-generate wind/snow loads |

### `DesignCalculationType`
Material family whose Expert design engine is launched by
[`LaunchDesignCalculation`](#post-apimodelanalysislaunchdesigncalculation). It lists only the families that
have a design engine behind them, so `rigid` and `undefined_material` of `DesignTemplateMaterialType` (the
design-template enumeration, listed further down this section) are deliberately absent; the values the two
enumerations do share are identical.

| Value | Int | Description |
|---|---|---|
| `Concrete` | 0 | Reinforced concrete design — per the concrete design code of the model. The engine also knows BS 8110 and STAS 10107, which Advance Design no longer offers and `ConcreteDesignCode` does not carry |
| `Steel` | 1 | Steelwork design — EC3, CM66, AISC or CSA, per the steel design code of the model |
| `Timber` | 2 | Timber design — EC5 or the North-American codes (O86 / NDS), per the timber design code of the model |
| `Composite` | 3 | Composite beam design (steel section + concrete slab) — EC4, AISC 360 or CSA S16, per the composite design code of the model |

### `ResultType`
| Value | Int | Description |
|---|---|---|
| `displacement` | 0 | Displacements |
| `forces` | 1 | Internal forces |
| `stresses` | 2 | Stresses |
| `resultantforces` | 4 | Resultant forces |

### `UserSectionContourType_API`
Kind of one contour of the outline of a user section — see
[`UserSectionShape`](#usersectionshape--the-outline-the-epure).

| Value | Int | Description |
|---|---|---|
| `Polygon` | 0 | A list of vertices, closed automatically; each vertex may carry a fillet radius |
| `Circle` | 1 | A full disc, given by a single centre point and a radius |

### `SectionRollingType_API`
How a cross-section was manufactured. It drives the residual-stress assumptions, hence the buckling curve
selection (EN 1993-1-1 table 6.2) and, for the cold-formed families, the k factor of EN 1993-1-3 3.2.2(3).
Not to be confused with `Ec3SectionRollingType`, which mirrors the design engine enumeration and uses
different numeric values.

| Value | Int | Description |
|---|---|---|
| `Unknown` | -1 | Not known |
| `Rolled` | 0 | Rolled |
| `HotRolled` | 1 | Hot rolled |
| `ColdRolled` | 2 | Cold rolled |
| `Welded` | 3 | Welded — what Advance Design assigns to a user section |
| `ThickWelded` | 4 | Welded from thick plate |
| `RolledColdFormed` | 5 | Cold-formed by roll forming |
| `BendColdFormed` | 6 | Cold-formed by another forming process |

### `UserSectionProfileType_API`
Which standard profile shape a user section behaves like. A user section carries no parametric
dimensions, so the steel design engines use this to pick the applicable formulae, buckling curves and
cross-section classification rules. Left `Unknown`, the shape-dependent verifications are skipped.

`Unknown` (0) · `Angle` (1) · `UnequalAngle` (2) · `DoubleAngle` (3) · `DoubleAngleDiagonal` (4) ·
`Circular` (5) · `Rectangular` (6) · `Square` (7) · `ISymmetric` (8) · `IDissymmetric` (9) · `U` (10) ·
`T` (11) · `TDissymmetric` (12) · `CircularTube` (13) · `RectangularTube` (14) · `SquareTube` (15) ·
`OvalTube` (16) · `TubeInTube` (17) · `RectangularChamfer` (18) · `SquareOpenTube` (19)

### `DiagnosticSeverity`
`Information` · `Warning` · `Error` · `Critical`

### `Language_Code`
`eLanguageFrench` · `eLanguageEnglish` · `eLanguageRomanian` · `eLanguageGerman` · `eLanguagePolish` · `eLanguageCzech` · `eLanguageDutch` · `eLanguageRussian` · `eLanguageSpanish` · `eLanguageEnglishUK` · `eLanguageHungarian` · `eLanguageChinese` · `eLanguageTaiwan` · `eLanguageJapanese` · `eLanguageKorean` · `eLanguageBulgarian` · `eLanguageGreek` · `eLanguageTurkish` · `eLanguageQuebec` · `eLanguageItalian` · `eLanguagePortuguese` · `eLanguagePortugueseBrazilian` · `eLanguageSlovak` · `eLanguageEndMark`

### `Localization_Code`
`LOCALIZATION_ALGERIA` · `LOCALIZATION_ASIA` · `LOCALIZATION_CANADA` · `LOCALIZATION_CZECH_REPUBLIC` · `LOCALIZATION_EUROPE` · `LOCALIZATION_FRANCE` · `LOCALIZATION_GERMANY` · `LOCALIZATION_MAROCCO` · `LOCALIZATION_POLAND` · `LOCALIZATION_ROMANIA` · `LOCALIZATION_TUNISIA` · `LOCALIZATION_UK` · `LOCALIZATION_US` · `LOCALIZATION_ITALIA` · `LOCALIZATION_SLOVAKIA` · `LOCALIZATION_BULGARIA` · `LOCALIZATION_SPAIN` · `LOCALIZATION_PORTUGAL`

### `LogVerboseLevel`
`Errors` · `ErrorsAndWarnings` · `AllDetails`

### Design standards and national annexes

The sixteen values of [`Environments.DesignCodes`](#311-the-environment-of-a-session) — the eight design
standards a session runs under, and the national annex beside each. **Send them by name**: the numbers
behind these members are Advance Design's own storage values, three of the lists are deliberately not
contiguous (the standards the API does not offer were left out rather than renumbered), and the generated
clients renumber their copy from 0, so a number cast into a client enumeration lands on a *different*
standard. A number that is not a member at all is refused with an error.

Which annex goes with which standard is the table in [3.1.1](#311-the-environment-of-a-session); each
enumeration below repeats its own rule.

**`eCombinationsStandard`** — the *Combinations* row. Also what `CombinationsGenerationSettings.Standard`
reports ([3.6.3](#363-settings-of-the-automatic-generation)).
`Eurocode` · `US` · `Canada` · `EurocodeItalia` · `CanadaNBC2015` · `NTC2018` · `Greenhouse_EN13031_1` ·
`US2018` · `CanadaCNB2020`
`Greenhouse_EN13031_1` is accepted only on an installation carrying the greenhouse computing option.

**`eCombinationsEurocodeAnnex`** — its national annex, accepted with `Eurocode` only.
`General` · `France` · `UK` · `Germany` · `Czech` · `Romania` · `Slovakia` · `Poland` · `Spain` ·
`Portugal`

**`SeismicDesignCode`** — the *Seismic* row.
`ps92_2010` · `p100` · `rps2000` · `en1998_1` · `ps92` · `rpa99_2003` · `p100_1_2013` · `ntc2008` ·
`cnb2005` · `ibc2012` · `cnb2015` · `ntc2018` · `ibc2018` · `cnb2020`
`ps92_2010` is the 2010 edition of the French standard and `ps92` the original one — that way round, as in
the dialog. `en1998_1` is the only seismic standard with a national annex.

**`SeismicEurocodeAnnex`** — national annex of EN 1998-1, accepted with `en1998_1` only.
`france` · `general` · `czech` · `romania` · `germany` · `slovakia` · `spain` · `portugal`

**`ClimaticDesignCode`** — the *Climatic* row: snow and wind.
`france` · `romania` · `eurocode` · `ntc2008` · `cbnsw2010` · `cbnsw2015` · `ibc2015` · `ntc2018` ·
`pr_en13031_1` · `ibc2018` · `cbnsw2020` · `asce_7_22`
`cbnsw2010` is the entry the dialog labels *CNB2005*; the three Canadian members keep the edition year
Advance Design stores. `pr_en13031_1` (greenhouses) is accepted only on an installation carrying the
greenhouse computing option.

**`ClimaticEurocodeAnnex`** — national annex of EN 1991.
`france` · `romania` · `general` · `uk` · `germany` · `czech` · `poland` · `slovakia` · `spain` ·
`portugal`
All ten with `eurocode`; `general` and `france` only with `pr_en13031_1`; `romania` only with `romania`,
which pins it anyway.

**`ConcreteDesignCode`** — the *Reinforced Concrete* row.
`bael91` · `eurocode` · `aci` · `can_csa` · `ntc2008` · `ntc2018` · `can_csa_2019`
Not contiguous: the two standards Advance Design no longer offers (STAS 10107, BS 8110) were left out, and
so was the 2nd generation EN 1992. `can_csa` is the 2014 edition of the Canadian standard and
`can_csa_2019` the current one.

**`ConcreteEurocodeAnnex`** — national annex of EN 1992.
`france` · `uk` · `germany` · `romania` · `general` · `czech` · `italy` · `poland` · `slovakia` · `spain` ·
`portugal`
Every value except `italy` goes with `eurocode`. `italy` is what `ntc2008` and `ntc2018` pin, and the only
annex those two accept — Italy is a concrete *standard* here, not an annex of Eurocode 2.

**`SteelDesignCode`** — the *Steelwork* row. The same enumeration the design templates and the steel
settings report; its members are listed under
[Expert design template enumerations](#expert-design-template-enumerations). As an input it takes eight of
its nine values, `can_csa_s16_2024` being refused.

**`SteelEurocodeAnnex`** — national annex of EN 1993, accepted with `eurocode` only.
`france` · `romania` · `general` · `uk` · `germany` · `czech` · `italy` · `poland` · `slovakia` · `spain` ·
`portugal`
`italy` is **not writable** — Italy is a steel standard here (`ntc2008` / `ntc2018`), not an annex. It is in
the list so that `GetEnvironment` can report a model that carries it.

**`eAiscDesignMethod`** — `DesignCodes.AiscMethod`, the AISC 360 design method, accepted with the `US` /
`US2018` combination standards or the `aisc` / `aisc_2016` steel standards. The same enumeration `USCombinationsGenerationSettings.AiscDesignMethod` reports.
`AllowableStrengthDesign` · `LoadAndResistanceFactorDesign`

**`TimberDesignCode`** — the *Timber* row.
`eurocode` · `ntc2008` · `ntc2018` · `nds_2018` · `o86_2019`
`eurocode` is the only one with a national annex. CSA O86-24 is not here: Advance Design removed it from
the dialog and the engine treats it exactly as `o86_2019`.

**`TimberEurocodeAnnex`** — national annex of EN 1995, accepted with `eurocode` only.
`france` · `uk` · `germany` · `romania` · `general` · `czech` · `italy` · `poland` · `slovakia` · `spain` ·
`portugal`
`italy` is **not writable**, for the same reason as steel.

**`MasonryDesignCode`** — the *Masonry* row.
`eurocode` · `ntc2018` · `cr6_2013`

**`MasonryEurocodeAnnex`** — national annex of EN 1996, accepted with `eurocode` only.
`general` · `czech` · `france` · `poland` · `romania` · `slovakia` · `uk` · `germany` · `portugal` ·
`spain`
Not contiguous: Advance Design's own numbering has no value 2, and the hole is reproduced rather than
closed, because those numbers are what the model stores.

**`CompositeDesignCode`** — the *Composite* row.
`eurocode` · `aisc_360_16`
CSA S16-19 composite is not here: the desktop application skips it and rewrites a model that carries it
back to the localization's default, so it is not a value a session can hold either.

**`CompositeEurocodeAnnex`** — national annex of EN 1994, accepted with `eurocode` only.
`general` · `france` · `uk` · `romania` · `germany` · `czech` · `poland` · `slovakia` · `spain` · `portugal`

> ⚠️ **The 2nd generation Eurocodes are not released, and nothing in this contract names one.** No member
> of any enumeration above stands for a 2nd generation Eurocode, and neither `SteelDesignCode` nor
> `eCombinationsStandard` has one either — so one cannot be sent by name, and its raw number is refused
> with an error rather than accepted. The DTO shapes built for them stay published, so that the contract
> does not change the day the standards ship: `Eurocode2ndGenCombinationsGenerationSettings` and its row
> types, `Ec3G2VerificationOptions`, `Ec3G2SafetyFactorOptions`, and the `ResEc3G2*` result payloads. On
> installations where the 2nd generation Eurocodes are enabled, the implementation behind them runs.
> Everywhere else a model that carries a 2nd generation code is **refused** — by the combination settings
> (`Get` / `Set` / `Reset`), the steel settings group, the design templates, `GenerateCombinations`,
> `LaunchDesignCalculation` and `GetDesignResults` — rather than answered under the previous generation of
> the standard, which would be a wrong answer that looks like a right one.
> [`GetEnvironment`](#get-apimodelmanagementgetenvironment) reports such a standard as `null`, with a
> warning.

### `MaterialBehaviourType`
`isotropic_behavior` · `orthotropic_behavior`

### `LinearElementFEMType_API`
`eLinearElementFEMTypeGeneral` · `eLinearElementFEMTypeCompositeBeam`

### `GeneralBeamType_API`
`bar` · `beamWStandardBending` · `sbeam` · `variablebeam` · `tie` · `strut` · `cable` · `rigid` · `CompositeBeamSimpleBeam` · `CompositeBeamSbeam` · `CompositeBeamVariable`

### `PlanarElementType`
`membrane` · `plate` · `shell` · `deformation_plane` · `steeldeck` · `layeredShell` · `slabonsteeldeck` · `planar_no_of_types`

### `LinearReleaseType`
Origin of a [linear release template](#105-linear-release-templates) stored in the model release pool. **Output only.**

| Value | Int | Description |
|---|---|---|
| `default_release` | 0 | One of the three built-in templates delivered with the model ("Free" / "Pinned" / "Fixed"). Cannot be modified or deleted through the API. |
| `user_defined_release` | 1 | Template created by the user (always the case for a template created through the API). |

### `LinearReleaseNonlinearityType`
Non-linearity applied to a released degree of freedom of a [linear release template](#105-linear-release-templates).

| Value | Int | Description |
|---|---|---|
| `none` | 0 | The released degree of freedom behaves linearly in both directions |
| `compression_only` | 1 | The released degree of freedom only reacts in compression |
| `tension_only` | 2 | The released degree of freedom only reacts in tension |

### Expert design template enumerations

Enumerations of the [Expert design templates](#106-expert-design-templates). Their members are written in
snake_case in the OpenAPI schema; the generated C# client turns them into Pascal-with-underscores
(`class_4` → `Class_4`, `auto_length` → `Auto_length`, `steel` → `Steel`).

**Identity of a template — output only**

| Enum | Values |
|---|---|
| `DesignTemplateMaterialType` | `concrete` · `steel` · `timber` · `composite` · `rigid` · `undefined_material` |
| `DesignTemplateElementType` | `linear` · `planar` · `undefined_element` |
| `SteelDesignCode` | `eurocode` · `aisc` · `can_csa_s16_2009` · `ntc2008` · `can_csa_s16_2014` · `ntc2018` · `aisc_2016` · `can_csa_s16_2019` · `can_csa_s16_2024` |

`SteelDesignCode` lists the **nine** codes the API works with, out of the eleven Advance Design knows: the
three of the Eurocode family, the two AISC editions and the four CAN/CSA S16 editions. Two are left out —
`cm66`, for which no expert property set and no settings group is exposed, and the 2nd generation
Eurocode 3, which is not released. Every property typed with this enumeration is behind a gate that refuses
either of them before anything is read or written. The values are the codes' own numbers and are therefore
not contiguous.

This enumeration is also what `Environments.DesignCodes.Steel`
([3.1.1](#311-the-environment-of-a-session)) takes, which is the one place the steel code is *written*;
there it accepts eight of the nine, `can_csa_s16_2024` being refused because Advance Design no longer
offers it and it calculates exactly as `can_csa_s16_2019`.

Which code maps to which template leaf is the table in
[10.6](#which-leaf-for-which-design-code); `SteelLinearDesignTemplate.DesignCode` is output only and
simply reports the model's setting.

**General properties**

| Enum | Values |
|---|---|
| `SteelSectionClass` | `auto_class` · `class_1` · `class_2` · `class_3` · `class_4` — the Eurocode and CAN/CSA list |
| `SteelSectionClassAISC` | `auto_class` · `compact` · `noncompact` · `slender_element` — the AISC list |
| `SteelManufacturingQuality` | `class_a` · `class_b` · `class_c` |
| `SteelExecutionClassType` | `global_execution_class` · `imposed_execution_class` |
| `SteelExecutionClass` | `exc1` · `exc2` · `exc3` · `exc4` · `not_set` |
| `SteelFamilyOfElement` | `family_n` · `family_a` · `family_b` · `family_c` · `family_d` · `family_d_plus` · `family_e` · `family_e_plus` |

> **Two section-class lists, one stored integer.** `EC3SteelLinearDesignTemplate.SectionClass` and
> `CSASteelLinearDesignTemplate.SectionClass` are typed with `SteelSectionClass`;
> `AISCSteelLinearDesignTemplate.SectionClass` is typed with `SteelSectionClassAISC`, because AISC 360
> clause B4 classifies a section as compact / noncompact / slender-element rather than Class 1 to 4. It is
> the same stored integer read against a different list. The last four enumerations of this block —
> manufacturing quality, execution class and family of element — exist on the **Eurocode leaf only**.

**Deflections**

| Enum | Values |
|---|---|
| `SteelDeflectionReferenceLengthOption` | `aligned` · `imposed` · `projected` |
| `SteelDeflectionLocation` | `extremity` · `span` · `envelope` |

**Buckling**

| Enum | Values |
|---|---|
| `SteelBucklingLengthMode` | `auto_length` · `imposed` · `calculated_from_element` · `calculated_from_mesh_size` · `calculated_from_super_element` · `lateral_torsional_buckling_length_upper_flange` · `lateral_torsional_buckling_length_lower_flange` |
| `SteelBucklingMuMode` | `auto_mu` · `imposed_mu` |
| `SteelBucklingNodesType` | `braced` · `unbraced` |
| `SteelBucklingCurve` | `auto_curve` · `curve_a0` · `curve_a` · `curve_b` · `curve_c` · `curve_d` |
| `SteelUniformMomentFactorMode` | `auto_factor` · `imposed` |
| `SteelTorsionalBucklingLengthMode` | `equal_to_lfz` · `equal_to_lfy` · `max_of_lfy_lfz` · `imposed` · `calculated_from_super_element` · `calculated_from_element` · `calculated_from_mesh_size` |

> **`SteelBucklingNodesType` is the one enumeration of this block with no default value.**
> `Buckling.BucklingLength.YNodesType` and `ZNodesType` are nullable, because the value a new template
> starts from follows the model's steel design code — `braced` under AISC and CAN/CSA, `unbraced` under the
> Eurocode — see [Section 10.6](#106-expert-design-templates).

**Lateral-torsional buckling**

| Enum | Values |
|---|---|
| `SteelLtbEndRestraint` | `no_restraint` · `pinned` · `fixed` |
| `SteelLtbIntermediateRestraintType` | `pinned` · `fixed` |
| `SteelLtbRestraintDefinitionType` | `by_number` · `by_abscissa` · `by_ratio` |
| `SteelLtbLoadApplicationPoint` | `neutral_fiber` · `upper_fiber` · `lower_fiber` · `destabilising` · `stabilising` · `imposed_value` |
| `SteelLtbLoadType` | `auto_load_type` · `column` · `simple_beam_with_uniform_loads` · `fixed_beam_with_uniform_loads` · `simple_beam_with_point_load` · `fixed_beam_with_point_load` · `another_case` |
| `SteelLtbCurveMethod` | `general_case` · `rolled_welded_profiles` · `least_favorable` · `most_favorable` · `second_generation_most_favorable` — the names describe the **1st-generation** list; see the note under this table |
| `SteelLtbCriticalMomentMode` | `auto_mcr` · `imposed_mcr` · `from_advanced_stability` |
| `SteelLtbReferenceCriticalMomentMode` | `auto_mcr0` · `imposed_mcr0` · `same_as_mcr` |
| `SteelLtbFCoefficientMode` | `auto_coefficient` · `none` · `imposed_kc` · `imposed_f` |
| `SteelLtbFactorFMMode` | `auto_factor` · `value_1` · `user_defined` |

> ⚠️ **`SteelLtbCurveMethod` is named after the 1st-generation list, and the 2nd generation reads the
> same stored integer against a different one.** The first four members spell out the option list of the
> 1st-generation Eurocode; `second_generation_most_favorable` exists only under the 2nd. Under the 2nd
> generation Eurocode 3 — which this contract does not name, and which only reaches a design template on
> an installation where the 2nd generation Eurocodes are enabled — Advance Design reads the stored
> integer against a longer list that agrees with the 1st-generation one on `general_case` **and on
> nothing else**: `least_favorable` there selects *buckling of the compression flange*, `most_favorable`
> selects *the least favourable*, and that generation's own *most favourable* is the fifth member, which
> the 1st generation does not have. Everywhere else a model carrying a 2nd generation code is refused by
> the design templates altogether ([10.6](#106-expert-design-templates)), so only the left-hand column
> below applies.
>
> | value | on a 1st-generation model | on a 2nd-generation model |
> |---|---|---|
> | `general_case` | general case, clause 6.3.2.2 | general case, clause 8.3.2.3(2) |
> | `rolled_welded_profiles` | rolled or equivalent welded profiles, clause 6.3.2.3 | doubly-symmetric I and H sections, clause 8.3.2.4(3) |
> | `least_favorable` | the least favourable is retained *(1st-generation default)* | buckling of the compression flange, clause 8.3.2.4 |
> | `most_favorable` | the most favourable is retained | the least favourable is retained *(2nd-generation default)* |
> | `second_generation_most_favorable` | — does not exist; read as the least favourable — | the most favourable is retained |
>
> Note also that Advance Design stores `least_favorable` in **every** newly created template whatever the
> generation, and never converts a template's value when the model switches generation — so a
> 2nd-generation template starts out on *buckling of the compression flange* rather than on that
> generation's own default.

> **The two critical-moment modes are two different enumerations on purpose.**
> `EC3SteelLateralTorsionalBucklingParameters.McrMode` is a `SteelLtbCriticalMomentMode` and `.Mcr0Mode` a
> `SteelLtbReferenceCriticalMomentMode`, because the third option of the two native lists is not the same
> one: Mcr can be taken from the **non-linear FEM analysis** (`from_advanced_stability`), whereas Mcr,0
> can be made **equal to Mcr** (`same_as_mcr`). Only `imposed_mcr` uses `McrValue` and only `imposed_mcr0`
> uses `Mcr0Value`; the other members compute the moment and ignore the companion value.
> `from_advanced_stability` is accepted by Advance Design **only for a cold-formed section under the
> 2nd-generation Eurocode** — the one configuration in which the non-linear FEM analysis produces the Mcr
> the check needs.

Of the enumerations tabulated above, only `SteelLtbEndRestraint` is reused by the North-American leaves —
everything else in that table belongs to the Eurocode C1/C2/C3 formulation. Their own factor is Cb:

| Enum | Values |
|---|---|
| `SteelLtbFactorCbMode` | `auto_cb` · `imposed_cb` |

**Global sway imperfections**

| Enum | Values |
|---|---|
| `SteelLocalBowImperfectionEffect` | `flexural_buckling` · `lateral_torsional_buckling` · `max_of_buckling_and_ltb` |
| `SteelBeamWeakAxisDirection` | `auto_direction` · `direction_y` · `direction_z` |
| `SteelBeamKMemberImperfectionMode` | `auto_k` · `imposed` |
| `SteelImperfectionForceDirectionPivot` | `none` · `use_points` · `use_dof_results` |

**Fire verification**

| Enum | Values |
|---|---|
| `SteelFireExposedFaces` | `four_faces` · `three_faces` |
| `SteelFireEmissivityCoeffType` | `carbon_steel` · `hot_dip_galvanised` · `imposed` |
| `SteelFireProtectionMode` | `contour_encasement` · `hollow_encasement` |

**Angle verification and web buckling**

| Enum | Values |
|---|---|
| `SteelAngleSectionLeg` | `larger_leg` · `smaller_leg` |
| `SteelAngleDiameterMode` | `auto_diameter` · `imposed` — backs `BoltNominalDiameterMode` and `BoltHoleDiameterMode` |
| `SteelAngleEffectiveSlendernessKMode` | `auto_k` · `imposed_k` — backs `EffectiveSlendernessKMode`, which used to borrow `SteelAngleDiameterMode` |
| `SteelWebStiffenersSides` | `one_side_of_web` · `both_sides_of_web` |
| `SteelWebLongitudinalStiffenersNumber` | `one_stiffener` · `two_stiffeners` |

**North-American steel specific properties and cold-formed design**

Used by `AISCSteelLinearDesignTemplate` and `CSASteelLinearDesignTemplate` only — see
[10.6](#the-north-american-sub-objects).

| Enum | Values |
|---|---|
| `SteelHssDesignThickness` | `reduced` · `nominal` — the fraction of the nominal wall thickness used as the HSS design thickness. `reduced` resolves to **0.93 under AISC** and **0.90 under CAN/CSA** |
| `SteelCFDSupportCondition` | `fastened` · `unfastened` |
| `SteelCFDFlangeCondition` | `stiffened_or_partially_stiffened` · `unstiffened` |
| `SteelCFDFlangeLoadingType` | `one_flange_loading_or_reaction` · `two_flange_loading_or_reaction` |

The last three are the three entries of the web-crippling coefficient table of AISI S100 clause G5.

**Non-linear FEM (2nd order)**

| Enum | Values |
|---|---|
| `SteelInitialDeformationShape` | `dominant_eigen_shape` · `parabolic_variation` |
| `SteelNonLinearFEMOffsetPosition` | `neutral_fiber` · `upper_fiber` · `bottom_fiber` · `imposed_value` · `indeterminate` |
| `SteelNonLinearFEMDofType` | `fully_rigid` · `auto_dof` · `hinged` · `custom` · `ty_rx_restraint` · `ty_restraint` · `fully_free` · `indeterminate` |
| `SteelNonLinearFEMDofStatus` | `auto_status` · `rigid` · `free` · `elastic` · `indeterminate` |
| `SteelNonLinearFEMLoadsOffset` | `top_left` · `top_center` · `top_right` · `center_left` · `center` · `center_right` · `bottom_left` · `bottom_center` · `bottom_right` · `user_defined` · `indeterminate` |

> `indeterminate` (value `-1`) means "never set". It is what a property that has not been initialized reads
> back as; do not send it as a value.

### `RigidSupportConstraint`
`fix` · `SupportHinge` · `other`

### `TractionOrCompressionBehaviour`
`compression` · `traction`

### `AdvancedRestraintType`
`eFree` · `eFixed` · `eElastic` · `eNLActiveForCompression` · `eNLActiveForTension` · `eNLGap` · `eNLHardening` · `eNLDiagram`

### `eCombinationType`
`eComboProjectSituationEluStrgeo` · `eComboProjectSituationEluEqu` · `eComboProjectSituationElsCharacteristic` · `eComboProjectSituationElsFrecvent` · `eComboProjectSituationElsQuasiPermanent` · `eComboProjectSituationELUA` · `eComboProjectSituationElsA` · `eComboProjectSituationNone`

### `ECombinationDesignCalculation`
The design (Expert) calculations a combination can take part in — the API replacement of the
`ExpertSelection` bit mask. Used as a list in `Combination.DesignCalculations`:
`ConcreteDesign` · `SteelProfiles` · `SteelConnections` · `SteelDeflections` · `SteelFire` · `TimberDeflections` · `TimberProfiles` · `TimberFire` · `PreCompositeDeflections` · `PostCompositeDeflections` · `PreCompositeProfiles` · `PostCompositeProfiles`

### `EConcreteCombinationType`
Limit state the reinforced concrete engine checks a combination under:
`Uls` · `UlsAccidental` · `Sls` · `None`

> Note the ordering: the timber engine uses its own limit-state enum with `Sls` and `UlsAccidental`
> **swapped**. The two are never interchangeable.

### `EConcreteLoadDuration`
Duration of the loading, drives creep, crack width and long-term deflection:
`ShortTerm` (< 1 h) · `MediumTerm` (1–24 h) · `LongTerm` (> 24 h) · `None`

### `EDesignVerificationState`
Whether a verification is actually evaluated for a combination that is listed for it — the
"Active"/"Inactive" column of the combinations dialog:
`Active` · `Inactive`

### `EDeflectionCriterion`
Deflection criterion checked by the steel and composite engines:
`FirstCriterion` · `SecondCriterion` · `BothCriteria` · `None`

> The meaning depends on the steel design code, and the API exposes the stored value one for one.
> Under **EC3** (1st and 2nd generation) all four members apply; under the 2nd generation Eurocode the
> two criteria are labelled Wmax and W3. Under **every other code** (CM66, AISC, CSA, BS, …) only the
> first three values exist and `BothCriteria` is read by the engine as "no criterion" — `None` is
> unused there.

### `ETimberCombinationType`
Limit state the timber engine checks a combination under:
`Uls` · `Sls` · `UlsAccidental`

### `ETimberLoadDuration`
Load-duration class, drives kmod and kdef:
`Permanent` (> 10 years) · `LongTerm` (6 months – 10 years) · `MediumTerm` (1 week – 6 months) · `ShortTerm` (< 1 week) · `Instantaneous`

### `ETimberDeflectionCriterion`
Deflection criterion checked by the timber engine — a different enum from `EDeflectionCriterion`:
`FirstCriterion` · `SecondCriterion` · `None`

### `ElementTypeEnum`
Used in `QueryElementsModel` to filter element queries:
`ElementBase` · `ElementLinear` · `ElementPlanar` · `ElementLoadArea` · `ElementMass` · `ElementLoadPunctual` · `ElementLoadLinear` · `ElementLoadPlanar` · `ElementImposedDisplacement` · `ElementRigidPunctualSupport` · `ElementElasticPunctualSupport` · `ElementTCPunctualSupport` · `ElementAdvancedPunctualSupport` · `ElementSinglePile` · `ElementRigidLinearSupport` · `ElementElasticLinearSupport` · `ElementTCLinearSupport` · `ElementAdvancedLinearSupport` · `ElementRigidPlanarSupport` · `ElementElasticPlanarSupport` · `ElementTCPlanarSupport` · `ElementAdvancedPlanarSupport` · `ElementPoint` · `ElementPolyLine`

### `InformationalElementTypeEnum`
Used in `QueryInfoModel` to filter informational element queries:
`InfoObjBase` · `LoadCase` · `LoadCaseFamily` · `LoadCaseFamily_Accidental` · `LoadCase_Accidental` · `LoadCaseFamily_DeadLoads` · `LoadCase_DeadLoads` · `LoadCaseFamily_DynamicTemporal` · `LoadCase_DynamicTemporal` · `LoadCaseFamily_LiveLoads` · `LoadCase_LiveLoads` · `LoadCaseFamily_Other` · `LoadCase_Other` · `LoadCaseFamily_PushOver` · `LoadCase_PushOver` · `LoadCaseFamily_Seismic` · `LoadCase_Seismic` · `LoadCaseFamily_SeismicCBN` · `LoadCase_SeismicCBN` · `LoadCaseFamily_SeismicEN_1998_1` · `LoadCase_SeismicEN_1998_1` · `LoadCaseFamily_SeismicIBC` · `LoadCase_SeismicIBC` · `LoadCaseFamily_Snow` · `LoadCase_Snow` · `LoadCaseFamily_Snow_1991_1_3` · `LoadCase_Snow_1991_1_3` · `LoadCaseFamily_SnowCBN` · `LoadCase_SnowCBN` · `LoadCaseFamily_SnowIBC` · `LoadCase_SnowIBC` · `LoadCaseFamily_Thermic` · `LoadCase_Thermic` · `LoadCaseFamily_Wind` · `LoadCase_Wind` · `LoadCaseFamily_Wind_1991_1_4` · `LoadCase_Wind_1991_1_4` · `LoadCaseFamily_WindCBN` · `LoadCase_WindCBN` · `LoadCaseFamily_WindIBC` · `LoadCase_WindIBC` · `Combination` · `LoadCaseFamily_AnalysisBuckling` · `LoadCase_Buckling` · `StructuralSystem` · `LinearReleaseTemplate` · `DesignTemplate`

`DesignTemplate` returns every [Expert design template](#106-expert-design-templates) of the model role pool, in pool order, whatever its material family and element type. The built-in default template ("None") is **not** part of the enumeration — Advance Design keeps it outside the pool — so a client only ever sees real templates here. Identify a template by its EID or its name, never by its index: deleting one shifts every entry after it.

`StructuralSystem` returns every [structural system](#104-structural-systems) of the model: the root system ("Structure 0") plus all sub-systems at any depth, empty ones included.

`LinearReleaseTemplate` returns the whole [release pool](#105-linear-release-templates) of the model, in order: the three built-in templates ("Free", "Pinned", "Fixed") followed by the user-defined ones. Only the **EIDs** are stable, not the positions: deleting a user-defined template shifts every entry after it, so identify a template by its EID or by its name — never by its index in this list.

### `PlacementType_Wind_EN1991_1_4_API`
`TERRAIN_CATEGORY_0` · `TERRAIN_CATEGORY_I` · `TERRAIN_CATEGORY_II` · `TERRAIN_CATEGORY_III` · `TERRAIN_CATEGORY_IV`

### `WindDirection_Wind_EN1991_1_4_API`
`X_PLUS` · `X_MINUS` · `Y_PLUS` · `Y_MINUS`

### `InternalPressure_Wind_EN1991_1_4_API`
`WIND_SURPRESSION` · `WIND_DEPRESSION` · `WIND_PRESSURE_COEFF`

`WIND_PRESSURE_COEFF` is obsolete (not a model option) and is applied as `WIND_SURPRESSION`.

### `LoadingsCaseType_Wind`
`NORMAL_TYPE_WIND` · `ACCIDENTAL_TYPE_WIND`

### `DetailedMeshSpacingOption`
`uniform` · `beginning` · `end` · `center` · `both_sides`

Distribution of the finite elements along a segment: `uniform` is the default; the others use
`SpacingFactor` as the secondary element size. Used by `DetailedMeshOnSegment` — see
[Sub-objects of the planar element](#sub-objects-of-the-planar-element).

### `SpectrumType_API`
`SPECTRE_ELASTIC_1` · `SPECTRE_DESIGN_1` · `SPECTRE_ELASTIC_2` · `SPECTRE_DESIGN_2`

### `ImportanceCategoryEN1998_API`
`CAT_I` · `CAT_II` · `CAT_III` · `CAT_IV`

### `DuctilityDeterminationEN1998_API`
`Q_CALCULATED` · `Q_IMPOSE`

### `DuctilityClassEN1998_API`
`LOW` · `MEDIUM` · `HIGH`

### `DirectionChoice_API` / `DirectionChoiceEN1998_API`
`X` · `Y` · `Z` · `XY`

The seismic effect takes one direction: `XY` is not available and is applied as `X`.

### `SignChoice_API`
`UNSIGNED` · `SIGNED_PREPONDERANT_MODE` · `SIGNED_SELECTED_MODE`

### `TypeStructure_API` / `TypeStructureIBC_API`
`StructureResistMomentFrame` · `StructureResistShearWall` · `StructureResistBracedFrame` · `StructureResistDualSystem` · `StructureResistOther`

### `MaterialSRFS_API`
`Concrete` · `Steel` · `Masonry` · `Timber`

### `SFRSConcrete_API`
`DuctileMomentResistingFrame` · `DuctileWallSystem` · `ModeratelyDuctileMomentResistingFrame` · `ModeratelyDuctileShearWall` · `ConventionalConstruction`

### `SFRSSteel_API`
`DuctileMomentResistingFrame` · `DuctileBracedFrame` · `ModeratelyDuctileMomentResistingFrame` · `ModeratelyDuctileBracedFrame` · `ConventionalConstruction`

### `SFRSMasonry_API` / `SFRSTimber_API`
`DuctileShearWall` · `ModeratelyDuctileShearWall` · `ConventionalConstruction`

### `SnowLoadCategory_API`
`eSnowZoneNord` · `eSnowZoneOver1000` · `eSnowZoneUnder1000` · `eSnowZoneUserDefined`

The site situation of a snow load case, which selects its EN 1990 Table A1.1 combination factors:
psi0/psi1/psi2 are 0.70 / 0.50 / 0.20 for Scandinavia and above 1000 m, and 0.50 / 0.20 / 0 at or
below 1000 m. The desktop default is `eSnowZoneUnder1000`, which is also the DTO default.

### `CalculMethod_API`
`SRSS` · `CQC` · `ABS`

### `Wind_Directions_type`
`DIRECTION_X_PLUS` · `DIRECTION_X_MINUS` · `DIRECTION_Y_PLUS` · `DIRECTION_Y_MINUS` · `GRCG_WIND_DIRECTIONS_NO`

### `eLoadCaseType`
`eADLoadCaseNone` · `eADLoadCaseGravity` · `eADLoadCaseExploitation` · `eADLoadCaseSnow` · `eADLoadCaseWind` · `eADLoadCaseThermic` · `eADLoadCaseSeismic` · `eADLoadCaseAccidental` · `eADLoadCaseTemporal` · `eADLoadCaseOther` · `eADLoadCaseEnvelope` · `eADLoadCaseBuckling` · `eADLoadCaseNonLinear` · `eADLoadCaseModal` · `eADLoadCaseCombination` · `eADLoadCaseTrafficLoads` · `eADLoadCaseSnowAcc` · `eADLoadCasePushOver` · `eADLoadCasePushOverAnalysis` · `eADLoadCaseCraneLoads` · `eADLoadCaseConstructionStagesAnalysis` · `eADLoadCaseWindAccidental`

### `LicenceProtectionType`
`NoLicence` (0) · `Demo` (1) · `Evaluation` (2) · `Education` (3) · `Commercial` (4) · `Reseller` (5) · `Internal` (6, Graitec internal) · `HomeUse` (7) · `Unknown` (-1)

The kind of licence the server runs under — the licence component's own classification, value for value;
used by [`LicenceStatus.protectionType`](#licencestatus) of `GetStatus`. It travels **by name**. `Unknown` is the one value the
licence component does not have: it stands for a number it reported that is none of the others, so a newer
licence component can never make this enumeration lie by name. Unlike `ServiceStatus.status`, it follows the
usual versioning of the API's enumerations: a new member is a change of the contract.

### Design-result enumerations

The request side of [`GetDesignResults`](#post-apimodelanalysisgetdesignresults):

**`SteelLinearDesignResultType`**
`All` · `Deflection1` · `Deflection2` · `BucklingLengths` · `LateralTorsionalBucklingLengths` · `Stability` ·
`CrossSectionResistance` · `FireResistance` · `AdvancedStabilityResistance` · `MaxWorkRatio` ·
`CrossSectionClass` · `FireCrossSectionClass` · `FireTemperature` · `WebShearBuckling` · `CrossSectionInfo` ·
`ColdFormedResistance` · `ColdFormedDeflection1` · `ColdFormedDeflection2` ·
`ColdFormedSectionInfo` · `ProfileOptimisation` · `HaunchSectionProperties` · `BucklingLengthCoefficients` · `BucklingLengthNodeRestraints` · `GlobalSwayImperfectionForces` · `LocalBowImperfectionForces`

**`SteelDesignResultSet`** — `Standard` · `ColdFormed` · `AdvancedStability`

The response side adds around twenty more, and they are documented per member in `swagger.json` rather than
repeated here: the cross-section vocabularies (`Ec3CrossSectionClass`, `Ec3ClassLoadingPattern`,
`Ec3SectionPartType`, `Ec3SectionPartPosition`, `Ec3SectionRollingType`, `Ec3CylinderLength`,
`Ec3FabricationQuality`), the check identifiers (`Ec3ResistanceCheckId`, `Ec3FireResistanceCheckId`,
`ShearBucklingVerificationType`, `Ec3GoverningCheck`, `SteelMaxWorkRatioComponent`), the deflection
vocabularies (`Ec3DeflectionVerificationId`, `Ec3DeflectionCriterion`, `DeflectionEntity`,
`SteelDeflectionLocation`, `DesignAxisDirection`), the 2nd-generation ones
(`Ec3G2VerificationId`, `Ec3G2Equation` ≈ 250 formula ids, `Ec3G2Symbol` ≈ 300 quantity ids,
`Ec3G2Message`, `DesignParameterUnitType`) and the cold-formed `Ec3ColdFormedEquation` (which EN 1993-1-3
formula a clause applied, where the clause offers alternatives; its latest members are `eq8_59`, `eq8_58` and `eq8_57`: Equation
(8.59) of EN 1993-1-3:2024, 2nd generation — total shear stress checked against the shear buckling
strength fbv instead of fya/√3; Equation (8.58) — total direct stress checked against fyb instead of fya;
Equation (8.57) — combined axial force, bending moment and shear force of I-shaped sections).

> **Buckling curves — all three of them — use `SteelBucklingCurve`.** Including the lateral-torsional one,
> on both `ResEc3Stability` and `ResEc3LateralTorsionalBucklingLengths`. A five-member lateral-torsional
> vocabulary without an a0 curve does exist inside Advance Design, but it belongs to the design template
> *setting*: the engine converts it to the six-member set before computing, and every result is stored in
> the six-member one. Reading a result through the shorter enum shifts every curve one letter along, so
> there is deliberately no separate result enum for it. `auto_curve` on the lateral-torsional curve is not
> a missing value — it is the general case of clause 6.3.2.2, where alpha,LT is computed rather than taken
> from a named curve, and Advance Design prints it as "Curve -".

> **Two naming traps, both mirroring Advance Design rather than reading naturally.**
> `Ec3ClassLoadingPattern.FlexuralBuckling` is the class used for the clause 6.3.1 check — the native suffix
> is `_F` for *flambement*, **not** *flexion*, and Advance Design labels that row "Buckling".
> `Ec3ClassLoadingPattern.ComposedBending` is biaxial bending with axial force, labelled "Oblique bending".
>
> **`Ec3G2VerificationId` numeric values are contractual.** The engine persists them as the keys of a
> serialized map whose type hash does not change when the enum is reordered, so the values are pinned
> explicitly and must not be renumbered.

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

**Note:** `ApiException` is thrown only for non-200 HTTP responses. Business-level errors (invalid geometry, missing section, etc.) are returned as `Success = false` with populated `Diagnostics`, not as exceptions. A **missing request body, malformed JSON, an unknown `$type` or an unknown enumerator** is an HTTP 400 and an `ApiException` — there is no envelope to read, and no diagnostic code. After `CloseSession`, every `api/Model/*` request is refused with **HTTP 503** (an `ApiException` too) and the code `SERVICE_STOPPING` in the first diagnostic of the response body.

### Diagnostic codes

Codes are machine-readable and stable. They follow a pattern, and the part before the underscore group names
the area (`Source` carries the same: `Management`, `Elements`, `Analysis`, `Actions`, …):

| Pattern | Severity | Meaning |
|---|---|---|
| `<NOUN>_CREATED`, `_UPDATED`, `_RETRIEVED`, `_COMPLETED` | Information | Success. Only returned with `LogVerbosity = AllDetails` |
| `<NOUN>_EMPTY_INPUT`, `<NOUN>_PARTIAL`, `<NOUN>_UPDATE_FAILED` | Warning | `Success` stays `true`: empty or partial `Data`, or an update that returned `false` |
| `<NOUN>_FAILED`, `_EXCEPTION`, `_REFUSED`, `_INVALID`, `_NULL_INPUT` | Error / Critical | The call failed |
| `ENGINE_ERROR`, `ENGINE_WARNING`, `ENGINE_INFO` | by name | A message of the Advance Design engine itself, collected during the call |

Codes a caller commonly branches on:

| Code | Raised by | When |
|---|---|---|
| `PROJECT_INVALID_FILENAME`, `PROJECT_INVALID_EXTENSION`, `PROJECT_FILE_NOT_FOUND` | `NewProject`, `OpenProject` (`PROJECT_INVALID_FILENAME` / `PROJECT_INVALID_EXTENSION` also `SaveProjectAs`) | Empty or invalid file name (for `SaveProjectAs` also a name holding the extension twice), or one containing characters not allowed in a Windows file name (a drive-relative path such as `D:m.fto` is refused too), or one with no name before the extension (`.fto`, `..fto`); not a `.fto` file; the file to open does not exist |
| `PROJECT_FOLDER_NOT_FOUND` | `NewProject`, `SaveProjectAs` | The folder that is to contain the new project does not exist; the server does not create it |
| `PROJECT_NOT_OPEN` | `SaveProject`, `SaveProjectAs` | No project is open |
| `PROJECT_SAVED` | `SaveProject` | Information. The project was saved (`data` is `true`) |
| `PROJECT_SAVE_FAILED` | `SaveProject` | The engine could not save the project (`data` is `false`); its own messages say why. The project stays open, as it was |
| `PROJECT_SAVE_EXCEPTION` | `SaveProject` | An internal error while saving (`data` is `false`) |
| `PROJECT_SAVED_AS` | `SaveProjectAs` | Information. The project was saved to the new file, which is now the session's project (`data` is `true`) |
| `PROJECT_SAVE_AS_FAILED` | `SaveProjectAs` | `data` is `false`; the message says which project the session is on. "The session's project is still ...": nothing was written (also the code for the engine-side refusals: a path over the length limit, a folder that cannot be written to), the open project is unchanged. "The session's project is now ...": the session is on a possibly partial new copy — do NOT call `SaveProject` or `CloseProject`; close it with `CloseProjectWithoutSave`, check both folders, and reopen the original |
| `PROJECT_SAVE_AS_EXCEPTION` | `SaveProjectAs` | An internal error (`data` is `false`); the message says which project the session is on, as for `PROJECT_SAVE_AS_FAILED` |
| `PROJECT_TARGET_EXISTS` | `SaveProjectAs` | The file or its project folder (the file's path without `.fto`) already exists — saving there would first empty it. Nothing is written |
| `PROJECT_TARGET_INVALID` | `SaveProjectAs` | The new project folder and the open project's folder would lie one inside the other. Nothing is written |
| `PROJECT_ENVIRONMENT_REFUSED`, `ENVIRONMENT_NO_SESSION` | `NewProject`, `OpenProject`, `GetEnvironment` | See [3.1.1](#311-the-environment-of-a-session) |
| `PROJECT_CREATION_FAILED`, `PROJECT_OPEN_FAILED` | `NewProject`, `OpenProject` | The engine could not create / open the project |
| `PROJECT_CLOSE_FAILED` | `CloseProject`, `CloseProjectWithoutSave` | The project could not be saved or closed cleanly (`data` is `false`); it is closed all the same, and the changes since the last save may be lost. The other diagnostics say why |
| `ELEMENT_DELETE_EMPTY_INPUT` | `DeleteElements` | Empty id list |
| `ELEMENTS_OBJECT_PARTIAL`, `INFORMATIONAL_ELEMENTS_PARTIAL` | `GetElementsObject`, `GetInformationalElementsObject` | An EID of the other kind was skipped; the warning names the right call |
| `ELEMENTS_QUERY_MISSING_DISCRIMINATOR`, `ELEMENTS_QUERY_TYPE_NOT_QUERYABLE` | `GetElementsID` | A query without `$type`; `ElementBase` / `InfoObjBase` used as a filter |
| `MODEL_SETTINGS_UNAVAILABLE`, `MODEL_SETTINGS_WRITE_REFUSED`, `MODEL_SETTINGS_RESET_REFUSED` | `GetModelSettings`, `SetModelSettings`, `ResetModelSettings` | See [3.7](#37-model--general-settings) |
| `ANALYSIS_COMPLETED`, `ANALYSIS_FAILED`, `ANALYSIS_EXCEPTION` | `LaunchAnalysis` | |
| `DESIGN_CALCULATION_COMPLETED`, `DESIGN_CALCULATION_FAILED`, `DESIGN_CALCULATION_EXCEPTION` | `LaunchDesignCalculation` | |
| `RESULTS_RETRIEVED`, `RESULTS_EMPTY_INPUT`, `RESULTS_QUERY_INVALID`, `RESULTS_RETRIEVAL_FAILED` | `GetResults`, `GetResultsByQuery` | |
| `DESIGN_RESULTS_RETRIEVED`, `DESIGN_RESULTS_EMPTY_INPUT`, `DESIGN_RESULTS_QUERY_INVALID`, `DESIGN_RESULTS_RETRIEVAL_FAILED` | `GetDesignResults` | |
| `ANALYSIS_MODEL_STATUS_UNAVAILABLE` | `GetAnalysisModelStatus` | No model is open |
| `SESSION_CLOSING` | `CloseSession`, `CloseSessionWithoutSave` | The call returns at once; the shutdown follows about 5 seconds later (after any running operation, the close of the open project and the licence release). The message states what the close in force does: the project is saved, or closed WITHOUT SAVING. The first close requested decides: a later `CloseSession` / `CloseSessionWithoutSave` (or a host stop) joins it, and its message says so and what that close does |
| `SERVICE_STOPPING` | any `api/Model/*` request (HTTP 503) | Sent after `CloseSession` or `CloseSessionWithoutSave`, or a host stop: the service is shutting down and refuses model requests until the process exits. `GetStatus` answers `Stopping` |

> ⚠️ The numeric and enumeration **query** parameters have no code at all when omitted: they silently take the
> default of their type — see the note at the start of [Section 3](#3-api-endpoints-reference).

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

// ULS combination. DesignCalculations is what makes the steel design below see it - and it has to be
// set before LaunchAnalysis, because the analysis model takes its own copy of the selection.
// The safety factors are left out, so the combination gets the model defaults.
client.AddCombination(new Combination
{
    ECombinationType = ECombinationType.eComboProjectSituationEluStrgeo,
    ListCasesCoeffs  = new List<EIDDoublePair>
    {
        new EIDDoublePair { Key = new EID { Value = deadId }, Value = 1.35 },
        new EIDDoublePair { Key = new EID { Value = liveId }, Value = 1.5  }
    },
    DesignCalculations = [ECombinationDesignCalculation.SteelProfiles]
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

// Expert design calculation - only legal once the analysis above has produced results
var designResult = client.LaunchDesignCalculation(DesignCalculationType.Steel, new List<long>());
foreach (var d in designResult.Details.Diagnostics)
    Console.WriteLine($"  [{d.Severity}] {d.Message}");
if (!designResult.Data)
    throw new Exception("Steel design calculation did not complete!");

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
        $"EID={el.UserID}  " +
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
        SpectrumType = SpectrumType_API.SPECTRE_DESIGN_1,
        AgRg         = 0.16,  // g
        SoilClass    = SoilClassEN1998_API.B,
        S            = 1.2,
        TbImposedValue = 0.15,
        TcImposedValue = 0.5,
        TdImposedValue = 2.0
    },
    Structure = new StructureSeismicEN1998
    {
        ImportanceCategory = ImportanceCategoryEN1998_API.CAT_II,
        CoefGammaI         = 1.0,
        CoefQHoriz         = 3.9,
        CoefQHorizY        = 3.9,
        CoefQVert          = 1.5,
        DuctilityClass     = DuctilityClassEN1998_API.MEDIUM
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
    (DirectionChoiceEN1998_API.X, SignChoice_API.SIGNED_PREPONDERANT_MODE),
    (DirectionChoiceEN1998_API.X, SignChoice_API.UNSIGNED),
    (DirectionChoiceEN1998_API.Y, SignChoice_API.SIGNED_PREPONDERANT_MODE),
    (DirectionChoiceEN1998_API.Y, SignChoice_API.UNSIGNED),
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
            DuctilityDetermination = DuctilityDeterminationEN1998_API.Q_IMPOSE,
            DuctilityCoefficient   = 1.0
        }
    });
}
```

---

> 📝 **Note:** This document was generated with the assistance of AI.

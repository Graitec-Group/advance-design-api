# Advance Design API v1 – Capabilities Summary

> **Source:** `swagger.json` (OpenAPI 3.0.4)  
> **Main tag:** `Model`  
> **Base path:** `/api/Model/`

---

## 1. Project Management

| Endpoint | Method | Description |
|---|---|---|
| `/api/Model/management/NewProject` | `POST` | Creates a new project at the specified path, with environment settings (language, localization, log verbosity) |
| `/api/Model/management/OpenProject` | `POST` | Opens an existing project |
| `/api/Model/management/CloseProject` | `POST` | Closes the current project |
| `/api/Model/management/CloseSession` | `POST` | Closes the current session and releases local API host resources (useful for parallel automation sessions) |

**Environment settings (`Environments`):**
- UI Language (`Language_Code`): `eLanguageFrench`, `eLanguageEnglish`, `eLanguageRomanian`, `eLanguageGerman`, `eLanguagePolish`, `eLanguageCzech`, `eLanguageDutch`, `eLanguageRussian`, `eLanguageSpanish`, `eLanguageEnglishUK`, `eLanguageHungarian`, `eLanguageChinese`, `eLanguageTaiwan`, `eLanguageJapanese`, `eLanguageKorean`, `eLanguageBulgarian`, `eLanguageGreek`, `eLanguageTurkish`, `eLanguageQuebec`, `eLanguageItalian`, `eLanguagePortuguese`, `eLanguagePortugueseBrazilian`, `eLanguageSlovak`, `eLanguageEndMark`
- Localization/standard (`Localization_Code`): `LOCALIZATION_ALGERIA`, `LOCALIZATION_ASIA`, `LOCALIZATION_CANADA`, `LOCALIZATION_CZECH_REPUBLIC`, `LOCALIZATION_EUROPE`, `LOCALIZATION_FRANCE`, `LOCALIZATION_GERMANY`, `LOCALIZATION_MAROCCO`, `LOCALIZATION_POLAND`, `LOCALIZATION_ROMANIA`, `LOCALIZATION_TUNISIA`, `LOCALIZATION_UK`, `LOCALIZATION_US`, `LOCALIZATION_ITALIA`, `LOCALIZATION_SLOVAKIA`, `LOCALIZATION_BULGARIA`, `LOCALIZATION_SPAIN`, `LOCALIZATION_PORTUGAL`
- Log verbosity (`LogVerboseLevel`): `Errors` / `ErrorsAndWarnings` / `AllDetails`

---

## 2. Materials

| Endpoint | Method | Description |
|---|---|---|
| `/api/Model/materials/CreateMaterial` | `POST` | Creates a new material in the current project |

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
| `/api/Model/sections/CreateSection` | `POST` | Creates a cross-section by name (e.g. `IPE400`, `HEA200`, `R20*30`) |

---

## 4. Structural Elements (geometric)

| Endpoint | Method | Description |
|---|---|---|
| `/api/Model/elements/CreateElement` | `POST` | Creates any geometric or load element |
| `/api/Model/elements/GetElementsID` | `POST` | Returns element IDs filtered by query |
| `/api/Model/elements/GetElementsObject` | `POST` | Returns the full data of elements by ID |

### 4.1 Linear Elements
- **`ElementLinear`** – column, beam, truss, cable
  - FEM type (`LinearElementFEMType_API`): `eLinearElementFEMTypeGeneral`, `eLinearElementFEMTypeCompositeBeam`
  - General beam type (`GeneralBeamType_API`): `bar`, `beamWStandardBending`, `sbeam`, `variablebeam`, `tie`, `strut`, `cable`, `rigid`, `CompositeBeamSimpleBeam`, `CompositeBeamSbeam`, `CompositeBeamVariable`
  - Properties: section orientation angle, eccentricity, inertia properties, end releases, haunch start/end, clipping, load area transfer

### 4.2 Planar Elements
- **`ElementPlanar`** – slab, wall, shell
  - Types (`PlanarElementType`): `membrane`, `plate`, `shell`, `deformation_plane`, `steeldeck`, `layeredShell`, `slabonsteeldeck`, `planar_no_of_types`
  - Variable thickness (X/Y slope), eccentricity, detailed mesh

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
| `ElementLoadArea` | Load area surface for climatic and distributed load transfer |

---

## 5. Loads

Loads are created via `CreateElement` using the following types:

| Type | Description |
|---|---|
| `ElementLoadPunctual` | Punctual force/moment (Fx, Fy, Fz, Mx, My, Mz) |
| `ElementLoadLinear` | Distributed linear load with linear variation (coeff1/coeff2) |
| `ElementLoadPlanar` | Distributed planar load with variation at 3 points |

---

## 6. Load Cases and Load Case Families

| Endpoint | Method | Description |
|---|---|---|
| `/api/Model/elements/CreateInformationalElement` | `POST` | Creates load cases, load case families, and combinations |
| `/api/Model/elements/GetInformationalElementsObject` | `POST` | Returns the full data of informational elements |

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
- Design method: `GRCG_WIND_IBC_METHOD_ENVELOPPE_LOW_RISE_BUIILDING`, torsional load cases
- Auto-generation: wind walls, pressure coefficients, split wind walls, load generation

**Snow IBC (`LoadCaseFamily_SnowIBC`):**
- Pg (ground snow load), altitude, W2 (winter wind parameter)
- Terrain category: B/C/D, Exposure: fully/partially/sheltered
- Thermal factor Ct, risk category

**Seismic IBC (`LoadCaseFamily_SeismicIBC`):**
- Ss, S1, site class (A–F), occupancy category (I–IV)
- Factors Fa, Fv, transition periods T0, Ts, TL
- Combination methods: SRSS, CQC, ABS + residual mode

**Wind EN 1991-1-4:**
- Wind speed, pressure, cDir, cSeas, structure base height
- Terrain category (0–IV), z0, Kr, Cs×Cd (auto/imposed)
- Dynamic parameters: delta, N, phi, correlation coefficient Kdc, turbulence KL
- Greenhouse-specific parameters (EN 13031-1)

**Seismic EN 1998-1:**
- Spectrum type (elastic/design type 1/2), ag×?I, soil class (A–E)
- Periods Tb, Tc, Td, topographic amplification ST
- Ductility factor q (calculated or imposed), ductility class (LOW/MEDIUM/HIGH)

### 6.3 Combinations

| Endpoint | Method | Description |
|---|---|---|
| `/api/Model/combinations/AddCombination` | `POST` | Creates a load combination with coefficients |

**`eCombinationType` values:** `eComboProjectSituationEluStrgeo`, `eComboProjectSituationEluEqu`, `eComboProjectSituationElsCharacteristic`, `eComboProjectSituationElsFrecvent`, `eComboProjectSituationElsQuasiPermanent`, `eComboProjectSituationELUA`, `eComboProjectSituationElsA`, `eComboProjectSituationNone`

> **Note:** `Combination.idComb` is **read-only** (managed internally by Advance Design). It should only be used when reading combinations, not when creating them.

---

## 7. Actions / Processing

| Endpoint | Method | Description |
|---|---|---|
| `/api/Model/actions/ProcessAction` | `POST` | Triggers actions on the model |
| `/api/Model/analysis/LaunchAnalysis` | `POST` | Launches the FEM calculation of the model |

**Available actions (`AD_API_ActionType`):**
- `ClimaticAutoGeneration` – automatic generation of climatic loads (wind + snow) for existing families

---

## 8. FEM Analysis Results

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

## 9. Auxiliary

| Endpoint | Method | Description |
|---|---|---|
| `/api/Model/elements/ExportAdditionalEnums` | `POST` | Exports auxiliary enums not directly used in model classes (`Wind_Directions_type`, `eLoadCaseType`) |

**`Wind_Directions_type`:** `DIRECTION_X_PLUS`, `DIRECTION_X_MINUS`, `DIRECTION_Y_PLUS`, `DIRECTION_Y_MINUS`, `GRCG_WIND_DIRECTIONS_NO`

**`eLoadCaseType`:** `eADLoadCaseNone`, `eADLoadCaseGravity`, `eADLoadCaseExploitation`, `eADLoadCaseSnow`, `eADLoadCaseWind`, `eADLoadCaseThermic`, `eADLoadCaseSeismic`, `eADLoadCaseAccidental`, `eADLoadCaseTemporal`, `eADLoadCaseOther`, `eADLoadCaseEnvelope`, `eADLoadCaseBuckling`, `eADLoadCaseNonLinear`, `eADLoadCaseModal`, `eADLoadCaseCombination`, `eADLoadCaseTrafficLoads`, `eADLoadCaseSnowAcc`, `eADLoadCasePushOver`, `eADLoadCasePushOverAnalysis`, `eADLoadCaseCraneLoads`, `eADLoadCaseConstructionStagesAnalysis`, `eADLoadCaseWindAccidental`

---

> 📝 **Note:** This document was generated with the assistance of AI.

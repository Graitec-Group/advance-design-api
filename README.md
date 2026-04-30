# Advance Design API

[**GRAITEC Advance Design**]([https://www.graitec.com/advance-design/](https://graitec.com/us/products/advance-design/)) is a professional structural analysis and design software for engineers, covering the full BIM workflow — from modelling and loading to finite-element analysis, code-based design checks and documentation.

The **Advance Design API** exposes a REST/HTTP interface that allows external applications (add-ins, automation scripts, test harnesses) to drive Advance Design programmatically:

- Open, create and close Advance Design projects.
- Populate the structural model with materials, sections, linear/planar elements and supports.
- Define load cases, load case families, climatic loads (wind, snow, seismic) and combinations.
- Trigger analysis and post-process finite-element results (displacements, forces, stresses).
- Query element IDs and retrieve fully deserialised element objects.

---

## Available Versions

| Version | Description | Link |
|---|---|---|
| **2027 BETA** | OpenAPI schema, documentation, C# samples and Python client | [v2027/README.md](v2027/README.md) |

---

## License & Legal

This repository is released under the [MIT License](LICENSE).  
Please also read the [Legal Notice](LEGAL_NOTICE.md) before using any materials from this repository.

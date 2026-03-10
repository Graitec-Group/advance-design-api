# Advance Design API

The **Advance Design API** exposes a REST/HTTP interface that allows external C# applications (add-ins, automation scripts, test harnesses) to:

- Open, create and close Advance Design projects.
- Populate the structural model with materials, sections, linear/planar elements and supports.
- Define load cases, load case families, climatic loads (wind, snow, seismic) and combinations.
- Trigger analysis and post-process finite-element results (displacements, forces, stresses).
- Query element IDs and retrieve fully deserialised element objects.

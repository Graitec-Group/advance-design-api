using AD.API.Client;

namespace Test.API.Client
{
  /// <summary>
  /// One runnable sample, as shown both in the console menu and in the GUI runner.
  /// </summary>
  /// <param name="Number">1-based number, identical to the console menu numbering.</param>
  /// <param name="Label">Menu label.</param>
  /// <param name="Run">Sample entry point.</param>
  /// <param name="HasVerification">
  /// True when the sample records explicit verifications through <see cref="SampleReport"/>.
  /// Samples that only create a model and print information are marked false; they can only end
  /// up as <see cref="SampleOutcome.CompletedNoChecks"/> (or Error/Failed on a fatal problem).
  /// </param>
  internal sealed record SampleDefinition(int Number, string Label, Action<AD_Client> Run, bool HasVerification);

  internal partial class Program
  {
    /// <summary>
    /// Single source of truth for the sample list, shared by the console menu and by the GUI
    /// runner, so the numbering shown in both is always the same.
    /// </summary>
    internal static IReadOnlyList<SampleDefinition> Samples { get; } = BuildSampleCatalog();

    private static IReadOnlyList<SampleDefinition> BuildSampleCatalog()
    {
      var entries = new (string Label, Action<AD_Client> Run, bool HasVerification)[]
      {
        ("Analysis results(on column): Imposed displacement structure with result verification",             c => Sample_ImposedDisplacementVerificationOnColumn(c), true),
        ("Analysis results(on supports and beam ): Portal frame 3 elements analysis verification (5% tol)",              c => Sample_PortalFrame3ElementsAnalysisVerificationOnBeamAndSupports(c), true),
        ("Analysis results(on supports): 4-column slab support with Fz applied on it, verification sum of Fz in supports(Europe, 1% tol)",        c => Sample_SlabOn4ColumnsSupportFzVerificationItReachesSupports(c), true),
        ("Analysis results(on slab): Slab with rigid linear supports, analysis and all results on slab", c => Sample_PlanarSlabRigidLinearSupportsFrance(c), true),

        ("Climatic: 3D building with wind (EN 1991-1-4) and snow (EN 1991-1-3) auto generation (France)", c => Sample_WindSnow3DBuildingAutoGeneration(c), true),
        ("Climatic: Portal 2D Structure and IBC (ASCE 7-22) Climatic Generation",          c => Sample_Portal2DStructureAndCLimaticGeneration(c), false),
        ("Climatic: Portal 2D Structure and CBN (NBC Canada) Climatic Generation",          c => Sample_Portal2DStructureAndCLimaticGeneration_CanadaCBN(c), false),
        ("Climatic: Portal 2D Structure and IBC (ASCE 7-22) Climatic Generation (multiple roof elements)", c => Sample_Portal2DStructureAndCLimaticGeneration_wMultipleElementsInTheRoof(c), false),
        ("Climatic: Portal 2D with Protruding Roof / Overhangs and IBC (ASCE 7-22) Climatic Generation", c => Sample_Portal2DwithProtrudingRoofOrOverhangsandClimaticGeneration(c), false),

        ("Intentional error testing: Structure with intentional material error",                           c => Sample_With_IntentionalMaterialError(c), false),
        ("Properties setting and model management: Custom properties set and verification after reopen",                 c => Sample_CustomPropertiesVerificationOnReopen(c), true),
        ("Planar Wall With Linear Support And Results",                                                    c => Sample_PlanarWallWithLinearSupportAndResults(c), true),
        ("Planar Support With Planar Element And Results",                                                 c => Sample_PlanarSupportWithPlanarElementAndResults(c), true),
        ("Pylon climatic                                ",                                                 c => Sample_PylonElectricityStructureAndClimaticGeneration(c), true),
        ("Load cases: Create multiple load case family types and display UserID for each case",          c => Sample_LoadCaseFamilyTypesUserIdDisplay(c), true),
        ("Generalized buckling: Open existing model, list buckling load cases and results",             c => Sample_GeneralizedBucklingExistingModelResults(c), false),
        ("Delete elements: Open existing model, delete timber/steel columns + their supports + the Fz=-30kN punctual force, then verify analysis", c => Sample_DeleteTimberAndSteelColumnsWithSupports(c), true),
        ("Planar Wall With Openings (holes test)",                                                       c => Sample_PlanarWallWithOpenings(c), true),
        ("Materials and Sections retrieval: GetListMaterials, GetListSections, GetMaterials, GetSections", c => Sample_MaterialAndSectionRetrieval(c), false),
        ("UpdateElement verification: Update linear + planar element properties and verify",    c => Sample_UpdateElementVerification(c), true),
        ("Analysis results(on linear elements): Open any model given through AD_MODEL_FILE or the prompt (no default), run FEM and print the nodal displacements/rotations (D, Dx, Dy, Dz, Rx, Ry, Rz, R) of every node of every linear element", c => Sample_LinearElementsNodalDisplacements(c), false),
      };

      var catalog = new SampleDefinition[entries.Length];
      for (int i = 0; i < entries.Length; i++)
        catalog[i] = new SampleDefinition(i + 1, entries[i].Label, entries[i].Run, entries[i].HasVerification);

      return catalog;
    }
  }
}

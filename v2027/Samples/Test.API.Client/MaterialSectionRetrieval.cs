using AD.API.Client;

namespace Test.API.Client
{
  internal partial class Program
  {
    /// <summary>
    /// Material and Section retrieval sample:
    /// - Creates a project with two materials (S235, C25/30) and one section (IPE400).
    /// - Creates a linear element referencing those materials and section.
    /// - Calls GetListMaterials to retrieve all material IDs in the project.
    /// - Calls GetListSections to retrieve all section IDs in the project.
    /// - Calls GetMaterials to retrieve full Material objects by IDs.
    /// - Calls GetSections to retrieve full Section objects by IDs.
    /// - Prints the retrieved data to the console.
    /// </summary>
    static void Sample_MaterialAndSectionRetrieval(AD.API.Client.AD_Client client)
    {
      LogVerboseLevel logVerbosityLevel = LogVerboseLevel.AllDetails;

      Environments env = new Environments()
      {
        Language = Language_Code.ELanguageEnglish,
        Localization = Localization_Code.LOCALIZATION_FRANCE,
        LogVerbosity = logVerbosityLevel
      };

      string projectPath = GetEnvironmentVariable("AD_MODELS_PATH")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                        "Graitec", "Advance Design", "2027", "Projects")
           + Path.DirectorySeparatorChar;

      if (string.IsNullOrEmpty(projectPath))
      {
        SampleReport.Fatal("ERROR: Environment variable 'AD_MODELS_PATH' is not set. Aborting.");
        Console.WriteLine("ERROR: Environment variable 'AD_MODELS_PATH' is not set. Aborting.");
        return;
      }

      // ── Create project ────────────────────────────────────────────────────────
      var respNewProj = client.NewProject(projectPath + "MaterialSectionRetrieval_" + Guid.NewGuid().ToString() + ".fto", env);
      PrintResultDetails(respNewProj.Details, logVerbosityLevel, "NewProject");
      if (!respNewProj.Details.Success || respNewProj.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: NewProject failed. Aborting.");
        Console.WriteLine("ERROR: NewProject failed. Aborting.");
        return;
      }

      // ── Create materials ──────────────────────────────────────────────────────
      var respMaterialSteel = client.CreateMaterial(new Material { Name = "S235" });
      PrintResultDetails(respMaterialSteel.Details, logVerbosityLevel, "CreateMaterial S235");
      if (!respMaterialSteel.Details.Success || respMaterialSteel.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: CreateMaterial S235 failed. Aborting.");
        Console.WriteLine("ERROR: CreateMaterial S235 failed. Aborting.");
        client.CloseProject();
        return;
      }
      Console.WriteLine($"  Steel material ID: {respMaterialSteel.Data.Value}");

      var respMaterialConcrete = client.CreateMaterial(new Material { Name = "C25/30" });
      PrintResultDetails(respMaterialConcrete.Details, logVerbosityLevel, "CreateMaterial C25/30");
      if (!respMaterialConcrete.Details.Success || respMaterialConcrete.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: CreateMaterial C25/30 failed. Aborting.");
        Console.WriteLine("ERROR: CreateMaterial C25/30 failed. Aborting.");
        client.CloseProject();
        return;
      }
      Console.WriteLine($"  Concrete material ID: {respMaterialConcrete.Data.Value}");

      // ── Create section ────────────────────────────────────────────────────────
      var respSection = client.CreateSection("IPE400");
      PrintResultDetails(respSection.Details, logVerbosityLevel, "CreateSection IPE400");
      if (!respSection.Details.Success || respSection.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: CreateSection IPE400 failed. Aborting.");
        Console.WriteLine("ERROR: CreateSection IPE400 failed. Aborting.");
        client.CloseProject();
        return;
      }
      Console.WriteLine($"  Section ID: {respSection.Data.Value}");

      // ── Create a linear element to ensure material/section are in use ─────────
      var respElement = client.CreateElement(new ElementLinear
      {
        Section = respSection.Data,
        Material = respMaterialSteel.Data,
        GeomPtStart = new Pt3D { X = 0.0, Y = 0.0, Z = 0.0 },
        GeomPtEnd = new Pt3D { X = 0.0, Y = 0.0, Z = 5.0 }
      });
      PrintResultDetails(respElement.Details, logVerbosityLevel, "CreateElement (column)");

      // ════════════════════════════════════════════════════════════════════════════
      // Retrieve materials and sections via the new API endpoints
      // ════════════════════════════════════════════════════════════════════════════

      Console.WriteLine();
      Console.WriteLine("═══ GetListMaterials ═══");
      var respListMaterials = client.GetListMaterials();
      PrintResultDetails(respListMaterials.Details, logVerbosityLevel, "GetListMaterials");
      if (respListMaterials.Data != null)
      {
        Console.WriteLine($"  Total material IDs: {respListMaterials.Data.Count}");
        foreach (var id in respListMaterials.Data)
          Console.WriteLine($"    ID: {id}");
      }

      Console.WriteLine();
      Console.WriteLine("═══ GetListSections ═══");
      var respListSections = client.GetListSections();
      PrintResultDetails(respListSections.Details, logVerbosityLevel, "GetListSections");
      if (respListSections.Data != null)
      {
        Console.WriteLine($"  Total section IDs: {respListSections.Data.Count}");
        foreach (var id in respListSections.Data)
          Console.WriteLine($"    ID: {id}");
      }

      Console.WriteLine();
      Console.WriteLine("═══ GetMaterials (by IDs) ═══");
      if (respListMaterials.Data != null && respListMaterials.Data.Count > 0)
      {
        var respMaterials = client.GetMaterials(respListMaterials.Data.ToList());
        PrintResultDetails(respMaterials.Details, logVerbosityLevel, "GetMaterials");
        if (respMaterials.Data != null)
        {
          Console.WriteLine($"  Retrieved {respMaterials.Data.Count} material(s):");
          foreach (var mat in respMaterials.Data)
            Console.WriteLine($"    Material: Name=\"{mat.Name}\"");
        }
      }
      else
      {
        Console.WriteLine("  (no material IDs to query)");
      }

      Console.WriteLine();
      Console.WriteLine("═══ GetSections (by IDs) ═══");
      if (respListSections.Data != null && respListSections.Data.Count > 0)
      {
        var respSections = client.GetSections(respListSections.Data.ToList());
        PrintResultDetails(respSections.Details, logVerbosityLevel, "GetSections");
        if (respSections.Data != null)
        {
          Console.WriteLine($"  Retrieved {respSections.Data.Count} section(s):");
          foreach (var sec in respSections.Data)
            Console.WriteLine($"    Section: Name=\"{sec.Name}\", Type={sec.Type}, Family=\"{sec.FamilyCode}\", Catalog=\"{sec.CatalogName}\"");
        }
      }
      else
      {
        Console.WriteLine("  (no section IDs to query)");
      }

      // ── Cleanup ───────────────────────────────────────────────────────────────
      Console.WriteLine();
      Console.WriteLine("Sample completed successfully.");
      client.CloseProject();
    }
  }
}

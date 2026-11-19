using AD.API.Client;

namespace Test.API.Client
{
  internal partial class Program
  {
    /// <summary>
    /// Opens an existing model and deletes a selected set of objects through the DeleteElement API:
    ///   - every linear element whose material is timber "C24" or steel "S235",
    ///   - the punctual supports located under those linear elements (matched by coordinates),
    ///   - the punctual force whose Fz is -30 kN.
    /// Then it runs the analysis and verifies that the (now reduced) model still computes OK.
    ///
    /// Element objects do not expose their internal EID, so every object is fetched one ID at a
    /// time (GetElementsObject with a single id) to keep the EID ↔ object mapping unambiguous -
    /// this matters because the result drives DeleteElement.
    /// The model is modified in memory but never saved.
    /// </summary>
    static void Sample_DeleteTimberAndSteelColumnsWithSupports(AD.API.Client.AD_Client client)
    {
      const string defaultModelFile = @"C:\Advance Design 22.1\Projects\DeletedObjects.fto";
      string modelFile = PromptForModelFile(defaultModelFile);

      // Linear elements carrying one of these materials (by name) are deleted.
      string[] targetMaterialNames = { "C24", "S235" };

      // Punctual force to delete, identified by its vertical component Fz = -30 kN (API forces are in N).
      const double targetFz = -30_000.0;
      const double fzToleranceN = 1.0;        // |Fz - targetFz| below this counts as a match
      const double pointToleranceM = 1.0e-4;  // coordinate match tolerance for "support under element"

      LogVerboseLevel logVerbosityLevel = LogVerboseLevel.ErrorsAndWarnings;

      const int separatorWidth = 72;
      string majorRule = new string('=', separatorWidth);
      string minorRule = new string('-', separatorWidth);

      void WriteBanner(string title)
      {
        Console.WriteLine();
        Console.WriteLine(majorRule);
        Console.WriteLine("  " + title);
        Console.WriteLine(majorRule);
      }

      WriteBanner("Delete timber/steel columns + their supports + a punctual force");
      Console.WriteLine($"  Model            : {modelFile}");
      Console.WriteLine($"  Target materials : {string.Join(", ", targetMaterialNames)}");
      Console.WriteLine($"  Target force Fz  : {targetFz} N (-30 kN)");
      Console.WriteLine();

      if (!File.Exists(modelFile))
      {
        SampleReport.Fatal($"ERROR: Model file not found: {modelFile}");
        Console.WriteLine($"ERROR: Model file not found: {modelFile}");
        return;
      }

      // Reflection-free helpers reused below ------------------------------------------------

      // Fetch a single element object by EID (one id in -> one object out), keeping the mapping exact.
      ElementBase? GetSingleObject(long eid)
      {
        var resp = client.GetElementsObject(new List<long> { eid });
        if (!resp.Details.Success || resp.Details.HasErrors)
          PrintResultDetails(resp.Details, logVerbosityLevel, $"GetElementsObject (EID={eid})");
        return resp.Data?.FirstOrDefault();
      }

      // Material name cache so each material EID is resolved at most once.
      var materialNameByEid = new Dictionary<long, string>();
      string MaterialName(long materialEid)
      {
        if (materialNameByEid.TryGetValue(materialEid, out var cached))
          return cached;

        string name = "";
        var resp = client.GetMaterials(new List<long> { materialEid });
        if (resp.Details.Success && !resp.Details.HasErrors)
          name = resp.Data?.FirstOrDefault()?.Name ?? "";
        else
          PrintResultDetails(resp.Details, logVerbosityLevel, $"GetMaterials (EID={materialEid})");

        materialNameByEid[materialEid] = name;
        return name;
      }

      bool IsTargetMaterial(string materialName) =>
        !string.IsNullOrWhiteSpace(materialName) &&
        targetMaterialNames.Any(n => string.Equals(n, materialName.Trim(), StringComparison.OrdinalIgnoreCase));

      bool SamePoint(Pt3D? a, Pt3D? b) =>
        a != null && b != null &&
        Math.Abs(a.X - b.X) <= pointToleranceM &&
        Math.Abs(a.Y - b.Y) <= pointToleranceM &&
        Math.Abs(a.Z - b.Z) <= pointToleranceM;

      // The GeomPt of any punctual support / single pile (all expose a "GeomPt" Pt3D).
      Pt3D? SupportPoint(ElementBase? obj) => obj switch
      {
        FamilySupportPunctual sup => sup.GeomPt,
        ElementSinglePile pile => pile.GeomPt,
        _ => null
      };

      // Query all element EIDs of a given type.
      List<long> QueryIds(ElementTypeEnum type)
      {
        var resp = client.GetElementsID(new List<QueryBase> { new QueryElementsModel { ElementType = type } });
        if (!resp.Details.Success || resp.Details.HasErrors)
          PrintResultDetails(resp.Details, logVerbosityLevel, $"GetElementsID ({type})");
        return resp.Data?.ToList() ?? new List<long>();
      }

      Environments env = new Environments
      {
        Localization = Localization_Code.LOCALIZATION_EUROPE,
        LogVerbosity = logVerbosityLevel
      };

      Console.WriteLine("[1/6] Opening project ...");
      var respOpen = client.OpenProject(modelFile, env);
      PrintResultDetails(respOpen.Details, logVerbosityLevel, "OpenProject");
      if (!respOpen.Details.Success || respOpen.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: OpenProject failed. Aborting sample.");
        Console.WriteLine("ERROR: OpenProject failed. Aborting sample.");
        return;
      }

      try
      {
        // -----------------------------------------------------------------
        // [2/6] Linear elements with a target material (timber C24 / steel S235)
        // -----------------------------------------------------------------
        Console.WriteLine("[2/6] Selecting linear elements by material ...");
        var linearIds = QueryIds(ElementTypeEnum.ElementLinear);
        Console.WriteLine($"  Linear elements in model: {linearIds.Count}");

        var targetBeamIds = new List<long>();
        var targetEndpoints = new List<Pt3D>();   // endpoints of the selected beams, used to find their supports
        foreach (long id in linearIds)
        {
          if (GetSingleObject(id) is not ElementLinear beam)
            continue;

          // Material is optional on the wire (Required.DisallowNull + NullValueHandling.Ignore),
          // so a linear element may come back without one - it cannot be a material target then.
          if (beam.Material == null)
          {
            Console.WriteLine($"    EID={id,-6} (no material)");
            continue;
          }

          string matName = MaterialName(beam.Material.Value);
          bool isTarget = IsTargetMaterial(matName);
          Console.WriteLine($"    EID={id,-6} material=\"{matName}\"{(isTarget ? "  -> DELETE" : "")}");
          if (!isTarget)
            continue;

          targetBeamIds.Add(id);
          if (beam.GeomPtStart != null) targetEndpoints.Add(beam.GeomPtStart);
          if (beam.GeomPtEnd != null) targetEndpoints.Add(beam.GeomPtEnd);
        }
        Console.WriteLine($"  Linear elements selected for deletion: {targetBeamIds.Count}");

        // -----------------------------------------------------------------
        // [3/6] Punctual supports located under the selected linear elements
        // -----------------------------------------------------------------
        Console.WriteLine("[3/6] Selecting supports under those elements ...");
        var punctualSupportTypes = new[]
        {
          ElementTypeEnum.ElementRigidPunctualSupport,
          ElementTypeEnum.ElementElasticPunctualSupport,
          ElementTypeEnum.ElementTCPunctualSupport,
          ElementTypeEnum.ElementAdvancedPunctualSupport,
          ElementTypeEnum.ElementSinglePile
        };

        var targetSupportIds = new List<long>();
        foreach (var supportType in punctualSupportTypes)
        {
          foreach (long id in QueryIds(supportType))
          {
            var pt = SupportPoint(GetSingleObject(id));
            if (pt == null)
              continue;

            if (targetEndpoints.Any(ep => SamePoint(ep, pt)))
            {
              targetSupportIds.Add(id);
              Console.WriteLine($"    {supportType} EID={id} at ({pt.X:F3}, {pt.Y:F3}, {pt.Z:F3})  -> DELETE");
            }
          }
        }
        Console.WriteLine($"  Supports selected for deletion: {targetSupportIds.Count}");

        // -----------------------------------------------------------------
        // [4/6] Punctual force with Fz = -30 kN (searched across all load cases)
        // -----------------------------------------------------------------
        Console.WriteLine("[4/6] Selecting the punctual force Fz = -30 kN ...");
        var respCaseIds = client.GetElementsID(new List<QueryBase>
        {
          new QueryInfoModel { InformationalElementType = InformationalElementTypeEnum.LoadCase }
        });
        PrintResultDetails(respCaseIds.Details, logVerbosityLevel, "GetElementsID (all load cases)");
        var loadCaseIds = respCaseIds.Data?.ToList() ?? new List<long>();

        var targetForceIds = new List<long>();
        foreach (long caseId in loadCaseIds)
        {
          var respLoadIds = client.GetElementsID(new List<QueryBase>
          {
            new QueryElementsLoads { ElementType = ElementTypeEnum.ElementLoadPunctual, LoadCaseId = new EID { Value = caseId } }
          });
          if (!respLoadIds.Details.Success || respLoadIds.Details.HasErrors)
          {
            PrintResultDetails(respLoadIds.Details, logVerbosityLevel, $"GetElementsID (punctual loads, case {caseId})");
            continue;
          }

          foreach (long loadId in respLoadIds.Data ?? new List<long>())
          {
            if (GetSingleObject(loadId) is not ElementLoadPunctual load)
              continue;

            if (Math.Abs(load.Fz - targetFz) <= fzToleranceN)
            {
              targetForceIds.Add(loadId);
              Console.WriteLine($"    Punctual load EID={loadId} (case {caseId}) Fz={load.Fz} N  -> DELETE");
            }
          }
        }
        Console.WriteLine($"  Punctual forces selected for deletion: {targetForceIds.Count}");

        // -----------------------------------------------------------------
        // [5/6] Delete everything in a SINGLE DeleteElements call.
        //       Order in the list: force(s) first, then supports, then beams.
        // -----------------------------------------------------------------
        Console.WriteLine("[5/6] Deleting selected objects (single DeleteElements call) ...");
        var idsToDelete = new List<long>();
        idsToDelete.AddRange(targetForceIds);
        idsToDelete.AddRange(targetSupportIds);
        idsToDelete.AddRange(targetBeamIds);

        Console.WriteLine($"  Deleting {idsToDelete.Count} object(s) in one call: " +
                          $"{targetForceIds.Count} force(s), {targetSupportIds.Count} support(s), {targetBeamIds.Count} linear element(s).");

        if (idsToDelete.Count == 0)
        {
          Console.WriteLine("  Nothing matched the selection; skipping delete.");
        }
        else
        {
          var respDelete = client.DeleteElements(idsToDelete);
          PrintResultDetails(respDelete.Details, logVerbosityLevel, "DeleteElements");

          bool allDeleted = respDelete.Details.Success && !respDelete.Details.HasErrors && respDelete.Data;
          SampleReport.Check(allDeleted, "DeleteElements", "not every object was reported as deleted");
          if (allDeleted)
          {
            Console.WriteLine($"  All {idsToDelete.Count} object(s) deleted.");
          }
          else
          {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  WARNING: DeleteElements reported that not every object was deleted; analysis will still be attempted.");
            Console.WriteLine($"    Diagnostics: {DiagText(respDelete.Details.Diagnostics)}");
            Console.ForegroundColor = ConsoleColor.White;
          }
        }

        // -----------------------------------------------------------------
        // [6/6] Run the analysis and verify it computed OK
        // -----------------------------------------------------------------
        Console.WriteLine("[6/6] Running analysis on the reduced model ...");
        var respCalc = client.LaunchAnalysis();
        PrintResultDetails(respCalc.Details, logVerbosityLevel, "LaunchAnalysis");

        bool analysisOk = respCalc.Details.Success && !respCalc.Details.HasErrors && respCalc.Data;

        Console.WriteLine();
        Console.WriteLine(majorRule);
        if (analysisOk)
        {
          SampleReport.Pass("Analysis after deletions");
          Console.ForegroundColor = ConsoleColor.Green;
          Console.WriteLine("  VERIFICATION PASSED: analysis completed successfully after the deletions.");
        }
        else
        {
          SampleReport.Fail("Analysis after deletions", "the analysis did not complete successfully");
          Console.ForegroundColor = ConsoleColor.Red;
          Console.WriteLine("  VERIFICATION FAILED: analysis did not complete successfully.");
          Console.WriteLine($"    Success={respCalc.Details.Success}, HasErrors={respCalc.Details.HasErrors}, Data={respCalc.Data}");
          Console.WriteLine($"    Diagnostics: {DiagText(respCalc.Details.Diagnostics)}");
        }
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(majorRule);
      }
      finally
      {
        // Model modified in memory only - close without saving.
        var respClose = client.CloseProject();
        PrintResultDetails(respClose.Details, logVerbosityLevel, "CloseProject");
      }
    }
  }
}

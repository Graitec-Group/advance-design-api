using AD.API.Client;

namespace Test.API.Client
{
  internal partial class Program
  {
    /// <summary>
    /// Structure with imposed displacement and result verification:
    /// - 3 linear elements (C25/30, R20*30): 2 vertical columns and 1 horizontal beam forming a portal frame.
    ///   Element 1: column (0,0,0)→(0,0,5), Element 2: column (10,0,0)→(10,0,5), Element 3: beam (0,0,5)→(10,0,5)
    /// - 2 rigid point supports at (0,0,0) and (10,0,0)
    /// - 1 live loads family with 1 live load case containing an imposed displacement of -3cm DZ at (10,0,0)
    /// - Runs linear analysis and verifies that the displacement on element 2 (column at x=10) is ~3cm.
    /// </summary>
    static void Sample_ImposedDisplacementVerificationOnColumn(AD.API.Client.AD_Client client)
    {
      LogVerboseLevel logVerbosityLevel = LogVerboseLevel.ErrorsAndWarnings;

      Environments env = new Environments()
      {
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

      // --- Create project ---
      var newProj = client.NewProject("\\\\?\\" + projectPath + "ImposedDisplacement" + Guid.NewGuid().ToString() + ".fto", env);
      PrintResultDetails(newProj.Details, logVerbosityLevel, "Create new project");
      if (!newProj.Details.Success || newProj.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: NewProject failed. Aborting.");
        Console.WriteLine("ERROR: NewProject failed. Aborting.");
        return;
      }

      // --- Create material C25/30 ---
      var respMaterial = client.CreateMaterial(new Material { Name = "C25/30" });
      PrintResultDetails(respMaterial.Details, logVerbosityLevel, "Create material C25/30");
      if (!respMaterial.Details.Success || respMaterial.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: CreateMaterial C25/30 failed. Aborting.");
        Console.WriteLine("ERROR: CreateMaterial C25/30 failed. Aborting.");
        client.CloseProject();
        return;
      }
      EID idMaterial = respMaterial.Data;
      Console.WriteLine("  Material C25/30 OID: " + idMaterial.Value);

      // --- Create section R20*30 ---
      var respSection = client.CreateSection("R20*30");
      PrintResultDetails(respSection.Details, logVerbosityLevel, "Create section R20*30");
      if (!respSection.Details.Success || respSection.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: CreateSection R20*30 failed. Aborting.");
        Console.WriteLine("ERROR: CreateSection R20*30 failed. Aborting.");
        client.CloseProject();
        return;
      }
      EID idSection = respSection.Data;
      Console.WriteLine("  Section R20*30 OID: " + idSection.Value);

      // --- Create 3 linear elements ---

      // Element 1: column at x=0, (0,0,0) → (0,0,5)
      EID idElement1;
      {
        var el = new ElementLinear
        {
          Section  = idSection,
          Material = idMaterial,
          GeomPtStart = new Pt3D { X = 0.0, Y = 0.0, Z = 0.0 },
          GeomPtEnd   = new Pt3D { X = 0.0, Y = 0.0, Z = 5.0 }
        };
        var resp = client.CreateElement(el);
        PrintResultDetails(resp.Details, logVerbosityLevel, "Create element 1 (column x=0, length=5m)");
        idElement1 = resp.Data;
        Console.WriteLine("  Element 1 OID: " + idElement1.Value);
      }

      // Element 2: column at x=10, (10,0,0) → (10,0,5) — in contact with imposed displacement
      EID idElement2;
      {
        var el = new ElementLinear
        {
          Section  = idSection,
          Material = idMaterial,
          GeomPtStart = new Pt3D { X = 10.0, Y = 0.0, Z = 0.0 },
          GeomPtEnd   = new Pt3D { X = 10.0, Y = 0.0, Z = 5.0 }
        };
        var resp = client.CreateElement(el);
        PrintResultDetails(resp.Details, logVerbosityLevel, "Create element 2 (column x=10, length=5m)");
        idElement2 = resp.Data;
        Console.WriteLine("  Element 2 OID: " + idElement2.Value);
      }

      // Element 3: horizontal beam (0,0,5) → (10,0,5), length=10m
      EID idElement3;
      {
        var el = new ElementLinear
        {
          Section  = idSection,
          Material = idMaterial,
          GeomPtStart = new Pt3D { X =  0.0, Y = 0.0, Z = 5.0 },
          GeomPtEnd   = new Pt3D { X = 10.0, Y = 0.0, Z = 5.0 }
        };
        var resp = client.CreateElement(el);
        PrintResultDetails(resp.Details, logVerbosityLevel, "Create element 3 (beam z=5, length=10m)");
        idElement3 = resp.Data;
        Console.WriteLine("  Element 3 OID: " + idElement3.Value);
      }

      // --- Create 2 rigid point supports ---

      // Support 1 at (0,0,0) — all DOFs enabled
      {
        var support = new ElementRigidPunctualSupport
        {
          GeomPt   = new Pt3D { X = 0.0, Y = 0.0, Z = 0.0 },
          Material = idMaterial
        };
        var resp = client.CreateElement(support);
        PrintResultDetails(resp.Details, logVerbosityLevel, "Create rigid support 1 at (0,0,0)");
        Console.WriteLine("  Support 1 OID: " + resp.Data.Value);
      }

      // Support 2 at (10,0,0) — all DOFs enabled
      {
        var support = new ElementRigidPunctualSupport
        {
          GeomPt   = new Pt3D { X = 10.0, Y = 0.0, Z = 0.0 },
          Material = idMaterial
        };
        var resp = client.CreateElement(support);
        PrintResultDetails(resp.Details, logVerbosityLevel, "Create rigid support 2 at (10,0,0)");
        Console.WriteLine("  Support 2 OID: " + resp.Data.Value);
      }

      // --- Create live loads family ---
      var respLiveFamily = client.CreateInformationalElement(new LoadCaseFamily_LiveLoads { Name = "Live Loads Family" });
      PrintResultDetails(respLiveFamily.Details, logVerbosityLevel, "Create live loads family");
      EID idLiveFamily = respLiveFamily.Data;
      Console.WriteLine("  Live Loads Family OID: " + idLiveFamily.Value);

      // --- Create live load case ---
      var liveLoadCase = new LoadCase_LiveLoads
      {
        Name             = "Live Load Case",
        LoadCaseFamilyID = idLiveFamily,
        LiveLoadCategory = 0
      };
      var respLiveCase = client.CreateInformationalElement(liveLoadCase);
      PrintResultDetails(respLiveCase.Details, logVerbosityLevel, "Create live load case");
      EID idLiveCase = respLiveCase.Data;
      Console.WriteLine("  Live Load Case OID: " + idLiveCase.Value);

      // --- Create imposed displacement at (10,0,0) with Dz = -0.03m (-3cm) ---
      var imposedDisp = new ElementImposedDisplacement
      {
        GeomPt    = new Pt3D { X = 10.0, Y = 0.0, Z = 0.0 },
        LoadCase  = idLiveCase,
        Dx        = 0.0,
        Dy        = 0.0,
        Dz        = -0.03,   // -3 cm in Z
        Rx        = 0.0,
        Ry        = 0.0,
        Rz        = 0.0
      };
      var respImposedDisp = client.CreateElement(imposedDisp);
      PrintResultDetails(respImposedDisp.Details, logVerbosityLevel, "Create imposed displacement Dz=-3cm at (10,0,0)");
      Console.WriteLine("  Imposed Displacement OID: " + respImposedDisp.Data.Value);

      // --- Launch analysis ---
      var respAnalysis = client.LaunchAnalysis();
      PrintResultDetails(respAnalysis.Details, logVerbosityLevel, "Launch analysis");
      bool analysisOk = respAnalysis.Data;
      Console.WriteLine("  Analysis result: " + (analysisOk ? "succeeded" : "FAILED"));
      if (analysisOk)
        SampleReport.Pass("LaunchAnalysis");

      if (!analysisOk)
      {
        SampleReport.Fatal("ERROR: Analysis failed. Cannot retrieve results.");
        Console.WriteLine("ERROR: Analysis failed. Cannot retrieve results.");
        client.CloseProject();
        return;
      }

      // --- Get mesh connectivity for element 2 ---
      var elemIds = new List<long> { idElement2.Value };
      var respMeshConn = client.GetMeshConnectivity(elemIds);
      PrintResultDetails(respMeshConn.Details, logVerbosityLevel, "GetMeshConnectivity for element 2");
      if (respMeshConn.Data != null)
      {
        Console.WriteLine($"  Mesh connectivity: {respMeshConn.Data.Count} element(s)");
        foreach (var mesh in respMeshConn.Data)
          Console.WriteLine($"    Elem {mesh.Id.Value}: connectivity=({mesh.Connectivity?.Rows}x{mesh.Connectivity?.Cols})");
      }

      // --- Get mesh nodes positions and find the node at the imposed displacement coordinates (10,0,0) ---
      var respMeshNodes = client.GetMeshNodesPosition();
      PrintResultDetails(respMeshNodes.Details, logVerbosityLevel, "GetMeshNodesPosition");

      long imposedDispNodeId = -1;
      double coordTolerance  = 0.001; // 1 mm tolerance for coordinate matching
      if (respMeshNodes.Data != null)
      {
        var meshNodesList = respMeshNodes.Data.ToList();
        Console.WriteLine($"  Mesh nodes: {meshNodesList.Count} node(s)");
        for (int i = 0; i < meshNodesList.Count; i++)
        {
          var nodePos = meshNodesList[i];
          if (Math.Abs(nodePos.X - 10.0) < coordTolerance &&
              Math.Abs(nodePos.Y -  0.0) < coordTolerance &&
              Math.Abs(nodePos.Z -  0.0) < coordTolerance)
          {
            imposedDispNodeId = i;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  >>> Found mesh node at imposed displacement location (10,0,0): index={i}, " +
              $"coords=({nodePos.X:F3}, {nodePos.Y:F3}, {nodePos.Z:F3})");
            Console.ForegroundColor = ConsoleColor.White;
          }
        }
        if (imposedDispNodeId < 0)
          Console.WriteLine("  WARNING: No mesh node found at coordinates (10,0,0).");
      }

      // --- Get displacement results for element 2 (column at x=10, in contact with imposed displacement) ---
      var respResults = client.GetResults(ResultType.Displacement, idLiveCase.Value, elemIds);
      PrintResultDetails(respResults.Details, logVerbosityLevel, "GetResults Displacement for element 2");

      var resultsList = respResults.Data;
      if (resultsList == null || resultsList.Count == 0)
      {
        Console.WriteLine("  WARNING: No displacement results returned for element 2.");
      }
      else
      {
        Console.WriteLine($"  Displacement results for element 2: {resultsList.Count} result(s) returned.");

        double toleranceCm = 0.5;    // 0.5 cm tolerance for ~3 cm check
        double expectedDz  = -0.03;  // -3 cm in metres
        bool   verified    = false;

        foreach (var result in resultsList)
        {
          if (result is ResElementLinear linResult)
          {
            Console.WriteLine($"  [ResElementLinear] ID={linResult.Id?.Value}");

            // --- Check ResDiagrams displacement diagrams ---
            var diagrams = linResult.ResDiagrams;
            if (diagrams?.ResDisplacements != null)
            {
              var diagDisp = diagrams.ResDisplacements;
              Console.WriteLine("    ResDiagrams.ResDisplacements:");

              if (diagDisp.Dx != null)
                foreach (var pt in diagDisp.Dx)
                  Console.WriteLine($"      Dx: abscissa={pt.Abscissa:F4}, value={pt.Value:F6} m ({pt.Value * 100.0:F3} cm)");

              if (diagDisp.Dy != null)
                foreach (var pt in diagDisp.Dy)
                  Console.WriteLine($"      Dy: abscissa={pt.Abscissa:F4}, value={pt.Value:F6} m ({pt.Value * 100.0:F3} cm)");

              if (diagDisp.Dz != null)
              {
                foreach (var pt in diagDisp.Dz)
                {
                  //if (Math.Abs(pt.Value - expectedDz) <= toleranceCm / 100.0)
                  //  verified = true;
                  Console.WriteLine($"      Dz: abscissa={pt.Abscissa:F4}, value={pt.Value:F6} m ({pt.Value * 100.0:F3} cm)");
                }
              }

              if (diagDisp.D != null)
                foreach (var pt in diagDisp.D)
                {
                  //if (Math.Abs(pt.Value - Math.Abs(expectedDz)) <= toleranceCm / 100.0)
                  //  verified = true;                  
                  Console.WriteLine($"      D:  abscissa={pt.Abscissa:F4}, value={pt.Value:F6} m ({pt.Value * 100.0:F3} cm)");
                }
            }
            else
            {
              Console.WriteLine("    ResDiagrams.ResDisplacements: (null)");
            }

            // --- Check inner ResNodes displacements ---
            var resNodes = linResult.ResNodes;
            if (resNodes != null && resNodes.Count > 0)
            {
              Console.WriteLine($"    ResNodes ({resNodes.Count} node(s)):");
              foreach (var node in resNodes)
              {
                var disp = node.ResDisplacements;
                bool isImposedDispNode = imposedDispNodeId >= 0 && node.Id?.Value == imposedDispNodeId;

                if (disp != null)
                {
                  string marker = isImposedDispNode ? " <<<< IMPOSED DISPLACEMENT NODE (10,0,0)" : "";
                  Console.WriteLine($"      Node ID={node.Id?.Value}: " +
                    $"Dx={disp.Dx:F6} m, Dy={disp.Dy:F6} m, Dz={disp.Dz:F6} m, D={disp.D:F6} m  {marker}");

                  if (isImposedDispNode)
                  {                    
                    if (Math.Abs(disp.D - Math.Abs(expectedDz)) <= toleranceCm / 100.0)
                    {
                      verified = true;
                      Console.ForegroundColor = ConsoleColor.Green;
                    }
                    Console.WriteLine($"      >>> Imposed displacement node D = {disp.D * 100.0:F3} cm (expected ~{Math.Abs(expectedDz) * 100.0:F1} cm)");
                    Console.ForegroundColor = ConsoleColor.White;
                  }
                }
                else
                {
                  Console.WriteLine($"      Node ID={node.Id?.Value}: ResDisplacements=(null)" +
                    (isImposedDispNode ? " <<<< IMPOSED DISPLACEMENT NODE (10,0,0)" : ""));
                }
              }
            }
            else
            {
              Console.WriteLine("    ResNodes: (empty or null)");
            }
          }
          else
          {
            Console.WriteLine($"  [Result type: {result?.GetType().Name}] (not ResElementLinear, skipped)");
          }
        }

        Console.WriteLine();
        if (verified)
        {
          SampleReport.Pass("Displacement on element 2 is ~3 cm");
          Console.ForegroundColor = ConsoleColor.Green;
          Console.WriteLine("  VERIFICATION PASSED: displacement on element 2 is ~3 cm as expected.");
        }
        else
        {
          SampleReport.Fail("Displacement on element 2 is ~3 cm", "no result confirmed the expected displacement");
          Console.ForegroundColor = ConsoleColor.Red;
          Console.WriteLine("  VERIFICATION WARNING: no result confirmed ~3 cm displacement. Check results above.");
        }        
        Console.ForegroundColor = ConsoleColor.White;

      }

      client.CloseProject();
      Console.WriteLine("Close project.");
    }
  }
}

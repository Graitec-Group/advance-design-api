using AD.API.Client;

namespace Test.API.Client
{
  internal partial class Program
  {
    /// <summary>
    /// Planar slab with rigid linear supports, analysis and result display (France localization):
    /// - Material: C25/30
    /// - 1 planar element (shell, 0.15m thick) at Z=0:
    ///     Point1=(11,0,0), Point2=(11,7,0), Point3=(5,7,0), Point4=(5,0,0) — Area=42m²
    /// - 2 rigid linear supports (all DOFs enabled):
    ///     Support 1: (5,7,0)→(11,7,0)
    ///     Support 2: (5,0,0)→(11,0,0)
    /// - Live load family + live load case containing 1 planar load:
    ///     Fz=-12 kN/m², Variation=(1,1,1), on same 4 corners as the slab
    /// - Dead load family + dead load case (empty, used as combination term)
    /// - ULS combination: 1.35 × DeadCase + 1.50 × LiveCase
    /// - Runs linear analysis and displays all results (Displacement, Forces, Stresses) for the planar element.
    /// </summary>
    static void Sample_PlanarSlabRigidLinearSupportsFrance(AD.API.Client.AD_Client client)
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

      // --- Create project ---
      var newProj = client.NewProject(projectPath + "PlanarSlabResults" + Guid.NewGuid().ToString() + ".fto", env);
      PrintResultDetails(newProj.Details, logVerbosityLevel, "Create new project");
      if (!newProj.Details.Success || newProj.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: NewProject failed. Aborting.");
        Console.WriteLine("ERROR: NewProject failed. Aborting.");
        return;
      }

      // --- Material C25/30 ---
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

      // --- Planar element (shell, 0.15m thick) at Z=0 ---
      // Points: (11,0,0), (11,7,0), (5,7,0), (5,0,0)  — Area = 6m × 7m = 42m²
      EID idPlanar;
      {
        var resp = client.CreateElement(new ElementPlanar
        {
          Material             = idMaterial,
          ElementType          = PlanarElementType.Shell,
          ThicknessIn1stVertex = 0.15,
          SlopeX               = 0.0,
          SlopeY               = 0.0,
          Eccentricity         = 0.0,
          GeomPtsList          = new List<Pt3D>
          {
            new Pt3D { X = 11.0, Y = 0.0, Z = 0.0 },
            new Pt3D { X = 11.0, Y = 7.0, Z = 0.0 },
            new Pt3D { X =  5.0, Y = 7.0, Z = 0.0 },
            new Pt3D { X =  5.0, Y = 0.0, Z = 0.0 }
          }
        });
        PrintResultDetails(resp.Details, logVerbosityLevel, "Create planar element (0.15m shell, 42m²)");
        if (!resp.Details.Success || resp.Details.HasErrors)
        {
          SampleReport.Fatal("ERROR: CreateElement (planar) failed. Aborting.");
          Console.WriteLine("ERROR: CreateElement (planar) failed. Aborting.");
          client.CloseProject();
          return;
        }
        idPlanar = resp.Data;
        Console.WriteLine("  Planar element OID: " + idPlanar.Value);
      }

      // --- Rigid linear support 1: (5,7,0)→(11,7,0) — all DOFs ---
      EID idLinSupport1;
      {
        var resp = client.CreateElement(new ElementRigidLinearSupport
        {
          GeomPtStart = new Pt3D { X =  5.0, Y = 7.0, Z = 0.0 },
          GeomPtEnd   = new Pt3D { X = 11.0, Y = 7.0, Z = 0.0 },
          Restraints  = new DegreeOfFreedomRestraints
          {
            Tx = true, Ty = true, Tz = true,
            Rx = true, Ry = true, Rz = true
          },
          Material = idMaterial
        });
        PrintResultDetails(resp.Details, logVerbosityLevel, "Create rigid linear support 1 (5,7,0)→(11,7,0)");
        if (!resp.Details.Success || resp.Details.HasErrors)
        {
          SampleReport.Fatal("ERROR: CreateElement (linear support 1) failed. Aborting.");
          Console.WriteLine("ERROR: CreateElement (linear support 1) failed. Aborting.");
          client.CloseProject();
          return;
        }
        idLinSupport1 = resp.Data;
        Console.WriteLine("  Rigid linear support 1 OID: " + idLinSupport1.Value);
      }

      // --- Rigid linear support 2: (5,0,0)→(11,0,0) — all DOFs ---
      EID idLinSupport2;
      {
        var resp = client.CreateElement(new ElementRigidLinearSupport
        {
          GeomPtStart = new Pt3D { X =  5.0, Y = 0.0, Z = 0.0 },
          GeomPtEnd   = new Pt3D { X = 11.0, Y = 0.0, Z = 0.0 },
          Restraints  = new DegreeOfFreedomRestraints
          {
            Tx = true, Ty = true, Tz = true,
            Rx = true, Ry = true, Rz = true
          },
          Material = idMaterial
        });
        PrintResultDetails(resp.Details, logVerbosityLevel, "Create rigid linear support 2 (5,0,0)→(11,0,0)");
        if (!resp.Details.Success || resp.Details.HasErrors)
        {
          SampleReport.Fatal("ERROR: CreateElement (linear support 2) failed. Aborting.");
          Console.WriteLine("ERROR: CreateElement (linear support 2) failed. Aborting.");
          client.CloseProject();
          return;
        }
        idLinSupport2 = resp.Data;
        Console.WriteLine("  Rigid linear support 2 OID: " + idLinSupport2.Value);
      }

      // --- Live load family ---
      var respLiveFamily = client.CreateInformationalElement(new LoadCaseFamily_LiveLoads { Name = "Live Loads Family" });
      PrintResultDetails(respLiveFamily.Details, logVerbosityLevel, "Create live loads family");
      if (!respLiveFamily.Details.Success || respLiveFamily.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: CreateInformationalElement (live family) failed. Aborting.");
        Console.WriteLine("ERROR: CreateInformationalElement (live family) failed. Aborting.");
        client.CloseProject();
        return;
      }
      EID idLiveFamily = respLiveFamily.Data;
      Console.WriteLine("  Live Loads Family OID: " + idLiveFamily.Value);

      // --- Live load case (inside the live family) ---
      var respLiveCase = client.CreateInformationalElement(new LoadCase_LiveLoads
      {
        Name             = "Live Loads",
        LoadCaseFamilyID = idLiveFamily,
        LiveLoadCategory = 0
      });
      PrintResultDetails(respLiveCase.Details, logVerbosityLevel, "Create live load case");
      if (!respLiveCase.Details.Success || respLiveCase.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: CreateInformationalElement (live case) failed. Aborting.");
        Console.WriteLine("ERROR: CreateInformationalElement (live case) failed. Aborting.");
        client.CloseProject();
        return;
      }
      EID idLiveCase = respLiveCase.Data;
      Console.WriteLine("  Live Load Case OID: " + idLiveCase.Value);

      // --- Planar load: Fz = -12 kN/m² (-12 000 N/m²), uniform (1,1,1) ---
      // Points follow the slab corners: (5,0,0), (5,7,0), (11,7,0), (11,0,0)
      {
        var resp = client.CreateElement(new ElementLoadPlanar
        {
          GeomPtsList = new List<Pt3D>
          {
            new Pt3D { X =  5.0, Y = 0.0, Z = 0.0 },
            new Pt3D { X =  5.0, Y = 7.0, Z = 0.0 },
            new Pt3D { X = 11.0, Y = 7.0, Z = 0.0 },
            new Pt3D { X = 11.0, Y = 0.0, Z = 0.0 }
          },
          CoordinateSystemType = CoordinateSystemForLinearOrPlanarGeometry.Global_or_user,
          LoadCase             = idLiveCase,
          Fx                   = 0.0,
          Fy                   = 0.0,
          Fz                   = -12000.0,   // -12 kN/m² expressed in N/m²
          Variation            = new PlanarVariation { Coefficient1 = 1.0, Coefficient2 = 1.0, Coefficient3 = 1.0 }
        });
        PrintResultDetails(resp.Details, logVerbosityLevel, "Create planar load Fz=-12kN/m²");
        if (!resp.Details.Success || resp.Details.HasErrors)
          Console.WriteLine("WARNING: CreateElement (planar load) failed.");
        else
          Console.WriteLine("  Planar load OID: " + resp.Data.Value);
      }

      // --- Dead load family ---
      var respDeadFamily = client.CreateInformationalElement(new LoadCaseFamily_DeadLoads { Name = "Dead Loads Family" });
      PrintResultDetails(respDeadFamily.Details, logVerbosityLevel, "Create dead loads family");
      if (!respDeadFamily.Details.Success || respDeadFamily.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: CreateInformationalElement (dead family) failed. Aborting.");
        Console.WriteLine("ERROR: CreateInformationalElement (dead family) failed. Aborting.");
        client.CloseProject();
        return;
      }
      EID idDeadFamily = respDeadFamily.Data;
      Console.WriteLine("  Dead Loads Family OID: " + idDeadFamily.Value);

      // --- Dead load case ---
      var respDeadCase = client.CreateInformationalElement(new LoadCase_DeadLoads
      {
        Name             = "Dead Loads",
        LoadCaseFamilyID = idDeadFamily
      });
      PrintResultDetails(respDeadCase.Details, logVerbosityLevel, "Create dead load case");
      if (!respDeadCase.Details.Success || respDeadCase.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: CreateInformationalElement (dead case) failed. Aborting.");
        Console.WriteLine("ERROR: CreateInformationalElement (dead case) failed. Aborting.");
        client.CloseProject();
        return;
      }
      EID idDeadCase = respDeadCase.Data;
      Console.WriteLine("  Dead Load Case OID: " + idDeadCase.Value);

      // --- ULS combination: 1.35 × Dead + 1.50 × Live ---
      var respCombo = client.AddCombination(new Combination
      {
        ECombinationType = ECombinationType.EComboProjectSituationEluStrgeo,
        ListCasesCoeffs  = new List<EIDDoublePair>
        {
          new EIDDoublePair { Key = idDeadCase, Value = 1.35 },
          new EIDDoublePair { Key = idLiveCase, Value = 1.50 }
        }
      });
      PrintResultDetails(respCombo.Details, logVerbosityLevel, "Add ULS combination 1.35×Dead + 1.50×Live");
      if (!respCombo.Details.Success || respCombo.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: AddCombination failed. Aborting.");
        Console.WriteLine("ERROR: AddCombination failed. Aborting.");
        client.CloseProject();
        return;
      }
      EID idCombo = respCombo.Data;
      Console.WriteLine("  Combination OID: " + idCombo.Value);

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

      // =========================================================================
      // Display all results for the planar element
      // =========================================================================

      var planarIds = new List<long> { idPlanar.Value };

      // --- Displacement results ---
      Console.WriteLine();
      Console.WriteLine("=== Planar element — Displacement results (ULS combination) ===");
      var respDisp = client.GetResults(ResultType.Displacement, idCombo.Value, planarIds);
      PrintResultDetails(respDisp.Details, logVerbosityLevel, "GetResults Displacement (planar, ULS combo)");
      PrintPlanarResults(respDisp, "Displacement");

      // --- Forces results ---
      Console.WriteLine();
      Console.WriteLine("=== Planar element — Forces results (ULS combination) ===");
      var respForces = client.GetResults(ResultType.Forces, idCombo.Value, planarIds);
      PrintResultDetails(respForces.Details, logVerbosityLevel, "GetResults Forces (planar, ULS combo)");
      PrintPlanarResults(respForces, "Forces");

      // --- Stresses results ---
      Console.WriteLine();
      Console.WriteLine("=== Planar element — Stresses results (ULS combination) ===");
      var respStresses = client.GetResults(ResultType.Stresses, idCombo.Value, planarIds);
      PrintResultDetails(respStresses.Details, logVerbosityLevel, "GetResults Stresses (planar, ULS combo)");
      PrintPlanarResults(respStresses, "Stresses");

      // --- Displacement results on live load case ---
      Console.WriteLine();
      Console.WriteLine("=== Planar element — Displacement results (live load case) ===");
      var respDispLive = client.GetResults(ResultType.Displacement, idLiveCase.Value, planarIds);
      PrintResultDetails(respDispLive.Details, logVerbosityLevel, "GetResults Displacement (planar, live case)");
      PrintPlanarResults(respDispLive, "Displacement (live)");

      // --- Forces results on live load case ---
      Console.WriteLine();
      Console.WriteLine("=== Planar element — Forces results (live load case) ===");
      var respForcesLive = client.GetResults(ResultType.Forces, idLiveCase.Value, planarIds);
      PrintResultDetails(respForcesLive.Details, logVerbosityLevel, "GetResults Forces (planar, live case)");
      PrintPlanarResults(respForcesLive, "Forces (live)");

      // --- Stresses results on live load case ---
      Console.WriteLine();
      Console.WriteLine("=== Planar element — Stresses results (live load case) ===");
      var respStressesLive = client.GetResults(ResultType.Stresses, idLiveCase.Value, planarIds);
      PrintResultDetails(respStressesLive.Details, logVerbosityLevel, "GetResults Stresses (planar, live case)");
      PrintPlanarResults(respStressesLive, "Stresses (live)");

      client.CloseProject();
      Console.WriteLine();
      Console.WriteLine("Close project.");
    }

    // -------------------------------------------------------------------------
    // Result display helpers (planar slab sample)
    // -------------------------------------------------------------------------

    private static void PrintPlanarResults(ResBaseListApiResponse resp, string label)
    {
      if (resp?.Data == null || resp.Data.Count == 0)
      {
        Console.WriteLine($"  [{label}] No data returned.");
        return;
      }

      foreach (var res in resp.Data)
      {
        if (res is ResElementPlanar planarRes)
        {
          Console.WriteLine($"  [ResElementPlanar] ID={planarRes.Id?.Value}");

          var nodes = planarRes.ResNodes;

          // --- Displacement min/max ---
          var dispNodes = nodes?.Where(n => n.ResDisplacements != null).Select(n => n.ResDisplacements!).ToList();
          if (dispNodes != null && dispNodes.Count > 0)
          {
            Console.WriteLine($"    Displacements ({nodes!.Count} nodes)  [min / max]:");
            PrintMinMax("Dx (m)",  dispNodes.Select(d => d.Dx));
            PrintMinMax("Dy (m)",  dispNodes.Select(d => d.Dy));
            PrintMinMax("Dz (m)",  dispNodes.Select(d => d.Dz));
            PrintMinMax("D  (m)",  dispNodes.Select(d => d.D));
            PrintMinMax("Rx (rad)", dispNodes.Select(d => d.Rx));
            PrintMinMax("Ry (rad)", dispNodes.Select(d => d.Ry));
            PrintMinMax("Rz (rad)", dispNodes.Select(d => d.Rz));
          }

          // --- Forces min/max ---
          var forceNodes = nodes?.Where(n => n.ResForces != null).Select(n => n.ResForces!).ToList();
          if (forceNodes != null && forceNodes.Count > 0)
          {
            Console.WriteLine($"    Forces ({nodes!.Count} nodes)  [min / max]:");
            PrintMinMax("Fxx", forceNodes.Select(f => f.Fxx));
            PrintMinMax("Fyy", forceNodes.Select(f => f.Fyy));
            PrintMinMax("Fxy", forceNodes.Select(f => f.Fxy));
            PrintMinMax("Fxz", forceNodes.Select(f => f.Fxz));
            PrintMinMax("Fyz", forceNodes.Select(f => f.Fyz));
            PrintMinMax("Fzz", forceNodes.Select(f => f.Fzz));
            PrintMinMax("Mxx", forceNodes.Select(f => f.Mxx));
            PrintMinMax("Myy", forceNodes.Select(f => f.Myy));
            PrintMinMax("Mxy", forceNodes.Select(f => f.Mxy));
            PrintMinMax("F1",  forceNodes.Select(f => f.F1));
            PrintMinMax("F2",  forceNodes.Select(f => f.F2));
            PrintMinMax("M1",  forceNodes.Select(f => f.M1));
            PrintMinMax("M2",  forceNodes.Select(f => f.M2));
          }

          // --- Stresses min/max ---
          var stressNodes = nodes?.Where(n => n.ResStresses != null).Select(n => n.ResStresses!).ToList();
          if (stressNodes != null && stressNodes.Count > 0)
          {
            Console.WriteLine($"    Stresses ({nodes!.Count} nodes)  [min / max]:");
            Console.WriteLine("      Top face:");
            PrintMinMax("SxxTop", stressNodes.Select(s => s.SxxTop));
            PrintMinMax("SyyTop", stressNodes.Select(s => s.SyyTop));
            PrintMinMax("SxyTop", stressNodes.Select(s => s.SxyTop));
            PrintMinMax("SvTop",  stressNodes.Select(s => s.SvTop));
            PrintMinMax("S1Top",  stressNodes.Select(s => s.S1Top));
            PrintMinMax("S2Top",  stressNodes.Select(s => s.S2Top));
            Console.WriteLine("      Bottom face:");
            PrintMinMax("SxxInf", stressNodes.Select(s => s.SxxInf));
            PrintMinMax("SyyInf", stressNodes.Select(s => s.SyyInf));
            PrintMinMax("SxyInf", stressNodes.Select(s => s.SxyInf));
            PrintMinMax("SvInf",  stressNodes.Select(s => s.SvInf));
            PrintMinMax("S1Inf",  stressNodes.Select(s => s.S1Inf));
            PrintMinMax("S2Inf",  stressNodes.Select(s => s.S2Inf));
            Console.WriteLine("      Mid plane:");
            PrintMinMax("SxxMid", stressNodes.Select(s => s.SxxMid));
            PrintMinMax("SyyMid", stressNodes.Select(s => s.SyyMid));
            PrintMinMax("SxyMid", stressNodes.Select(s => s.SxyMid));
            PrintMinMax("SvMid",  stressNodes.Select(s => s.SvMid));
            PrintMinMax("S1Mid",  stressNodes.Select(s => s.S1Mid));
            PrintMinMax("S2Mid",  stressNodes.Select(s => s.S2Mid));
          }

          if (dispNodes == null && forceNodes == null && stressNodes == null)
            Console.WriteLine("    ResNodes: (empty or null)");

          // --- Torsors min/max ---
          var torsors = planarRes.ResTorsors?.ToList();
          if (torsors != null && torsors.Count > 0)
          {
            Console.WriteLine($"    Torsors ({torsors.Count})  [min / max]:");
            Console.WriteLine("      Bottom→Top face:");
            PrintMinMax("N_BT",   torsors.Select(t => t.N_BottomTop));
            PrintMinMax("Mz_BT",  torsors.Select(t => t.Mz_BottomTop));
            PrintMinMax("Txy_BT", torsors.Select(t => t.Txy_BottomTop));
            PrintMinMax("Tyz_BT", torsors.Select(t => t.Tyz_BottomTop));
            PrintMinMax("Mf_BT",  torsors.Select(t => t.Mf_BottomTop));
            Console.WriteLine("      Left→Right face:");
            PrintMinMax("N_LR",   torsors.Select(t => t.N_LeftRight));
            PrintMinMax("Mz_LR",  torsors.Select(t => t.Mz_LeftRight));
            PrintMinMax("Txy_LR", torsors.Select(t => t.Txy_LeftRight));
            PrintMinMax("Tyz_LR", torsors.Select(t => t.Tyz_LeftRight));
            PrintMinMax("Mf_LR",  torsors.Select(t => t.Mf_LeftRight));
          }
          else
          {
            Console.WriteLine("    ResTorsors: (empty or null)");
          }
        }
        else
        {
          Console.WriteLine($"  [{label}] Result type: {res?.GetType().Name} (not ResElementPlanar, skipped)");
        }
      }
    }

    private static void PrintMinMax(string name, IEnumerable<double> values)
    {
      var list = values.ToList();
      if (list.Count == 0) return;
      double min = list.Min();
      double max = list.Max();
      Console.WriteLine($"      {name,-12}  min={min,14:G6}   max={max,14:G6}");
    }
  }
}

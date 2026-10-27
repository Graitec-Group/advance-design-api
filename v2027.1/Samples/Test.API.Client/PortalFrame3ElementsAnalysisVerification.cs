using AD.API.Client;

namespace Test.API.Client
{
  internal partial class Program
  {
    /// <summary>
    /// Portal frame with 3 linear elements, analysis and result verification:
    /// - Material: C25/30, Section: R20/30
    /// - Column 1: (0,0,0) → (0,0,4)
    /// - Beam:     (0,0,4) → (5,0,4)
    /// - Column 2: (5,0,4) → (5,0,0)
    /// - 2 fully fixed rigid punctual supports at (0,0,0) and (5,0,0)
    /// - Dead load family + case (default)
    /// - Live load family + case with 3 punctual loads:
    ///     1. F(0, 0, -120 kN) at (3,0,4)
    ///     2. F(50, 0, 100 kN) at (1,0,4)
    ///     3. F(-80, 60, 0 kN) at (5,0,4)
    /// - ULS combination: Dead*1.0 + Live*1.50
    /// - Linear analysis with 5% tolerance verification on:
    ///     support reaction forces (Fx, Fy, Fz) for both supports,
    ///     beam Fz and My force diagrams,
    ///     beam Dz displacement diagram,
    ///     beam SxxMax stress diagram.
    /// </summary>
    static void Sample_PortalFrame3ElementsAnalysisVerificationOnBeamAndSupports(AD.API.Client.AD_Client client)
    {
      LogVerboseLevel logVerbosityLevel = LogVerboseLevel.ErrorsAndWarnings;
      const double RelativeTolerance = 0.05; // 5%

      Environments env = new Environments()
      {
        Localization = Localization_Code.LOCALIZATION_EUROPE,
        Language     = Language_Code.ELanguageEnglish,
        LogVerbosity = logVerbosityLevel
      };

      string projectPath = GetEnvironmentVariable("AD_MODELS_PATH")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                        "Graitec", "Advance Design", "2027", "Projects")
           + Path.DirectorySeparatorChar;

      // --- Create project ---
      var newProj = client.NewProject(projectPath + "PortalBeamAndSupportsResults" + Guid.NewGuid().ToString() + ".fto", env);
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
        SampleReport.Fatal("ERROR: CreateMaterial failed. Aborting.");
        Console.WriteLine("ERROR: CreateMaterial failed. Aborting.");
        client.CloseProject();
        return;
      }
      EID idMaterial = respMaterial.Data;
      Console.WriteLine("  Material C25/30 OID: " + idMaterial.Value);

      // --- Section R20/30 ---
      var respSection = client.CreateSection("R20/30");
      PrintResultDetails(respSection.Details, logVerbosityLevel, "Create section R20/30");
      if (!respSection.Details.Success || respSection.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: CreateSection failed. Aborting.");
        Console.WriteLine("ERROR: CreateSection failed. Aborting.");
        client.CloseProject();
        return;
      }
      EID idSection = respSection.Data;
      Console.WriteLine("  Section R20/30 OID: " + idSection.Value);

      // --- Column 1: (0,0,0) → (0,0,4) ---
      EID idColumn1;
      {
        var resp = client.CreateElement(new ElementLinear
        {
          Section     = idSection,
          Material    = idMaterial,
          GeomPtStart = new Pt3D { X = 0.0, Y = 0.0, Z = 0.0 },
          GeomPtEnd   = new Pt3D { X = 0.0, Y = 0.0, Z = 4.0 }
        });
        PrintResultDetails(resp.Details, logVerbosityLevel, "Create column 1 (0,0,0)→(0,0,4)");
        idColumn1 = resp.Data;
        Console.WriteLine("  Column 1 OID: " + idColumn1.Value);
      }

      // --- Beam: (0,0,4) → (5,0,4) ---
      EID idBeam;
      {
        var resp = client.CreateElement(new ElementLinear
        {
          Section     = idSection,
          Material    = idMaterial,
          GeomPtStart = new Pt3D { X = 0.0, Y = 0.0, Z = 4.0 },
          GeomPtEnd   = new Pt3D { X = 5.0, Y = 0.0, Z = 4.0 }
        });
        PrintResultDetails(resp.Details, logVerbosityLevel, "Create beam (0,0,4)→(5,0,4)");
        idBeam = resp.Data;
        Console.WriteLine("  Beam OID: " + idBeam.Value);
      }

      // --- Column 2: (5,0,4) → (5,0,0) ---
      EID idColumn2;
      {
        var resp = client.CreateElement(new ElementLinear
        {
          Section     = idSection,
          Material    = idMaterial,
          GeomPtStart = new Pt3D { X = 5.0, Y = 0.0, Z = 4.0 },
          GeomPtEnd   = new Pt3D { X = 5.0, Y = 0.0, Z = 0.0 }
        });
        PrintResultDetails(resp.Details, logVerbosityLevel, "Create column 2 (5,0,4)→(5,0,0)");
        idColumn2 = resp.Data;
        Console.WriteLine("  Column 2 OID: " + idColumn2.Value);
      }

      // --- Rigid support 1 at (0,0,0) – fully fixed ---
      EID idSupport1;
      {
        var resp = client.CreateElement(new ElementRigidPunctualSupport
        {
          GeomPt   = new Pt3D { X = 0.0, Y = 0.0, Z = 0.0 },
          Material = idMaterial,
          Restraints = new DegreeOfFreedomRestraints
          {
            Tx = true, Ty = true, Tz = true,
            Rx = true, Ry = true, Rz = true
          }
        });
        PrintResultDetails(resp.Details, logVerbosityLevel, "Create rigid support 1 at (0,0,0)");
        idSupport1 = resp.Data;
        Console.WriteLine("  Support 1 OID: " + idSupport1.Value);
      }

      // --- Rigid support 2 at (5,0,0) – fully fixed ---
      EID idSupport2;
      {
        var resp = client.CreateElement(new ElementRigidPunctualSupport
        {
          GeomPt   = new Pt3D { X = 5.0, Y = 0.0, Z = 0.0 },
          Material = idMaterial,
          Restraints = new DegreeOfFreedomRestraints
          {
            Tx = true, Ty = true, Tz = true,
            Rx = true, Ry = true, Rz = true
          }
        });
        PrintResultDetails(resp.Details, logVerbosityLevel, "Create rigid support 2 at (5,0,0)");
        idSupport2 = resp.Data;
        Console.WriteLine("  Support 2 OID: " + idSupport2.Value);
      }

      // --- Dead load family + case ---
      var respDeadFamily = client.CreateInformationalElement(new LoadCaseFamily_DeadLoads { Name = "Dead Loads Family" });
      PrintResultDetails(respDeadFamily.Details, logVerbosityLevel, "Create dead loads family");
      EID idDeadFamily = respDeadFamily.Data;
      Console.WriteLine("  Dead Loads Family OID: " + idDeadFamily.Value);

      var respDeadCase = client.CreateInformationalElement(new LoadCase_DeadLoads
      {
        Name             = "Dead Loads",
        LoadCaseFamilyID = idDeadFamily
      });
      PrintResultDetails(respDeadCase.Details, logVerbosityLevel, "Create dead load case");
      EID idDeadCase = respDeadCase.Data;
      Console.WriteLine("  Dead Load Case OID: " + idDeadCase.Value);

      // --- Live load family + case ---
      var respLiveFamily = client.CreateInformationalElement(new LoadCaseFamily_LiveLoads { Name = "Live Loads Family" });
      PrintResultDetails(respLiveFamily.Details, logVerbosityLevel, "Create live loads family");
      EID idLiveFamily = respLiveFamily.Data;
      Console.WriteLine("  Live Loads Family OID: " + idLiveFamily.Value);

      var respLiveCase = client.CreateInformationalElement(new LoadCase_LiveLoads
      {
        Name             = "Live Loads",
        LoadCaseFamilyID = idLiveFamily
      });
      PrintResultDetails(respLiveCase.Details, logVerbosityLevel, "Create live load case");
      EID idLiveCase = respLiveCase.Data;
      Console.WriteLine("  Live Load Case OID: " + idLiveCase.Value);

      // --- 3 punctual loads in live load case ---

      // Load 1: F(0, 0, -120 kN) at (3,0,4)
      {
        var resp = client.CreateElement(new ElementLoadPunctual
        {
          GeomPt   = new Pt3D { X = 3.0, Y = 0.0, Z = 4.0 },
          LoadCase = idLiveCase,
          Fx       = 0.0,
          Fy       = 0.0,
          Fz       = -120000.0,
          Moment   = new MomentComponents { Mx = 0.0, My = 0.0, Mz = 0.0 }
        });
        PrintResultDetails(resp.Details, logVerbosityLevel, "Create punctual load 1: F(0,0,-120kN) at (3,0,4)");
        Console.WriteLine("  Load 1 OID: " + resp.Data.Value);
      }

      // Load 2: F(50, 0, 100 kN) at (1,0,4)
      {
        var resp = client.CreateElement(new ElementLoadPunctual
        {
          GeomPt   = new Pt3D { X = 1.0, Y = 0.0, Z = 4.0 },
          LoadCase = idLiveCase,
          Fx       = 50000.0,
          Fy       = 0.0,
          Fz       = 100000.0,
          Moment   = new MomentComponents { Mx = 0.0, My = 0.0, Mz = 0.0 }
        });
        PrintResultDetails(resp.Details, logVerbosityLevel, "Create punctual load 2: F(50,0,100kN) at (1,0,4)");
        Console.WriteLine("  Load 2 OID: " + resp.Data.Value);
      }

      // Load 3: F(-80, 60, 0 kN) at (5,0,4)
      {
        var resp = client.CreateElement(new ElementLoadPunctual
        {
          GeomPt   = new Pt3D { X = 5.0, Y = 0.0, Z = 4.0 },
          LoadCase = idLiveCase,
          Fx       = -80000.0,
          Fy       = 60000.0,
          Fz       = 0.0,
          Moment   = new MomentComponents { Mx = 0.0, My = 0.0, Mz = 0.0 }
        });
        PrintResultDetails(resp.Details, logVerbosityLevel, "Create punctual load 3: F(-80,60,0kN) at (5,0,4)");
        Console.WriteLine("  Load 3 OID: " + resp.Data.Value);
      }

      // --- ULS combination: Dead*1.0 + Live*1.50 ---
      var respCombo = client.AddCombination(new Combination
      {
        ECombinationType = ECombinationType.EComboProjectSituationEluStrgeo,
        ListCasesCoeffs = new List<EIDDoublePair>
        {
          new EIDDoublePair { Key = idDeadCase, Value = 1.0  },
          new EIDDoublePair { Key = idLiveCase, Value = 1.50 }
        }
      });
      PrintResultDetails(respCombo.Details, logVerbosityLevel, "Add ULS combination Dead*1.0 + Live*1.50");
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

      // =====================================================================
      // Reference values from PortalFrame3Elements_AnalysisIntegrationTests
      // =====================================================================

      // Support 1 at (0,0,0) – expected reaction forces on ULS combination
      const double ExpSupport1Fx =  -35920.503963726333;
      const double ExpSupport1Fy = 12321.464467447920;
      const double ExpSupport1Fz = 27482.278698386952;

      // Support 2 at (5,0,0) – expected reaction forces on ULS combination
      const double ExpSupport2Fx =   -9079.4960362901693;
      const double ExpSupport2Fy = 77678.535532544833;
      const double ExpSupport2Fz =  -76605.246198386871;

      // Beam Fz diagram on combination (abscissa, value)
      var ExpFz = new (double Abscissa, double Value)[]
      {
        (0.0000000000000000,    33366.268698386943),
        (0.25000000000000000,   33734.018073386942),
        (0.50000000000000000,   34101.767448386941),
        (0.75000000000000000,   34469.516823386941),
        (0.99999899999999997,   34837.266198386940),
        (1.0000009999999999,   -115162.73380161295),
        (1.2500000000000000,   -114794.98442661295),
        (1.5000000000000000,   -114427.23505161295),
        (1.7500000000000000,   -114059.48567661295),
        (2.0000000000000000,   -113691.73630161295),
        (2.2500000000000000,   -113323.98692661284),
        (2.5000000000000000,   -112956.23755161284),
        (2.7500000000000000,   -112588.48817661284),
        (2.9999989999999999,   -112220.73880161284),
        (3.0000010000000001,    67779.261198386652),
        (3.2500000000000000,    68147.010573386651),
        (3.5000000000000000,    68514.759948386651),
        (3.7500000000000000,    68882.509323386650),
        (4.0000000000000000,    69250.258698386649),
        (4.2500000000000000,    69618.008073386867),
        (4.5000000000000000,    69985.757448386867),
        (4.7500000000000000,    70353.506823386866),
        (5.0000000000000000,    70721.256198386865),
      };

      // Beam My diagram on combination (abscissa, value)
      var ExpMy = new (double Abscissa, double Value)[]
      {
        (0.0000000000000000,    63548.359574762675),
        (0.25000000000000000,   71935.895421234411),
        (0.50000000000000000,   80415.368611456142),
        (0.75000000000000000,   88986.779145427878),
        (1.0000000000000000,    97650.127023149631),
        (1.2500000000000000,    68905.412244621402),
        (1.5000000000000000,    40252.634809843170),
        (1.7500000000000000,    11691.794718814936),
        (2.0000000000000000,   -16777.108028463303),
        (2.2500000000000000,   -45154.073431991739),
        (2.5000000000000000,   -73439.101491769950),
        (2.7500000000000000,  -101632.19220779814),
        (3.0000000000000000,  -129733.34558007636),
        (3.2500000000000000,  -112742.56160860478),
        (3.5000000000000000,   -95659.840293383109),
        (3.7500000000000000,   -78485.181634411449),
        (4.0000000000000000,   -61218.585631689784),
        (4.2500000000000000,   -43860.052285218109),
        (4.5000000000000000,   -26409.581594996394),
        (4.7500000000000000,    -8867.1735610246797),
        (5.0000000000000000,     8767.1718166970368),
      };

      // Beam Dz displacement diagram on combination (abscissa, value)
      var ExpDz = new (double Abscissa, double Value)[]
      {
        (0.0000000000000000,    6.4439490865644461e-05),
        (0.25000000000000000,   0.00051634228797227519),
        (0.50000000000000000,   0.00065093001192865173),
        (0.75000000000000000,   0.00043078647679967727),
        (1.0000000000000000,   -0.00018191018241387448),
        (1.2500000000000000,   -0.0012549957894590995),
        (1.5000000000000000,   -0.0026320242801959126),
        (1.7500000000000000,   -0.0041865635492320955),
        (2.0000000000000000,   -0.0057925871702395621),
        (2.2500000000000000,   -0.0073244743959543571),
        (2.5000000000000000,   -0.0086570101581766503),
        (2.7500000000000000,   -0.0096653850677707511),
        (3.0000000000000000,   -0.010225195414665092),
        (3.2500000000000000,   -0.010176913232231759),
        (3.5000000000000000,   -0.0096310413132485277),
        (3.7500000000000000,   -0.0086629581939368303),
        (4.0000000000000000,   -0.0073484480895822284),
        (4.2500000000000000,   -0.0057637008945344145),
        (4.5000000000000000,   -0.0039853121822072037),
        (4.7500000000000000,   -0.0020902832050785506),
        (5.0000000000000000,   -0.00015602089469053695),
      };

      // Beam SxxMax stress diagram on combination (abscissa, value)
      var ExpSxxMax = new (double Abscissa, double Value)[]
      {
        (0.0000000000000000,    35985942.043172553),
        (0.25000000000000000,   37241604.266898602),
        (0.50000000000000000,   38527912.271874644),
        (0.75000000000000000,   39844866.058100693),
        (0.99999899999999997,   41192465.625576749),
        (1.0000009999999999,    39942465.625576027),
        (1.2500000000000000,    28820710.974302061),
        (1.5000000000000000,    17729602.104278095),
        (1.7500000000000000,     6669139.0155041274),
        (2.0000000000000000,     6824060.3936223639),
        (2.2500000000000000,    14742865.803034259),
        (2.5000000000000000,    22631025.431195859),
        (2.7500000000000000,    33568905.394967422),
        (3.0000000000000000,    44476139.577491298),
        (3.2500000000000000,    40352727.978765711),
        (3.5000000000000000,    36198670.598790184),
        (3.7500000000000000,    32013967.437564664),
        (4.0000000000000000,    27798618.495089144),
        (4.2500000000000000,    23552623.771362975),
        (4.5000000000000000,    19275983.266387224),
        (4.7500000000000000,    14968696.980161482),
        (5.0000000000000000,    16475546.123817092),
      };

      // =====================================================================
      // Retrieve and verify support reaction forces on combination
      // =====================================================================

      int passCount  = 0;
      int totalCount = 0;

      Console.WriteLine();
      Console.WriteLine("=== Support Forces on ULS Combination ===");

      var respSupport1Forces = client.GetResults(ResultType.Forces, idCombo.Value, new List<long> { idSupport1.Value });
      PrintResultDetails(respSupport1Forces.Details, logVerbosityLevel, "GetResults Forces for support 1");

      var respSupport2Forces = client.GetResults(ResultType.Forces, idCombo.Value, new List<long> { idSupport2.Value });
      PrintResultDetails(respSupport2Forces.Details, logVerbosityLevel, "GetResults Forces for support 2");

      VerifySupportForces(respSupport1Forces, "Support 1 (0,0,0)",
        ExpSupport1Fx, ExpSupport1Fy, ExpSupport1Fz,
        RelativeTolerance, ref passCount, ref totalCount);

      VerifySupportForces(respSupport2Forces, "Support 2 (5,0,0)",
        ExpSupport2Fx, ExpSupport2Fy, ExpSupport2Fz,
        RelativeTolerance, ref passCount, ref totalCount);

      // =====================================================================
      // Retrieve and verify beam diagram results on combination
      // =====================================================================

      var beamIds = new List<long> { idBeam.Value };

      Console.WriteLine();
      Console.WriteLine("=== Beam Force Diagrams (Fz, My) on ULS Combination ===");

      var respBeamForces = client.GetResults(ResultType.Forces, idCombo.Value, beamIds);
      PrintResultDetails(respBeamForces.Details, logVerbosityLevel, "GetResults Forces for beam");

      VerifyBeamDiagram(respBeamForces, "Beam Fz (Forces)",
        b => b.ResDiagrams?.ResForces?.Fz,
        ExpFz, RelativeTolerance, ref passCount, ref totalCount);

      VerifyBeamDiagram(respBeamForces, "Beam My (Forces)",
        b => b.ResDiagrams?.ResForces?.My,
        ExpMy, RelativeTolerance, ref passCount, ref totalCount);

      Console.WriteLine();
      Console.WriteLine("=== Beam Displacement Diagram (Dz) on ULS Combination ===");

      var respBeamDisp = client.GetResults(ResultType.Displacement, idCombo.Value, beamIds);
      PrintResultDetails(respBeamDisp.Details, logVerbosityLevel, "GetResults Displacement for beam");

      VerifyBeamDiagram(respBeamDisp, "Beam Dz (Displacement)",
        b => b.ResDiagrams?.ResDisplacements?.Dz,
        ExpDz, RelativeTolerance, ref passCount, ref totalCount);

      Console.WriteLine();
      Console.WriteLine("=== Beam Stress Diagram (SxxMax) on ULS Combination ===");

      var respBeamStress = client.GetResults(ResultType.Stresses, idCombo.Value, beamIds);
      PrintResultDetails(respBeamStress.Details, logVerbosityLevel, "GetResults Stresses for beam");

      VerifyBeamDiagram(respBeamStress, "Beam SxxMax (Stresses)",
        b => b.ResDiagrams?.ResStresses?.SxxMax,
        ExpSxxMax, RelativeTolerance, ref passCount, ref totalCount);

      // =====================================================================
      // Final summary
      // =====================================================================

      Console.WriteLine();
      Console.WriteLine($"=== Verification Summary: {passCount}/{totalCount} checks passed ===");
      if (passCount == totalCount)
      {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  ALL CHECKS PASSED.");
      }
      else
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  {totalCount - passCount} CHECK(S) FAILED. Review results above.");
      }
      Console.ForegroundColor = ConsoleColor.White;

      client.CloseProject();
      Console.WriteLine("Close project.");
    }

    // -------------------------------------------------------------------------
    // Verification helpers (portal frame sample)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies Fx, Fy and Fz reaction forces of a punctual support against reference values
    /// using a relative tolerance.
    /// </summary>
    private static void VerifySupportForces(
      ResBaseListApiResponse resp,
      string label,
      double expFx, double expFy, double expFz,
      double relativeTolerance,
      ref int passCount, ref int totalCount)
    {
      if (resp?.Data == null)
      {
        SampleReport.Fail(label, "no data returned, force verification skipped");
        Console.WriteLine($"  {label}: no data returned – skipping force verification.");
        return;
      }

      var sup = resp.Data.OfType<ResPunctualSupport>().FirstOrDefault();
      if (sup?.ResNode?.ResForces == null)
      {
        SampleReport.Fail(label, "no ResPunctualSupport with ResForces found, verification skipped");
        Console.WriteLine($"  {label}: no ResPunctualSupport with ResForces found – skipping.");
        return;
      }

      var f = sup.ResNode.ResForces;
      Console.WriteLine($"  {label}: Fx={f.Fx:F3}  Fy={f.Fy:F3}  Fz={f.Fz:F3}");

      CheckValue($"{label} Fx", f.Fx, expFx, relativeTolerance, ref passCount, ref totalCount);
      CheckValue($"{label} Fy", f.Fy, expFy, relativeTolerance, ref passCount, ref totalCount);
      CheckValue($"{label} Fz", f.Fz, expFz, relativeTolerance, ref passCount, ref totalCount);
    }

    /// <summary>
    /// Verifies a beam result diagram (Fz, My, Dz, SxxMax …) point by point against reference
    /// values using a relative tolerance. The diagram selector is a lambda on ResElementLinear.
    /// </summary>
    private static void VerifyBeamDiagram(
      ResBaseListApiResponse resp,
      string label,
      Func<ResElementLinear, ICollection<ResAbscissaValue>?> diagramSelector,
      (double Abscissa, double Value)[] expected,
      double relativeTolerance,
      ref int passCount, ref int totalCount)
    {
      if (resp?.Data == null)
      {
        SampleReport.Fail(label, "no data returned, diagram verification skipped");
        Console.WriteLine($"  {label}: no data returned – skipping diagram verification.");
        return;
      }

      var beam = resp.Data.OfType<ResElementLinear>().FirstOrDefault();
      if (beam == null)
      {
        SampleReport.Fail(label, "no ResElementLinear found, diagram verification skipped");
        Console.WriteLine($"  {label}: no ResElementLinear found – skipping.");
        return;
      }

      var diagram = diagramSelector(beam);
      if (diagram == null || diagram.Count == 0)
      {
        SampleReport.Fail(label, "diagram is empty, verification skipped");
        Console.WriteLine($"  {label}: diagram is empty – skipping.");
        return;
      }

      var actual = diagram.OrderBy(p => p.Abscissa).ToList();

      if (actual.Count != expected.Length)
      {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  {label}: point count mismatch – expected {expected.Length}, got {actual.Count}. Checking up to min count.");
        Console.ForegroundColor = ConsoleColor.White;
      }

      int count = Math.Min(actual.Count, expected.Length);
      for (int i = 0; i < count; i++)
      {
        string checkLabel = $"{label}[{i}] abscissa≈{expected[i].Abscissa:F4}";
        CheckValue(checkLabel, actual[i].Value, expected[i].Value, relativeTolerance, ref passCount, ref totalCount);
      }
    }

    /// <summary>
    /// Checks one value against a reference using relative tolerance and prints PASS / FAIL.
    /// </summary>
    private static void CheckValue(
      string label, double actual, double expected,
      double relativeTolerance, ref int passCount, ref int totalCount)
    {
      totalCount++;
      double allowedDelta = Math.Abs(expected) * relativeTolerance;
      bool pass = Math.Abs(actual - expected) <= allowedDelta;

      if (pass)
      {
        passCount++;
        SampleReport.Pass(label);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"    PASS  {label}: actual={actual:G6}  expected={expected:G6}  (±{relativeTolerance * 100:F0}%)");
      }
      else
      {
        SampleReport.Fail(label, $"actual={actual:G6}, expected={expected:G6}, allowed=±{relativeTolerance * 100:F0}%");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"    FAIL  {label}: actual={actual:G6}  expected={expected:G6}  " +
          $"diff={Math.Abs(actual - expected):G4}  allowed={allowedDelta:G4}  (±{relativeTolerance * 100:F0}%)");
      }
      Console.ForegroundColor = ConsoleColor.White;
    }
  }
}

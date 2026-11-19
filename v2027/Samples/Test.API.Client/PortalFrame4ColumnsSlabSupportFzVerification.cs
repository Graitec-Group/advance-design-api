using AD.API.Client;

namespace Test.API.Client
{
  internal partial class Program
  {
    /// <summary>
    /// Europe localized verification sample:
    /// - 4 linear elements (C25/30, R20*30): columns at the slab corners, each 5m high
    /// - 1 planar shell element (0.20m thick) forming a 10m x 11m slab at Z=5m
    /// - 4 rigid point supports at the column bases, fully fixed
    /// - 1 live load family + case containing one planar load Fz=-30 kN/m² on the slab
    /// - 1 dead load family + case
    /// - Runs analysis and checks that the sum of the four support Fz values on the live load case
    ///   matches the applied slab load (Fz * area) within 1% tolerance.
    /// </summary>
    static void Sample_SlabOn4ColumnsSupportFzVerificationItReachesSupports(AD_Client client)
    {
      LogVerboseLevel logVerbosityLevel = LogVerboseLevel.ErrorsAndWarnings;
      const double relativeTolerance = 0.01;
      const double slabAreaM2 = 110.0;
      const double planarLoadFzKnPerM2 = -30.0;
      double expectedTotalLoadN = planarLoadFzKnPerM2 * 1000.0 * slabAreaM2;

      Environments env = new Environments()
      {
        Localization = Localization_Code.LOCALIZATION_EUROPE,
        Language = Language_Code.ELanguageEnglish,
        LogVerbosity = logVerbosityLevel
      };

      string projectPath = GetEnvironmentVariable("AD_MODELS_PATH")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                        "Graitec", "Advance Design", "2027", "Projects")
           + Path.DirectorySeparatorChar;

      var newProj = client.NewProject(projectPath + "FzSumInSupports" + Guid.NewGuid().ToString() + ".fto", env);
      PrintResultDetails(newProj.Details, logVerbosityLevel, "Create new project");
      if (!newProj.Details.Success || newProj.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: NewProject failed. Aborting.");
        Console.WriteLine("ERROR: NewProject failed. Aborting.");
        return;
      }

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

      var columnPoints = new (string Label, Pt3D Start, Pt3D End)[]
      {
        ("Column 1", new Pt3D { X =  5.0, Y =  0.0, Z = 0.0 }, new Pt3D { X =  5.0, Y =  0.0, Z = 5.0 }),
        ("Column 2", new Pt3D { X = 15.0, Y =  0.0, Z = 0.0 }, new Pt3D { X = 15.0, Y =  0.0, Z = 5.0 }),
        ("Column 3", new Pt3D { X =  5.0, Y = 11.0, Z = 0.0 }, new Pt3D { X =  5.0, Y = 11.0, Z = 5.0 }),
        ("Column 4", new Pt3D { X = 15.0, Y = 11.0, Z = 0.0 }, new Pt3D { X = 15.0, Y = 11.0, Z = 5.0 })
      };

      List<EID> columnIds = new List<EID>(capacity: columnPoints.Length);
      for (int i = 0; i < columnPoints.Length; i++)
      {
        var column = columnPoints[i];
        var resp = client.CreateElement(new ElementLinear
        {
          Section = idSection,
          Material = idMaterial,
          GeomPtStart = column.Start,
          GeomPtEnd = column.End
        });
        PrintResultDetails(resp.Details, logVerbosityLevel, $"Create {column.Label} ({FormatPoint(column.Start)}→{FormatPoint(column.End)})");
        if (!resp.Details.Success || resp.Details.HasErrors)
        {
          SampleReport.Fatal($"ERROR: CreateElement for {column.Label} failed. Aborting.");
          Console.WriteLine($"ERROR: CreateElement for {column.Label} failed. Aborting.");
          client.CloseProject();
          return;
        }

        columnIds.Add(resp.Data);
        Console.WriteLine($"  {column.Label} OID: {resp.Data.Value}");
      }

      var respSlab = client.CreateElement(new ElementPlanar
      {
        Material = idMaterial,
        ElementType = PlanarElementType.Shell,
        ThicknessIn1stVertex = 0.20,
        SlopeX = 0.0,
        SlopeY = 0.0,
        Eccentricity = 0.0,
        GeomPtsList = new List<Pt3D>
        {
          new Pt3D { X =  5.0, Y = 11.0, Z = 5.0 },
          new Pt3D { X =  5.0, Y =  0.0, Z = 5.0 },
          new Pt3D { X = 15.0, Y =  0.0, Z = 5.0 },
          new Pt3D { X = 15.0, Y = 11.0, Z = 5.0 }
        }
      });
      PrintResultDetails(respSlab.Details, logVerbosityLevel, "Create planar slab element (10m x 11m, thickness 0.20m)");
      if (!respSlab.Details.Success || respSlab.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: CreateElement for planar slab failed. Aborting.");
        Console.WriteLine("ERROR: CreateElement for planar slab failed. Aborting.");
        client.CloseProject();
        return;
      }
      EID idSlab = respSlab.Data;
      Console.WriteLine("  Slab OID: " + idSlab.Value);

      var supportPoints = new (string Label, Pt3D Point)[]
      {
        ("Support 1", new Pt3D { X =  5.0, Y =  0.0, Z = 0.0 }),
        ("Support 2", new Pt3D { X = 15.0, Y =  0.0, Z = 0.0 }),
        ("Support 3", new Pt3D { X =  5.0, Y = 11.0, Z = 0.0 }),
        ("Support 4", new Pt3D { X = 15.0, Y = 11.0, Z = 0.0 })
      };

      List<(string Label, EID Id)> supportIds = new List<(string Label, EID Id)>(capacity: supportPoints.Length);
      foreach (var supportPoint in supportPoints)
      {
        var resp = client.CreateElement(new ElementRigidPunctualSupport
        {
          GeomPt = supportPoint.Point,
          Material = idMaterial,
          Restraints = new DegreeOfFreedomRestraints
          {
            Tx = true,
            Ty = true,
            Tz = true,
            Rx = true,
            Ry = true,
            Rz = true
          }
        });
        PrintResultDetails(resp.Details, logVerbosityLevel, $"Create {supportPoint.Label} at {FormatPoint(supportPoint.Point)}");
        if (!resp.Details.Success || resp.Details.HasErrors)
        {
          SampleReport.Fatal($"ERROR: CreateElement for {supportPoint.Label} failed. Aborting.");
          Console.WriteLine($"ERROR: CreateElement for {supportPoint.Label} failed. Aborting.");
          client.CloseProject();
          return;
        }

        supportIds.Add((supportPoint.Label, resp.Data));
        Console.WriteLine($"  {supportPoint.Label} OID: {resp.Data.Value}");
      }

      var respLiveFamily = client.CreateInformationalElement(new LoadCaseFamily_LiveLoads { Name = "Live Loads Family" });
      PrintResultDetails(respLiveFamily.Details, logVerbosityLevel, "Create live loads family");
      if (!respLiveFamily.Details.Success || respLiveFamily.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: CreateInformationalElement for live loads family failed. Aborting.");
        Console.WriteLine("ERROR: CreateInformationalElement for live loads family failed. Aborting.");
        client.CloseProject();
        return;
      }
      EID idLiveFamily = respLiveFamily.Data;
      Console.WriteLine("  Live Loads Family OID: " + idLiveFamily.Value);

      var respLiveCase = client.CreateInformationalElement(new LoadCase_LiveLoads
      {
        Name = "Live Loads",
        LoadCaseFamilyID = idLiveFamily,
        LiveLoadCategory = 0
      });
      PrintResultDetails(respLiveCase.Details, logVerbosityLevel, "Create live load case");
      if (!respLiveCase.Details.Success || respLiveCase.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: CreateInformationalElement for live load case failed. Aborting.");
        Console.WriteLine("ERROR: CreateInformationalElement for live load case failed. Aborting.");
        client.CloseProject();
        return;
      }
      EID idLiveCase = respLiveCase.Data;
      Console.WriteLine("  Live Load Case OID: " + idLiveCase.Value);

      var respPlanarLoad = client.CreateElement(new ElementLoadPlanar
      {
        GeomPtsList = new List<Pt3D>
        {
          new Pt3D { X =  5.0, Y = 11.0, Z = 5.0 },
          new Pt3D { X =  5.0, Y =  0.0, Z = 5.0 },
          new Pt3D { X = 15.0, Y =  0.0, Z = 5.0 },
          new Pt3D { X = 15.0, Y = 11.0, Z = 5.0 }
        },
        CoordinateSystemType = CoordinateSystemForLinearOrPlanarGeometry.Global_or_user,
        LoadCase = idLiveCase,
        Fx = 0.0,
        Fy = 0.0,
        Fz = planarLoadFzKnPerM2 * 1000.0,
        Moment = new MomentComponents { Mx = 0.0, My = 0.0, Mz = 0.0 },
        Variation = new PlanarVariation
        {
          Coefficient1 = 1.0,
          Coefficient2 = 1.0,
          Coefficient3 = 1.0
        }
      });
      PrintResultDetails(respPlanarLoad.Details, logVerbosityLevel, "Create planar live load Fz=-30kN/m² on slab");
      if (!respPlanarLoad.Details.Success || respPlanarLoad.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: CreateElement for planar load failed. Aborting.");
        Console.WriteLine("ERROR: CreateElement for planar load failed. Aborting.");
        client.CloseProject();
        return;
      }
      Console.WriteLine("  Planar load OID: " + respPlanarLoad.Data.Value);

      var respDeadFamily = client.CreateInformationalElement(new LoadCaseFamily_DeadLoads { Name = "Dead Loads Family" });
      PrintResultDetails(respDeadFamily.Details, logVerbosityLevel, "Create dead loads family");
      if (!respDeadFamily.Details.Success || respDeadFamily.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: CreateInformationalElement for dead loads family failed. Aborting.");
        Console.WriteLine("ERROR: CreateInformationalElement for dead loads family failed. Aborting.");
        client.CloseProject();
        return;
      }
      EID idDeadFamily = respDeadFamily.Data;
      Console.WriteLine("  Dead Loads Family OID: " + idDeadFamily.Value);

      var respDeadCase = client.CreateInformationalElement(new LoadCase_DeadLoads
      {
        Name = "Dead Loads",
        LoadCaseFamilyID = idDeadFamily
      });
      PrintResultDetails(respDeadCase.Details, logVerbosityLevel, "Create dead load case");
      if (!respDeadCase.Details.Success || respDeadCase.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: CreateInformationalElement for dead load case failed. Aborting.");
        Console.WriteLine("ERROR: CreateInformationalElement for dead load case failed. Aborting.");
        client.CloseProject();
        return;
      }
      EID idDeadCase = respDeadCase.Data;
      Console.WriteLine("  Dead Load Case OID: " + idDeadCase.Value);

      var respAnalysis = client.LaunchAnalysis();
      PrintResultDetails(respAnalysis.Details, logVerbosityLevel, "Launch analysis");
      if (!respAnalysis.Data)
      {
        SampleReport.Fatal("ERROR: Analysis failed. Cannot retrieve support results.");
        Console.WriteLine("ERROR: Analysis failed. Cannot retrieve support results.");
        client.CloseProject();
        return;
      }
      Console.WriteLine("  Analysis result: succeeded");

      Console.WriteLine();
      Console.WriteLine("=== Live load case support Fz verification ===");
      Console.WriteLine($"  Slab OID used for analysis: {idSlab.Value}");
      Console.WriteLine($"  Dead load case created: {idDeadCase.Value}");
      Console.WriteLine($"  Expected applied load = {planarLoadFzKnPerM2:F3} kN/m² * {slabAreaM2:F3} m² = {expectedTotalLoadN:F3} N");

      double summedSupportFz = 0.0;
      bool allSupportResultsFound = true;

      foreach (var support in supportIds)
      {
        var respSupportForces = client.GetResults(ResultType.Forces, idLiveCase.Value, new List<long> { support.Id.Value });
        PrintResultDetails(respSupportForces.Details, logVerbosityLevel, $"GetResults Forces for {support.Label} on live load case");

        if (!TryGetSupportFz(respSupportForces, out double supportFz))
        {
          allSupportResultsFound = false;
          Console.WriteLine($"  {support.Label}: no support force result found.");
          continue;
        }

        summedSupportFz += supportFz;
        Console.WriteLine($"  {support.Label}: Fz = {supportFz:F3} N");
      }

      double absoluteExpected = Math.Abs(expectedTotalLoadN);
      double signedDifference = Math.Abs(summedSupportFz - expectedTotalLoadN);
      double oppositeSignDifference = Math.Abs(summedSupportFz + expectedTotalLoadN);
      bool matchesSigned = signedDifference <= absoluteExpected * relativeTolerance;
      bool matchesOppositeSign = oppositeSignDifference <= absoluteExpected * relativeTolerance;
      bool verificationPassed = allSupportResultsFound && (matchesSigned || matchesOppositeSign);
      string signConventionMessage = matchesSigned
        ? "matched the applied load sign."
        : matchesOppositeSign
          ? "matched the opposite sign, which is consistent with reaction-force sign convention."
          : "did not match either sign convention within tolerance.";

      Console.WriteLine();
      Console.WriteLine($"  Support Fz sum = {summedSupportFz:F3} N");
      Console.WriteLine($"  Signed difference to expected load = {signedDifference:F3} N");
      Console.WriteLine($"  Opposite-sign difference to expected load = {oppositeSignDifference:F3} N");
      Console.WriteLine($"  1% tolerance band = {absoluteExpected * relativeTolerance:F3} N");

      Console.ForegroundColor = verificationPassed ? ConsoleColor.Green : ConsoleColor.Red;
      SampleReport.Check(verificationPassed, "Sum of support Fz matches the applied load", signConventionMessage);
      Console.WriteLine($"  VERIFICATION {(verificationPassed ? "TRUE" : "FALSE")}: {signConventionMessage}");
      Console.ForegroundColor = ConsoleColor.White;

      var closeProjectResponse = client.CloseProject();
      PrintResultDetails(closeProjectResponse.Details, logVerbosityLevel, "Close project");
      Console.WriteLine("Close project.");
    }

    private static bool TryGetSupportFz(ResBaseListApiResponse response, out double supportFz)
    {
      supportFz = 0.0;

      var punctualSupport = response.Data?.OfType<ResPunctualSupport>().FirstOrDefault();
      if (punctualSupport?.ResNode?.ResForces == null)
        return false;

      supportFz = punctualSupport.ResNode.ResForces.Fz;
      return true;
    }

    private static string FormatPoint(Pt3D point) => $"({point.X:F2},{point.Y:F2},{point.Z:F2})";
  }
}

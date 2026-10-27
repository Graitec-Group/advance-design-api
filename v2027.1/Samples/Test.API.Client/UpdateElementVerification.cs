using AD.API.Client;

namespace Test.API.Client
{
  internal partial class Program
  {
    /// <summary>
    /// UpdateElement verification sample:
    /// - Creates a linear element (column) and updates several properties
    /// - Creates a planar element (wall with opening) and updates geometry, properties, and openings
    /// - Retrieves elements via GetElementsObject
    /// - Verifies that the updated properties match
    /// </summary>
    static void Sample_UpdateElementVerification(AD.API.Client.AD_Client client)
    {
      LogVerboseLevel logVerbosityLevel = LogVerboseLevel.ErrorsAndWarnings;

      Environments env = new Environments()
      {
        Localization = Localization_Code.LOCALIZATION_FRANCE,
        LogVerbosity  = logVerbosityLevel
      };

      string projectPath = GetEnvironmentVariable("AD_MODELS_PATH")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                        "Graitec", "Advance Design", "2027", "Projects")
           + Path.DirectorySeparatorChar;

      string savedProjectFile = projectPath + "UpdateElmVerif_" + Guid.NewGuid().ToString() + ".fto";

      // ── Create project ────────────────────────────────────────────────────────
      var respNewProj = client.NewProject(savedProjectFile, env);
      PrintResultDetails(respNewProj.Details, logVerbosityLevel, "NewProject");
      if (!respNewProj.Details.Success || respNewProj.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: NewProject failed. Aborting.");
        Console.WriteLine("ERROR: NewProject failed. Aborting.");
        return;
      }

      // ── Create material C25/30 ────────────────────────────────────────────────
      var respMaterial = client.CreateMaterial(new Material { Name = "C25/30" });
      PrintResultDetails(respMaterial.Details, logVerbosityLevel, "CreateMaterial C25/30");
      if (!respMaterial.Details.Success || respMaterial.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: CreateMaterial C25/30 failed. Aborting.");
        Console.WriteLine("ERROR: CreateMaterial C25/30 failed. Aborting.");
        client.CloseProject();
        return;
      }
      EID idMaterial = respMaterial.Data;
      Console.WriteLine("  Material OID: " + idMaterial.Value);

      // ── Create section R30*50 ─────────────────────────────────────────────────
      var respSection = client.CreateSection("R30*50");
      PrintResultDetails(respSection.Details, logVerbosityLevel, "CreateSection R30*50");
      if (!respSection.Details.Success || respSection.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: CreateSection R30*50 failed. Aborting.");
        Console.WriteLine("ERROR: CreateSection R30*50 failed. Aborting.");
        client.CloseProject();
        return;
      }
      EID idSection = respSection.Data;
      Console.WriteLine("  Section OID: " + idSection.Value);

      const double tolerance = 0.01;
      bool allPassed = true;

      bool CheckProp<T>(string propName, T expected, T actual, bool pass)
      {
        if (pass)
        {
          SampleReport.Pass(propName);
          Console.ForegroundColor = ConsoleColor.Green;
          Console.WriteLine($"  [PASS] {propName}: expected={expected}, actual={actual}");
        }
        else
        {
          SampleReport.Fail(propName, $"expected={expected}, actual={actual}");
          Console.ForegroundColor = ConsoleColor.Red;
          Console.WriteLine($"  [FAIL] {propName}: expected={expected}, actual={actual}");
          allPassed = false;
        }
        Console.ForegroundColor = ConsoleColor.White;
        return pass;
      }

      // ══════════════════════════════════════════════════════════════════════════
      // PART 1: LINEAR ELEMENT UPDATE VERIFICATION
      // ══════════════════════════════════════════════════════════════════════════

      Console.WriteLine();
      Console.WriteLine("══════════════════════════════════════════════════════════");
      Console.WriteLine("  PART 1: Linear Element Update Verification");
      Console.WriteLine("══════════════════════════════════════════════════════════");

      // ── Create default column: (0,0,0) → (0,0,4) ─────────────────────────────
      EID idColumn;
      {
        var el = new ElementLinear
        {
          Section     = idSection,
          Material    = idMaterial,
          GeomPtStart = new Pt3D { X = 0.0, Y = 0.0, Z = 0.0 },
          GeomPtEnd   = new Pt3D { X = 0.0, Y = 0.0, Z = 4.0 }
        };
        var resp = client.CreateElement(el);
        PrintResultDetails(resp.Details, logVerbosityLevel, "CreateElement (default column)");
        if (!resp.Details.Success || resp.Details.HasErrors)
        {
          SampleReport.Fatal("ERROR: CreateElement failed. Aborting.");
          Console.WriteLine("ERROR: CreateElement failed. Aborting.");
          client.CloseProject();
          return;
        }
        idColumn = resp.Data;
        Console.WriteLine("  Column OID: " + idColumn.Value);
      }

      // ── Build updated element ─────────────────────────────────────────────────
      const double updatedOrientationAngle = 25.0;

      var updatedElement = new ElementLinear
      {
        Section     = idSection,
        Material    = idMaterial,
        GeomPtStart = new Pt3D { X = 1.0, Y = 0.0, Z = 0.0 },
        GeomPtEnd   = new Pt3D { X = 1.0, Y = 0.0, Z = 5.0 },
        SectionOrientationAngle = updatedOrientationAngle,
        AutoMesh    = false,
        ExtendIntoWall = true,
        ElementActive = false,
        DetailedMeshProperties = new LinearElementMeshStyle
        {
          NumberOfElements = 6,
          SizeOfElements = 0.8,
          SpacingOption = 0,
          SpacingFactor = 1.2
        },
        InertiaProperties = new LinearElementInnertia
        {
          LinearConcreteInertiaType = 1,
          LinearCoeffCrackedInertia = 0.8,
          LinearCoeffCrackedInertiaIz = 0.7,
          LinearCoeffCrackedInertiaTorsion = 0.5,
          LinearCoeffCrackedInertiaAxialStiff = 0.9,
          ProductOfInertiaIyz0 = true
        },
        SectionExcentration = new SectionOffsetStyle
        {
          Option = SectionOffsetOption_API.Center_alignment,
          DeltaY1 = 0.05,
          DeltaZ1 = 0.02,
          DeltaY2 = 0.03,
          DeltaZ2 = 0.01,
          ConsideredInFEM = true
        },
        LoadAreaLoadTransferProperties = new LinearLoadAreaLoadTransfer
        {
          LoadSpanFront = 2.5,
          LoadSpanBehind = 1.5,
          ContinuityCoeff = 1.3,
          SnowGuards2D = true,
          ClimaticLE2D = true,
          IsScaffoldingSupport = true,
          IsWindWallSupport = true
        },
        RelaxationTotale = new ConnectionTotale
        {
          StartBoundaryConnection = new ConnectionStyle
          {
            RelaxationTx = false, RelaxationTy = true, RelaxationTz = false,
            RelaxationRx = false, RelaxationRy = false, RelaxationRz = false
          },
          EndBoundaryConnection = new ConnectionStyle
          {
            RelaxationTx = false, RelaxationTy = false, RelaxationTz = false,
            RelaxationRx = false, RelaxationRy = true, RelaxationRz = false
          }
        }
      };

      // ── Call UpdateElement ─────────────────────────────────────────────────────
      Console.WriteLine();
      Console.WriteLine("=== Calling UpdateElement (Linear) ===");
      var updateResp = client.UpdateElement(idColumn.Value, updatedElement);
      PrintResultDetails(updateResp.Details, logVerbosityLevel, "UpdateElement (Linear)");
      Console.WriteLine($"  UpdateElement returned Data={updateResp.Data}");

      if (!updateResp.Details.Success || updateResp.Details.HasErrors || !updateResp.Data)
      {
        SampleReport.Fatal("ERROR: UpdateElement (Linear) failed. Aborting verification.");
        Console.WriteLine("ERROR: UpdateElement (Linear) failed. Aborting verification.");
        client.CloseProject();
        return;
      }

      // ── Retrieve element via GetElementsObject ────────────────────────────────
      var getResp = client.GetElementsObject(new List<long> { idColumn.Value });
      PrintResultDetails(getResp.Details, logVerbosityLevel, $"GetElementsObject (EID={idColumn.Value})");

      if (getResp.Data?.FirstOrDefault() is not ElementLinear lin)
      {
        SampleReport.Fatal("ERROR: Retrieved element is null or not ElementLinear. Aborting.");
        Console.WriteLine("ERROR: Retrieved element is null or not ElementLinear. Aborting.");
        client.CloseProject();
        return;
      }

      // ── Verify linear properties ──────────────────────────────────────────────
      Console.WriteLine();
      Console.WriteLine("=== Linear Element property verification ===");

      // Geometry
      CheckProp("GeomPtStart.X", 1.0, lin.GeomPtStart.X, Math.Abs(lin.GeomPtStart.X - 1.0) <= tolerance);
      CheckProp("GeomPtStart.Y", 0.0, lin.GeomPtStart.Y, Math.Abs(lin.GeomPtStart.Y - 0.0) <= tolerance);
      CheckProp("GeomPtStart.Z", 0.0, lin.GeomPtStart.Z, Math.Abs(lin.GeomPtStart.Z - 0.0) <= tolerance);
      CheckProp("GeomPtEnd.X", 1.0, lin.GeomPtEnd.X, Math.Abs(lin.GeomPtEnd.X - 1.0) <= tolerance);
      CheckProp("GeomPtEnd.Y", 0.0, lin.GeomPtEnd.Y, Math.Abs(lin.GeomPtEnd.Y - 0.0) <= tolerance);
      CheckProp("GeomPtEnd.Z", 5.0, lin.GeomPtEnd.Z, Math.Abs(lin.GeomPtEnd.Z - 5.0) <= tolerance);

      // Scalar properties
      CheckProp("SectionOrientationAngle", updatedOrientationAngle, lin.SectionOrientationAngle ?? 0.0,
        Math.Abs((lin.SectionOrientationAngle ?? 0.0) - updatedOrientationAngle) <= tolerance);
      CheckProp("AutoMesh", false, lin.AutoMesh ?? true, lin.AutoMesh == false);
      CheckProp("ExtendIntoWall", true, lin.ExtendIntoWall ?? false, lin.ExtendIntoWall == true);
      CheckProp("ElementActive", false, lin.ElementActive ?? true, lin.ElementActive == false);

      // Mesh properties
      if (lin.DetailedMeshProperties != null)
      {
        CheckProp("Mesh.NumberOfElements", 6, lin.DetailedMeshProperties.NumberOfElements,
          lin.DetailedMeshProperties.NumberOfElements == 6);
        CheckProp("Mesh.SizeOfElements", 0.8, lin.DetailedMeshProperties.SizeOfElements,
          Math.Abs(lin.DetailedMeshProperties.SizeOfElements - 0.8) <= tolerance);
        CheckProp("Mesh.SpacingFactor", 1.2, lin.DetailedMeshProperties.SpacingFactor,
          Math.Abs(lin.DetailedMeshProperties.SpacingFactor - 1.2) <= tolerance);
      }
      else
      {
        Console.ForegroundColor = ConsoleColor.Red;
        SampleReport.Fail("DetailedMeshProperties", "is null");
        Console.WriteLine("  [FAIL] DetailedMeshProperties is null.");
        Console.ForegroundColor = ConsoleColor.White;
        allPassed = false;
      }

      // Inertia properties
      if (lin.InertiaProperties != null)
      {
        CheckProp("Inertia.Type", 1, lin.InertiaProperties.LinearConcreteInertiaType,
          lin.InertiaProperties.LinearConcreteInertiaType == 1);
        CheckProp("Inertia.CrackedInertia", 0.8, lin.InertiaProperties.LinearCoeffCrackedInertia,
          Math.Abs(lin.InertiaProperties.LinearCoeffCrackedInertia - 0.8) <= tolerance);
        CheckProp("Inertia.CrackedInertiaIz", 0.7, lin.InertiaProperties.LinearCoeffCrackedInertiaIz,
          Math.Abs(lin.InertiaProperties.LinearCoeffCrackedInertiaIz - 0.7) <= tolerance);
        CheckProp("Inertia.CrackedInertiaTorsion", 0.5, lin.InertiaProperties.LinearCoeffCrackedInertiaTorsion,
          Math.Abs(lin.InertiaProperties.LinearCoeffCrackedInertiaTorsion - 0.5) <= tolerance);
        CheckProp("Inertia.CrackedInertiaAxialStiff", 0.9, lin.InertiaProperties.LinearCoeffCrackedInertiaAxialStiff,
          Math.Abs(lin.InertiaProperties.LinearCoeffCrackedInertiaAxialStiff - 0.9) <= tolerance);
        CheckProp("Inertia.ProductOfInertiaIyz0", true, lin.InertiaProperties.ProductOfInertiaIyz0,
          lin.InertiaProperties.ProductOfInertiaIyz0 == true);
      }
      else
      {
        Console.ForegroundColor = ConsoleColor.Red;
        SampleReport.Fail("InertiaProperties", "is null");
        Console.WriteLine("  [FAIL] InertiaProperties is null.");
        Console.ForegroundColor = ConsoleColor.White;
        allPassed = false;
      }

      // Section offset
      if (lin.SectionExcentration != null)
      {
        CheckProp("Offset.Option", SectionOffsetOption_API.Center_alignment, lin.SectionExcentration.Option,
          lin.SectionExcentration.Option == SectionOffsetOption_API.Center_alignment);
        CheckProp("Offset.DeltaY1", 0.05, lin.SectionExcentration.DeltaY1,
          Math.Abs(lin.SectionExcentration.DeltaY1 - 0.05) <= 0.001);
        CheckProp("Offset.DeltaZ1", 0.02, lin.SectionExcentration.DeltaZ1,
          Math.Abs(lin.SectionExcentration.DeltaZ1 - 0.02) <= 0.001);
        CheckProp("Offset.DeltaY2", 0.03, lin.SectionExcentration.DeltaY2,
          Math.Abs(lin.SectionExcentration.DeltaY2 - 0.03) <= 0.001);
        CheckProp("Offset.DeltaZ2", 0.01, lin.SectionExcentration.DeltaZ2,
          Math.Abs(lin.SectionExcentration.DeltaZ2 - 0.01) <= 0.001);
        CheckProp("Offset.ConsideredInFEM", true, lin.SectionExcentration.ConsideredInFEM,
          lin.SectionExcentration.ConsideredInFEM == true);
      }
      else
      {
        Console.ForegroundColor = ConsoleColor.Red;
        SampleReport.Fail("SectionExcentration", "is null");
        Console.WriteLine("  [FAIL] SectionExcentration is null.");
        Console.ForegroundColor = ConsoleColor.White;
        allPassed = false;
      }

      // Load area transfer
      if (lin.LoadAreaLoadTransferProperties != null)
      {
        CheckProp("LoadArea.LoadSpanFront", 2.5, lin.LoadAreaLoadTransferProperties.LoadSpanFront,
          Math.Abs(lin.LoadAreaLoadTransferProperties.LoadSpanFront - 2.5) <= tolerance);
        CheckProp("LoadArea.LoadSpanBehind", 1.5, lin.LoadAreaLoadTransferProperties.LoadSpanBehind,
          Math.Abs(lin.LoadAreaLoadTransferProperties.LoadSpanBehind - 1.5) <= tolerance);
        CheckProp("LoadArea.ContinuityCoeff", 1.3, lin.LoadAreaLoadTransferProperties.ContinuityCoeff,
          Math.Abs(lin.LoadAreaLoadTransferProperties.ContinuityCoeff - 1.3) <= tolerance);
        CheckProp("LoadArea.SnowGuards2D", true, lin.LoadAreaLoadTransferProperties.SnowGuards2D,
          lin.LoadAreaLoadTransferProperties.SnowGuards2D == true);
        CheckProp("LoadArea.ClimaticLE2D", true, lin.LoadAreaLoadTransferProperties.ClimaticLE2D,
          lin.LoadAreaLoadTransferProperties.ClimaticLE2D == true);
        CheckProp("LoadArea.IsScaffoldingSupport", true, lin.LoadAreaLoadTransferProperties.IsScaffoldingSupport,
          lin.LoadAreaLoadTransferProperties.IsScaffoldingSupport == true);
        CheckProp("LoadArea.IsWindWallSupport", true, lin.LoadAreaLoadTransferProperties.IsWindWallSupport,
          lin.LoadAreaLoadTransferProperties.IsWindWallSupport == true);
      }
      else
      {
        Console.ForegroundColor = ConsoleColor.Red;
        SampleReport.Fail("LoadAreaLoadTransferProperties", "is null");
        Console.WriteLine("  [FAIL] LoadAreaLoadTransferProperties is null.");
        Console.ForegroundColor = ConsoleColor.White;
        allPassed = false;
      }

      // Relaxation
      if (lin.RelaxationTotale?.StartBoundaryConnection != null)
      {
        CheckProp("StartRelaxation.Tx", false, lin.RelaxationTotale.StartBoundaryConnection.RelaxationTx,
          lin.RelaxationTotale.StartBoundaryConnection.RelaxationTx == false);
        CheckProp("StartRelaxation.Ty", true, lin.RelaxationTotale.StartBoundaryConnection.RelaxationTy,
          lin.RelaxationTotale.StartBoundaryConnection.RelaxationTy == true);
        CheckProp("StartRelaxation.Tz", false, lin.RelaxationTotale.StartBoundaryConnection.RelaxationTz,
          lin.RelaxationTotale.StartBoundaryConnection.RelaxationTz == false);
        CheckProp("StartRelaxation.Rx", false, lin.RelaxationTotale.StartBoundaryConnection.RelaxationRx,
          lin.RelaxationTotale.StartBoundaryConnection.RelaxationRx == false);
        CheckProp("StartRelaxation.Ry", false, lin.RelaxationTotale.StartBoundaryConnection.RelaxationRy,
          lin.RelaxationTotale.StartBoundaryConnection.RelaxationRy == false);
        CheckProp("StartRelaxation.Rz", false, lin.RelaxationTotale.StartBoundaryConnection.RelaxationRz,
          lin.RelaxationTotale.StartBoundaryConnection.RelaxationRz == false);
      }
      else
      {
        Console.ForegroundColor = ConsoleColor.Red;
        SampleReport.Fail("StartBoundaryConnection", "is null");
        Console.WriteLine("  [FAIL] StartBoundaryConnection is null.");
        Console.ForegroundColor = ConsoleColor.White;
        allPassed = false;
      }

      if (lin.RelaxationTotale?.EndBoundaryConnection != null)
      {
        CheckProp("EndRelaxation.Tx", false, lin.RelaxationTotale.EndBoundaryConnection.RelaxationTx,
          lin.RelaxationTotale.EndBoundaryConnection.RelaxationTx == false);
        CheckProp("EndRelaxation.Ty", false, lin.RelaxationTotale.EndBoundaryConnection.RelaxationTy,
          lin.RelaxationTotale.EndBoundaryConnection.RelaxationTy == false);
        CheckProp("EndRelaxation.Tz", false, lin.RelaxationTotale.EndBoundaryConnection.RelaxationTz,
          lin.RelaxationTotale.EndBoundaryConnection.RelaxationTz == false);
        CheckProp("EndRelaxation.Rx", false, lin.RelaxationTotale.EndBoundaryConnection.RelaxationRx,
          lin.RelaxationTotale.EndBoundaryConnection.RelaxationRx == false);
        CheckProp("EndRelaxation.Ry", true, lin.RelaxationTotale.EndBoundaryConnection.RelaxationRy,
          lin.RelaxationTotale.EndBoundaryConnection.RelaxationRy == true);
        CheckProp("EndRelaxation.Rz", false, lin.RelaxationTotale.EndBoundaryConnection.RelaxationRz,
          lin.RelaxationTotale.EndBoundaryConnection.RelaxationRz == false);
      }
      else
      {
        Console.ForegroundColor = ConsoleColor.Red;
        SampleReport.Fail("EndBoundaryConnection", "is null");
        Console.WriteLine("  [FAIL] EndBoundaryConnection is null.");
        Console.ForegroundColor = ConsoleColor.White;
        allPassed = false;
      }

      // ══════════════════════════════════════════════════════════════════════════
      // PART 2: PLANAR ELEMENT UPDATE VERIFICATION
      // ══════════════════════════════════════════════════════════════════════════

      Console.WriteLine();
      Console.WriteLine("══════════════════════════════════════════════════════════");
      Console.WriteLine("  PART 2: Planar Element Update Verification");
      Console.WriteLine("══════════════════════════════════════════════════════════");

      // ── Create planar element (wall 6m×4m at Y=0, with 1 opening) ─────────────
      EID idWall;
      {
        var el = new ElementPlanar
        {
          Material = idMaterial,
          ElementType = PlanarElementType.Shell,
          GeomPtsList = new List<Pt3D>
          {
            new() { X = 0, Y = 0, Z = 0 },
            new() { X = 6, Y = 0, Z = 0 },
            new() { X = 6, Y = 0, Z = 4 },
            new() { X = 0, Y = 0, Z = 4 }
          },
          ThicknessIn1stVertex = 0.2,
          Openings = new List<ICollection<Pt3D>>
          {
            new List<Pt3D>
            {
              new() { X = 1.0, Y = 0, Z = 1.0 },
              new() { X = 1.0, Y = 0, Z = 2.5 },
              new() { X = 2.5, Y = 0, Z = 2.5 },
              new() { X = 2.5, Y = 0, Z = 1.0 }
            }
          },
          MeshProperties = new PlanarElementMeshProperties
          {
            AutomaticMesh = true,
            MeshType = PlanarMeshType.Complete,
            MeshDensity = PlanarMeshDensity.Element_size,
            MeshElementSize = 1.0
          }
        };
        var resp = client.CreateElement(el);
        PrintResultDetails(resp.Details, logVerbosityLevel, "CreateElement (planar wall with opening)");
        if (!resp.Details.Success || resp.Details.HasErrors)
        {
          SampleReport.Fatal("ERROR: CreateElement (planar) failed. Aborting planar verification.");
          Console.WriteLine("ERROR: CreateElement (planar) failed. Aborting planar verification.");
          client.CloseProject();
          return;
        }
        idWall = resp.Data;
        Console.WriteLine("  Wall OID: " + idWall.Value);
      }

      // ── Update planar element: change type, thickness, mesh, geometry, and replace opening ──
      var updatedPlanar = new ElementPlanar
      {
        Material = idMaterial,
        ElementType = PlanarElementType.Plate,
        GeomPtsList = new List<Pt3D>
        {
          new() { X = 0, Y = 0, Z = 0 },
          new() { X = 8, Y = 0, Z = 0 },
          new() { X = 8, Y = 0, Z = 5 },
          new() { X = 0, Y = 0, Z = 5 }
        },
        ThicknessIn1stVertex = 0.3,
        Eccentricity = 0.1,
        SlopeX = 0.01,
        SlopeY = 0.02,
        SupportingElement = true,
        Openings = new List<ICollection<Pt3D>>
        {
          new List<Pt3D>
          {
            new() { X = 1.0, Y = 0, Z = 1.0 },
            new() { X = 1.0, Y = 0, Z = 3.0 },
            new() { X = 3.0, Y = 0, Z = 3.0 },
            new() { X = 3.0, Y = 0, Z = 1.0 }
          },
          new List<Pt3D>
          {
            new() { X = 5.0, Y = 0, Z = 1.5 },
            new() { X = 5.0, Y = 0, Z = 4.0 },
            new() { X = 7.0, Y = 0, Z = 4.0 },
            new() { X = 7.0, Y = 0, Z = 1.5 }
          }
        },
        MeshProperties = new PlanarElementMeshProperties
        {
          AutomaticMesh = false,
          MeshType = PlanarMeshType.Triangulation,
          MeshDensity = PlanarMeshDensity.Element_size,
          MeshElementSize = 0.5
        }
      };

      Console.WriteLine();
      Console.WriteLine("=== Calling UpdateElement (Planar) ===");
      var updatePlanarResp = client.UpdateElement(idWall.Value, updatedPlanar);
      PrintResultDetails(updatePlanarResp.Details, logVerbosityLevel, "UpdateElement (Planar)");
      Console.WriteLine($"  UpdateElement returned Data={updatePlanarResp.Data}");

      if (!updatePlanarResp.Details.Success || updatePlanarResp.Details.HasErrors || !updatePlanarResp.Data)
      {
        SampleReport.Fatal("ERROR: UpdateElement (Planar) failed. Aborting planar verification.");
        Console.WriteLine("ERROR: UpdateElement (Planar) failed. Aborting planar verification.");
        client.CloseProject();
        return;
      }

      // ── Retrieve planar element ───────────────────────────────────────────────
      var getPlanarResp = client.GetElementsObject(new List<long> { idWall.Value });
      PrintResultDetails(getPlanarResp.Details, logVerbosityLevel, $"GetElementsObject (EID={idWall.Value})");

      if (getPlanarResp.Data?.FirstOrDefault() is not ElementPlanar planar)
      {
        SampleReport.Fatal("ERROR: Retrieved element is null or not ElementPlanar. Aborting.");
        Console.WriteLine("ERROR: Retrieved element is null or not ElementPlanar. Aborting.");
        client.CloseProject();
        return;
      }

      // ── Verify planar properties ──────────────────────────────────────────────
      Console.WriteLine();
      Console.WriteLine("=== Planar Element property verification ===");

      CheckProp("ElementType", PlanarElementType.Plate, planar.ElementType,
        planar.ElementType == PlanarElementType.Plate);
      CheckProp("ThicknessIn1stVertex", 0.3, planar.ThicknessIn1stVertex,
        Math.Abs(planar.ThicknessIn1stVertex - 0.3) <= tolerance);
      CheckProp("Eccentricity", 0.1, planar.Eccentricity,
        Math.Abs(planar.Eccentricity - 0.1) <= tolerance);
      CheckProp("SlopeX", 0.01, planar.SlopeX,
        Math.Abs(planar.SlopeX - 0.01) <= tolerance);
      CheckProp("SlopeY", 0.02, planar.SlopeY,
        Math.Abs(planar.SlopeY - 0.02) <= tolerance);
      CheckProp("SupportingElement", true, planar.SupportingElement,
        planar.SupportingElement == true);

      // Geometry (updated to 8×5)
      CheckProp("GeomPtsList.Count", 4, planar.GeomPtsList?.Count ?? 0,
        planar.GeomPtsList?.Count == 4);

      if (planar.GeomPtsList != null && planar.GeomPtsList.Count >= 4)
      {
        var pt2 = planar.GeomPtsList.ElementAt(1);
        CheckProp("GeomPt[1].X (width)", 8.0, pt2.X, Math.Abs(pt2.X - 8.0) <= tolerance);
        var pt3 = planar.GeomPtsList.ElementAt(2);
        CheckProp("GeomPt[2].Z (height)", 5.0, pt3.Z, Math.Abs(pt3.Z - 5.0) <= tolerance);
      }

      // Openings (should have 2 after update)
      CheckProp("Openings.Count", 2, planar.Openings?.Count ?? 0,
        planar.Openings?.Count == 2);

      if (planar.Openings != null && planar.Openings.Count >= 2)
      {
        CheckProp("Opening[0].PointCount", 4, planar.Openings.First().Count,
          planar.Openings.First().Count == 4);
        CheckProp("Opening[1].PointCount", 4, planar.Openings.ElementAt(1).Count,
          planar.Openings.ElementAt(1).Count == 4);
      }

      // Mesh properties
      if (planar.MeshProperties != null)
      {
        CheckProp("MeshProperties.AutoMesh", false, planar.MeshProperties.AutomaticMesh,
          planar.MeshProperties.AutomaticMesh == false);
        CheckProp("MeshProperties.MeshType", PlanarMeshType.Triangulation, planar.MeshProperties.MeshType,
          planar.MeshProperties.MeshType == PlanarMeshType.Triangulation);
        CheckProp("MeshProperties.MeshDensity", PlanarMeshDensity.Element_size, planar.MeshProperties.MeshDensity,
          planar.MeshProperties.MeshDensity == PlanarMeshDensity.Element_size);
        CheckProp("MeshProperties.MeshElementSize", 0.5, planar.MeshProperties.MeshElementSize,
          Math.Abs(planar.MeshProperties.MeshElementSize - 0.5) <= tolerance);
      }
      else
      {
        Console.ForegroundColor = ConsoleColor.Red;
        SampleReport.Fail("MeshProperties", "is null");
        Console.WriteLine("  [FAIL] MeshProperties is null.");
        Console.ForegroundColor = ConsoleColor.White;
        allPassed = false;
      }

      // ── Final result ──────────────────────────────────────────────────────────
      Console.WriteLine();
      if (allPassed)
      {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("VERIFICATION PASSED: all updated properties match after UpdateElement + GetElementsObject round-trip.");
      }
      else
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("VERIFICATION FAILED: one or more properties did not match. Check results above.");
      }
      Console.ForegroundColor = ConsoleColor.White;

      var respClose = client.CloseProject();
      PrintResultDetails(respClose.Details, logVerbosityLevel, "CloseProject");
    }
  }
}
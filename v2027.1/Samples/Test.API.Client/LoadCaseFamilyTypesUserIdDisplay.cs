using AD.API.Client;

namespace Test.API.Client
{
  internal partial class Program
  {
    /// <summary>
    /// Load case family types + UserID display sample:
    /// - Creates a new project (France / Eurocode localization).
    /// - For each of the 7 standard non-climatic load case family types, creates
    ///   the corresponding family and one load case inside it:
    ///     DeadLoads   : LoadCaseFamily_DeadLoads   → LoadCase_DeadLoads
    ///     LiveLoads   : LoadCaseFamily_LiveLoads   → LoadCase_LiveLoads
    ///     Accidental  : LoadCaseFamily_Accidental  → LoadCase_Accidental
    ///     Other       : LoadCaseFamily_Other       → LoadCase_Other
    ///     Snow        : LoadCaseFamily_Snow        → LoadCase_Snow
    ///     Wind        : LoadCaseFamily_Wind        → LoadCase_Wind
    ///     Thermic     : LoadCaseFamily_Thermic     → LoadCase_Thermic
    /// - After each creation, calls GetInformationalElementsObject for both the
    ///   family and the load case and prints their UserID values.
    /// - Closes the project without saving (no reopen).
    /// </summary>
    static void Sample_LoadCaseFamilyTypesUserIdDisplay(AD.API.Client.AD_Client client)
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

      string projectFile = projectPath + "LoadCaseFamilyTypesUserIdDisplay_" + Guid.NewGuid().ToString() + ".fto";

      // ── Create project ────────────────────────────────────────────────────────
      var respNewProj = client.NewProject(projectFile, env);
      PrintResultDetails(respNewProj.Details, logVerbosityLevel, "NewProject");
      if (!respNewProj.Details.Success || respNewProj.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: NewProject failed. Aborting.");
        Console.WriteLine("ERROR: NewProject failed. Aborting.");
        return;
      }

      Console.WriteLine();
      Console.WriteLine("=== Creating load case families + cases, then reading UserID for each ===");
      Console.WriteLine();

      // ── Helper: create family + case, then read back UserID ──────────────────
      void CreateFamilyAndCase(
        string label,
        InformationalElementBase family,
        Func<EID, InformationalElementBase> makeCase)
      {
        // Create family
        var respFamily = client.CreateInformationalElement(family);
        PrintResultDetails(respFamily.Details, logVerbosityLevel, $"CreateInformationalElement ({label} family)");
        if (!respFamily.Details.Success || respFamily.Details.HasErrors)
        {
          Console.WriteLine($"  ERROR: CreateInformationalElement ({label} family) failed. Skipping.");
          return;
        }
        EID familyId = respFamily.Data;
        Console.WriteLine($"  {label} family EID: {familyId.Value}");

        // Retrieve the family object and display UserID
        var respFamilyObj = client.GetInformationalElementsObject(new List<long> { familyId.Value });
        PrintResultDetails(respFamilyObj.Details, logVerbosityLevel, $"GetInformationalElementsObject ({label} family)");
        var familyElement = respFamilyObj.Data?.FirstOrDefault();
        if (familyElement is not null)
        {
          Console.ForegroundColor = ConsoleColor.Green;
          SampleReport.Pass($"  [{label} family] UserID = {familyElement.UserID}");
          Console.WriteLine($"  [{label} family] UserID = {familyElement.UserID}");
          Console.ForegroundColor = ConsoleColor.White;
        }
        else
        {
          Console.ForegroundColor = ConsoleColor.Yellow;
          SampleReport.Fail($"  [{label} family] WARNING: no element returned by GetInformationalElementsObject.");
          Console.WriteLine($"  [{label} family] WARNING: no element returned by GetInformationalElementsObject.");
          Console.ForegroundColor = ConsoleColor.White;
        }

        Console.WriteLine();

        // Create load case linked to the family
        var loadCase = makeCase(familyId);
        var respCase = client.CreateInformationalElement(loadCase);
        PrintResultDetails(respCase.Details, logVerbosityLevel, $"CreateInformationalElement ({label} case)");
        if (!respCase.Details.Success || respCase.Details.HasErrors)
        {
          Console.WriteLine($"  ERROR: CreateInformationalElement ({label} case) failed. Skipping.");
          return;
        }
        EID caseId = respCase.Data;
        Console.WriteLine($"  {label} case EID: {caseId.Value}");

        // Retrieve the load case object and display UserID
        var respObj = client.GetInformationalElementsObject(new List<long> { caseId.Value });
        PrintResultDetails(respObj.Details, logVerbosityLevel, $"GetInformationalElementsObject ({label} case)");
        var element = respObj.Data?.FirstOrDefault();
        if (element is not null)
        {
          Console.ForegroundColor = ConsoleColor.Cyan;
          SampleReport.Pass($"  [{label}] UserID = {element.UserID}");
          Console.WriteLine($"  [{label}] UserID = {element.UserID}");
          Console.ForegroundColor = ConsoleColor.White;
        }
        else
        {
          Console.ForegroundColor = ConsoleColor.Yellow;
          SampleReport.Fail($"  [{label}] WARNING: no element returned by GetInformationalElementsObject.");
          Console.WriteLine($"  [{label}] WARNING: no element returned by GetInformationalElementsObject.");
          Console.ForegroundColor = ConsoleColor.White;
        }

        Console.WriteLine();
      }

      // ── 1. Dead loads ─────────────────────────────────────────────────────────
      CreateFamilyAndCase(
        "DeadLoads",
        new LoadCaseFamily_DeadLoads { Name = "Dead Loads Family" },
        id => new LoadCase_DeadLoads { Name = "Dead Loads Case", LoadCaseFamilyID = id });

      // ── 2. Live loads ─────────────────────────────────────────────────────────
      CreateFamilyAndCase(
        "LiveLoads",
        new LoadCaseFamily_LiveLoads { Name = "Live Loads Family" },
        id => new LoadCase_LiveLoads { Name = "Live Loads Case", LoadCaseFamilyID = id, LiveLoadCategory = 0 });

      // ── 3. Accidental ─────────────────────────────────────────────────────────
      CreateFamilyAndCase(
        "Accidental",
        new LoadCaseFamily_Accidental { Name = "Accidental Family" },
        id => new LoadCase_Accidental { Name = "Accidental Case", LoadCaseFamilyID = id });

      // ── 4. Other ──────────────────────────────────────────────────────────────
      CreateFamilyAndCase(
        "Other",
        new LoadCaseFamily_Other { Name = "Other Family" },
        id => new LoadCase_Other { Name = "Other Case", LoadCaseFamilyID = id });

      // ── 5. Snow ───────────────────────────────────────────────────────────────
      CreateFamilyAndCase(
        "Snow",
        new LoadCaseFamily_Snow { Name = "Snow Family" },
        id => new LoadCase_Snow { Name = "Snow Case", LoadCaseFamilyID = id });

      // ── 6. Wind ───────────────────────────────────────────────────────────────
      CreateFamilyAndCase(
        "Wind",
        new LoadCaseFamily_Wind { Name = "Wind Family" },
        id => new LoadCase_Wind { Name = "Wind Case", LoadCaseFamilyID = id });

      // ── 7. Thermic ────────────────────────────────────────────────────────────
      CreateFamilyAndCase(
        "Thermic",
        new LoadCaseFamily_Thermic { Name = "Thermic Family" },
        id => new LoadCase_Thermic { Name = "Thermic Case", LoadCaseFamilyID = id });

      // ── Close project ─────────────────────────────────────────────────────────
      var respClose = client.CloseProject();
      PrintResultDetails(respClose.Details, logVerbosityLevel, "CloseProject");
    }
  }
}

using AD.API.Client;

namespace Test.API.Client
{
  internal partial class Program
  {
    /// <summary>
    /// Opens an existing model, runs analysis, reads buckling load cases and prints
    /// their properties, then for every buckling case and every mode reports:
    ///   - the generalized buckling result (critical load factor lambda), and
    ///   - the bending moment "My" efforts diagram on the linear element EID=4103.
    /// The model is never modified or saved.
    /// </summary>
    static void Sample_GeneralizedBucklingExistingModelResults(AD.API.Client.AD_Client client)
    {
      const string defaultModelFile = @"C:\Advance Design 22.1\Projects\GeneralizedBuckling.fto";
      string modelFile = PromptForModelFile(defaultModelFile);
      const long forcesElementId = 4103;   // linear element for which the My efforts are reported
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

      WriteBanner("Generalized Buckling - Existing Model Results");
      Console.WriteLine($"  Model           : {modelFile}");
      Console.WriteLine($"  My efforts on   : linear element EID={forcesElementId}");
      Console.WriteLine();

      if (!File.Exists(modelFile))
      {
        SampleReport.Fatal($"ERROR: Model file not found: {modelFile}");
        Console.WriteLine($"ERROR: Model file not found: {modelFile}");
        return;
      }

      Environments env = new Environments
      {
        Localization = Localization_Code.LOCALIZATION_EUROPE,
        LogVerbosity = logVerbosityLevel
      };

      Console.WriteLine("[1/4] Opening project ...");
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
        Console.WriteLine("[2/4] Running analysis ...");
        var respCalc = client.LaunchAnalysis();
        PrintResultDetails(respCalc.Details, logVerbosityLevel, "LaunchAnalysis");
        if (!respCalc.Details.Success || respCalc.Details.HasErrors)
        {
          SampleReport.Fatal("ERROR: LaunchAnalysis failed. Aborting sample.");
          Console.WriteLine("ERROR: LaunchAnalysis failed. Aborting sample.");
          return;
        }

        // -----------------------------------------------------------------
        // [3/4] Locate the linear element used for the My efforts (EID=4103)
        // -----------------------------------------------------------------
        Console.WriteLine("[3/4] Locating linear elements ...");
        var forcesElementIds = new List<long> { forcesElementId };

        var respLinearElementIds = client.GetElementsID(new List<QueryBase>
        {
          new QueryElementsModel { ElementType = ElementTypeEnum.ElementLinear }
        });
        PrintResultDetails(respLinearElementIds.Details, logVerbosityLevel, "GetElementsID (linear elements)");

        bool hasLinearElements = respLinearElementIds.Details.Success
                                 && !respLinearElementIds.Details.HasErrors
                                 && respLinearElementIds.Data != null
                                 && respLinearElementIds.Data.Count > 0;
        if (!hasLinearElements)
        {
          SampleReport.Fatal("ERROR: No linear element found in model. Cannot request generalized buckling results.");
          Console.WriteLine("ERROR: No linear element found in model. Cannot request generalized buckling results.");
          return;
        }

        Console.WriteLine($"  Linear elements found: {respLinearElementIds.Data!.Count}");
        if (respLinearElementIds.Data.Contains(forcesElementId))
        {
          Console.WriteLine($"  Target element EID={forcesElementId} is present in the model.");
        }
        else
        {
          Console.ForegroundColor = ConsoleColor.Yellow;
          Console.WriteLine($"  WARNING: target element EID={forcesElementId} not found among linear elements; My efforts may be empty.");
          Console.ForegroundColor = ConsoleColor.White;
        }

        // -----------------------------------------------------------------
        // [4/4] Collect candidate buckling load cases
        // -----------------------------------------------------------------
        Console.WriteLine("[4/4] Collecting buckling load cases ...");
        var bucklingCaseIds = new List<long>();
        bool queriedSpecificBucklingType = false;

        if (Enum.TryParse("LoadCase_Buckling", out InformationalElementTypeEnum bucklingCaseType))
        {
          queriedSpecificBucklingType = true;
          var respBucklingCaseIds = client.GetElementsID(new List<QueryBase>
          {
            new QueryInfoModel { InformationalElementType = bucklingCaseType }
          });
          PrintResultDetails(respBucklingCaseIds.Details, logVerbosityLevel, "GetElementsID (LoadCase_Buckling)");
          if (respBucklingCaseIds.Details.Success && !respBucklingCaseIds.Details.HasErrors && respBucklingCaseIds.Data != null)
            bucklingCaseIds.AddRange(respBucklingCaseIds.Data);
        }

        if (bucklingCaseIds.Count == 0)
        {
          var respAllCaseIds = client.GetElementsID(new List<QueryBase>
          {
            new QueryInfoModel { InformationalElementType = InformationalElementTypeEnum.LoadCase }
          });
          PrintResultDetails(respAllCaseIds.Details, logVerbosityLevel, "GetElementsID (all load cases)");
          if (respAllCaseIds.Details.Success && !respAllCaseIds.Details.HasErrors && respAllCaseIds.Data != null)
            bucklingCaseIds.AddRange(respAllCaseIds.Data);
        }

        bucklingCaseIds = bucklingCaseIds.Distinct().ToList();
        Console.WriteLine($"  Candidate load case IDs: {bucklingCaseIds.Count}");

        bool IsBucklingCase(InformationalElementBase loadCaseObj)
        {
          string typeName = loadCaseObj.GetType().Name;
          return typeName.Contains("Buckling", StringComparison.OrdinalIgnoreCase)
                 || loadCaseObj.GetType().GetProperty("NoModes") != null
                 || loadCaseObj.GetType().GetProperty("MaxLambda") != null;
        }

        static long? ReadLongProperty(object obj, params string[] propertyNames)
        {
          foreach (string propName in propertyNames)
          {
            var prop = obj.GetType().GetProperty(propName);
            if (prop == null)
              continue;

            object? value = prop.GetValue(obj);
            if (value is null)
              continue;
            if (value is EID eid)
              return eid.Value;
            if (value is int i)
              return i;
            if (value is long l)
              return l;
            if (value is short s)
              return s;
          }
          return null;
        }

        static double? ReadDoubleProperty(object obj, params string[] propertyNames)
        {
          foreach (string propName in propertyNames)
          {
            var prop = obj.GetType().GetProperty(propName);
            if (prop == null)
              continue;

            object? value = prop.GetValue(obj);
            if (value is null)
              continue;
            if (value is double d)
              return d;
            if (value is float f)
              return f;
            if (value is decimal m)
              return (double)m;
          }
          return null;
        }

        static string? ReadStringProperty(object obj, params string[] propertyNames)
        {
          foreach (string propName in propertyNames)
          {
            var prop = obj.GetType().GetProperty(propName);
            if (prop == null)
              continue;

            object? value = prop.GetValue(obj);
            if (value is string s && !string.IsNullOrWhiteSpace(s))
              return s;
          }
          return null;
        }

        // Requests the My efforts diagram for element EID=4103 on the given buckling case/mode
        // and prints it as an aligned table. Returns the number of diagram points printed.
        int PrintMyEffortsForElement(long bucklingCaseId, int mode)
        {
          var respForces = client.GetResultsByQuery(new ResultsQueryFEM
          {
            EResType = ResultType.Forces,
            ListElementsIDs = forcesElementIds,
            AnalysisCaseInfo = new LoadCaseAnalysisBuckling
            {
              Id = new EID { Value = bucklingCaseId },
              Mode = mode
            }
          });

          PrintResultDetails(respForces.Details, logVerbosityLevel,
            $"GetResults Forces (element={forcesElementId}, case={bucklingCaseId}, mode={mode})");

          var beam = respForces.Data?.OfType<ResElementLinear>().FirstOrDefault();
          var myDiagram = beam?.ResDiagrams?.ResForces?.My;

          Console.WriteLine($"    My efforts on linear element EID={forcesElementId}:");
          if (myDiagram == null || myDiagram.Count == 0)
          {
            Console.WriteLine("      (no My diagram returned for this case/mode)");
            return 0;
          }

          var points = myDiagram.OrderBy(p => p.Abscissa).ToList();
          Console.WriteLine("      {0,14}   {1,16}", "Abscissa [m]", "My");
          Console.WriteLine("      " + new string('-', 33));
          foreach (var p in points)
            Console.WriteLine("      {0,14:F4}   {1,16:F4}", p.Abscissa, p.Value);

          return points.Count;
        }

        // -----------------------------------------------------------------
        // Per-case / per-mode results
        // -----------------------------------------------------------------
        int printedBucklingCases = 0;
        foreach (long caseId in bucklingCaseIds)
        {
          var respCaseObj = client.GetInformationalElementsObject(new List<long> { caseId });
          PrintResultDetails(respCaseObj.Details, logVerbosityLevel, $"GetInformationalElementsObject (case {caseId})");
          var loadCaseObj = respCaseObj.Data?.FirstOrDefault();
          if (loadCaseObj == null)
            continue;

          if (!queriedSpecificBucklingType && !IsBucklingCase(loadCaseObj))
            continue;

          printedBucklingCases++;

          string name = ReadStringProperty(loadCaseObj, "Name", "UserName") ?? "(no name)";
          long? innerLoadCaseId = ReadLongProperty(loadCaseObj, "InnerLoadCaseId", "AnalysisId");
          int noModes = (int)(ReadLongProperty(loadCaseObj, "NoModes", "NumberOfModes") ?? 1);
          double maxLambda = ReadDoubleProperty(loadCaseObj, "MaxLambda", "TargetLambda") ?? 0.0;
          double tolerance = ReadDoubleProperty(loadCaseObj, "Tolerance", "Energy") ?? 0.0;
          int maxIterations = (int)(ReadLongProperty(loadCaseObj, "MaxIterations", "IterationsMax") ?? 0);

          Console.WriteLine();
          Console.WriteLine(minorRule);
          Console.WriteLine($"  Buckling case #{printedBucklingCases}  -  EID={caseId}  -  Name={name}");
          Console.WriteLine(minorRule);
          Console.WriteLine($"    InnerLoadCaseId : {innerLoadCaseId?.ToString() ?? "n/a"}");
          Console.WriteLine($"    NoModes         : {noModes}");
          Console.WriteLine($"    MaxLambda       : {maxLambda}");
          Console.WriteLine($"    Tolerance       : {tolerance}");
          Console.WriteLine($"    MaxIterations   : {maxIterations}");

          int modeCount = Math.Max(1, noModes);
          for (int mode = 0; mode < modeCount; mode++)
          {
            Console.WriteLine();
            Console.WriteLine($"    Mode {mode}");

            // Generalized buckling result (critical load factor lambda) - model-wide.
            var respResults = client.GetResultsByQuery(new ResultsQueryFEM
            {
              EResType = ResultType.GeneralizedBucklingRes,
              ListElementsIDs = forcesElementIds,
              AnalysisCaseInfo = new LoadCaseAnalysisBuckling
              {
                Id = new EID { Value = caseId },
                Mode = mode
              }
            });

            PrintResultDetails(respResults.Details, logVerbosityLevel,
              $"GetResults GeneralizedBucklingRes (case={caseId}, mode={mode})");

            var lambdaResult = respResults.Data?.OfType<ResGeneralizedBuckling>().FirstOrDefault();
            Console.WriteLine(lambdaResult != null
              ? $"      Lambda factor : {lambdaResult.LambdaFactor:F6}"
              : "      Lambda factor : (not available)");

            // Bending moment My efforts on element EID=4103 for this case/mode.
            PrintMyEffortsForElement(caseId, mode);
          }
        }

        Console.WriteLine();
        Console.WriteLine(majorRule);
        if (printedBucklingCases == 0)
          Console.WriteLine("  No buckling load cases found in this model.");
        else
          Console.WriteLine($"  Done. Reported {printedBucklingCases} buckling load case(s).");
        Console.WriteLine(majorRule);
      }
      finally
      {
        var respClose = client.CloseProject();
        PrintResultDetails(respClose.Details, logVerbosityLevel, "CloseProject");
      }
    }
  }
}

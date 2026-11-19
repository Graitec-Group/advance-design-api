using AD.API.Client;
using System.Collections;

namespace Test.API.Client
{
  internal partial class Program
  {
    /// <summary>Exit code of a run in which no sample failed.</summary>
    private const int ExitSuccess = 0;

    /// <summary>Exit code when at least one sample failed or errored.</summary>
    private const int ExitFailure = 1;

    /// <summary>Exit code when the command line could not be parsed. Used by the sample runner.</summary>
    private const int ExitBadCommandLine = 2;

    private static string? GetEnvironmentVariable(string name) =>
        Environment.GetEnvironmentVariable(name) ??   //first try from launchSettings.json
        Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User) ?? //then try from user variables
        Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine); //finally try from machine variables

    /// <summary>Base URL of the API server, from the environment or the default one.</summary>
    internal static string GetApiServerUrl() =>
        GetEnvironmentVariable("AD_API_SERVER_URL") ?? CSessionManager.DefaultApiBaseUrl;

    /// <summary>
    /// Entry point.
    /// <para>
    /// This project is the console sample client. When the optional Test.API.Client.SampleRunner
    /// folder is present next to it, that folder adds the runner window, the unattended batch
    /// run and the command line on top - see SampleRunnerEntryPoint.cs there. Without it, this
    /// is the numbered menu it has always been.
    /// </para>
    /// <para>
    /// <c>[STAThread]</c> is unconditional: WPF requires it, and it costs nothing here.
    /// </para>
    /// </summary>
    [STAThread]
    static int Main(string[] args)
    {
#if SAMPLE_RUNNER
      return RunSampleRunner(args);
#else
      return RunConsoleMenu();
#endif
    }

#if !SAMPLE_RUNNER
    /// <summary>
    /// Numbered menu, one sample, one verdict. Deliberately self contained: this is what the
    /// public sample ships, and it must not depend on anything outside this folder.
    /// </summary>
    private static int RunConsoleMenu()
    {
      string serverUrl = GetApiServerUrl();

      //if the server isn't already running, start a new one (assuming the API server is located in the installation path)
      if (!CSessionManager.IsServiceAvailable(serverUrl))
        serverUrl = CSessionManager.StartNewSession(AssemblyResolver.ServerBinPath, /*windowLessConsole*/ false);

      if (string.IsNullOrEmpty(serverUrl))
      {
        Console.WriteLine("Could not reach or start the API server.");
        Console.WriteLine("Check AD_API_SERVER_URL and AD_API_SERVER_BINARY_PATH in the user or system environment variables.");
        Console.ReadLine();
        return ExitFailure;
      }

      Console.WriteLine("Connected to " + serverUrl);

      // The list lives in SampleCatalog.cs.
      var samples = Samples;

      Console.WriteLine();
      Console.WriteLine("Select a sample to run:");
      for (int i = 0; i < samples.Count; i++)
        Console.WriteLine($"  [{samples[i].Number}] {samples[i].Label}");
      Console.Write($"Choice (1-{samples.Count}): ");

      int choice;
      while (true)
      {
        string? input = Console.ReadLine();
        if (input is null)
          return ExitFailure;   // stdin closed: nothing to choose from
        if (int.TryParse(input, out choice) && choice >= 1 && choice <= samples.Count)
          break;
        Console.Write($"Invalid input. Enter a number between 1 and {samples.Count}: ");
      }

      var selected = samples[choice - 1];
      Console.WriteLine($"Running: {selected.Label}");
      Console.WriteLine();

      SampleOutcome outcome = SampleOutcome.NotRun;
      string detail = string.Empty;

      // The sample runs on its own thread, as it always did.
      var worker = new Thread(() =>
      {
        using var httpClient = new HttpClient(new SocketsHttpHandler
        {
          PooledConnectionLifetime = Timeout.InfiniteTimeSpan,
        });
        var client = new AD_Client(serverUrl, httpClient);

        // The report scope lets the sample record its verifications explicitly, so the verdict
        // below comes from the sample code and not from parsing the output.
        using var scope = SampleReport.BeginSample(selected.Label);
        Exception? error = null;

        try
        {
          selected.Run(client);
        }
        catch (Exception ex)
        {
          error = ex;
          Console.WriteLine($"EXCEPTION: {ex}");
        }
        finally
        {
          // Best effort: do not lose the verdict because closing the session throws.
          try
          {
            client.CloseSession();
          }
          catch (Exception ex)
          {
            Console.WriteLine($"CloseSession failed: {ex.Message}");
          }
        }

        outcome = scope.Report.Resolve(error, canceled: false);
        detail = error is null ? scope.Report.Describe() : error.Message;
      });

      worker.Start();
      worker.Join();

      Console.WriteLine();
      Console.WriteLine($"RESULT: {outcome.ToDisplayText().ToUpperInvariant()} ({detail})");
      Console.WriteLine("done");
      Console.ReadLine();

      return outcome.IsFailure() ? ExitFailure : ExitSuccess;
    }
#endif

#region diagnostics_helpers
    /// <summary>
    /// dump all the error details
    /// </summary>
    /// <param name="response"></param>
    static void DumpError(ApiResponseDetails details)
    {
      var diagnostics = details?.Diagnostics;
      if (diagnostics == null)
        return;

      foreach (var diagnostic in diagnostics)
      {
        Console.WriteLine($"[{diagnostic.Severity}] {diagnostic.Code}: {diagnostic.Message}");
      }
    }

    static string DiagText(ICollection<DiagnosticEntry>? diagnostics)
    {
      return diagnostics == null || diagnostics.Count == 0
        ? "(none)"
        : string.Join("; ", diagnostics.Select(d => d?.Message ?? "?"));
    }

    /// <summary>
    /// Prompts for a model (.fto) file path. Pressing ENTER accepts the default; surrounding
    /// quotes (from copy/paste or drag &amp; drop) are trimmed.
    /// <para>
    /// This method is the seam a sample uses to ask for a value. On its own it prompts on the
    /// console, exactly as it always did. With the sample runner add-on the request is routed
    /// through <c>SampleInput</c> instead, which turns it into a prefilled dialog in the window
    /// and into the declared default in an unattended run. A sample never calls either directly.
    /// </para>
    /// </summary>
    static string PromptForModelFile(string defaultPath)
    {
#if SAMPLE_RUNNER
      return SampleInput.AskForModelFile(defaultPath);
#else
      Console.WriteLine("Enter the path to the .fto model file, or press ENTER to use the default:");
      Console.WriteLine($"  default: {defaultPath}");
      Console.Write("Path: ");

      string? input = Console.ReadLine();
      if (string.IsNullOrWhiteSpace(input))
        return defaultPath;

      return input.Trim().Trim('"');
#endif
    }

		static void PrintResultDetails(ApiResponseDetails details, LogVerboseLevel levelOfVerbosity, string introMessage)
		{
			if (details is null)
			{
				Console.WriteLine($"{introMessage}: (no details)");
				return;
			}

			Console.WriteLine($"{introMessage}: Success={details.Success}, HasErrors={details.HasErrors}, HasWarnings={details.HasWarnings}");

			if (details.Diagnostics is null || details.Diagnostics.Count == 0)
				return;

			DiagnosticSeverity minSeverity = levelOfVerbosity switch
			{
				LogVerboseLevel.Errors            => DiagnosticSeverity.Error,
				LogVerboseLevel.ErrorsAndWarnings => DiagnosticSeverity.Warning,
				LogVerboseLevel.AllDetails        => DiagnosticSeverity.Information,
				_                                 => DiagnosticSeverity.Error
      };

			foreach (var diag in details.Diagnostics)
			{
				if (diag is null || diag.Severity < minSeverity)
					continue;

				string sourceInfo = string.IsNullOrEmpty(diag.Source) ? "" : $" [{diag.Source}]";
				Console.WriteLine($"  [{diag.Severity}] {diag.Code}: {diag.Message}{sourceInfo}");
			}
		}
    #endregion

# region print_objects_details
    static void PrintLoad(AD.API.Client.ElementBase? elementToPrint)
    {
      if (elementToPrint is null)
        return;

      if (elementToPrint is ElementLoadPunctual ptLoad)
      {
        Console.WriteLine($"        [PunctualLoad] Pt=({ptLoad.GeomPt.X}, {ptLoad.GeomPt.Y}, {ptLoad.GeomPt.Z})" + "\n\t\t\t" +
          $" LoadCase={ptLoad.LoadCase?.Value}" + $" CoordinateSystemType={ptLoad.CoordinateSystemType}" + "\n\t\t\t" +
          $" F=({ptLoad.Fx}, {ptLoad.Fy}, {ptLoad.Fz})" + "\n\t\t\t" +
          (ptLoad.Moment != null ? $" M=({ptLoad.Moment.Mx}, {ptLoad.Moment.My}, {ptLoad.Moment.Mz})" : "") +
          (ptLoad.UserComment != null ? $"\n\t\t\t UserComment={ptLoad.UserComment}" : ""));
      }
      else if (elementToPrint is ElementLoadLinear linLoad)
      {
        Console.WriteLine($"        [LinearLoad] Start=({linLoad.GeomPtStart.X}, {linLoad.GeomPtStart.Y}, {linLoad.GeomPtStart.Z})" + "\n\t\t\t" +
          $" End=({linLoad.GeomPtEnd.X}, {linLoad.GeomPtEnd.Y}, {linLoad.GeomPtEnd.Z})" + "\n\t\t\t" +
          $" LoadCase={linLoad.LoadCase?.Value}" + $" CoordinateSystemType={linLoad.CoordinateSystemType}" + "\n\t\t\t" +
          $" F=({linLoad.Fx}, {linLoad.Fy}, {linLoad.Fz})" + "\n\t\t\t" +
          (linLoad.Variation != null ? $" Var=({linLoad.Variation.Coefficient1}, {linLoad.Variation.Coefficient2})" : "") +
          (linLoad.UserComment != null ? $"\n\t\t\t UserComment={linLoad.UserComment}" : ""));
      }
      else if (elementToPrint is ElementLoadPlanar planarLoad)
      {
        string pts = planarLoad.GeomPtsList != null
          ? string.Join(", ", planarLoad.GeomPtsList.Select(p => $"({p.X}, {p.Y}, {p.Z})"))
          : "";
        Console.WriteLine($"        [PlanarLoad] Pts=[{pts}]" + "\n\t\t\t" +
          $" LoadCase={planarLoad.LoadCase?.Value}" + $" CoordinateSystemType={planarLoad.CoordinateSystemType}" + "\n\t\t\t" +
          $" F=({planarLoad.Fx}, {planarLoad.Fy}, {planarLoad.Fz})" + "\n\t\t\t" +
          (planarLoad.Variation != null ? $" Var=({planarLoad.Variation.Coefficient1}, {planarLoad.Variation.Coefficient2}, {planarLoad.Variation.Coefficient3})" : "") +
          (planarLoad.UserComment != null ? $"\n\t\t\t UserComment={planarLoad.UserComment}" : ""));
      }
    }

		static void PrintLoadCase(AD.API.Client.InformationalElementBase? elementToPrint)
		{
			if (elementToPrint is null)
				return;

      if (elementToPrint is LoadCase_Wind_1991_1_4 loadCaseWind_1991_1_4)
      {
        Console.WriteLine($"        [LoadCase_Wind_1991_1_4] Name={loadCaseWind_1991_1_4.Name}" +
          (loadCaseWind_1991_1_4.EffectParameters != null
            ? $" WindDirection={loadCaseWind_1991_1_4.EffectParameters.WindDirection}" +
              $" InternalPressure={loadCaseWind_1991_1_4.EffectParameters.InternalPressure}" +
              $" TypeOfLoadingsCase={loadCaseWind_1991_1_4.EffectParameters.TypeOfLoadingsCase}"
            : ""));
      }
      else if (elementToPrint is LoadCase_WindIBC loadCaseWindIBC)
      {
        Console.WriteLine($"        [LoadCase_WindIBC] Name={loadCaseWindIBC.Name}" +
          (loadCaseWindIBC.EffectParameters != null
            ? $" WindDirection={loadCaseWindIBC.EffectParameters.WindDirection}" +
              $" InternalPressure={loadCaseWindIBC.EffectParameters.InternalPressure}" +
              $" TypeOfLoadingsCase={loadCaseWindIBC.EffectParameters.TypeOfLoadingsCase}"
            : ""));
      }
      else if(elementToPrint is LoadCase_WindCBN loadCaseWind_CBN)
      {
        Console.WriteLine($"        [LoadCase_WindCBN] Name={loadCaseWind_CBN.Name}" +
          (loadCaseWind_CBN.EffectParameters != null
            ? $" WindDirection={loadCaseWind_CBN.EffectParameters.WindDirection}" +
              $" InternalPressure={loadCaseWind_CBN.EffectParameters.InternalPressure}" +
              $" TypeOfLoadingsCase={loadCaseWind_CBN.EffectParameters.TypeOfLoadingsCase}"
            : ""));
      }      
      else if (elementToPrint is LoadCase_Snow_1991_1_3 loadCaseSnow__1991_1_3)
      {
        Console.WriteLine($"        [LoadCase_Snow_1991_1_3] Name={loadCaseSnow__1991_1_3.Name}" +
          (loadCaseSnow__1991_1_3.EffectParameters != null
            ? $" TypeOfLoadingsCase={loadCaseSnow__1991_1_3.EffectParameters.TypeOfLoadingsCase}"
            : "") +
          (loadCaseSnow__1991_1_3.SnowLoadCategory.HasValue
            ? $" SnowLoadCategory={loadCaseSnow__1991_1_3.SnowLoadCategory}"
            : ""));
      }
      else if (elementToPrint is LoadCase_SnowIBC loadCaseSnowIBC)
      {
        Console.WriteLine($"        [LoadCase_SnowIBC] Name={loadCaseSnowIBC.Name}" +
          (loadCaseSnowIBC.EffectParameters != null
            ? $" TypeOfLoadingsCase={loadCaseSnowIBC.EffectParameters.TypeOfLoadingsCase}"
            : "") +
          (loadCaseSnowIBC.SnowLoadCategory.HasValue
            ? $" SnowLoadCategory={loadCaseSnowIBC.SnowLoadCategory}"
            : ""));
      }
      else if (elementToPrint is LoadCase_SnowCBN loadCaseSnowICBN)
      {
        Console.WriteLine($"        [LoadCase_SnowCBN] Name={loadCaseSnowICBN.Name}" +
          (loadCaseSnowICBN.EffectParameters != null
            ? $" TypeOfLoadingsCase={loadCaseSnowICBN.EffectParameters.TypeOfLoadingsCase}"
            : "") +
          (loadCaseSnowICBN.SnowLoadCategory.HasValue
            ? $" SnowLoadCategory={loadCaseSnowICBN.SnowLoadCategory}"
            : ""));
      }
    }

#endregion

  }
}

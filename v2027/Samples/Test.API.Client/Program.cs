using AD.API.Client;
using System.Collections;
using System.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Test.API.Client
{
  internal partial class Program
  {
    private static string? GetEnvironmentVariable(string name) =>
        Environment.GetEnvironmentVariable(name) ??   //first try from launchSettings.json
        Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User) ?? //then try from user variables
        Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine); //finally try from machine variables    

    //async version of main to allow awaiting the server start and service availability check without blocking the main thread
    static async Task Main(string[] args)
    {
      // Assembly resolver is registered at module-load time via AssemblyResolver.Initialize()
      // (see AssemblyResolver.cs) so that AD.API.Client.dll and its transitive dependencies
      // are available before the JIT compiles this method.
      string serverBinPath = AssemblyResolver.ServerBinPath;

      string connectionServer= GetEnvironmentVariable("AD_API_SERVER_URL") ?? CSessionManager.DefaultApiBaseUrl;

      //if the server isn't already running at DefaultApiBaseUrl, start a new one (assuming the API server is located in the installation path)
      if (!await CSessionManager.IsServiceAvailableAsync(connectionServer))
      {        
				connectionServer = await CSessionManager.StartNewSessionAsync(serverBinPath, /*windowlessConsole : true for no server console window*/false);
      }
        
			Console.WriteLine("Connecting to API server " + connectionServer);
			//Console.WriteLine("Press ENTER to continue"); Console.ReadLine();

      if (System.String.IsNullOrEmpty(connectionServer))
      {
        Console.WriteLine("connection server empty due to AD.API.Srv timeout or missing AD_API_SERVER_URL and AD_API_SERVER_BINARY_PATH in launchSettings.json or user/system environement variables !!");
        Console.ReadLine();
      }      

      // Present a menu so the user can choose which sample to run
      var samples = new (string Label, Action<AD_Client> Run)[]
      {        
        ("Analysis results(on column): Imposed displacement structure with result verification",             c => Sample_ImposedDisplacementVerificationOnColumn(c)),
        ("Analysis results(on supports and beam ): Portal frame 3 elements analysis verification (5% tol)",              c => Sample_PortalFrame3ElementsAnalysisVerificationOnBeamAndSupports(c)),
        ("Analysis results(on supports): 4-column slab support with Fz applied on it, verification sum of Fz in supports(Europe, 1% tol)",        c => Sample_SlabOn4ColumnsSupportFzVerificationItReachesSupports(c)),
        ("Analysis results(on slab): Slab with rigid linear supports, analysis and all results on slab", c => Sample_PlanarSlabRigidLinearSupportsFrance(c)),
        
        ("Climatic: 3D building with wind (EN 1991-1-4) and snow (EN 1991-1-3) auto generation (France)", c => Sample_WindSnow3DBuildingAutoGeneration(c)),
        ("Climatic: Portal 2D Structure and IBC (ASCE 7-22) Climatic Generation",          c => Sample_Portal2DStructureAndCLimaticGeneration(c)),
        ("Climatic: Portal 2D Structure and IBC (ASCE 7-22) Climatic Generation (multiple roof elements)", c => Sample_Portal2DStructureAndCLimaticGeneration_wMultipleElementsInTheRoof(c)),
        ("Climatic: Portal 2D with Protruding Roof / Overhangs and IBC (ASCE 7-22) Climatic Generation", c => Sample_Portal2DwithProtrudingRoofOrOverhangsandClimaticGeneration(c)),

        ("Intentional error testing: Structure with intentional material error",                           c => Sample_With_IntentionalMaterialError(c)),
        ("Properties setting and model management: Custom properties set and verification after reopen",                 c => Sample_CustomPropertiesVerificationOnReopen(c)),
			  ("Planar Wall With Linear Support And Results",                                                    c => Sample_PlanarWallWithLinearSupportAndResults(c)),
			  ("Planar Support With Planar Element And Results",                                                 c => Sample_PlanarSupportWithPlanarElementAndResults(c)),
        ("Pylon climatic                                ",                                                 c => Sample_PylonElectricityStructureAndClimaticGeneration(c)),
        ("Planar Wall With Openings (holes test)",                                                       c => Sample_PlanarWallWithOpenings(c)),
        
      };

      Console.WriteLine();
      Console.WriteLine("Select a sample to run:");
      for (int i = 0; i < samples.Length; i++)
        Console.WriteLine($"  [{i + 1}] {samples[i].Label}");
      Console.Write("Choice (1-{0}): ", samples.Length);

      int choice = 0;
      while (true)
      {
        string? input = Console.ReadLine();
        if (int.TryParse(input, out choice) && choice >= 1 && choice <= samples.Length)
          break;
        Console.Write($"Invalid input. Enter a number between 1 and {samples.Length}: ");
      }
      
      var selectedSample = samples[choice - 1];
      Console.WriteLine($"Running: {selectedSample.Label}");
      Console.WriteLine();
      //end samples options menu

      // lauch two threads to test the client
      Thread thread1 = new Thread(() =>
      {
        using var httpClient = new HttpClient(new System.Net.Http.SocketsHttpHandler
        {
          PooledConnectionLifetime = System.Threading.Timeout.InfiniteTimeSpan,
        });
        AD.API.Client.AD_Client client = new AD_Client(connectionServer, httpClient);

        //choose your sample to run
        selectedSample.Run(client);

        client.CloseSession();
      });

      //Thread thread2 = new Thread(() =>
      //{ // Create a second client in the main thread
      //  string connectionServer2 = CSessionManager.StartNewSession();
      //  using var httpClient2 = new HttpClient();
      //  AD.API.Client.AD_Client client2 = new AD_Client(connectionServer2, httpClient2);

      //  Sample_SlabOn4ColumnsSupportFzVerification(client); //BigSampletestingMostAPIFunctions
      //  client2.CloseSession().GetAwaiter().GetResult();
      //});

      thread1.Start();
//      thread2.Start();

      thread1.Join(); // Wait for the thread to finish before continuing
//      thread2.Join(); // Wait for the thread to finish before continuing

      Console.WriteLine("done");
      Console.ReadLine();
    }

    //synchronous version of main, kept for reference example and easy switching if needed
    /*static void Main(string[] args)
    {
      string connectionServer = GetEnvironmentVariable("AD_API_SERVER_URL") ?? CSessionManager.DefaultApiBaseUrl;

      //if the server isn't already running at DefaultApiBaseUrl, start a new one (assuming the API server is located in the installation path)
      if (!CSessionManager.IsServiceAvailable(connectionServer))
      {
        string serverFullPath = GetEnvironmentVariable("AD_API_SERVER_BINARY_PATH") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Graitec", "Advance Design", "2027", "Bin") + Path.DirectorySeparatorChar;
				connectionServer = CSessionManager.StartNewSession(serverFullPath, false);// windowlessConsole : true for no server console window
      }

      Console.WriteLine("Connecting to API server " + connectionServer);
          //Console.WriteLine("Press ENTER to continue"); Console.ReadLine();

          if (System.String.IsNullOrEmpty(connectionServer))
          {
            Console.WriteLine("connection server empty !! Check system variables AD_API_SERVER_URL and  AD_API_SERVER_BINARY_PATH !!");
            Console.ReadLine();
          }

          using var httpClient = new HttpClient(new System.Net.Http.SocketsHttpHandler
          {
            PooledConnectionLifetime = System.Threading.Timeout.InfiniteTimeSpan,
          });
          AD.API.Client.AD_Client client = new AD_Client(connectionServer, httpClient);

      
          Sample_Portal2DStructureAndCLimaticGeneration(client);

          client.CloseSession();
      
          Console.WriteLine("done");
          Console.ReadLine();
        }
    */

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

using AD.API.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.API.Client
{
	/// <summary>
	/// Creates a planar element wall with a rigid linear support at its base,
	/// applies a linear load on the live load case, creates a ULS StrGeo combination,
	/// runs analysis and retrieves results (Forces, Displacements, Stresses, Resultant forces)
	/// for both the planar element and the linear support.
	///
	/// Model description:
	/// - Material: C25/30
	/// - 1 planar element (default properties) with geometry:
	///     (0,0,0) (0,0,3) (5,0,3) (5,0,0)
	/// - 1 rigid linear support (fixed, all DOFs) at base:
	///     (0,0,0) → (5,0,0)
	/// - Dead load case (default, no explicit loads)
	/// - Live load case with 1 linear load:
	///     geometry: (0,0,3) → (5,0,3)
	///     force: Fx=-80000, Fy=50000, Fz=-100000 [N/m], moment=0
	/// - ULS combination (StrGeo): 1.0 × Dead + 1.5 × Live
	/// - Analysis + results retrieval for the combination
	/// </summary>
	internal partial class Program
	{
		public static void Sample_PlanarWallWithLinearSupportAndResults(AD.API.Client.AD_Client client)
		{
			Environments env = new Environments()
			{
				Localization = Localization_Code.LOCALIZATION_EUROPE,
			};

			string projectPath = GetEnvironmentVariable("AD_MODELS_PATH") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Graitec", "Advance Design", "2027", "Projects") + Path.DirectorySeparatorChar;
			if (System.String.IsNullOrEmpty(projectPath))
			{
				SampleReport.Fatal("ERROR: Environment variable 'AD_MODELS_PATH' is not set. Aborting.");
				Console.WriteLine("ERROR: Environment variable 'AD_MODELS_PATH' is not set. Aborting.");
				return;
			}

			var newProj = client.NewProject(projectPath + Guid.NewGuid().ToString() + ".fto", env);

			// --- Material C25/30 ---
			Material materialObj = new Material { Name = "C25/30" };
			EID idMaterial = client.CreateMaterial(materialObj).Data;
			Console.WriteLine("Create material OID: " + idMaterial.Value.ToString());

			// --- Planar element (default properties) ---
			// Geometry: (0,0,0) (0,0,3) (5,0,3) (5,0,0)
			EID idPlanar;
			{
				ElementPlanar newPlanarElem = new ElementPlanar();
				newPlanarElem.Material = idMaterial;
				newPlanarElem.ElementType = PlanarElementType.Shell;
				newPlanarElem.GeomPtsList = new List<Pt3D>
				{
					new Pt3D { X = 0.0, Y = 0.0, Z = 0.0 },
					new Pt3D { X = 0.0, Y = 0.0, Z = 3.0 },
					new Pt3D { X = 5.0, Y = 0.0, Z = 3.0 },
					new Pt3D { X = 5.0, Y = 0.0, Z = 0.0 }
				};

				idPlanar = client.CreateElement(newPlanarElem).Data;
				Console.WriteLine("Create planar element ID: " + idPlanar.Value.ToString());
			}

			// --- Rigid linear support (fixed, all DOFs) at base: (0,0,0) → (5,0,0) ---
			EID idLinSupport;
			{
				ElementRigidLinearSupport newLinSupport = new ElementRigidLinearSupport();
				newLinSupport.GeomPtStart = new Pt3D { X = 0.0, Y = 0.0, Z = 0.0 };
				newLinSupport.GeomPtEnd = new Pt3D { X = 5.0, Y = 0.0, Z = 0.0 };
				newLinSupport.Material = idMaterial;
				newLinSupport.Restraints = new DegreeOfFreedomRestraints
				{
					Tx = true,
					Ty = true,
					Tz = true,
					Rx = true,
					Ry = true,
					Rz = true
				};

				idLinSupport = client.CreateElement(newLinSupport).Data;
				Console.WriteLine("Create rigid linear support ID: " + idLinSupport.Value.ToString());
			}

			// --- Dead load family and case (default) ---
			EID idDeadFamily;
			{
				LoadCaseFamily_DeadLoads deadFamily = new LoadCaseFamily_DeadLoads();
				deadFamily.Name = "Dead Loads Family";
				idDeadFamily = client.CreateInformationalElement(deadFamily).Data;
				Console.WriteLine("Create dead load family ID: " + idDeadFamily.Value.ToString());
			}

			EID idDeadCase;
			{
				LoadCase_DeadLoads deadCase = new LoadCase_DeadLoads();
				deadCase.Name = "Dead Loads";
				deadCase.LoadCaseFamilyID = idDeadFamily;
				idDeadCase = client.CreateInformationalElement(deadCase).Data;
				Console.WriteLine("Create dead load case ID: " + idDeadCase.Value.ToString());
			}

			// --- Live load family and case (default) ---
			EID idLiveFamily;
			{
				LoadCaseFamily_LiveLoads liveFamily = new LoadCaseFamily_LiveLoads();
				liveFamily.Name = "Live Loads Family";
				idLiveFamily = client.CreateInformationalElement(liveFamily).Data;
				Console.WriteLine("Create live load family ID: " + idLiveFamily.Value.ToString());
			}

			EID idLiveCase;
			{
				LoadCase_LiveLoads liveCase = new LoadCase_LiveLoads();
				liveCase.Name = "Live Loads";
				liveCase.LoadCaseFamilyID = idLiveFamily;
				idLiveCase = client.CreateInformationalElement(liveCase).Data;
				Console.WriteLine("Create live load case ID: " + idLiveCase.Value.ToString());
			}

			// --- Linear load on the live load case ---
			// Geometry: (0,0,3) → (5,0,3), Force: Fx=-80000, Fy=50000, Fz=-100000 [N/m], Moment=0
			{
				ElementLoadLinear newLoadLine = new ElementLoadLinear
				{
					GeomPtStart = new Pt3D { X = 0.0, Y = 0.0, Z = 3.0 },
					GeomPtEnd = new Pt3D { X = 5.0, Y = 0.0, Z = 3.0 },
					LoadCase = idLiveCase,
					Fx = -80000.0,
					Fy = 50000.0,
					Fz = -100000.0,
					Moment = new MomentComponents { Mx = 0.0, My = 0.0, Mz = 0.0 },
					Variation = new LinearVariation { Coefficient1 = 1.0, Coefficient2 = 1.0 },
				};
				EID idLinearLoad = client.CreateElement(newLoadLine).Data;
				Console.WriteLine("Create linear load ID: " + idLinearLoad.Value.ToString());
			}

			// --- ULS combination (StrGeo): 1.0 × Dead + 1.5 × Live ---
			EID idCombo;
			{
				Combination combInput = new Combination();
				combInput.ECombinationType = ECombinationType.EComboProjectSituationEluStrgeo;
				combInput.ListCasesCoeffs = new List<EIDDoublePair>
				{
					new EIDDoublePair { Key = idDeadCase, Value = 1.0 },
					new EIDDoublePair { Key = idLiveCase, Value = 1.5 }
				};

				idCombo = client.AddCombination(combInput).Data;
				Console.WriteLine("Create combination ID: " + idCombo.Value.ToString());
			}
      // Add 2nd combination
      {
        Combination combInput = new Combination();
        combInput.ECombinationType = ECombinationType.EComboProjectSituationEluStrgeo;
        combInput.ListCasesCoeffs = new List<EIDDoublePair>
        {
          new EIDDoublePair { Key = idDeadCase, Value = 1.0 },
          new EIDDoublePair { Key = idLiveCase, Value = 1.7 }
        };

        EID idComboNew = client.AddCombination(combInput).Data;
        Console.WriteLine("Create combination ID: " + idComboNew.Value.ToString());
      }
      // Add 3nd combination (with inner EXISTING) combi
      {
        Combination combInput = new Combination();
        combInput.ECombinationType = ECombinationType.EComboProjectSituationEluStrgeo;
        combInput.ListCasesCoeffs = new List<EIDDoublePair>
        {
          new EIDDoublePair { Key = idDeadCase, Value = 1.0 },
          new EIDDoublePair { Key = idCombo   , Value = 1.25 }
        };

        EID idComboNew3 = client.AddCombination(combInput).Data;
        Console.WriteLine("Create combination ID: " + idComboNew3.Value.ToString());
      }


      // --- Launch analysis ---
      bool bCalculResult = client.LaunchAnalysis().Data;
			Console.WriteLine("Launch calculation result: " + ((bCalculResult) ? "succeeded" : "failed"));
			SampleReport.Check(bCalculResult, "LaunchAnalysis", "the analysis did not complete successfully");

			if (bCalculResult)
			{
				// =========================================================================
				// Results for the PLANAR element
				// =========================================================================
				var planarIds = new List<long> { idPlanar.Value };

				Console.WriteLine();
				Console.WriteLine("=== Planar element — Displacement results (ULS combination) ===");
				var resPlanarDisp = client.GetResults(ResultType.Displacement, idCombo.Value, planarIds);
				if (resPlanarDisp?.Data != null)
					Console.WriteLine($"  Received {resPlanarDisp.Data.Count} result(s)");

				Console.WriteLine();
				Console.WriteLine("=== Planar element — Forces results (ULS combination) ===");
				var resPlanarForces = client.GetResults(ResultType.Forces, idCombo.Value, planarIds);
				if (resPlanarForces?.Data != null)
					Console.WriteLine($"  Received {resPlanarForces.Data.Count} result(s)");

				Console.WriteLine();
				Console.WriteLine("=== Planar element — Stresses results (ULS combination) ===");
				var resPlanarStresses = client.GetResults(ResultType.Stresses, idCombo.Value, planarIds);
				if (resPlanarStresses?.Data != null)
					Console.WriteLine($"  Received {resPlanarStresses.Data.Count} result(s)");

				Console.WriteLine();
				Console.WriteLine("=== Planar element — Resultant forces results (ULS combination) ===");
				var resPlanarResultant = client.GetResults(ResultType.Resultantforces, idCombo.Value, planarIds);
				if (resPlanarResultant?.Data != null)
					Console.WriteLine($"  Received {resPlanarResultant.Data.Count} result(s)");

				// =========================================================================
				// Results for the LINEAR SUPPORT
				// =========================================================================
				var supportIds = new List<long> { idLinSupport.Value };

				Console.WriteLine();
				Console.WriteLine("=== Linear support — Displacement results (ULS combination) ===");
				var resSupportDisp = client.GetResults(ResultType.Displacement, idCombo.Value, supportIds);
				if (resSupportDisp?.Data != null)
					Console.WriteLine($"  Received {resSupportDisp.Data.Count} result(s)");

				Console.WriteLine();
				Console.WriteLine("=== Linear support — Forces results (ULS combination) ===");
				var resSupportForces = client.GetResults(ResultType.Forces, idCombo.Value, supportIds);
				if (resSupportForces?.Data != null)
					Console.WriteLine($"  Received {resSupportForces.Data.Count} result(s)");

				Console.WriteLine();
				Console.WriteLine("=== Linear support — Stresses results (ULS combination) ===");
				var resSupportStresses = client.GetResults(ResultType.Stresses, idCombo.Value, supportIds);
				if (resSupportStresses?.Data != null)
					Console.WriteLine($"  Received {resSupportStresses.Data.Count} result(s)");

				Console.WriteLine();
				Console.WriteLine("=== Linear support — Resultant forces results (ULS combination) ===");
				var resSupportResultant = client.GetResults(ResultType.Resultantforces, idCombo.Value, supportIds);
				if (resSupportResultant?.Data != null)
					Console.WriteLine($"  Received {resSupportResultant.Data.Count} result(s)");
			}

			// Close the project
			client.CloseProject();
			Console.WriteLine("Close the project.");
		}
	}
}

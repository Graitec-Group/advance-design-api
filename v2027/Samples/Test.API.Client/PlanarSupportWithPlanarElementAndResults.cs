using AD.API.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.API.Client
{
	/// <summary>
	/// Creates a rigid planar support (full fixed) with a planar element on it,
	/// applies a planar load on the live load case, creates a ULS StrGeo combination,
	/// runs analysis and retrieves results (Forces, Displacements, Stresses, Resultant forces)
	/// for the planar support.
	///
	/// Model description:
	/// - Material: C25/30
	/// - 1 rigid planar support (fixed, all DOFs) with geometry:
	///     (5,0,0) (5,3,0) (0,3,0) (0,0,0)
	/// - 1 planar element (default properties) with same geometry:
	///     (5,0,0) (5,3,0) (0,3,0) (0,0,0)
	/// - Dead load case (default, no explicit loads)
	/// - Live load case with 1 planar load:
	///     geometry: (5,0,0) (5,3,0) (0,3,0) (0,0,0)
	///     force: Fx=-10000, Fy=25000, Fz=-60000 [N/m²], moment=0
	/// - ULS combination (StrGeo): 1.0 × Dead + 1.5 × Live
	/// - Analysis + results retrieval for the planar support
	/// </summary>
	internal partial class Program
	{
		public static void Sample_PlanarSupportWithPlanarElementAndResults(AD.API.Client.AD_Client client)
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

			// Common geometry points for support, planar element and planar load
			var geomPts = new List<Pt3D>
			{
				new Pt3D { X = 5.0, Y = 0.0, Z = 0.0 },
				new Pt3D { X = 5.0, Y = 3.0, Z = 0.0 },
				new Pt3D { X = 0.0, Y = 3.0, Z = 0.0 },
				new Pt3D { X = 0.0, Y = 0.0, Z = 0.0 }
			};

			// --- Rigid planar support (fixed, all DOFs) ---
			EID idPlanarSupport;
			{
				ElementRigidPlanarSupport newPlanarSupport = new ElementRigidPlanarSupport();
				newPlanarSupport.GeomPtsList = geomPts;
				newPlanarSupport.ConstraintsType = RigidSupportConstraint.Fix;
				newPlanarSupport.Restraints = new DegreeOfFreedomRestraints
				{
					Tx = true,
					Ty = true,
					Tz = true,
					Rx = true,
					Ry = true,
					Rz = true
				};

				idPlanarSupport = client.CreateElement(newPlanarSupport).Data;
				Console.WriteLine("Create rigid planar support ID: " + idPlanarSupport.Value.ToString());
			}

			// --- Planar element (default properties) with same geometry ---
			EID idPlanar;
			{
				ElementPlanar newPlanarElem = new ElementPlanar();
				newPlanarElem.Material = idMaterial;
				newPlanarElem.ElementType = PlanarElementType.Shell;
				newPlanarElem.GeomPtsList = geomPts;

				idPlanar = client.CreateElement(newPlanarElem).Data;
				Console.WriteLine("Create planar element ID: " + idPlanar.Value.ToString());
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

			// --- Planar load on the live load case ---
			// Geometry: same as support/element, Force: Fx=-10000, Fy=25000, Fz=-60000 [N/m²], Moment=0
			{
				ElementLoadPlanar newLoadPlanar = new ElementLoadPlanar
				{
					GeomPtsList = geomPts,
					CoordinateSystemType = CoordinateSystemForLinearOrPlanarGeometry.Global_or_user,
					LoadCase = idLiveCase,
					Fx = -10000.0,
					Fy = 25000.0,
					Fz = -60000.0,
					Moment = new MomentComponents { Mx = 0.0, My = 0.0, Mz = 0.0 },
					Variation = new PlanarVariation { Coefficient1 = 1.0, Coefficient2 = 1.0, Coefficient3 = 1.0 },
				};
				EID idPlanarLoad = client.CreateElement(newLoadPlanar).Data;
				Console.WriteLine("Create planar load ID: " + idPlanarLoad.Value.ToString());
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

			// --- Launch analysis ---
			bool bCalculResult = client.LaunchAnalysis().Data;
			Console.WriteLine("Launch calculation result: " + ((bCalculResult) ? "succeeded" : "failed"));
			SampleReport.Check(bCalculResult, "LaunchAnalysis", "the analysis did not complete successfully");

			if (bCalculResult)
			{
				// =========================================================================
				// Results for the PLANAR SUPPORT
				// =========================================================================
				var supportIds = new List<long> { idPlanarSupport.Value };

				Console.WriteLine();
				Console.WriteLine("=== Planar support — Displacement results (ULS combination) ===");
				var resSupportDisp = client.GetResults(ResultType.Displacement, idCombo.Value, supportIds);
				if (resSupportDisp?.Data != null)
					Console.WriteLine($"  Received {resSupportDisp.Data.Count} result(s)");

				Console.WriteLine();
				Console.WriteLine("=== Planar support — Forces results (ULS combination) ===");
				var resSupportForces = client.GetResults(ResultType.Forces, idCombo.Value, supportIds);
				if (resSupportForces?.Data != null)
					Console.WriteLine($"  Received {resSupportForces.Data.Count} result(s)");

				Console.WriteLine();
				Console.WriteLine("=== Planar support — Stresses results (ULS combination) ===");
				var resSupportStresses = client.GetResults(ResultType.Stresses, idCombo.Value, supportIds);
				if (resSupportStresses?.Data != null)
					Console.WriteLine($"  Received {resSupportStresses.Data.Count} result(s)");

				Console.WriteLine();
				Console.WriteLine("=== Planar support — Resultant forces results (ULS combination) ===");
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

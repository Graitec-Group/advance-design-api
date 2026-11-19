using AD.API.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.API.Client
{
	/// <summary>
	/// Creates a planar wall element with two interior openings (holes),
	/// then retrieves the element back to verify that the openings were correctly stored.
	///
	/// Model description:
	/// - Material: C25/30
	/// - 1 planar element (shell) with geometry:
	///     (0,0,0) (0,0,4) (6,0,4) (6,0,0)
	/// - 2 openings (holes) in the wall:
	///     Opening 1 (small window):  (1,0,1) (1,0,2) (2,0,2) (2,0,1)
	///     Opening 2 (door):          (3.5,0,0.5) (3.5,0,3) (5,0,3) (5,0,0.5)
	/// - 1 rigid linear support at base: (0,0,0) → (6,0,0)
	/// - Dead load case (self-weight only)
	/// - After creation, GetElementsObject is called to verify openings are read back
	/// </summary>
	internal partial class Program
	{
		public static void Sample_PlanarWallWithOpenings(AD.API.Client.AD_Client client)
		{
			Environments env = new Environments()
			{
				Localization = Localization_Code.LOCALIZATION_EUROPE,
			};

			string projectPath = GetEnvironmentVariable("AD_MODELS_PATH") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Graitec", "Advance Design", "2027", "Projects") + Path.DirectorySeparatorChar;
			if (string.IsNullOrEmpty(projectPath))
			{
				SampleReport.Fatal("ERROR: Environment variable 'AD_MODELS_PATH' is not set. Aborting.");
				Console.WriteLine("ERROR: Environment variable 'AD_MODELS_PATH' is not set. Aborting.");
				return;
			}

			var newProj = client.NewProject(projectPath + Guid.NewGuid().ToString() + ".fto", env);
			Console.WriteLine("=== Sample: Planar Wall With Openings ===");
			Console.WriteLine();

			// --- Material C25/30 ---
			Material materialObj = new Material { Name = "C25/30" };
			EID idMaterial = client.CreateMaterial(materialObj).Data;
			Console.WriteLine("Create material OID: " + idMaterial.Value.ToString());

			// --- Planar element (wall) with two openings ---
			// Wall geometry: 6m wide × 4m tall, in the XZ plane (Y=0)
			//   (0,0,0) (0,0,4) (6,0,4) (6,0,0)
			//
			// Opening 1 (small window): 1m×1m centered at X=1.5, Z=1.5
			//   (1,0,1) (1,0,2) (2,0,2) (2,0,1)
			//
			// Opening 2 (door-like): 1.5m×2.5m
			//   (3.5,0,0.5) (3.5,0,3) (5,0,3) (5,0,0.5)
			EID idPlanar;
			{
				ElementPlanar newPlanarElem = new ElementPlanar();
				newPlanarElem.Material = idMaterial;
				newPlanarElem.ElementType = PlanarElementType.Shell;
				newPlanarElem.GeomPtsList = new List<Pt3D>
				{
					new Pt3D { X = 0.0, Y = 0.0, Z = 0.0 },
					new Pt3D { X = 0.0, Y = 0.0, Z = 4.0 },
					new Pt3D { X = 6.0, Y = 0.0, Z = 4.0 },
					new Pt3D { X = 6.0, Y = 0.0, Z = 0.0 }
				};

				// Define two openings
				newPlanarElem.Openings = new List<ICollection<Pt3D>>
				{
					// Opening 1: small window (1m × 1m)
					new List<Pt3D>
					{
						new Pt3D { X = 1.0, Y = 0.0, Z = 1.0 },
						new Pt3D { X = 1.0, Y = 0.0, Z = 2.0 },
						new Pt3D { X = 2.0, Y = 0.0, Z = 2.0 },
						new Pt3D { X = 2.0, Y = 0.0, Z = 1.0 }
					},
					// Opening 2: door-like opening (1.5m × 2.5m)
					new List<Pt3D>
					{
						new Pt3D { X = 3.5, Y = 0.0, Z = 0.5 },
						new Pt3D { X = 3.5, Y = 0.0, Z = 3.0 },
						new Pt3D { X = 5.0, Y = 0.0, Z = 3.0 },
						new Pt3D { X = 5.0, Y = 0.0, Z = 0.5 }
					}
				};

				Console.WriteLine("Creating planar element with 2 openings...");
				var createResp = client.CreateElement(newPlanarElem);
				idPlanar = createResp.Data;
				PrintResultDetails(createResp.Details, LogVerboseLevel.AllDetails, "  CreateElement");
				Console.WriteLine("  Planar element ID: " + idPlanar.Value.ToString());
			}

			// --- Rigid linear support at base: (0,0,0) → (6,0,0) ---
			EID idLinSupport;
			{
				ElementRigidLinearSupport newLinSupport = new ElementRigidLinearSupport();
				newLinSupport.GeomPtStart = new Pt3D { X = 0.0, Y = 0.0, Z = 0.0 };
				newLinSupport.GeomPtEnd = new Pt3D { X = 6.0, Y = 0.0, Z = 0.0 };
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

			// --- Dead load family and case ---
			{
				LoadCaseFamily_DeadLoads deadFamily = new LoadCaseFamily_DeadLoads();
				deadFamily.Name = "Dead Loads Family";
				EID idDeadFamily = client.CreateInformationalElement(deadFamily).Data;
				Console.WriteLine("Create dead load family ID: " + idDeadFamily.Value.ToString());

				LoadCase_DeadLoads deadCase = new LoadCase_DeadLoads();
				deadCase.Name = "Dead Loads";
				deadCase.LoadCaseFamilyID = idDeadFamily;
				EID idDeadCase = client.CreateInformationalElement(deadCase).Data;
				Console.WriteLine("Create dead load case ID: " + idDeadCase.Value.ToString());
			}

			// ====================================================================
			// Retrieve the planar element back and verify openings
			// ====================================================================
			Console.WriteLine();
			Console.WriteLine("=== Retrieving planar element to verify openings ===");
			{
				var resp = client.GetElementsObject(new List<long> { idPlanar.Value });
				PrintResultDetails(resp.Details, LogVerboseLevel.AllDetails, "  GetElementsObject");

				if (resp.Data?.FirstOrDefault() is ElementPlanar pl)
				{
					Console.WriteLine($"  Element type: {pl.ElementType}");
					Console.WriteLine($"  Geometry points: {pl.GeomPtsList?.Count ?? 0}");
					if (pl.GeomPtsList != null)
					{
						foreach (var pt in pl.GeomPtsList)
							Console.WriteLine($"    ({pt.X}, {pt.Y}, {pt.Z})");
					}

					Console.WriteLine($"  Openings count: {pl.Openings?.Count ?? 0}");
					if (pl.Openings != null && pl.Openings.Count > 0)
					{
						int openingIdx = 0;
						foreach (var opening in pl.Openings)
						{
							openingIdx++;
							Console.WriteLine($"  Opening {openingIdx}: {opening?.Count ?? 0} points");
							if (opening != null)
							{
								foreach (var pt in opening)
									Console.WriteLine($"    ({pt.X}, {pt.Y}, {pt.Z})");
							}
						}

						// Verify expected counts
						if (pl.Openings.Count == 2)
						{
							SampleReport.Pass("Openings count");
							Console.WriteLine("  [PASS] Expected 2 openings, got 2.");
						}
						else
						{
							SampleReport.Fail($"Expected 2 openings, got {pl.Openings.Count}.");
							Console.WriteLine($"  [FAIL] Expected 2 openings, got {pl.Openings.Count}.");
						}
					}
					else
					{
						SampleReport.Fail("No openings returned! Expected 2 openings.");
						Console.WriteLine("  [FAIL] No openings returned! Expected 2 openings.");
					}
				}
				else
				{
					SampleReport.Fail($"Element not found or unexpected type (EID={idPlanar.Value}).");
					Console.WriteLine($"  [FAIL] Element not found or unexpected type (EID={idPlanar.Value}).");
				}
			}

			// Close the project
			client.CloseProject();
			Console.WriteLine();
			Console.WriteLine("Close the project.");
		}
	}
}

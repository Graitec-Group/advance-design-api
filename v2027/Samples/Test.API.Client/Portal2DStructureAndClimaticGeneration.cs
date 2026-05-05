using AD.API.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.API.Client
{
  internal partial class Program
  {
    /// <summary>
    /// Portal structure example 1: 4 steel linear elements (S235/IPE400) forming a portal frame
    /// with 2 rigid supports at the base. Creates IBC(ASCE 7-22) wind and snow family load cases,
    /// runs ClimaticAutoGeneration, and reads back the generated loads.
    /// </summary>
    static void Sample_Portal2DStructureAndCLimaticGeneration(AD.API.Client.AD_Client client)
    {
      // Set desired log verbosity level for diagnostics
      LogVerboseLevel logVerbosityLevel = LogVerboseLevel.Errors;//LogVerboseLevel.AllDetails gives also warnings and additional information
      Environments env = new Environments()
      {
        Language = Language_Code.ELanguageEnglish,
        Localization = Localization_Code.LOCALIZATION_US,
        LogVerbosity = logVerbosityLevel
      };

      string projectPath = GetEnvironmentVariable("AD_MODELS_PATH") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Graitec", "Advance Design", "2027", "Projects") + Path.DirectorySeparatorChar;
      if (System.String.IsNullOrEmpty(projectPath))
      {
        Console.WriteLine("ERROR: Environment variable 'AD_MODELS_PATH' is not set. Aborting.");
        return;
      }      

      var newProj = client.NewProject(projectPath + "WindAndSnowOn2DPortalA_" + Guid.NewGuid().ToString() + ".fto", env);
      //result diagnostics
      //way 1:// Console.WriteLine($"Create new project Success={newProj.Details.Success}, HasErrors={newProj.Details.HasErrors}, Diagnostics={DiagText(newProj.Details.Diagnostics)}");
      //way 2:
      /*if (newProj.Details.HasErrors)
      {
        DumpError(newProj.Details);
      }*/
      //way 3:
      PrintResultDetails(newProj.Details, logVerbosityLevel, "Create new project");

      // Create material S235 (steel) for linear elements
      Material materialS235 = new Material { Name = "S235" };
      var respMaterialS235 = client.CreateMaterial(materialS235);
      Console.WriteLine($"Create material S235: Success={respMaterialS235.Details.Success}, HasErrors={respMaterialS235.Details.HasErrors}, Diagnostics={DiagText(respMaterialS235.Details.Diagnostics)}");
      if (!respMaterialS235.Details.Success || respMaterialS235.Details.HasErrors)
      {
        Console.WriteLine("ERROR: CreateMaterial S235 failed. Aborting.");
        client.CloseProject();
        return;
      }

      //create concrete material for supports
      Material materialConcrete = new Material { Name = "C25/30" };
      var respMaterialConcrete = client.CreateMaterial(materialConcrete);
      Console.WriteLine($"Create material C25/30: Success={respMaterialConcrete.Details.Success}, HasErrors={respMaterialConcrete.Details.HasErrors}, Diagnostics={DiagText(respMaterialConcrete.Details.Diagnostics)}");
      if (!respMaterialConcrete.Details.Success || respMaterialConcrete.Details.HasErrors)
      {
        Console.WriteLine("ERROR: CreateMaterial C25/30 failed. Aborting.");
        client.CloseProject();
        return;
      }

      EID idMaterialS235 = respMaterialS235.Data;
      Console.WriteLine("  Material S235 OID: " + idMaterialS235.Value.ToString());

      // Create section IPE400
      var respSectionIPE400 = client.CreateSection("IPE400");
      Console.WriteLine($"Create section IPE400: Success={respSectionIPE400.Details.Success}, HasErrors={respSectionIPE400.Details.HasErrors}, Diagnostics={DiagText(respSectionIPE400.Details.Diagnostics)}");
      if (!respSectionIPE400.Details.Success || respSectionIPE400.Details.HasErrors)
      {
        Console.WriteLine("ERROR: CreateSection IPE400 failed. Aborting.");
        client.CloseProject();
        return;
      }
      EID idSectionIPE400 = respSectionIPE400.Data;
      Console.WriteLine("  Section IPE400 OID: " + idSectionIPE400.Value.ToString());

      // Create 4 linear elements forming the portal frame
      // Element 1: column (5,0,0) -> (5,0,5)
      {
        ElementLinear el = new ElementLinear
        {
          Section = idSectionIPE400,
          Material = idMaterialS235,
          GeomPtStart = new Pt3D { X = 5.0, Y = 0.0, Z = 0.0 },
          GeomPtEnd = new Pt3D { X = 5.0, Y = 0.0, Z = 5.0 },
          LoadAreaLoadTransferProperties = new LinearLoadAreaLoadTransfer { ClimaticLE2D = true }
        };
        var resp = client.CreateElement(el);
        Console.WriteLine($"Create linear element 1 (column): Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
        if (resp.Details.Success && !resp.Details.HasErrors)
          Console.WriteLine("  Element 1 ID: " + resp.Data.Value.ToString());
      }

      // Element 2: rafter (5,0,5) -> (15,0,7)
      {
        ElementLinear el = new ElementLinear
        {
          Section = idSectionIPE400,
          Material = idMaterialS235,
          GeomPtStart = new Pt3D { X = 5.0, Y = 0.0, Z = 5.0 },
          GeomPtEnd = new Pt3D { X = 15.0, Y = 0.0, Z = 7.0 },
          LoadAreaLoadTransferProperties = new LinearLoadAreaLoadTransfer { ClimaticLE2D = true }
        };
        var resp = client.CreateElement(el);
        Console.WriteLine($"Create linear element 2 (rafter): Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
        if (resp.Details.Success && !resp.Details.HasErrors)
          Console.WriteLine("  Element 2 ID: " + resp.Data.Value.ToString());
      }

      // Element 3: rafter (15,0,7) -> (29,0,5)
      {
        ElementLinear el = new ElementLinear
        {
          Section = idSectionIPE400,
          Material = idMaterialS235,
          GeomPtStart = new Pt3D { X = 15.0, Y = 0.0, Z = 7.0 },
          GeomPtEnd = new Pt3D { X = 29.0, Y = 0.0, Z = 5.0 },
          LoadAreaLoadTransferProperties = new LinearLoadAreaLoadTransfer { ClimaticLE2D = true }
        };
        var resp = client.CreateElement(el);
        Console.WriteLine($"Create linear element 3 (rafter): Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
        if (resp.Details.Success && !resp.Details.HasErrors)
          Console.WriteLine("  Element 3 ID: " + resp.Data.Value.ToString());
      }

      // Element 4: column (29,0,5) -> (29,0,0)
      {
        ElementLinear el = new ElementLinear
        {
          Section = idSectionIPE400,
          Material = idMaterialS235,
          GeomPtStart = new Pt3D { X = 29.0, Y = 0.0, Z = 5.0 },
          GeomPtEnd = new Pt3D { X = 29.0, Y = 0.0, Z = 0.0 },
          LoadAreaLoadTransferProperties = new LinearLoadAreaLoadTransfer { ClimaticLE2D = true }
        };
        var resp = client.CreateElement(el);
        Console.WriteLine($"Create linear element 4 (column): Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
        if (resp.Details.Success && !resp.Details.HasErrors)
          Console.WriteLine("  Element 4 ID: " + resp.Data.Value.ToString());
      }

      // Create 2 rigid point supports at the base
      {
        ElementRigidPunctualSupport support = new ElementRigidPunctualSupport
        {
          GeomPt = new Pt3D { X = 5.0, Y = 0.0, Z = 0.0 },
          //Restraints = new DegreeOfFreedomRestraints { Tx = true, Ty = true, Tz = true, Rx = true, Ry = true, Rz = true }
          Material = respMaterialConcrete.Data // Assign concrete material to supports
        };
        var resp = client.CreateElement(support);
        Console.WriteLine($"Create rigid support 1: Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
        if (resp.Details.Success && !resp.Details.HasErrors)
          Console.WriteLine("  Support 1 ID: " + resp.Data.Value.ToString());
      }
      {
        ElementRigidPunctualSupport support = new ElementRigidPunctualSupport
        {
          GeomPt = new Pt3D { X = 29.0, Y = 0.0, Z = 0.0 },
          //Restraints = new DegreeOfFreedomRestraints { Tx = true, Ty = true, Tz = true, Rx = true, Ry = true, Rz = true },
          Material = respMaterialConcrete.Data // Assign concrete material to supports
        };
        var resp = client.CreateElement(support);
        Console.WriteLine($"Create rigid support 2: Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
        if (resp.Details.Success && !resp.Details.HasErrors)
          Console.WriteLine("  Support 2 ID: " + resp.Data.Value.ToString());
      }

      // Create Wind IBC (ASCE 7-22) family load case - with WindDesign set to 0 (Chapter 28 Envelope Method for low rise buildings)
      {
        LoadCaseFamily_WindIBC windFamily = new LoadCaseFamily_WindIBC
        {
          Name = "Wind ASCE Chapter 28 Envelope Method",
          Building2D = new Building2DWindIBC
          {
            BuildingLength = 30.0,
            PortalPosition = 5.0,
            OpeningPosition = PortalOpeningPosition_Wind.CG_WINDEC1_OPENING2D_NONE,

            // EWind2DRealLoadsInSpan -> this transfers the load from the span to the linear element (LoadSpanFront and LoadSpanBehind can be set in the LoadAreaLoadTransferProperties on each linear element)  
            //https://graitec.com/uk/resources/technical-support/documentation/advance-design-technical-articles/climatic-loads-generator-advance-design/
            Wind2DLoadsDeterminationMethod = Wind2DLoadsDeterminationMethod_Wind_EN1991_1_4_API.EWind2DRealLoadsInSpan
          },
          AutoGeneration = new AutoGenerationWindIBC
          {
            WindWallStatus = true,
            PressureCoeff = true,
            SplitWindWalls = false,
            LoadGeneration = true
          },
          BasePressure = new BasePressureWindIBC
          {
            V = 67.0,
            ExposureCategory = ExposureCategory_WindIBC_API.WINDIBCWd2015_EXPOSURE_B,
            RiskCategory = RiskCategory_WindIBC_API.CLIMATIC_IBC2015_RISK_I,
            Kzt = 1.0,
            Kd = 0.85,
            Ke = 1.0,
            DGust = 0.85,
            Ri = 1.0,
            WindDesign = 0,//WindDesign_WindIBC_API.GRCG_WIND_IBC_METHOD_ENVELOPPE_LOW_RISE_BUIILDING,
            TorsionalLoadCases = false,
            HeightOfStructureBase = 0.0
          }
        };
        var resp = client.CreateInformationalElement(windFamily);
        Console.WriteLine($"Create Wind IBC family: Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
        if (resp.Details.Success && !resp.Details.HasErrors)
          Console.WriteLine("  Wind IBC family ID: " + resp.Data.Value.ToString());
      }

			// Create Snow IBC family load case
			{
				LoadCaseFamily_SnowIBC snowFamily = new LoadCaseFamily_SnowIBC
				{
					Name = "Snow IBC family",
					Implantation = new ImplantationSnowASCE722
					{
						Pg = 2395.0,
						Altitude = 0.0,
						W2 = 0.5,
						TerrainCategory = TerrainCategory_SnowASCE722_API.SNOWIBCSn2015_TERRAIN_B,
						Exposure = Exposure_SnowASCE722_API.SNOWIBCSn2015_EXPOSURE_PARTIALLY,
						Ct = ThermalFactorCt_SnowASCE722_API.SNOWIBCSn2015_CT_10,
						RiskCategory = RiskCategory_SnowASCE722_API.CLIMATIC_IBC2015_RISK_I
					},
					Building2D = new Building2DSnowASCE722
					{
						BuildingLength = 30.0,
						PortalPosition = 5.0
					},
					//SnowLoadCategory = SnowLoadCategory_API.ESnowZoneUnder1000
				};
				var resp = client.CreateInformationalElement(snowFamily);
				Console.WriteLine($"Create Snow IBC family: Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
				if (resp.Details.Success && !resp.Details.HasErrors)
					Console.WriteLine("  Snow IBC family ID: " + resp.Data.Value.ToString());
			}

     	Console.WriteLine("Setup completed (sturcture modedelization), now running climatic auto generation... Press any key to continue");
			//Console.ReadLine();
			// Run climatic auto generation
			{
				var resp = client.ProcessAction(AD_API_ActionType.ClimaticAutoGeneration, null);
				PrintResultDetails(newProj.Details, logVerbosityLevel, "ProcessAction(AD_API_ActionType.ClimaticAutoGeneration");
				//Console.WriteLine($"ClimaticAutoGeneration: Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Data={resp.Data}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
				if (!resp.Details.Success || resp.Details.HasErrors)
				{
					Console.WriteLine("ERROR: ClimaticAutoGeneration failed check details above.");
					client.CloseProject();
					return;
				}
			}

			// Read back loads from wind family load cases
			{
				var windFamilyQueries = new List<QueryBase>
				{
					new QueryInfoModel { InformationalElementType = InformationalElementTypeEnum.LoadCaseFamily_Wind }
				};
				var respWindFamilies = client.GetElementsID(windFamilyQueries);
				Console.WriteLine($"GetElementsID (wind families): Success={respWindFamilies.Details.Success}, HasErrors={respWindFamilies.Details.HasErrors}, Diagnostics={DiagText(respWindFamilies.Details.Diagnostics)}");
				if (respWindFamilies.Details.Success && !respWindFamilies.Details.HasErrors && respWindFamilies.Data != null && respWindFamilies.Data.Count > 0)
				{
					Console.WriteLine($"  Wind family load cases: received {respWindFamilies.Data.Count} families");

					foreach (var famId in respWindFamilies.Data)
					{
						EID famEid = new EID { Value = famId };// or use directly the EID genertaed when calling resp = client.CreateInformationalElement(windFamily);
						var windCaseQueries = new List<QueryBase>
						{
							new QueryInfoLoadCase { InformationalElementType = InformationalElementTypeEnum.LoadCase_Wind, CaseFamilyId = famEid }
						};
						var respWindCases = client.GetElementsID(windCaseQueries);
						Console.WriteLine($"  GetElementsID (wind cases in family {famId}): Success={respWindCases.Details.Success}, HasErrors={respWindCases.Details.HasErrors}, Diagnostics={DiagText(respWindCases.Details.Diagnostics)}");
						if (respWindCases.Details.Success && !respWindCases.Details.HasErrors && respWindCases.Data != null && respWindCases.Data.Count > 0)
						{
							Console.WriteLine($"    Wind family {famId}: {respWindCases.Data.Count} load cases");

							foreach (var caseIdVal in respWindCases.Data)
							{
								EID lcId = new EID { Value = caseIdVal };

								var lcObjects = client.GetInformationalElementsObject(new List<long> { caseIdVal });
								PrintLoadCase(lcObjects.Data?.FirstOrDefault());

								var loadQueries = new List<QueryBase>
								{
									new QueryElementsLoads { ElementType = ElementTypeEnum.ElementLoadLinear, LoadCaseId = lcId },
									new QueryElementsLoads { ElementType = ElementTypeEnum.ElementLoadPunctual, LoadCaseId = lcId },
									new QueryElementsLoads { ElementType = ElementTypeEnum.ElementLoadPlanar, LoadCaseId = lcId }
								};
								var respLoadIds = client.GetElementsID(loadQueries);
								Console.WriteLine($"    GetElementsID (loads in wind case {caseIdVal}): Success={respLoadIds.Details.Success}, HasErrors={respLoadIds.Details.HasErrors}, Diagnostics={DiagText(respLoadIds.Details.Diagnostics)}");
								if (respLoadIds.Details.Success && !respLoadIds.Details.HasErrors && respLoadIds.Data != null && respLoadIds.Data.Count > 0)
								{
									Console.WriteLine($"      Wind load case {caseIdVal}: {respLoadIds.Data.Count} loads found");

									var respObjects = client.GetElementsObject(respLoadIds.Data);
									Console.WriteLine($"      GetElementsObject: Success={respObjects.Details.Success}, HasErrors={respObjects.Details.HasErrors}, Diagnostics={DiagText(respObjects.Details.Diagnostics)}");
									if (respObjects.Details.Success && !respObjects.Details.HasErrors && respObjects.Data != null)
									{
										Console.WriteLine($"      Read {respObjects.Data.Count} load objects");
										foreach (var obj in respObjects.Data)
										{
											//Console.WriteLine($"        Load type: {obj.GetType().Name}");
											PrintLoad(obj);
										}
									}
								}
								else
								{
									Console.WriteLine($"      Wind load case {caseIdVal}: no loads found");
								}
							}
						}
					}
				}
				else
				{
					Console.WriteLine("  No wind family load cases found");
				}
			}

			// Read back loads from snow family load cases
			{
				var snowFamilyQueries = new List<QueryBase>
				{
					new QueryInfoModel { InformationalElementType = InformationalElementTypeEnum.LoadCaseFamily_Snow }
				};
				var respSnowFamilies = client.GetElementsID(snowFamilyQueries);
				Console.WriteLine($"GetElementsID (snow families): Success={respSnowFamilies.Details.Success}, HasErrors={respSnowFamilies.Details.HasErrors}, Diagnostics={DiagText(respSnowFamilies.Details.Diagnostics)}");
				if (respSnowFamilies.Details.Success && !respSnowFamilies.Details.HasErrors && respSnowFamilies.Data != null && respSnowFamilies.Data.Count > 0)
				{
					Console.WriteLine($"  Snow family load cases: received {respSnowFamilies.Data.Count} families");

					foreach (var famId in respSnowFamilies.Data)
					{
						EID famEid = new EID { Value = famId };// or use directly the EID genertaed when calling resp = client.CreateInformationalElement(snowFamily);
						var snowCaseQueries = new List<QueryBase>
						{
							new QueryInfoLoadCase { InformationalElementType = InformationalElementTypeEnum.LoadCase_Snow, CaseFamilyId = famEid }
						};
						var respSnowCases = client.GetElementsID(snowCaseQueries);
						Console.WriteLine($"  GetElementsID (snow cases in family {famId}): Success={respSnowCases.Details.Success}, HasErrors={respSnowCases.Details.HasErrors}, Diagnostics={DiagText(respSnowCases.Details.Diagnostics)}");
						if (respSnowCases.Details.Success && !respSnowCases.Details.HasErrors && respSnowCases.Data != null && respSnowCases.Data.Count > 0)
						{
							Console.WriteLine($"    Snow family {famId}: {respSnowCases.Data.Count} load cases");

							foreach (var caseIdVal in respSnowCases.Data)
							{
								EID lcId = new EID { Value = caseIdVal };
								var lcObjects = client.GetInformationalElementsObject(new List<long> { caseIdVal });
								PrintLoadCase(lcObjects.Data?.FirstOrDefault());

								var loadQueries = new List<QueryBase>
								{
									new QueryElementsLoads { ElementType = ElementTypeEnum.ElementLoadLinear, LoadCaseId = lcId },
									new QueryElementsLoads { ElementType = ElementTypeEnum.ElementLoadPunctual, LoadCaseId = lcId },
									new QueryElementsLoads { ElementType = ElementTypeEnum.ElementLoadPlanar, LoadCaseId = lcId }
								};
								var respLoadIds = client.GetElementsID(loadQueries);
								Console.WriteLine($"    GetElementsID (loads in snow case {caseIdVal}): Success={respLoadIds.Details.Success}, HasErrors={respLoadIds.Details.HasErrors}, Diagnostics={DiagText(respLoadIds.Details.Diagnostics)}");
								if (respLoadIds.Details.Success && !respLoadIds.Details.HasErrors && respLoadIds.Data != null && respLoadIds.Data.Count > 0)
								{
									Console.WriteLine($"      Snow load case {caseIdVal}: {respLoadIds.Data.Count} loads found");

									var respObjects = client.GetElementsObject(respLoadIds.Data);
									Console.WriteLine($"      GetElementsObject: Success={respObjects.Details.Success}, HasErrors={respObjects.Details.HasErrors}, Diagnostics={DiagText(respObjects.Details.Diagnostics)}");
									if (respObjects.Details.Success && !respObjects.Details.HasErrors && respObjects.Data != null)
									{
										Console.WriteLine($"      Read {respObjects.Data.Count} load objects");
										foreach (var obj in respObjects.Data)
										{
											//Console.WriteLine($"        Load type: {obj.GetType().Name}");
											PrintLoad(obj);
										}
									}
								}
								else
								{
									Console.WriteLine($"      Snow load case {caseIdVal}: no loads found");
								}
							}
						}
					}
				}
				else
				{
					Console.WriteLine("  No snow family load cases found");
				}
			}

			client.CloseProject();
			Console.WriteLine("Sample_PortalStructure1: Close the project.");
		}

		/// <summary>
		/// Portal structure example 2:
		/// forming a portal frame with 2 rigid supports at the base. Creates IBC wind and snow family load cases,
		/// runs ClimaticAutoGeneration, and reads back the generated loads.
		/// </summary>
		static void Sample_Portal2DStructureAndCLimaticGeneration_wMultipleElementsInTheRoof(AD.API.Client.AD_Client client)
    {
      // Set desired log verbosity level for diagnostics
      LogVerboseLevel logVerbosityLevel = LogVerboseLevel.Errors;//LogVerboseLevel.AllDetails gives also warnings an additional information
      Environments env = new Environments()
      {
        Language = Language_Code.ELanguageEnglish,
        Localization = Localization_Code.LOCALIZATION_US,
        LogVerbosity = logVerbosityLevel
      };

      string projectPath = GetEnvironmentVariable("AD_MODELS_PATH") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Graitec", "Advance Design", "2027", "Projects") + Path.DirectorySeparatorChar;
      if (System.String.IsNullOrEmpty(projectPath))
      {
        Console.WriteLine("ERROR: Environment variable 'AD_MODELS_PATH' is not set. Aborting.");
        return;
      }
      //Console.ReadLine();

      var newProj = client.NewProject(projectPath + "WindAndSnowOn2DPortalB_" + Guid.NewGuid().ToString() + ".fto", env);
      //result diagnostics
      //way 1:// Console.WriteLine($"Create new project Success={newProj.Details.Success}, HasErrors={newProj.Details.HasErrors}, Diagnostics={DiagText(newProj.Details.Diagnostics)}");
      //way 2:
      /*if (newProj.Details.HasErrors)
      {
        DumpError(newProj.Details);
      }*/
      //way 3:
      PrintResultDetails(newProj.Details, logVerbosityLevel, "Create new project");


      //create concrete material for supports
      Material materialConcrete = new Material { Name = "C25/30" };
      var respMaterialConcrete = client.CreateMaterial(materialConcrete);
      Console.WriteLine($"Create material S235: Success={respMaterialConcrete.Details.Success}, HasErrors={respMaterialConcrete.Details.HasErrors}, Diagnostics={DiagText(respMaterialConcrete.Details.Diagnostics)}");
      if (!respMaterialConcrete.Details.Success || respMaterialConcrete.Details.HasErrors)
      {
        Console.WriteLine("ERROR: CreateMaterial S235 failed. Aborting.");
        client.CloseProject();
        return;
      }

      // Create materials
      Material materialS235 = new Material { Name = "S235" };
      var respMaterialS235 = client.CreateMaterial(materialS235);
      Console.WriteLine($"Create material S235: Success={respMaterialS235.Details.Success}, HasErrors={respMaterialS235.Details.HasErrors}, Diagnostics={DiagText(respMaterialS235.Details.Diagnostics)}");
      if (!respMaterialS235.Details.Success || respMaterialS235.Details.HasErrors) { Console.WriteLine("ERROR: CreateMaterial S235 failed. Aborting."); client.CloseProject(); return; }
      EID idMaterialS235 = respMaterialS235.Data;
      Console.WriteLine("  Material S235 OID: " + idMaterialS235.Value.ToString());

      Material materialC25 = new Material { Name = "C25/30" };
      var respMaterialC25 = client.CreateMaterial(materialC25);
      Console.WriteLine($"Create material C25/30: Success={respMaterialC25.Details.Success}, HasErrors={respMaterialC25.Details.HasErrors}, Diagnostics={DiagText(respMaterialC25.Details.Diagnostics)}");
      if (!respMaterialC25.Details.Success || respMaterialC25.Details.HasErrors) { Console.WriteLine("ERROR: CreateMaterial C25/30 failed. Aborting."); client.CloseProject(); return; }
      EID idMaterialC25 = respMaterialC25.Data;
      Console.WriteLine("  Material C25/30 OID: " + idMaterialC25.Value.ToString());

      // Create sections
      var respSectionIPE400 = client.CreateSection("IPE400");
      Console.WriteLine($"Create section IPE400: Success={respSectionIPE400.Details.Success}, HasErrors={respSectionIPE400.Details.HasErrors}, Diagnostics={DiagText(respSectionIPE400.Details.Diagnostics)}");
      if (!respSectionIPE400.Details.Success || respSectionIPE400.Details.HasErrors) { Console.WriteLine("ERROR: CreateSection IPE400 failed. Aborting."); client.CloseProject(); return; }
      EID idSectionIPE400 = respSectionIPE400.Data;
      Console.WriteLine("  Section IPE400 OID: " + idSectionIPE400.Value.ToString());

      var respSectionR20x30 = client.CreateSection("R20*30");
      Console.WriteLine($"Create section R20*30: Success={respSectionR20x30.Details.Success}, HasErrors={respSectionR20x30.Details.HasErrors}, Diagnostics={DiagText(respSectionR20x30.Details.Diagnostics)}");
      if (!respSectionR20x30.Details.Success || respSectionR20x30.Details.HasErrors) { Console.WriteLine("ERROR: CreateSection R20*30 failed. Aborting."); client.CloseProject(); return; }
      EID idSectionR20x30 = respSectionR20x30.Data;
      Console.WriteLine("  Section R20*30 OID: " + idSectionR20x30.Value.ToString());

      // Create 6 linear elements forming the portal frame
      // Element 1 (S235/IPE400): column (5,0,0) -> (5,0,5)
      {
        ElementLinear el = new ElementLinear
        {
          Section = idSectionIPE400,
          Material = idMaterialS235,
          GeomPtStart = new Pt3D { X = 5.0, Y = 0.0, Z = 0.0 },
          GeomPtEnd = new Pt3D { X = 5.0, Y = 0.0, Z = 5.0 },

          //LoadSpanFront and LoadSpanBehind for 2D climatic generation also can be set on each element, preferably the same values for all
          LoadAreaLoadTransferProperties = new LinearLoadAreaLoadTransfer { ClimaticLE2D = true },

        };
        var resp = client.CreateElement(el);
        Console.WriteLine($"Create linear element 1 (S235/IPE400 column): Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
        if (resp.Details.Success && !resp.Details.HasErrors) Console.WriteLine("  Element 1 ID: " + resp.Data.Value.ToString());
      }

      // Element 3 (S235/IPE400): rafter (15,0,7) -> (24,0,5.71)
      {
        ElementLinear el = new ElementLinear
        {
          Section = idSectionIPE400,
          Material = idMaterialS235,
          GeomPtStart = new Pt3D { X = 15.0, Y = 0.0, Z = 7.0 },
          GeomPtEnd = new Pt3D { X = 24.0, Y = 0.0, Z = 5.71 },
          LoadAreaLoadTransferProperties = new LinearLoadAreaLoadTransfer { ClimaticLE2D = true }
        };
        var resp = client.CreateElement(el);
        Console.WriteLine($"Create linear element 3 (S235/IPE400 rafter): Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
        if (resp.Details.Success && !resp.Details.HasErrors) Console.WriteLine("  Element 3 ID: " + resp.Data.Value.ToString());
      }

      // Element 4 (S235/IPE400): column (29,0,5) -> (29,0,0)
      {
        ElementLinear el = new ElementLinear
        {
          Section = idSectionIPE400,
          Material = idMaterialS235,
          GeomPtStart = new Pt3D { X = 29.0, Y = 0.0, Z = 5.0 },
          GeomPtEnd = new Pt3D { X = 29.0, Y = 0.0, Z = 0.0 },
          LoadAreaLoadTransferProperties = new LinearLoadAreaLoadTransfer { ClimaticLE2D = true }
        };
        var resp = client.CreateElement(el);
        Console.WriteLine($"Create linear element 4 (S235/IPE400 column): Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
        if (resp.Details.Success && !resp.Details.HasErrors) Console.WriteLine("  Element 4 ID: " + resp.Data.Value.ToString());
      }

      // Element 5 (C25/30/R20*30): concrete rafter (15,0,7) -> (10.53,0,6.11)
      {
        ElementLinear el = new ElementLinear
        {
          Section = idSectionR20x30,
          Material = idMaterialC25,
          GeomPtStart = new Pt3D { X = 15.0, Y = 0.0, Z = 7.0 },
          GeomPtEnd = new Pt3D { X = 10.53, Y = 0.0, Z = 6.11 },
          LoadAreaLoadTransferProperties = new LinearLoadAreaLoadTransfer { ClimaticLE2D = true }
        };
        var resp = client.CreateElement(el);
        Console.WriteLine($"Create linear element 5 (C25/30/R20*30 rafter): Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
        if (resp.Details.Success && !resp.Details.HasErrors) Console.WriteLine("  Element 5 ID: " + resp.Data.Value.ToString());
      }

      // Element 10 (S235/IPE400): rafter (24,0,5.71) -> (29,0,5)
      {
        ElementLinear el = new ElementLinear
        {
          Section = idSectionIPE400,
          Material = idMaterialS235,
          GeomPtStart = new Pt3D { X = 24.0, Y = 0.0, Z = 5.71 },
          GeomPtEnd = new Pt3D { X = 29.0, Y = 0.0, Z = 5.0 },
          LoadAreaLoadTransferProperties = new LinearLoadAreaLoadTransfer { ClimaticLE2D = true }
        };
        var resp = client.CreateElement(el);
        Console.WriteLine($"Create linear element 10 (S235/IPE400 rafter): Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
        if (resp.Details.Success && !resp.Details.HasErrors) Console.WriteLine("  Element 10 ID: " + resp.Data.Value.ToString());
      }

      // Element 11 (C25/30/R20*30): concrete rafter (10.53,0,6.11) -> (5,0,5)
      {
        ElementLinear el = new ElementLinear
        {
          Section = idSectionR20x30,
          Material = idMaterialC25,
          GeomPtStart = new Pt3D { X = 10.53, Y = 0.0, Z = 6.11 },
          GeomPtEnd = new Pt3D { X = 5.0, Y = 0.0, Z = 5.0 },
          LoadAreaLoadTransferProperties = new LinearLoadAreaLoadTransfer { ClimaticLE2D = true }
        };
        var resp = client.CreateElement(el);
        Console.WriteLine($"Create linear element 11 (C25/30/R20*30 rafter): Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
        if (resp.Details.Success && !resp.Details.HasErrors) Console.WriteLine("  Element 11 ID: " + resp.Data.Value.ToString());
      }

      // Create 2 rigid point supports at the base
      {
        ElementRigidPunctualSupport support = new ElementRigidPunctualSupport
        {
          GeomPt = new Pt3D { X = 5.0, Y = 0.0, Z = 0.0 },
          //Restraints = new DegreeOfFreedomRestraints { Tx = true, Ty = true, Tz = true, Rx = true, Ry = true, Rz = true }
          Material = respMaterialConcrete.Data // Assign concrete material to supports
        };
        var resp = client.CreateElement(support);
        Console.WriteLine($"Create rigid support 1: Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
        if (resp.Details.Success && !resp.Details.HasErrors)
          Console.WriteLine("  Support 1 ID: " + resp.Data.Value.ToString());
      }
      {
        ElementRigidPunctualSupport support = new ElementRigidPunctualSupport
        {
          GeomPt = new Pt3D { X = 29.0, Y = 0.0, Z = 0.0 },
          //Restraints = new DegreeOfFreedomRestraints { Tx = true, Ty = true, Tz = true, Rx = true, Ry = true, Rz = true },
          Material = respMaterialConcrete.Data // Assign concrete material to supports
        };
        var resp = client.CreateElement(support);
        Console.WriteLine($"Create rigid support 2: Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
        if (resp.Details.Success && !resp.Details.HasErrors)
          Console.WriteLine("  Support 2 ID: " + resp.Data.Value.ToString());
      }

      // Create Wind IBC ASCE 7-22 family load case with WindDesign (method) set to 0 (Chapter 28 Envelope Method for low rise buildings), and loads determined in span (not section) and transferred from the whole span area to linear elements (LoadSpanFront and LoadSpanBehind can be set in the LoadAreaLoadTransferProperties on each linear element)
      {
        LoadCaseFamily_WindIBC windFamily = new LoadCaseFamily_WindIBC
        {
          Name = "Wind IBC Chapter 28 Envelope Method",
          Building2D = new Building2DWindIBC
          {
            BuildingLength = 30.0,
            PortalPosition = 5.0,
            OpeningPosition = PortalOpeningPosition_Wind.CG_WINDEC1_OPENING2D_NONE,

            // EWind2DRealLoadsInSpan -> this transfers the load from the span to the linear element (LoadSpanFront and LoadSpanBehind can be set in the LoadAreaLoadTransferProperties on each linear element)  
            //https://graitec.com/uk/resources/technical-support/documentation/advance-design-technical-articles/climatic-loads-generator-advance-design/
            Wind2DLoadsDeterminationMethod = Wind2DLoadsDeterminationMethod_Wind_EN1991_1_4_API.EWind2DRealLoadsInSpan 
          },
          AutoGeneration = new AutoGenerationWindIBC
          {
            WindWallStatus = true,
            PressureCoeff = true,
            SplitWindWalls = false,
            LoadGeneration = true
          },
          BasePressure = new BasePressureWindIBC
          {
            V = 67.0,
            ExposureCategory = ExposureCategory_WindIBC_API.WINDIBCWd2015_EXPOSURE_B,
            RiskCategory = RiskCategory_WindIBC_API.CLIMATIC_IBC2015_RISK_I,
            Kzt = 1.0,
            Kd = 0.85,
            Ke = 1.0,
            DGust = 0.85,
            Ri = 1.0,
            WindDesign = 0,//WindDesign_WindIBC_API.GRCG_WIND_IBC_METHOD_ENVELOPPE_LOW_RISE_BUIILDING,
            TorsionalLoadCases = false,
            HeightOfStructureBase = 0.0
          }
        };
        var resp = client.CreateInformationalElement(windFamily);
        Console.WriteLine($"Create Wind IBC family: Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
        if (resp.Details.Success && !resp.Details.HasErrors)
          Console.WriteLine("  Wind IBC family ID: " + resp.Data.Value.ToString());
      }

      // Create Snow IBC family load case
      {
        LoadCaseFamily_SnowIBC snowFamily = new LoadCaseFamily_SnowIBC
        {
          Name = "Snow IBC family",
          Implantation = new ImplantationSnowASCE722
          {
            Pg = 2395.0,
            Altitude = 0.0,
            W2 = 0.5,
            TerrainCategory = TerrainCategory_SnowASCE722_API.SNOWIBCSn2015_TERRAIN_B,
            Exposure = Exposure_SnowASCE722_API.SNOWIBCSn2015_EXPOSURE_PARTIALLY,
            Ct = ThermalFactorCt_SnowASCE722_API.SNOWIBCSn2015_CT_10,
            RiskCategory = RiskCategory_SnowASCE722_API.CLIMATIC_IBC2015_RISK_I
          },
          Building2D = new Building2DSnowASCE722
          {
            BuildingLength = 30.0,
            PortalPosition = 5.0
          },
          //SnowLoadCategory = SnowLoadCategory_API.ESnowZoneUnder1000
        };
        var resp = client.CreateInformationalElement(snowFamily);
        Console.WriteLine($"Create Snow IBC family: Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
        if (resp.Details.Success && !resp.Details.HasErrors)
          Console.WriteLine("  Snow IBC family ID: " + resp.Data.Value.ToString());
      }
     
      Console.WriteLine("Setup completed (sturcture modedelization), now running climatic auto generation... Press any key to continue");
      //Console.ReadLine();
      // Run climatic auto generation
      {
        var resp = client.ProcessAction(AD_API_ActionType.ClimaticAutoGeneration, null);
        PrintResultDetails(newProj.Details, logVerbosityLevel, "ProcessAction(AD_API_ActionType.ClimaticAutoGeneration");
        //Console.WriteLine($"ClimaticAutoGeneration: Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Data={resp.Data}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
        if (!resp.Details.Success || resp.Details.HasErrors)
        {
          Console.WriteLine("ERROR: ClimaticAutoGeneration failed check details above.");
          client.CloseProject();
          return;
        }
      }

      // Read back loads from wind family load cases
      {
        var windFamilyQueries = new List<QueryBase>
        {
          new QueryInfoModel { InformationalElementType = InformationalElementTypeEnum.LoadCaseFamily_Wind }
        };
        var respWindFamilies = client.GetElementsID(windFamilyQueries);
        Console.WriteLine($"GetElementsID (wind families): Success={respWindFamilies.Details.Success}, HasErrors={respWindFamilies.Details.HasErrors}, Diagnostics={DiagText(respWindFamilies.Details.Diagnostics)}");
        if (respWindFamilies.Details.Success && !respWindFamilies.Details.HasErrors && respWindFamilies.Data != null && respWindFamilies.Data.Count > 0)
        {
          Console.WriteLine($"  Wind family load cases: received {respWindFamilies.Data.Count} families");

          foreach (var famId in respWindFamilies.Data)
          {
            EID famEid = new EID { Value = famId };// or use directly the EID genertaed when calling resp = client.CreateInformationalElement(windFamily);
            var windCaseQueries = new List<QueryBase>
            {
              new QueryInfoLoadCase { InformationalElementType = InformationalElementTypeEnum.LoadCase_Wind, CaseFamilyId = famEid }
            };
            var respWindCases = client.GetElementsID(windCaseQueries);
            Console.WriteLine($"  GetElementsID (wind cases in family {famId}): Success={respWindCases.Details.Success}, HasErrors={respWindCases.Details.HasErrors}, Diagnostics={DiagText(respWindCases.Details.Diagnostics)}");
            if (respWindCases.Details.Success && !respWindCases.Details.HasErrors && respWindCases.Data != null && respWindCases.Data.Count > 0)
            {
              Console.WriteLine($"    Wind family {famId}: {respWindCases.Data.Count} load cases");

              foreach (var caseIdVal in respWindCases.Data)
              {
                EID lcId = new EID { Value = caseIdVal };

                var lcObjects = client.GetInformationalElementsObject(new List<long> { caseIdVal });
                PrintLoadCase(lcObjects.Data?.FirstOrDefault());

                var loadQueries = new List<QueryBase>
                {
                  new QueryElementsLoads { ElementType = ElementTypeEnum.ElementLoadLinear, LoadCaseId = lcId },
                  new QueryElementsLoads { ElementType = ElementTypeEnum.ElementLoadPunctual, LoadCaseId = lcId },
                  new QueryElementsLoads { ElementType = ElementTypeEnum.ElementLoadPlanar, LoadCaseId = lcId }
                };
                var respLoadIds = client.GetElementsID(loadQueries);
                Console.WriteLine($"    GetElementsID (loads in wind case {caseIdVal}): Success={respLoadIds.Details.Success}, HasErrors={respLoadIds.Details.HasErrors}, Diagnostics={DiagText(respLoadIds.Details.Diagnostics)}");
                if (respLoadIds.Details.Success && !respLoadIds.Details.HasErrors && respLoadIds.Data != null && respLoadIds.Data.Count > 0)
                {
                  Console.WriteLine($"      Wind load case {caseIdVal}: {respLoadIds.Data.Count} loads found");

                  var respObjects = client.GetElementsObject(respLoadIds.Data);
                  Console.WriteLine($"      GetElementsObject: Success={respObjects.Details.Success}, HasErrors={respObjects.Details.HasErrors}, Diagnostics={DiagText(respObjects.Details.Diagnostics)}");
                  if (respObjects.Details.Success && !respObjects.Details.HasErrors && respObjects.Data != null)
                  {
                    Console.WriteLine($"      Read {respObjects.Data.Count} load objects");
                    foreach (var obj in respObjects.Data)
                    {
                      //Console.WriteLine($"        Load type: {obj.GetType().Name}");
                      PrintLoad(obj);
                    }
                  }
                }
                else
                {
                  Console.WriteLine($"      Wind load case {caseIdVal}: no loads found");
                }
              }
            }
          }
        }
        else
        {
          Console.WriteLine("  No wind family load cases found");
        }
      }

      // Read back loads from snow family load cases
      {
        var snowFamilyQueries = new List<QueryBase>
        {
          new QueryInfoModel { InformationalElementType = InformationalElementTypeEnum.LoadCaseFamily_Snow }
        };
        var respSnowFamilies = client.GetElementsID(snowFamilyQueries);
        Console.WriteLine($"GetElementsID (snow families): Success={respSnowFamilies.Details.Success}, HasErrors={respSnowFamilies.Details.HasErrors}, Diagnostics={DiagText(respSnowFamilies.Details.Diagnostics)}");
        if (respSnowFamilies.Details.Success && !respSnowFamilies.Details.HasErrors && respSnowFamilies.Data != null && respSnowFamilies.Data.Count > 0)
        {
          Console.WriteLine($"  Snow family load cases: received {respSnowFamilies.Data.Count} families");

          foreach (var famId in respSnowFamilies.Data)
          {
            EID famEid = new EID { Value = famId };// or use directly the EID genertaed when calling resp = client.CreateInformationalElement(snowFamily);
            var snowCaseQueries = new List<QueryBase>
            {
              new QueryInfoLoadCase { InformationalElementType = InformationalElementTypeEnum.LoadCase_Snow, CaseFamilyId = famEid }
            };
            var respSnowCases = client.GetElementsID(snowCaseQueries);
            Console.WriteLine($"  GetElementsID (snow cases in family {famId}): Success={respSnowCases.Details.Success}, HasErrors={respSnowCases.Details.HasErrors}, Diagnostics={DiagText(respSnowCases.Details.Diagnostics)}");
            if (respSnowCases.Details.Success && !respSnowCases.Details.HasErrors && respSnowCases.Data != null && respSnowCases.Data.Count > 0)
            {
              Console.WriteLine($"    Snow family {famId}: {respSnowCases.Data.Count} load cases");

              foreach (var caseIdVal in respSnowCases.Data)
              {
                EID lcId = new EID { Value = caseIdVal };
                var lcObjects = client.GetInformationalElementsObject(new List<long> { caseIdVal });
                PrintLoadCase(lcObjects.Data?.FirstOrDefault());

                var loadQueries = new List<QueryBase>
                {
                  new QueryElementsLoads { ElementType = ElementTypeEnum.ElementLoadLinear, LoadCaseId = lcId },
                  new QueryElementsLoads { ElementType = ElementTypeEnum.ElementLoadPunctual, LoadCaseId = lcId },
                  new QueryElementsLoads { ElementType = ElementTypeEnum.ElementLoadPlanar, LoadCaseId = lcId }
                };
                var respLoadIds = client.GetElementsID(loadQueries);
                Console.WriteLine($"    GetElementsID (loads in snow case {caseIdVal}): Success={respLoadIds.Details.Success}, HasErrors={respLoadIds.Details.HasErrors}, Diagnostics={DiagText(respLoadIds.Details.Diagnostics)}");
                if (respLoadIds.Details.Success && !respLoadIds.Details.HasErrors && respLoadIds.Data != null && respLoadIds.Data.Count > 0)
                {
                  Console.WriteLine($"      Snow load case {caseIdVal}: {respLoadIds.Data.Count} loads found");

                  var respObjects = client.GetElementsObject(respLoadIds.Data);
                  Console.WriteLine($"      GetElementsObject: Success={respObjects.Details.Success}, HasErrors={respObjects.Details.HasErrors}, Diagnostics={DiagText(respObjects.Details.Diagnostics)}");
                  if (respObjects.Details.Success && !respObjects.Details.HasErrors && respObjects.Data != null)
                  {
                    Console.WriteLine($"      Read {respObjects.Data.Count} load objects");
                    foreach (var obj in respObjects.Data)
                    {
                      //Console.WriteLine($"        Load type: {obj.GetType().Name}");
                      PrintLoad(obj);
                    }
                  }
                }
                else
                {
                  Console.WriteLine($"      Snow load case {caseIdVal}: no loads found");
                }
              }
            }
          }
        }
        else
        {
          Console.WriteLine("  No snow family load cases found");
        }
      }

      client.CloseProject();
      Console.WriteLine("Sample_PortalStructure1: Close the project.");
    }



    /// <summary>
    /// Portal 2D structure with protruding roof / overhangs example:
    /// 6 linear elements forming an asymmetric portal frame
    /// with 2 protruding roof overhangs (elements 1 and 11) and 2 rigid supports at the base.
    /// Creates IBC (ASCE 7-22) wind and snow family load cases, runs ClimaticAutoGeneration,
    /// and reads back the generated loads.
    ///
    /// Structure geometry (all elements: C25/30, R20*30, ClimaticLE2D=true):
    ///   El 1  (left overhang  / protruding roof): (2.0,0,4.0)   → (4.0,0,4.86)
    ///   El 4  (left column)                     : (4.0,0,0.0)   → (4.0,0,4.86)
    ///   El 5  (left rafter)                     : (4.0,0,4.86)  → (9.0,0,7.0)
    ///   El 2  (right rafter)                    : (9.0,0,7.0)   → (13.5,0,5.5)
    ///   El 3  (right column)                    : (13.5,0,5.5)  → (13.5,0,0.0)
    ///   El 11 (right overhang / protruding roof): (13.5,0,5.5)  → (15.0,0,5.0)
    ///
    /// Note: The protruding-roof character of overhangs (CG_LOADAREA_PROTRUDING_ROOF) is
    /// auto-detected by the climatic engine from the element geometry. All linear elements
    /// participate in 2D climatic generation via ClimaticLE2D = true.
    /// </summary>
    static void Sample_Portal2DwithProtrudingRoofOrOverhangsandClimaticGeneration(AD.API.Client.AD_Client client)
    {
      // Set desired log verbosity level for diagnostics
      LogVerboseLevel logVerbosityLevel = LogVerboseLevel.Errors; // LogVerboseLevel.AllDetails gives also warnings and additional information
      Environments env = new Environments()
      {
        Language = Language_Code.ELanguageEnglish,
        Localization = Localization_Code.LOCALIZATION_US,
        LogVerbosity = logVerbosityLevel
      };

      string projectPath = GetEnvironmentVariable("AD_MODELS_PATH") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Graitec", "Advance Design", "2027", "Projects") + Path.DirectorySeparatorChar;
      if (System.String.IsNullOrEmpty(projectPath))
      {
        Console.WriteLine("ERROR: Environment variable 'AD_MODELS_PATH' is not set. Aborting.");
        return;
      }

      var newProj = client.NewProject(projectPath + "WindAndSnowOn2DPortalWithOverhangs_" + Guid.NewGuid().ToString() + ".fto", env);
      PrintResultDetails(newProj.Details, logVerbosityLevel, "Create new project");

      // Create concrete material C25/30 for all elements
      Material materialConcrete = new Material { Name = "C25/30" };
      var respMaterialConcrete = client.CreateMaterial(materialConcrete);
      Console.WriteLine($"Create material C25/30: Success={respMaterialConcrete.Details.Success}, HasErrors={respMaterialConcrete.Details.HasErrors}, Diagnostics={DiagText(respMaterialConcrete.Details.Diagnostics)}");
      if (!respMaterialConcrete.Details.Success || respMaterialConcrete.Details.HasErrors)
      {
        Console.WriteLine("ERROR: CreateMaterial C25/30 failed. Aborting.");
        client.CloseProject();
        return;
      }
      EID idMaterialConcrete = respMaterialConcrete.Data;
      Console.WriteLine("  Material C25/30 OID: " + idMaterialConcrete.Value.ToString());

      Material materialS235 = new Material { Name = "S235" };
      var respMaterialS235 = client.CreateMaterial(materialS235);
      Console.WriteLine($"Create material S235: Success={respMaterialS235.Details.Success}, HasErrors={respMaterialS235.Details.HasErrors}, Diagnostics={DiagText(respMaterialS235.Details.Diagnostics)}");
      if (!respMaterialS235.Details.Success || respMaterialS235.Details.HasErrors) { Console.WriteLine("ERROR: CreateMaterial S235 failed. Aborting."); client.CloseProject(); return; }
      EID idMaterialS235 = respMaterialS235.Data;
      Console.WriteLine("  Material S235 OID: " + idMaterialS235.Value.ToString());
     
      // Create sections
      var respSectionIPE400 = client.CreateSection("IPE400");
      Console.WriteLine($"Create section IPE400: Success={respSectionIPE400.Details.Success}, HasErrors={respSectionIPE400.Details.HasErrors}, Diagnostics={DiagText(respSectionIPE400.Details.Diagnostics)}");
      if (!respSectionIPE400.Details.Success || respSectionIPE400.Details.HasErrors) { Console.WriteLine("ERROR: CreateSection IPE400 failed. Aborting."); client.CloseProject(); return; }
      EID idSectionIPE400 = respSectionIPE400.Data;
      Console.WriteLine("  Section IPE400 OID: " + idSectionIPE400.Value.ToString());

      // Create section R20*30
      var respSectionR20x30 = client.CreateSection("R20*30");
      Console.WriteLine($"Create section R20*30: Success={respSectionR20x30.Details.Success}, HasErrors={respSectionR20x30.Details.HasErrors}, Diagnostics={DiagText(respSectionR20x30.Details.Diagnostics)}");
      if (!respSectionR20x30.Details.Success || respSectionR20x30.Details.HasErrors)
      {
        Console.WriteLine("ERROR: CreateSection R20*30 failed. Aborting.");
        client.CloseProject();
        return;
      }
      EID idSectionR20x30 = respSectionR20x30.Data;
      Console.WriteLine("  Section R20*30 OID: " + idSectionR20x30.Value.ToString());

      // Create 6 linear elements forming the portal frame with protruding overhangs.
      // Elements 1 and 11 are the protruding roof overhangs; all carry ClimaticLE2D=true
      // so the climatic engine can auto-detect their protruding-roof character from geometry.

      // Element 1 (C25/30 / R20*30): left protruding roof overhang (2.0,0,4.0) → (4.0,0,4.86)
      {
        ElementLinear el = new ElementLinear
        {          
          Section = idSectionIPE400,
          Material = idMaterialS235,
          GeomPtStart = new Pt3D { X = 2.0, Y = 0.0, Z = 4.0 },
          GeomPtEnd = new Pt3D { X = 4.0, Y = 0.0, Z = 4.86 },
          // Protruding roof overhang: participates in 2D climatic generation;
          // the climatic engine auto-detects the protruding roof type from geometry.
          LoadAreaLoadTransferProperties = new LinearLoadAreaLoadTransfer { ClimaticLE2D = true,
            ClimaticType = Load_Area_Type.CG_LOADAREA_PROTRUDING_ROOF
          }
        };
        var resp = client.CreateElement(el);
        Console.WriteLine($"Create linear element 1 (left protruding overhang): Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
        if (resp.Details.Success && !resp.Details.HasErrors)
          Console.WriteLine("  Element 1 ID: " + resp.Data.Value.ToString());
      }

      // Element 4 (C25/30 / R20*30): left column (4.0,0,0.0) → (4.0,0,4.86)
      {
        ElementLinear el = new ElementLinear
        {
          Section = idSectionIPE400,
          Material = idMaterialS235,
          GeomPtStart = new Pt3D { X = 4.0, Y = 0.0, Z = 0.0 },
          GeomPtEnd = new Pt3D { X = 4.0, Y = 0.0, Z = 4.86 },
          LoadAreaLoadTransferProperties = new LinearLoadAreaLoadTransfer { ClimaticLE2D = true, ClimaticType = Load_Area_Type.CG_LOADAREA_BUILDING }
        };
        var resp = client.CreateElement(el);
        Console.WriteLine($"Create linear element 4 (left column): Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
        if (resp.Details.Success && !resp.Details.HasErrors)
          Console.WriteLine("  Element 4 ID: " + resp.Data.Value.ToString());
      }

      // Element 5 (C25/30 / R20*30): left rafter (4.0,0,4.86) → (9.0,0,7.0)
      {
        ElementLinear el = new ElementLinear
        {
          Section = idSectionIPE400,
          Material = idMaterialS235,
          GeomPtStart = new Pt3D { X = 4.0, Y = 0.0, Z = 4.86 },
          GeomPtEnd = new Pt3D { X = 9.0, Y = 0.0, Z = 7.0 },
          LoadAreaLoadTransferProperties = new LinearLoadAreaLoadTransfer { ClimaticLE2D = true, ClimaticType = Load_Area_Type.CG_LOADAREA_BUILDING }
        };
        var resp = client.CreateElement(el);
        Console.WriteLine($"Create linear element 5 (left rafter): Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
        if (resp.Details.Success && !resp.Details.HasErrors)
          Console.WriteLine("  Element 5 ID: " + resp.Data.Value.ToString());
      }

      // Element 2 (C25/30 / R20*30): right rafter (9.0,0,7.0) → (13.5,0,5.5)
      {
        ElementLinear el = new ElementLinear
        {
          Section = idSectionIPE400,
          Material = idMaterialS235,
          GeomPtStart = new Pt3D { X = 9.0, Y = 0.0, Z = 7.0 },
          GeomPtEnd = new Pt3D { X = 13.5, Y = 0.0, Z = 5.5 },
          LoadAreaLoadTransferProperties = new LinearLoadAreaLoadTransfer { ClimaticLE2D = true, ClimaticType = Load_Area_Type.CG_LOADAREA_BUILDING }
        };
        var resp = client.CreateElement(el);
        Console.WriteLine($"Create linear element 2 (right rafter): Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
        if (resp.Details.Success && !resp.Details.HasErrors)
          Console.WriteLine("  Element 2 ID: " + resp.Data.Value.ToString());
      }

      // Element 3 (C25/30 / R20*30): right column (13.5,0,5.5) → (13.5,0,0.0)
      {
        ElementLinear el = new ElementLinear
        {
          Section = idSectionIPE400,
          Material = idMaterialS235,
          GeomPtStart = new Pt3D { X = 13.5, Y = 0.0, Z = 5.5 },
          GeomPtEnd = new Pt3D { X = 13.5, Y = 0.0, Z = 0.0 },
          LoadAreaLoadTransferProperties = new LinearLoadAreaLoadTransfer { ClimaticLE2D = true, ClimaticType = Load_Area_Type.CG_LOADAREA_BUILDING }
        };
        var resp = client.CreateElement(el);
        Console.WriteLine($"Create linear element 3 (right column): Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
        if (resp.Details.Success && !resp.Details.HasErrors)
          Console.WriteLine("  Element 3 ID: " + resp.Data.Value.ToString());
      }

      // Element 11 (C25/30 / R20*30): right protruding roof overhang (13.5,0,5.5) → (15.0,0,5.0)
      {
        ElementLinear el = new ElementLinear
        {
          Section = idSectionIPE400,
          Material = idMaterialS235,
          GeomPtStart = new Pt3D { X = 13.5, Y = 0.0, Z = 5.5 },
          GeomPtEnd = new Pt3D { X = 15.0, Y = 0.0, Z = 5.0 },
          // Protruding roof overhang: participates in 2D climatic generation;
          // the climatic engine auto-detects the protruding roof type from geometry.
          LoadAreaLoadTransferProperties = new LinearLoadAreaLoadTransfer { ClimaticLE2D = true, ClimaticType = Load_Area_Type.CG_LOADAREA_PROTRUDING_ROOF }
        };
        var resp = client.CreateElement(el);
        Console.WriteLine($"Create linear element 11 (right protruding overhang): Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
        if (resp.Details.Success && !resp.Details.HasErrors)
          Console.WriteLine("  Element 11 ID: " + resp.Data.Value.ToString());
      }

      // Create 2 rigid point supports at column bases
      {
        ElementRigidPunctualSupport support = new ElementRigidPunctualSupport
        {
          GeomPt = new Pt3D { X = 4.0, Y = 0.0, Z = 0.0 },
          Material = idMaterialConcrete
        };
        var resp = client.CreateElement(support);
        Console.WriteLine($"Create rigid support 1: Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
        if (resp.Details.Success && !resp.Details.HasErrors)
          Console.WriteLine("  Support 1 ID: " + resp.Data.Value.ToString());
      }
      {
        ElementRigidPunctualSupport support = new ElementRigidPunctualSupport
        {
          GeomPt = new Pt3D { X = 13.5, Y = 0.0, Z = 0.0 },
          Material = idMaterialConcrete
        };
        var resp = client.CreateElement(support);
        Console.WriteLine($"Create rigid support 2: Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
        if (resp.Details.Success && !resp.Details.HasErrors)
          Console.WriteLine("  Support 2 ID: " + resp.Data.Value.ToString());
      }

      // Create Wind IBC (ASCE 7-22) family — Chapter 28 Envelope Method (low-rise buildings)
      // BuildingLength = 15.0 (X=0 to X=15, including overhangs), PortalPosition = 4.0 (left column)
      {
        LoadCaseFamily_WindIBC windFamily = new LoadCaseFamily_WindIBC
        {
          Name = "Wind IBC Chapter 28 Envelope Method",
          Building2D = new Building2DWindIBC
          {
            BuildingLength = 20.0,
            PortalPosition = 10.0,
            OpeningPosition = PortalOpeningPosition_Wind.CG_WINDEC1_OPENING2D_NONE,
            Wind2DLoadsDeterminationMethod = Wind2DLoadsDeterminationMethod_Wind_EN1991_1_4_API.EWind2DRealLoadsInSpan
          },
          AutoGeneration = new AutoGenerationWindIBC
          {
            WindWallStatus = true,
            PressureCoeff = true,
            SplitWindWalls = false,
            LoadGeneration = true
          },
          BasePressure = new BasePressureWindIBC
          {
            V = 67.0,
            ExposureCategory = ExposureCategory_WindIBC_API.WINDIBCWd2015_EXPOSURE_B,
            RiskCategory = RiskCategory_WindIBC_API.CLIMATIC_IBC2015_RISK_I,
            Kzt = 1.0,
            Kd = 0.85,
            Ke = 1.0,
            DGust = 0.85,
            Ri = 1.0,
            WindDesign = 0, // Chapter 28 Envelope Method for low-rise buildings
            TorsionalLoadCases = false,
            HeightOfStructureBase = 0.0
          }
        };
        var resp = client.CreateInformationalElement(windFamily);
        Console.WriteLine($"Create Wind IBC family (Ch. 28): Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
        if (resp.Details.Success && !resp.Details.HasErrors)
          Console.WriteLine("  Wind IBC family (Ch. 28) ID: " + resp.Data.Value.ToString());
      }     

      // Create Snow IBC (ASCE 7-22) family load case
      {
        LoadCaseFamily_SnowIBC snowFamily = new LoadCaseFamily_SnowIBC
        {
          Name = "Snow IBC family",
          Implantation = new ImplantationSnowASCE722
          {
            Pg = 2395.0,
            Altitude = 0.0,
            W2 = 0.5,
            TerrainCategory = TerrainCategory_SnowASCE722_API.SNOWIBCSn2015_TERRAIN_B,
            Exposure = Exposure_SnowASCE722_API.SNOWIBCSn2015_EXPOSURE_PARTIALLY,
            Ct = ThermalFactorCt_SnowASCE722_API.SNOWIBCSn2015_CT_10,
            RiskCategory = RiskCategory_SnowASCE722_API.CLIMATIC_IBC2015_RISK_I
          },
          Building2D = new Building2DSnowASCE722
          {
            BuildingLength = 15.0,
            PortalPosition = 4.0
          }
        };
        var resp = client.CreateInformationalElement(snowFamily);
        Console.WriteLine($"Create Snow IBC family: Success={resp.Details.Success}, HasErrors={resp.Details.HasErrors}, Diagnostics={DiagText(resp.Details.Diagnostics)}");
        if (resp.Details.Success && !resp.Details.HasErrors)
          Console.WriteLine("  Snow IBC family ID: " + resp.Data.Value.ToString());
      }

      Console.WriteLine("Setup completed (structure modelling), now running climatic auto generation...");
      // Run climatic auto generation
      {
        var resp = client.ProcessAction(AD_API_ActionType.ClimaticAutoGeneration, null);
        PrintResultDetails(resp.Details, logVerbosityLevel, "ProcessAction(AD_API_ActionType.ClimaticAutoGeneration)");
        if (!resp.Details.Success || resp.Details.HasErrors)
        {
          Console.WriteLine("ERROR: ClimaticAutoGeneration failed. Check details above.");
          client.CloseProject();
          return;
        }
      }

      // Read back loads from wind family load cases
      {
        var windFamilyQueries = new List<QueryBase>
        {
          new QueryInfoModel { InformationalElementType = InformationalElementTypeEnum.LoadCaseFamily_Wind }
        };
        var respWindFamilies = client.GetElementsID(windFamilyQueries);
        Console.WriteLine($"GetElementsID (wind families): Success={respWindFamilies.Details.Success}, HasErrors={respWindFamilies.Details.HasErrors}, Diagnostics={DiagText(respWindFamilies.Details.Diagnostics)}");
        if (respWindFamilies.Details.Success && !respWindFamilies.Details.HasErrors && respWindFamilies.Data != null && respWindFamilies.Data.Count > 0)
        {
          Console.WriteLine($"  Wind family load cases: received {respWindFamilies.Data.Count} families");

          foreach (var famId in respWindFamilies.Data)
          {
            EID famEid = new EID { Value = famId };
            var windCaseQueries = new List<QueryBase>
            {
              new QueryInfoLoadCase { InformationalElementType = InformationalElementTypeEnum.LoadCase_Wind, CaseFamilyId = famEid }
            };
            var respWindCases = client.GetElementsID(windCaseQueries);
            Console.WriteLine($"  GetElementsID (wind cases in family {famId}): Success={respWindCases.Details.Success}, HasErrors={respWindCases.Details.HasErrors}, Diagnostics={DiagText(respWindCases.Details.Diagnostics)}");
            if (respWindCases.Details.Success && !respWindCases.Details.HasErrors && respWindCases.Data != null && respWindCases.Data.Count > 0)
            {
              Console.WriteLine($"    Wind family {famId}: {respWindCases.Data.Count} load cases");

              foreach (var caseIdVal in respWindCases.Data)
              {
                EID lcId = new EID { Value = caseIdVal };

                var lcObjects = client.GetInformationalElementsObject(new List<long> { caseIdVal });
                PrintLoadCase(lcObjects.Data?.FirstOrDefault());

                var loadQueries = new List<QueryBase>
                {
                  new QueryElementsLoads { ElementType = ElementTypeEnum.ElementLoadLinear, LoadCaseId = lcId },
                  new QueryElementsLoads { ElementType = ElementTypeEnum.ElementLoadPunctual, LoadCaseId = lcId },
                  new QueryElementsLoads { ElementType = ElementTypeEnum.ElementLoadPlanar, LoadCaseId = lcId }
                };
                var respLoadIds = client.GetElementsID(loadQueries);
                Console.WriteLine($"    GetElementsID (loads in wind case {caseIdVal}): Success={respLoadIds.Details.Success}, HasErrors={respLoadIds.Details.HasErrors}, Diagnostics={DiagText(respLoadIds.Details.Diagnostics)}");
                if (respLoadIds.Details.Success && !respLoadIds.Details.HasErrors && respLoadIds.Data != null && respLoadIds.Data.Count > 0)
                {
                  Console.WriteLine($"      Wind load case {caseIdVal}: {respLoadIds.Data.Count} loads found");

                  var respObjects = client.GetElementsObject(respLoadIds.Data);
                  Console.WriteLine($"      GetElementsObject: Success={respObjects.Details.Success}, HasErrors={respObjects.Details.HasErrors}, Diagnostics={DiagText(respObjects.Details.Diagnostics)}");
                  if (respObjects.Details.Success && !respObjects.Details.HasErrors && respObjects.Data != null)
                  {
                    Console.WriteLine($"      Read {respObjects.Data.Count} load objects");
                    foreach (var obj in respObjects.Data)
                      PrintLoad(obj);
                  }
                }
                else
                {
                  Console.WriteLine($"      Wind load case {caseIdVal}: no loads found");
                }
              }
            }
          }
        }
        else
        {
          Console.WriteLine("  No wind family load cases found");
        }
      }

      // Read back loads from snow family load cases
      {
        var snowFamilyQueries = new List<QueryBase>
        {
          new QueryInfoModel { InformationalElementType = InformationalElementTypeEnum.LoadCaseFamily_Snow }
        };
        var respSnowFamilies = client.GetElementsID(snowFamilyQueries);
        Console.WriteLine($"GetElementsID (snow families): Success={respSnowFamilies.Details.Success}, HasErrors={respSnowFamilies.Details.HasErrors}, Diagnostics={DiagText(respSnowFamilies.Details.Diagnostics)}");
        if (respSnowFamilies.Details.Success && !respSnowFamilies.Details.HasErrors && respSnowFamilies.Data != null && respSnowFamilies.Data.Count > 0)
        {
          Console.WriteLine($"  Snow family load cases: received {respSnowFamilies.Data.Count} families");

          foreach (var famId in respSnowFamilies.Data)
          {
            EID famEid = new EID { Value = famId };
            var snowCaseQueries = new List<QueryBase>
            {
              new QueryInfoLoadCase { InformationalElementType = InformationalElementTypeEnum.LoadCase_Snow, CaseFamilyId = famEid }
            };
            var respSnowCases = client.GetElementsID(snowCaseQueries);
            Console.WriteLine($"  GetElementsID (snow cases in family {famId}): Success={respSnowCases.Details.Success}, HasErrors={respSnowCases.Details.HasErrors}, Diagnostics={DiagText(respSnowCases.Details.Diagnostics)}");
            if (respSnowCases.Details.Success && !respSnowCases.Details.HasErrors && respSnowCases.Data != null && respSnowCases.Data.Count > 0)
            {
              Console.WriteLine($"    Snow family {famId}: {respSnowCases.Data.Count} load cases");

              foreach (var caseIdVal in respSnowCases.Data)
              {
                EID lcId = new EID { Value = caseIdVal };
                var lcObjects = client.GetInformationalElementsObject(new List<long> { caseIdVal });
                PrintLoadCase(lcObjects.Data?.FirstOrDefault());

                var loadQueries = new List<QueryBase>
                {
                  new QueryElementsLoads { ElementType = ElementTypeEnum.ElementLoadLinear, LoadCaseId = lcId },
                  new QueryElementsLoads { ElementType = ElementTypeEnum.ElementLoadPunctual, LoadCaseId = lcId },
                  new QueryElementsLoads { ElementType = ElementTypeEnum.ElementLoadPlanar, LoadCaseId = lcId }
                };
                var respLoadIds = client.GetElementsID(loadQueries);
                Console.WriteLine($"    GetElementsID (loads in snow case {caseIdVal}): Success={respLoadIds.Details.Success}, HasErrors={respLoadIds.Details.HasErrors}, Diagnostics={DiagText(respLoadIds.Details.Diagnostics)}");
                if (respLoadIds.Details.Success && !respLoadIds.Details.HasErrors && respLoadIds.Data != null && respLoadIds.Data.Count > 0)
                {
                  Console.WriteLine($"      Snow load case {caseIdVal}: {respLoadIds.Data.Count} loads found");

                  var respObjects = client.GetElementsObject(respLoadIds.Data);
                  Console.WriteLine($"      GetElementsObject: Success={respObjects.Details.Success}, HasErrors={respObjects.Details.HasErrors}, Diagnostics={DiagText(respObjects.Details.Diagnostics)}");
                  if (respObjects.Details.Success && !respObjects.Details.HasErrors && respObjects.Data != null)
                  {
                    Console.WriteLine($"      Read {respObjects.Data.Count} load objects");
                    foreach (var obj in respObjects.Data)
                      PrintLoad(obj);
                  }
                }
                else
                {
                  Console.WriteLine($"      Snow load case {caseIdVal}: no loads found");
                }
              }
            }
          }
        }
        else
        {
          Console.WriteLine("  No snow family load cases found");
        }
      }

      client.CloseProject();
      Console.WriteLine("Sample_Portal2DwithProtrudingRoofOrOverhangsandClimaticGeneration: Close the project.");
    }

  }
}

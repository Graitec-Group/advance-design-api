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

    static void Sample_With_IntentionalMaterialError(AD.API.Client.AD_Client client, ConsoleColor ConsoleColor = ConsoleColor.White)
    {
      //Console.ForegroundColor = ConsoleColor;

      Environments env = new Environments();
      env.Language = Language_Code.ELanguageEnglish;

      string projectPath = GetEnvironmentVariable("AD_MODELS_PATH") ?? "C:\\ProgramData\\Graitec\\Advance Design\\2027\\Projects\\";
      client.NewProject(projectPath + "SampleWithMaterialError" + Guid.NewGuid().ToString(), env);

      Material materialObj = new Material { Name = "C25/30" };
      EID idMaterial = client.CreateMaterial(materialObj).Data;
      Console.WriteLine("Create material OID: " + idMaterial.Value.ToString());

      ElementRigidPunctualSupport newPctSupport1 = new ElementRigidPunctualSupport
      {
        GeomPt = new Pt3D { X = 0, Y = 0, Z = 0 },
        Material = new EID { Value = 9999 } // non existing material to create an error
      };

      ElementRigidPunctualSupport newPctSupport2 = new ElementRigidPunctualSupport
      {
        GeomPt = new Pt3D { X = 4, Y = 0, Z = 0 },
        Material = new EID { Value = 9999 } // non existing material to create an error
      };
      client.CreateElement(newPctSupport2);


      var retVal = client.LaunchAnalysis();
      if (!retVal.Details.Success)
      {
        // dump the error details
        if (retVal.Details.HasErrors)
        {
          DumpError(retVal.Details);
        }
      }

    }

  }
}

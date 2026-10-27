using AD.API.Client;

namespace Test.API.Client
{
  internal partial class Program
  {
    /// <summary>
    /// 3D steel building structure (France / Eurocode localization)
    /// demonstrating wind (EN 1991-1-4) and snow (EN 1991-1-3) automatic climatic load generation:
    /// - Material: S275 steel
    /// - Cross-sections: IPE400, IPE300, IPE140, CE505, DCED505, IPE100, HEA100, HEA180
    /// - 144 linear elements (columns, rafters, purlins, bracing, secondary members)
    /// - 22 pinned rigid point supports (TX/TY/TZ restrained, RX/RY/RZ free)
    /// - 8 load area elements (building walls, roof surfaces, parapets)
    /// - EN 1991-1-4 wind load case family + EN 1991-1-3 snow load case family
    /// - Automatic climatic load generation (ClimaticAutoGeneration)
    /// </summary>
    static void Sample_WindSnow3DBuildingAutoGeneration(AD.API.Client.AD_Client client)
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

      // --- Create project ---
      var newProj = client.NewProject(projectPath + "WindAndSnowOn3DHall" + Guid.NewGuid().ToString() + ".fto", env);
      PrintResultDetails(newProj.Details, logVerbosityLevel, "Create new project");
      if (!newProj.Details.Success || newProj.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: NewProject failed. Aborting.");
        Console.WriteLine("ERROR: NewProject failed. Aborting.");
        return;
      }

      // --- Material: S275 ---
      var respMat = client.CreateMaterial(new Material { Name = "S275" });
      PrintResultDetails(respMat.Details, logVerbosityLevel, "Create material S275");
      if (!respMat.Details.Success || respMat.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: CreateMaterial S275 failed. Aborting.");
        Console.WriteLine("ERROR: CreateMaterial S275 failed. Aborting.");
        client.CloseProject();
        return;
      }
      EID idMaterialS275 = respMat.Data;
      Console.WriteLine("  Material S275 OID: " + idMaterialS275.Value);

      // --- Cross-sections ---
      var sectionNames = new[]
      {
        "IPE400",
        "IPE300",
        "IPE140",
        "CE505",
        "DCED505",
        "IPE100",
        "HEA100",
        "HEA180"
      };

      var sectionMap = new Dictionary<string, EID>();
      foreach (var sectionName in sectionNames)
      {
        var respSec = client.CreateSection(sectionName);
        PrintResultDetails(respSec.Details, logVerbosityLevel, $"Create section {sectionName}");
        if (!respSec.Details.Success || respSec.Details.HasErrors)
        {
          SampleReport.Fatal($"ERROR: CreateSection {sectionName} failed. Aborting.");
          Console.WriteLine($"ERROR: CreateSection {sectionName} failed. Aborting.");
          client.CloseProject();
          return;
        }
        sectionMap[sectionName] = respSec.Data;
        Console.WriteLine($"  Section {sectionName} OID: {respSec.Data.Value}");
      }

      // --- 144 linear elements: (section, x1,y1,z1, x2,y2,z2) ---
      var elementData = new (string Section, double X1, double Y1, double Z1, double X2, double Y2, double Z2)[]
      {
        // Columns IPE400 (elements 1-28 alternating columns and rafters)
        ("IPE400",   0.00,  0.00, 0.00,   0.00,  0.00, 5.00),  //  1
        ("IPE400",  20.00,  0.00, 0.00,  20.00,  0.00, 5.00),  //  2
        ("IPE300",   0.00,  0.00, 5.00,  10.00,  0.00, 6.00),  //  3
        ("IPE300",  20.00,  0.00, 5.00,  10.00,  0.00, 6.00),  //  4
        ("IPE400",   0.00,  5.00, 0.00,   0.00,  5.00, 5.00),  //  5
        ("IPE400",  20.00,  5.00, 0.00,  20.00,  5.00, 5.00),  //  6
        ("IPE300",   0.00,  5.00, 5.00,  10.00,  5.00, 6.00),  //  7
        ("IPE300",  20.00,  5.00, 5.00,  10.00,  5.00, 6.00),  //  8
        ("IPE400",   0.00, 10.00, 0.00,   0.00, 10.00, 5.00),  //  9
        ("IPE400",  20.00, 10.00, 0.00,  20.00, 10.00, 5.00),  // 10
        ("IPE300",   0.00, 10.00, 5.00,  10.00, 10.00, 6.00),  // 11
        ("IPE300",  20.00, 10.00, 5.00,  10.00, 10.00, 6.00),  // 12
        ("IPE400",   0.00, 15.00, 0.00,   0.00, 15.00, 5.00),  // 13
        ("IPE400",  20.00, 15.00, 0.00,  20.00, 15.00, 5.00),  // 14
        ("IPE300",   0.00, 15.00, 5.00,  10.00, 15.00, 6.00),  // 15
        ("IPE300",  20.00, 15.00, 5.00,  10.00, 15.00, 6.00),  // 16
        ("IPE400",   0.00, 20.00, 0.00,   0.00, 20.00, 5.00),  // 17
        ("IPE400",  20.00, 20.00, 0.00,  20.00, 20.00, 5.00),  // 18
        ("IPE300",   0.00, 20.00, 5.00,  10.00, 20.00, 6.00),  // 19
        ("IPE300",  20.00, 20.00, 5.00,  10.00, 20.00, 6.00),  // 20
        ("IPE400",   0.00, 25.00, 0.00,   0.00, 25.00, 5.00),  // 21
        ("IPE400",  20.00, 25.00, 0.00,  20.00, 25.00, 5.00),  // 22
        ("IPE300",   0.00, 25.00, 5.00,  10.00, 25.00, 6.00),  // 23
        ("IPE300",  20.00, 25.00, 5.00,  10.00, 25.00, 6.00),  // 24
        ("IPE400",   0.00, 30.00, 0.00,   0.00, 30.00, 5.00),  // 25
        ("IPE400",  20.00, 30.00, 0.00,  20.00, 30.00, 5.00),  // 26
        ("IPE300",   0.00, 30.00, 5.00,  10.00, 30.00, 6.00),  // 27
        ("IPE300",  20.00, 30.00, 5.00,  10.00, 30.00, 6.00),  // 28

        // Purlins IPE140 along Y at X=0.00 to X=20.00
        ("IPE140",   0.00,  0.00, 5.00,   0.00,  5.00, 5.00),  // 29
        ("IPE140",   1.99,  0.00, 5.20,   1.99,  5.00, 5.20),  // 30
        ("IPE140",   3.98,  0.00, 5.40,   3.98,  5.00, 5.40),  // 31
        ("IPE140",   5.97,  0.00, 5.60,   5.97,  5.00, 5.60),  // 32
        ("IPE140",   7.96,  0.00, 5.80,   7.96,  5.00, 5.80),  // 33
        ("IPE140",  10.00,  0.00, 6.00,  10.00,  5.00, 6.00),  // 34
        ("IPE140",  14.03,  0.00, 5.60,  14.03,  5.00, 5.60),  // 35
        ("IPE140",  20.00,  0.00, 5.00,  20.00,  5.00, 5.00),  // 36
        ("IPE140",  16.02,  0.00, 5.40,  16.02,  5.00, 5.40),  // 37
        ("IPE140",  18.01,  0.00, 5.20,  18.01,  5.00, 5.20),  // 38
        ("IPE140",  12.04,  0.00, 5.80,  12.04,  5.00, 5.80),  // 39
        ("IPE140",   5.97,  5.00, 5.60,   5.97, 10.00, 5.60),  // 40
        ("IPE140",   0.00,  5.00, 5.00,   0.00, 10.00, 5.00),  // 41
        ("IPE140",   3.98,  5.00, 5.40,   3.98, 10.00, 5.40),  // 42
        ("IPE140",   1.99,  5.00, 5.20,   1.99, 10.00, 5.20),  // 43
        ("IPE140",   7.96,  5.00, 5.80,   7.96, 10.00, 5.80),  // 44
        ("IPE140",  10.00,  5.00, 6.00,  10.00, 10.00, 6.00),  // 45
        ("IPE140",  16.02,  5.00, 5.40,  16.02, 10.00, 5.40),  // 46
        ("IPE140",  20.00,  5.00, 5.00,  20.00, 10.00, 5.00),  // 47
        ("IPE140",  14.03,  5.00, 5.60,  14.03, 10.00, 5.60),  // 48
        ("IPE140",  18.01,  5.00, 5.20,  18.01, 10.00, 5.20),  // 49
        ("IPE140",  12.04,  5.00, 5.80,  12.04, 10.00, 5.80),  // 50
        ("IPE140",   5.97, 10.00, 5.60,   5.97, 15.00, 5.60),  // 51
        ("IPE140",   0.00, 10.00, 5.00,   0.00, 15.00, 5.00),  // 52
        ("IPE140",   3.98, 10.00, 5.40,   3.98, 15.00, 5.40),  // 53
        ("IPE140",   1.99, 10.00, 5.20,   1.99, 15.00, 5.20),  // 54
        ("IPE140",   7.96, 10.00, 5.80,   7.96, 15.00, 5.80),  // 55
        ("IPE140",  10.00, 10.00, 6.00,  10.00, 15.00, 6.00),  // 56
        ("IPE140",  16.02, 10.00, 5.40,  16.02, 15.00, 5.40),  // 57
        ("IPE140",  20.00, 10.00, 5.00,  20.00, 15.00, 5.00),  // 58
        ("IPE140",  14.03, 10.00, 5.60,  14.03, 15.00, 5.60),  // 59
        ("IPE140",  18.01, 10.00, 5.20,  18.01, 15.00, 5.20),  // 60
        ("IPE140",  12.04, 10.00, 5.80,  12.04, 15.00, 5.80),  // 61
        ("IPE140",   5.97, 15.00, 5.60,   5.97, 20.00, 5.60),  // 62
        ("IPE140",   0.00, 15.00, 5.00,   0.00, 20.00, 5.00),  // 63
        ("IPE140",   3.98, 15.00, 5.40,   3.98, 20.00, 5.40),  // 64
        ("IPE140",   1.99, 15.00, 5.20,   1.99, 20.00, 5.20),  // 65
        ("IPE140",   7.96, 15.00, 5.80,   7.96, 20.00, 5.80),  // 66
        ("IPE140",  10.00, 15.00, 6.00,  10.00, 20.00, 6.00),  // 67
        ("IPE140",  16.02, 15.00, 5.40,  16.02, 20.00, 5.40),  // 68
        ("IPE140",  20.00, 15.00, 5.00,  20.00, 20.00, 5.00),  // 69
        ("IPE140",  14.03, 15.00, 5.60,  14.03, 20.00, 5.60),  // 70
        ("IPE140",  18.01, 15.00, 5.20,  18.01, 20.00, 5.20),  // 71
        ("IPE140",  12.04, 15.00, 5.80,  12.04, 20.00, 5.80),  // 72
        ("IPE140",   5.97, 20.00, 5.60,   5.97, 25.00, 5.60),  // 73
        ("IPE140",   0.00, 20.00, 5.00,   0.00, 25.00, 5.00),  // 74
        ("IPE140",   3.98, 20.00, 5.40,   3.98, 25.00, 5.40),  // 75
        ("IPE140",   1.99, 20.00, 5.20,   1.99, 25.00, 5.20),  // 76
        ("IPE140",   7.96, 20.00, 5.80,   7.96, 25.00, 5.80),  // 77
        ("IPE140",  10.00, 20.00, 6.00,  10.00, 25.00, 6.00),  // 78
        ("IPE140",  16.02, 20.00, 5.40,  16.02, 25.00, 5.40),  // 79
        ("IPE140",  20.00, 20.00, 5.00,  20.00, 25.00, 5.00),  // 80
        ("IPE140",  14.03, 20.00, 5.60,  14.03, 25.00, 5.60),  // 81
        ("IPE140",  18.01, 20.00, 5.20,  18.01, 25.00, 5.20),  // 82
        ("IPE140",  12.04, 20.00, 5.80,  12.04, 25.00, 5.80),  // 83
        ("IPE140",   5.97, 25.00, 5.60,   5.97, 30.00, 5.60),  // 84
        ("IPE140",   0.00, 25.00, 5.00,   0.00, 30.00, 5.00),  // 85
        ("IPE140",   3.98, 25.00, 5.40,   3.98, 30.00, 5.40),  // 86
        ("IPE140",   1.99, 25.00, 5.20,   1.99, 30.00, 5.20),  // 87
        ("IPE140",   7.96, 25.00, 5.80,   7.96, 30.00, 5.80),  // 88
        ("IPE140",  10.00, 25.00, 6.00,  10.00, 30.00, 6.00),  // 89
        ("IPE140",  16.02, 25.00, 5.40,  16.02, 30.00, 5.40),  // 90
        ("IPE140",  20.00, 25.00, 5.00,  20.00, 30.00, 5.00),  // 91
        ("IPE140",  14.03, 25.00, 5.60,  14.03, 30.00, 5.60),  // 92
        ("IPE140",  18.01, 25.00, 5.20,  18.01, 30.00, 5.20),  // 93
        ("IPE140",  12.04, 25.00, 5.80,  12.04, 30.00, 5.80),  // 94

        // Bracing CE505 / DCED505 / IPE100 at front bay (Y=0..5)
        ("CE505",   20.00,  0.00, 0.00,  20.00,  5.00, 5.00),  // 95
        ("DCED505", 20.00,  5.00, 0.00,  20.00,  0.00, 5.00),  // 96
        ("CE505",   20.00,  0.00, 5.00,  16.02,  5.00, 5.40),  // 97
        ("CE505",   20.00,  5.00, 5.00,  16.02,  0.00, 5.40),  // 98
        ("CE505",   16.02,  0.00, 5.40,  12.04,  5.00, 5.80),  // 99
        ("CE505",   16.02,  5.00, 5.40,  12.04,  0.00, 5.80),  // 100
        ("CE505",   12.04,  0.00, 5.80,   7.96,  5.00, 5.80),  // 101
        ("CE505",   12.04,  5.00, 5.80,   7.96,  0.00, 5.80),  // 102
        ("CE505",    7.96,  0.00, 5.80,   3.98,  5.00, 5.40),  // 103
        ("CE505",    7.96,  5.00, 5.80,   3.98,  0.00, 5.40),  // 104
        ("CE505",    3.98,  0.00, 5.40,   0.00,  5.00, 5.00),  // 105
        ("CE505",    3.98,  5.00, 5.40,   0.00,  0.00, 5.00),  // 106
        ("CE505",    0.00,  0.00, 5.00,   0.00,  5.00, 0.00),  // 107
        ("IPE100",   0.00,  5.00, 5.00,   0.00,  0.00, 0.00),  // 108

        // Bracing CE505 at rear bay (Y=25..30)
        ("CE505",   20.00, 30.00, 0.00,  20.00, 25.00, 5.00),  // 109
        ("CE505",   20.00, 25.00, 0.00,  20.00, 30.00, 5.00),  // 110
        ("CE505",   20.00, 25.00, 5.00,  16.02, 30.00, 5.40),  // 111
        ("CE505",   20.00, 30.00, 5.00,  16.02, 25.00, 5.40),  // 112
        ("CE505",   16.02, 25.00, 5.40,  12.04, 30.00, 5.80),  // 113
        ("CE505",   16.02, 30.00, 5.40,  12.04, 25.00, 5.80),  // 114
        ("CE505",   12.04, 25.00, 5.80,   7.96, 30.00, 5.80),  // 115
        ("CE505",   12.04, 30.00, 5.80,   7.96, 25.00, 5.80),  // 116
        ("CE505",    7.96, 25.00, 5.80,   3.98, 30.00, 5.40),  // 117
        ("CE505",    7.96, 30.00, 5.80,   3.98, 25.00, 5.40),  // 118
        ("CE505",    3.98, 25.00, 5.40,   0.00, 30.00, 5.00),  // 119
        ("CE505",    3.98, 30.00, 5.40,   0.00, 25.00, 5.00),  // 120
        ("CE505",    0.00, 25.00, 5.00,   0.00, 30.00, 0.00),  // 121
        ("CE505",    0.00, 30.00, 5.00,   0.00, 25.00, 0.00),  // 122
        // Note: identifier 123 is not present in source data

        // Short vertical posts HEA100 (elements 124-137)
        ("HEA100",   0.00,  0.00, 5.00,   0.00,  0.00, 6.00),  // 124
        ("HEA100",   0.00,  5.00, 5.00,   0.00,  5.00, 6.00),  // 125
        ("HEA100",   0.00, 10.00, 5.00,   0.00, 10.00, 6.00),  // 126
        ("HEA100",   0.00, 15.00, 5.00,   0.00, 15.00, 6.00),  // 127
        ("HEA100",   0.00, 20.00, 5.00,   0.00, 20.00, 6.00),  // 128
        ("HEA100",   0.00, 25.00, 5.00,   0.00, 25.00, 6.00),  // 129
        ("HEA100",   0.00, 30.00, 5.00,   0.00, 30.00, 6.00),  // 130
        ("HEA100",  20.00, 30.00, 5.00,  20.00, 30.00, 6.00),  // 131
        ("HEA100",  20.00, 25.00, 5.00,  20.00, 25.00, 6.00),  // 132
        ("HEA100",  20.00, 20.00, 5.00,  20.00, 20.00, 6.00),  // 133
        ("HEA100",  20.00, 15.00, 5.00,  20.00, 15.00, 6.00),  // 134
        ("HEA100",  20.00, 10.00, 5.00,  20.00, 10.00, 6.00),  // 135
        ("HEA100",  20.00,  5.00, 5.00,  20.00,  5.00, 6.00),  // 136
        ("HEA100",  20.00,  0.00, 5.00,  20.00,  0.00, 6.00),  // 137

        // Intermediate columns HEA180 along the front and rear eaves
        ("HEA180",   3.98,  0.00, 0.00,   3.98,  0.00, 5.40),  // 138
        ("HEA180",   7.96,  0.00, 0.00,   7.96,  0.00, 5.80),  // 139
        ("HEA180",  12.04,  0.00, 0.00,  12.04,  0.00, 5.80),  // 140
        ("HEA180",  16.02,  0.00, 0.00,  16.02,  0.00, 5.40),  // 141
        ("HEA180",   7.96, 30.00, 0.00,   7.96, 30.00, 5.80),  // 142
        ("HEA180",   3.98, 30.00, 0.00,   3.98, 30.00, 5.40),  // 143
        ("HEA180",  12.04, 30.00, 0.00,  12.04, 30.00, 5.80),  // 144
        ("HEA180",  16.02, 30.00, 0.00,  16.02, 30.00, 5.40),  // 145
      };

      int elementCount = 0;
      foreach (var (section, x1, y1, z1, x2, y2, z2) in elementData)
      {
        var el = new ElementLinear
        {
          Section     = sectionMap[section],
          Material    = idMaterialS275,
          GeomPtStart = new Pt3D { X = x1, Y = y1, Z = z1 },
          GeomPtEnd   = new Pt3D { X = x2, Y = y2, Z = z2 }
        };
        var resp = client.CreateElement(el);
        elementCount++;
        PrintResultDetails(resp.Details, logVerbosityLevel, $"Create linear element {elementCount} ({section})");
        if (!resp.Details.Success || resp.Details.HasErrors)
        {
          SampleReport.Fatal($"ERROR: CreateElement linear {elementCount} ({section}) failed. Aborting.");
          Console.WriteLine($"ERROR: CreateElement linear {elementCount} ({section}) failed. Aborting.");
          client.CloseProject();
          return;
        }
      }
      Console.WriteLine($"  Created {elementCount} linear elements.");

      // --- 22 pinned rigid point supports (TX/TY/TZ restrained, RX/RY/RZ free) ---
      var supportData = new (double X, double Y, double Z)[]
      {
        (  0.00,  0.00, 0.00),  //  1
        ( 20.00,  0.00, 0.00),  //  2
        ( 20.00,  5.00, 0.00),  //  3
        (  0.00,  5.00, 0.00),  //  4
        ( 20.00, 10.00, 0.00),  //  5
        (  0.00, 10.00, 0.00),  //  6
        ( 20.00, 15.00, 0.00),  //  7
        (  0.00, 15.00, 0.00),  //  8
        ( 20.00, 20.00, 0.00),  //  9
        (  0.00, 20.00, 0.00),  // 10
        ( 20.00, 25.00, 0.00),  // 11
        (  0.00, 25.00, 0.00),  // 12
        ( 20.00, 30.00, 0.00),  // 13
        (  0.00, 30.00, 0.00),  // 14
        (  3.98,  0.00, 0.00),  // 15
        (  7.96,  0.00, 0.00),  // 16
        ( 12.04,  0.00, 0.00),  // 17
        ( 16.02,  0.00, 0.00),  // 18
        (  3.98, 30.00, 0.00),  // 19
        (  7.96, 30.00, 0.00),  // 20
        ( 12.04, 30.00, 0.00),  // 21
        ( 16.02, 30.00, 0.00),  // 22
      };

      int supportCount = 0;
      foreach (var (sx, sy, sz) in supportData)
      {
        var support = new ElementRigidPunctualSupport
        {
          GeomPt     = new Pt3D { X = sx, Y = sy, Z = sz },
          Material   = idMaterialS275,
          ConstraintsType = RigidSupportConstraint.SupportHinge,
          Restraints = new DegreeOfFreedomRestraints
          {
            Tx = true, Ty = true, Tz = true,
            Rx = false, Ry = false, Rz = false
          }
        };
        var resp = client.CreateElement(support);
        supportCount++;
        PrintResultDetails(resp.Details, logVerbosityLevel, $"Create rigid support {supportCount} at ({sx},{sy},{sz})");
        if (!resp.Details.Success || resp.Details.HasErrors)
        {
          SampleReport.Fatal($"ERROR: CreateElement rigid support {supportCount} at ({sx},{sy},{sz}) failed. Aborting.");
          Console.WriteLine($"ERROR: CreateElement rigid support {supportCount} at ({sx},{sy},{sz}) failed. Aborting.");
          client.CloseProject();
          return;
        }
      }
      Console.WriteLine($"  Created {supportCount} rigid point supports.");

      // --- 8 load area elements ---
      // Building load areas (walls and roof surfaces) and parapet areas.
      // Identifier 3, 6 are absent in source data; areas 1,2,4,5,7,8,9,10 are present.
      var loadAreaData = new (Load_Area_Type Type, bool AvailableForWind, bool AvailableForSnow, Pt3D[] Points)[]
      {
        // Area 1: side wall X=0 (wind only, vertical wall)
        (Load_Area_Type.CG_LOADAREA_BUILDING, true, false, new[]
        {
          new Pt3D{X= 0.00, Y=30.00, Z=0.00},
          new Pt3D{X= 0.00, Y=30.00, Z=5.00},
          new Pt3D{X= 0.00, Y= 0.00, Z=5.00},
          new Pt3D{X= 0.00, Y= 0.00, Z=0.00}
        }),
        // Area 2: side wall X=20 (wind only, vertical wall)
        (Load_Area_Type.CG_LOADAREA_BUILDING, true, false, new[]
        {
          new Pt3D{X=20.00, Y=30.00, Z=0.00},
          new Pt3D{X=20.00, Y=30.00, Z=5.00},
          new Pt3D{X=20.00, Y= 0.00, Z=5.00},
          new Pt3D{X=20.00, Y= 0.00, Z=0.00}
        }),
        // Area 4: left roof slope (wind + snow)
        (Load_Area_Type.CG_LOADAREA_BUILDING, true, true, new[]
        {
          new Pt3D{X= 0.00, Y= 0.00, Z=5.00},
          new Pt3D{X=10.00, Y= 0.00, Z=6.00},
          new Pt3D{X=10.00, Y=30.00, Z=6.00},
          new Pt3D{X= 0.00, Y=30.00, Z=5.00}
        }),
        // Area 5: right roof slope (wind + snow)
        (Load_Area_Type.CG_LOADAREA_BUILDING, true, true, new[]
        {
          new Pt3D{X=20.00, Y= 0.00, Z=5.00},
          new Pt3D{X=10.00, Y= 0.00, Z=6.00},
          new Pt3D{X=10.00, Y=30.00, Z=6.00},
          new Pt3D{X=20.00, Y=30.00, Z=5.00}
        }),
        // Area 7: front gable wall Y=0 (wind only, pentagonal)
        (Load_Area_Type.CG_LOADAREA_BUILDING, true, false, new[]
        {
          new Pt3D{X= 0.00, Y=0.00, Z=0.00},
          new Pt3D{X=20.00, Y=0.00, Z=0.00},
          new Pt3D{X=20.00, Y=0.00, Z=5.00},
          new Pt3D{X=10.00, Y=0.00, Z=6.00},
          new Pt3D{X= 0.00, Y=0.00, Z=5.00}
        }),
        // Area 8: rear gable wall Y=30 (wind only, pentagonal)
        (Load_Area_Type.CG_LOADAREA_BUILDING, true, false, new[]
        {
          new Pt3D{X= 0.00, Y=30.00, Z=0.00},
          new Pt3D{X=20.00, Y=30.00, Z=0.00},
          new Pt3D{X=20.00, Y=30.00, Z=5.00},
          new Pt3D{X=10.00, Y=30.00, Z=6.00},
          new Pt3D{X= 0.00, Y=30.00, Z=5.00}
        }),
        // Area 9: parapet X=0 face (wind only)
        (Load_Area_Type.CG_LOADAREA_PARAPET, true, true, new[]
        {
          new Pt3D{X=0.00, Y= 0.00, Z=5.00},
          new Pt3D{X=0.00, Y= 0.00, Z=6.00},
          new Pt3D{X=0.00, Y=30.00, Z=6.00},
          new Pt3D{X=0.00, Y=30.00, Z=5.00}
        }),
        // Area 10: parapet X=20 face (wind only)
        (Load_Area_Type.CG_LOADAREA_PARAPET, true, true, new[]
        {
          new Pt3D{X=20.00, Y=30.00, Z=5.00},
          new Pt3D{X=20.00, Y=30.00, Z=6.00},
          new Pt3D{X=20.00, Y= 0.00, Z=6.00},
          new Pt3D{X=20.00, Y= 0.00, Z=5.00}
        }),
      };

      int loadAreaCount = 0;
      foreach (var (laType, availWind, availSnow, points) in loadAreaData)
      {
        var loadArea = new ElementLoadArea
        {
          GeomPtsList        = new List<Pt3D>(points),
          ClimaticProperties = new LoadAreaClimaticProperties
          {
            ClimaticType                           = laType,
            AvailableForWind                       = availWind,
            AvailableForSnow                       = availSnow,
            Gutter                                 = Gutter_type.CG_GUTTER_EFFECT_NONE,
            ParapetReductionHeightByPurlinsAndRoofCover = 0,
            SnowGuardsPresence                     = false,
            UnobstructedSlippery                   = false,
            OpeningType                            = LoadArea_Opening_type.CG_WINDWALL_OPENING_TYPE_CLOSED_NO_OPENINGS,
            OpeningValue                           = 0,
            SolidityRatio                          = 1
          }
        };
        var respLA = client.CreateElement(loadArea);
        loadAreaCount++;
        PrintResultDetails(respLA.Details, logVerbosityLevel, $"Create load area {loadAreaCount} ({laType})");
        if (!respLA.Details.Success || respLA.Details.HasErrors)
        {
          SampleReport.Fatal($"ERROR: CreateElement load area {loadAreaCount} failed. Aborting.");
          Console.WriteLine($"ERROR: CreateElement load area {loadAreaCount} failed. Aborting.");
          client.CloseProject();
          return;
        }
      }
      Console.WriteLine($"  Created {loadAreaCount} load area elements.");

      // --- EN 1991-1-4 Wind load case family (France) ---
      var windFamily = new LoadCaseFamily_Wind_1991_1_4
      {
        Name       = "Wind EN 1991-1-4 Family",
        AutoGeneration = new AutoGenerationParameters_Wind_EN1991_1_4
        {
          WindWallStatus        = true,
          PressureCoeff         = true,
          SplitWindWalls        = false,
          LoadGeneration        = true,
          AutoCalculationCpeCpi = true,
          UseCTICMCodeUpdates   = true,
          ImposedCpeType        = ImposedCpeType_Wind_EN1991_1_4_API.E_CPE_MODE_default,
          PitchType             = PitchType_Wind_EN1991_1_4_API.PITCH_TYPE_DUO
        },
        BasePressure = new BasePressureParameters_Wind_EN1991_1_4
        {
          Speed                                      = 26.0,   // vb [m/s] — France Zone 2
          Pressure                                   = 0.0,
          CDir                                       = 1.0,
          CSeas                                      = 1.0,
          HeightOfStructureBase                      = 0.0,
          HeightOfStructureBaseMaxForAllComputations = false,
          PlacementType                              = PlacementType_Wind_EN1991_1_4_API.TERRAIN_CATEGORY_II,
          RigLength                                  = 1.0,
          Kr                                         = 1.0,
          CsCdCalcMode                               = CsCdCalcMode_Wind_EN1991_1_4_API.CS_CD_AUTO,
          CsCdVal                                    = 1.0,
          CsCdValMin                                 = 1.0,
          Delta                                      = 0.1,
          N                                          = 0.0,
          NAuto                                      = true,
          PhiScaffolding                             = 0.4,
          PhiScaffoldingAuto                         = true,
          CorrelationCoefficientType                 = CorrelationCoefficientType_Wind_EN1991_1_4_API.CORREL_AUTO,
          CorrelationCoefficientKdc                  = 1.0,
          CalculOfTurbulenceFactorKL                 = CalculOfTurbulenceFactorKL_Wind_EN1991_1_4_API.WINDEC1_EN1991_1_4_KLFORMULA_OTHER,
          TurbulenceFactorKL                         = 1.0
        }
      };

      var respWindFamily = client.CreateInformationalElement(windFamily);
      PrintResultDetails(respWindFamily.Details, logVerbosityLevel, "Create EN 1991-1-4 wind family");
      if (!respWindFamily.Details.Success || respWindFamily.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: CreateInformationalElement wind family failed. Aborting.");
        Console.WriteLine("ERROR: CreateInformationalElement wind family failed. Aborting.");
        client.CloseProject();
        return;
      }
      EID idWindFamily = respWindFamily.Data;
      Console.WriteLine("  Wind family OID: " + idWindFamily.Value);

      // --- EN 1991-1-3 Snow load case family (France / NF EN 1991-1-3) ---
      var snowFamily = new LoadCaseFamily_Snow_1991_1_3
      {
        Name = "Snow EN 1991-1-3 Family",
        ProjectSituation = new ProjectSituationSnowNF_EC1
        {
          SnowExpFall = false,
          SnowExpAcc  = false
        },
        EurocodeParameters = new EurocodeGeneralParametersSnowNF_EC1
        {
          PressureEuGen             = 450.0,   // Characteristic snow load on ground [Pa] — France Zone A1
          ExceptCoefEuGen           = 0.0,
          CoefficientExpEuGen       = ExposureCoefficient_SnowNF_EC1_API.GRCG_SNOWNF_NE_1991_1_3_UNDER_WIND_08,
          CoefficientTermicEuGen    = 1.0,
          AltitudeEuGen             = 0.0,
          PeriodN                   = 50,
          VariationCoeffV           = 0.0,
          AdjustFactorOption        = AdjustFactorOption_SnowNF_EC1_API.E_Option_A_U_auto,
          AdjustFactor              = 1.0,
          ComputingPresure_n_years  = AdjustFactorOption_SnowNF_EC1_API.E_Option_A_U_auto,
          PressureEuGenSn           = 450.0,
          ExceptCoefEuGenSAdn       = 0.0
        },
        GreenhouseParameters = new GreenhouseParametersSnowNF_EC1
        {
          ExpCoeffCe       = 1.0,
          GreenhouseClass  = GreenhouseClass_SnowNF_EC1_API.GREENHOUSE_CLASS_B,
          SurfaceMaterialCm = SurfaceMaterialCm_SnowNF_EC1_API.GREENHOUSE_MATERIAL_CM_1_2_PLUS
        },
        SnowLoadCategory = SnowLoadCategory_API.ESnowZoneUnder1000
      };

      var respSnowFamily = client.CreateInformationalElement(snowFamily);
      PrintResultDetails(respSnowFamily.Details, logVerbosityLevel, "Create EN 1991-1-3 snow family");
      if (!respSnowFamily.Details.Success || respSnowFamily.Details.HasErrors)
      {
        SampleReport.Fatal("ERROR: CreateInformationalElement snow family failed. Aborting.");
        Console.WriteLine("ERROR: CreateInformationalElement snow family failed. Aborting.");
        client.CloseProject();
        return;
      }
      EID idSnowFamily = respSnowFamily.Data;
      Console.WriteLine("  Snow family OID: " + idSnowFamily.Value);

      // --- Auto-generate climatic loads ---
      var respClimatic = client.ProcessAction(AD_API_ActionType.ClimaticAutoGeneration, null);
      PrintResultDetails(respClimatic.Details, logVerbosityLevel, "Auto-generate climatic loads");
      if (!respClimatic.Details.Success || respClimatic.Details.HasErrors)
      {
        Console.WriteLine("WARNING: ClimaticAutoGeneration reported issues.");
      }

      // --- Read back generated wind load cases and verify loads ---
      Console.WriteLine("  Reading back generated wind families and load cases...");
      var windFamilyQueries = new List<QueryBase>
      {
        new QueryInfoModel { InformationalElementType = InformationalElementTypeEnum.LoadCaseFamily_Wind }
      };
      var respWindFamilies = client.GetElementsID(windFamilyQueries);
      PrintResultDetails(respWindFamilies.Details, logVerbosityLevel, "GetElementsID (wind families)");
      if (respWindFamilies.Details.Success && !respWindFamilies.Details.HasErrors
          && respWindFamilies.Data != null && respWindFamilies.Data.Count > 0)
      {
        Console.WriteLine($"  Found {respWindFamilies.Data.Count} wind family(ies).");
        foreach (var famId in respWindFamilies.Data)
        {
          EID famEid = new EID { Value = famId };
          var windCaseQueries = new List<QueryBase>
          {
            new QueryInfoLoadCase { InformationalElementType = InformationalElementTypeEnum.LoadCase_Wind, CaseFamilyId = famEid }
          };
          var respWindCases = client.GetElementsID(windCaseQueries);
          PrintResultDetails(respWindCases.Details, logVerbosityLevel, $"GetElementsID (wind cases in family {famId})");
          if (respWindCases.Details.Success && !respWindCases.Details.HasErrors
              && respWindCases.Data != null && respWindCases.Data.Count > 0)
          {
            Console.WriteLine($"    Wind family {famId}: {respWindCases.Data.Count} generated load case(s).");
            foreach (var caseIdVal in respWindCases.Data)
            {
              EID lcId = new EID { Value = caseIdVal };

              var lcObjects = client.GetInformationalElementsObject(new List<long> { caseIdVal });
              PrintResultDetails(lcObjects.Details, logVerbosityLevel, $"GetInformationalElementsObject (wind case {caseIdVal})");
              PrintLoadCase(lcObjects.Data?.FirstOrDefault());

              var loadQueries = new List<QueryBase>
              {
                new QueryElementsLoads { ElementType = ElementTypeEnum.ElementLoadLinear,   LoadCaseId = lcId },
                new QueryElementsLoads { ElementType = ElementTypeEnum.ElementLoadPunctual, LoadCaseId = lcId },
                new QueryElementsLoads { ElementType = ElementTypeEnum.ElementLoadPlanar,   LoadCaseId = lcId }
              };
              var respLoadIds = client.GetElementsID(loadQueries);
              PrintResultDetails(respLoadIds.Details, logVerbosityLevel, $"GetElementsID (loads in wind case {caseIdVal})");
              if (respLoadIds.Details.Success && !respLoadIds.Details.HasErrors
                  && respLoadIds.Data != null && respLoadIds.Data.Count > 0)
              {
                Console.ForegroundColor = ConsoleColor.Green;
                SampleReport.Pass($"      Wind load case {caseIdVal}: {respLoadIds.Data.Count} load(s) found.");
                Console.WriteLine($"      Wind load case {caseIdVal}: {respLoadIds.Data.Count} load(s) found.");
                Console.ForegroundColor = ConsoleColor.White;
                var respObjects = client.GetElementsObject(respLoadIds.Data);
                PrintResultDetails(respObjects.Details, logVerbosityLevel, $"GetElementsObject (loads in wind case {caseIdVal})");
                if (respObjects.Details.Success && !respObjects.Details.HasErrors && respObjects.Data != null)
                {
                  Console.WriteLine($"      Read {respObjects.Data.Count} load object(s).");
                  foreach (var obj in respObjects.Data)
                    PrintLoad(obj);
                }
              }
              else
              {
                Console.ForegroundColor = ConsoleColor.Red;
                SampleReport.Fail($"      Wind load case {caseIdVal}: no loads found.");
                Console.WriteLine($"      Wind load case {caseIdVal}: no loads found.");
                Console.ForegroundColor = ConsoleColor.White;
              }
            }
          }
          else
          {
            Console.ForegroundColor = ConsoleColor.Red;
            SampleReport.Fail($"    Wind family {famId}: no load cases found after auto-generation.");
            Console.WriteLine($"    Wind family {famId}: no load cases found after auto-generation.");
            Console.ForegroundColor = ConsoleColor.White;
          }
        }
      }
      else
      {
        Console.ForegroundColor = ConsoleColor.Red;
        SampleReport.Fail("  No wind families found after ClimaticAutoGeneration.");
        Console.WriteLine("  No wind families found after ClimaticAutoGeneration.");
        Console.ForegroundColor = ConsoleColor.White;
      }

      // --- Read back generated snow load cases and verify loads ---
      Console.WriteLine("  Reading back generated snow families and load cases...");
      var snowFamilyQueries = new List<QueryBase>
      {
        new QueryInfoModel { InformationalElementType = InformationalElementTypeEnum.LoadCaseFamily_Snow }
      };
      var respSnowFamilies = client.GetElementsID(snowFamilyQueries);
      PrintResultDetails(respSnowFamilies.Details, logVerbosityLevel, "GetElementsID (snow families)");
      if (respSnowFamilies.Details.Success && !respSnowFamilies.Details.HasErrors
          && respSnowFamilies.Data != null && respSnowFamilies.Data.Count > 0)
      {
        Console.WriteLine($"  Found {respSnowFamilies.Data.Count} snow family(ies).");
        foreach (var famId in respSnowFamilies.Data)
        {
          EID famEid = new EID { Value = famId };
          var snowCaseQueries = new List<QueryBase>
          {
            new QueryInfoLoadCase { InformationalElementType = InformationalElementTypeEnum.LoadCase_Snow, CaseFamilyId = famEid }
          };
          var respSnowCases = client.GetElementsID(snowCaseQueries);
          PrintResultDetails(respSnowCases.Details, logVerbosityLevel, $"GetElementsID (snow cases in family {famId})");
          if (respSnowCases.Details.Success && !respSnowCases.Details.HasErrors
              && respSnowCases.Data != null && respSnowCases.Data.Count > 0)
          {
            Console.WriteLine($"    Snow family {famId}: {respSnowCases.Data.Count} generated load case(s).");
            foreach (var caseIdVal in respSnowCases.Data)
            {
              EID lcId = new EID { Value = caseIdVal };

              var lcObjects = client.GetInformationalElementsObject(new List<long> { caseIdVal });
              PrintResultDetails(lcObjects.Details, logVerbosityLevel, $"GetInformationalElementsObject (snow case {caseIdVal})");
              PrintLoadCase(lcObjects.Data?.FirstOrDefault());

              var loadQueries = new List<QueryBase>
              {
                new QueryElementsLoads { ElementType = ElementTypeEnum.ElementLoadLinear,   LoadCaseId = lcId },
                new QueryElementsLoads { ElementType = ElementTypeEnum.ElementLoadPunctual, LoadCaseId = lcId },
                new QueryElementsLoads { ElementType = ElementTypeEnum.ElementLoadPlanar,   LoadCaseId = lcId }
              };
              var respLoadIds = client.GetElementsID(loadQueries);
              PrintResultDetails(respLoadIds.Details, logVerbosityLevel, $"GetElementsID (loads in snow case {caseIdVal})");
              if (respLoadIds.Details.Success && !respLoadIds.Details.HasErrors
                  && respLoadIds.Data != null && respLoadIds.Data.Count > 0)
              {
                Console.ForegroundColor = ConsoleColor.Green;
                SampleReport.Pass($"      Snow load case {caseIdVal}: {respLoadIds.Data.Count} load(s) found.");
                Console.WriteLine($"      Snow load case {caseIdVal}: {respLoadIds.Data.Count} load(s) found.");
                Console.ForegroundColor = ConsoleColor.White;
                var respObjects = client.GetElementsObject(respLoadIds.Data);
                PrintResultDetails(respObjects.Details, logVerbosityLevel, $"GetElementsObject (loads in snow case {caseIdVal})");
                if (respObjects.Details.Success && !respObjects.Details.HasErrors && respObjects.Data != null)
                {
                  Console.WriteLine($"      Read {respObjects.Data.Count} load object(s).");
                  foreach (var obj in respObjects.Data)
                    PrintLoad(obj);
                }
              }
              else
              {
                Console.ForegroundColor = ConsoleColor.Red;
                SampleReport.Fail($"      Snow load case {caseIdVal}: no loads found.");
                Console.WriteLine($"      Snow load case {caseIdVal}: no loads found.");
                Console.ForegroundColor = ConsoleColor.White;
              }
            }
          }
          else
          {
            Console.ForegroundColor = ConsoleColor.Red;
            SampleReport.Fail($"    Snow family {famId}: no load cases found after auto-generation.");
            Console.WriteLine($"    Snow family {famId}: no load cases found after auto-generation.");
            Console.ForegroundColor = ConsoleColor.White;
          }
        }
      }
      else
      {
        Console.ForegroundColor = ConsoleColor.Red;
        SampleReport.Fail("  No snow families found after ClimaticAutoGeneration.");
        Console.WriteLine("  No snow families found after ClimaticAutoGeneration.");
        Console.ForegroundColor = ConsoleColor.White;
      }

      Console.WriteLine("Sample_WindSnow3DBuildingAutoGeneration completed.");
      Console.ForegroundColor = ConsoleColor.White;

      client.CloseProject();
    }
  }
}

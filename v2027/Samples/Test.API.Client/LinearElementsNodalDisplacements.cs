using AD.API.Client;

namespace Test.API.Client
{
  internal partial class Program
  {
    /// <summary>
    /// Opens the Advance Design model (.fto) it is given, runs the FEM analysis and prints, for
    /// every linear element and for each of its mesh nodes, the nodal displacement and rotation
    /// results (D, Dx, Dy, Dz, Rx, Ry, Rz and the resultant rotation R) of every load case of the
    /// model, followed by the same results read on the mesh nodes themselves.
    /// <para>
    /// A displacement answer carries two families: a <c>ResElementLinear</c> per element, whose
    /// nodes are read along the element and in its own frame, and a bare <c>ResNode</c> per
    /// distinct mesh node of the elements asked about, read through the node accessor in the
    /// global frame. Both are printed, the elements first and the nodes after them: the
    /// components are laid out along different axes, while D and R - being norms - match.
    /// </para>
    /// <para>
    /// The sample carries no model of its own, so that it runs on any model: the path comes from
    /// the <c>AD_MODEL_FILE</c> environment variable when it is set - which is what an unattended
    /// run uses - and from the prompt otherwise (a typed path on the console, a Browse dialog in
    /// the sample runner window). Naming no model at all is an error, not a fallback.
    /// </para>
    /// <para>
    /// FE results carry the analysis OID of the element, not the EID the query names, and an
    /// element that produced nothing is dropped from the answer - so neither the returned id nor
    /// the position in the list identifies an element. A request naming a single element can only
    /// be answered about that element: that is how each result is attributed to its EID below, and
    /// the resulting OID -> EID mapping is then reused for the remaining cases.
    /// </para>
    /// <para>
    /// The node coordinates come from the mesh node positions: the Id of a <c>ResNode</c> is the
    /// index of that node in the list returned by <c>GetMeshNodesPosition</c>.
    /// </para>
    /// <para>
    /// The rotations are read like the translations. They come in the frame the post-processing is
    /// configured with, so a model whose linear elements bend in one plane only shows them on a
    /// single axis - and a node where the element carries no rotation reports 0.
    /// </para>
    /// <para>
    /// Buckling load cases are left out: they are read per mode, through <c>GetResultsByQuery</c>
    /// with a <c>LoadCaseAnalysisBuckling</c> - see the generalized buckling sample. A model
    /// holding no load case at all is reported on its combinations instead.
    /// </para>
    /// The model is never modified or saved.
    /// </summary>
    static void Sample_LinearElementsNodalDisplacements(AD.API.Client.AD_Client client)
    {
      LogVerboseLevel logVerbosityLevel = LogVerboseLevel.ErrorsAndWarnings;

      const int separatorWidth = 118;
      string majorRule = new string('=', separatorWidth);
      string minorRule = new string('-', separatorWidth);

      void WriteBanner(string title)
      {
        Console.WriteLine();
        Console.WriteLine(majorRule);
        Console.WriteLine("  " + title);
        Console.WriteLine(majorRule);
      }

      // Keeps a column readable for both the usual millimetre-sized results and the very small
      // ones, which a plain fixed-point format would show as a row of zeros.
      static string Fmt(double value)
      {
        string text = value != 0.0 && Math.Abs(value) < 1.0e-4
          ? value.ToString("0.000000E+00")
          : value.ToString("0.000000");
        return text.PadLeft(13);
      }

      // The model to open. No default: an empty answer is an error, so that the sample never
      // silently works on a model the caller did not ask for.
      string ResolveModelFile()
      {
        string answer = PromptForModelFile(GetEnvironmentVariable("AD_MODEL_FILE") ?? string.Empty);
        answer = Environment.ExpandEnvironmentVariables(answer.Trim().Trim('"'));

        if (answer.Length == 0)
        {
          SampleReport.Fatal("ERROR: No model file given.");
          Console.WriteLine("ERROR: No model file given. Set the AD_MODEL_FILE environment variable to the");
          Console.WriteLine("       .fto model to open, or type its path when asked for it.");
          return string.Empty;
        }

        string fullPath;
        try
        {
          fullPath = Path.GetFullPath(answer);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
          SampleReport.Fatal($"ERROR: '{answer}' is not a usable file path.");
          Console.WriteLine($"ERROR: '{answer}' is not a usable file path: {ex.Message}");
          return string.Empty;
        }

        if (Directory.Exists(fullPath))
        {
          SampleReport.Fatal($"ERROR: '{fullPath}' is a folder, not a model file.");
          Console.WriteLine($"ERROR: '{fullPath}' is a folder, not a model file.");
          return string.Empty;
        }

        if (!File.Exists(fullPath))
        {
          SampleReport.Fatal($"ERROR: Model file not found: {fullPath}");
          Console.WriteLine($"ERROR: Model file not found: {fullPath}");
          return string.Empty;
        }

        if (!string.Equals(Path.GetExtension(fullPath), ".fto", StringComparison.OrdinalIgnoreCase))
        {
          Console.ForegroundColor = ConsoleColor.Yellow;
          Console.WriteLine($"  WARNING: '{fullPath}' is not a .fto file; opening it may fail.");
          Console.ForegroundColor = ConsoleColor.White;
        }

        return fullPath;
      }

      WriteBanner("Linear elements - nodal displacements and rotations");
      Console.WriteLine("  Reported : D, Dx, Dy, Dz [m] and Rx, Ry, Rz, R [rad] on every node of every linear element");
      Console.WriteLine("  Model    : the one given through AD_MODEL_FILE or through the prompt (no default)");
      Console.WriteLine();

      string modelFile = ResolveModelFile();
      if (modelFile.Length == 0)
        return;

      Console.WriteLine($"  Model: {modelFile}");
      Console.WriteLine();

      Environments env = new Environments
      {
        Localization = Localization_Code.LOCALIZATION_EUROPE,
        LogVerbosity = logVerbosityLevel
      };

      Console.WriteLine("[1/5] Opening project ...");
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
        // -----------------------------------------------------------------
        // [2/5] FEM analysis - without it there is no result to read
        // -----------------------------------------------------------------
        Console.WriteLine("[2/5] Running the FEM analysis ...");
        var respCalc = client.LaunchAnalysis();
        PrintResultDetails(respCalc.Details, logVerbosityLevel, "LaunchAnalysis");
        if (!respCalc.Details.Success || respCalc.Details.HasErrors || !respCalc.Data)
        {
          SampleReport.Fatal("ERROR: LaunchAnalysis failed. Aborting sample.");
          Console.WriteLine("ERROR: LaunchAnalysis failed. Aborting sample.");
          return;
        }

        // -----------------------------------------------------------------
        // [3/5] Linear elements of the model
        // -----------------------------------------------------------------
        Console.WriteLine("[3/5] Collecting the linear elements ...");
        var respLinearElementIds = client.GetElementsID(new List<QueryBase>
        {
          new QueryElementsModel { ElementType = ElementTypeEnum.ElementLinear }
        });
        PrintResultDetails(respLinearElementIds.Details, logVerbosityLevel, "GetElementsID (linear elements)");

        var linearElementIds = respLinearElementIds.Details.Success && !respLinearElementIds.Details.HasErrors
          ? respLinearElementIds.Data?.Distinct().OrderBy(id => id).ToList() ?? new List<long>()
          : new List<long>();

        if (linearElementIds.Count == 0)
        {
          // Not an error: a model made of planar elements only is a perfectly valid model, it just
          // has nothing this sample reports on.
          Console.WriteLine("  This model holds no linear element - nothing to report.");
          return;
        }
        Console.WriteLine($"  Linear elements found: {linearElementIds.Count}");

        // Mesh node positions, used to show where each result node is. The Id of a ResNode is the
        // index of the node in this list.
        var respMeshNodes = client.GetMeshNodesPosition();
        PrintResultDetails(respMeshNodes.Details, logVerbosityLevel, "GetMeshNodesPosition");
        var meshNodes = respMeshNodes.Details.Success && !respMeshNodes.Details.HasErrors
          ? respMeshNodes.Data?.ToList() ?? new List<Pt3D>()
          : new List<Pt3D>();
        Console.WriteLine($"  Mesh nodes found     : {meshNodes.Count}");

        Pt3D? MeshNodePosition(long? nodeId) =>
          nodeId.HasValue && nodeId.Value >= 0 && nodeId.Value < meshNodes.Count
            ? meshNodes[(int)nodeId.Value]
            : null;

        // -----------------------------------------------------------------
        // [4/5] Analysis cases the results are asked for
        // -----------------------------------------------------------------
        Console.WriteLine("[4/5] Collecting the analysis cases ...");

        List<long> QueryInfoIds(InformationalElementTypeEnum infoType, out bool answered)
        {
          var resp = client.GetElementsID(new List<QueryBase>
          {
            new QueryInfoModel { InformationalElementType = infoType }
          });
          PrintResultDetails(resp.Details, logVerbosityLevel, $"GetElementsID ({infoType})");
          answered = resp.Details.Success && !resp.Details.HasErrors;
          return answered
            ? resp.Data?.Distinct().OrderBy(id => id).ToList() ?? new List<long>()
            : new List<long>();
        }

        var analysisCaseIds = QueryInfoIds(InformationalElementTypeEnum.LoadCase, out bool loadCasesListed);
        string analysisCaseKind = "Load case";
        if (!loadCasesListed)
        {
          SampleReport.Fatal("ERROR: The load cases could not be listed. Aborting sample.");
          Console.WriteLine("ERROR: The load cases could not be listed. Aborting sample.");
          return;
        }

        // A buckling case answers per mode, so its results are asked for through
        // GetResultsByQuery / LoadCaseAnalysisBuckling - see the generalized buckling sample.
        // Asking for them by EID alone names no mode, so they are left out here. The query above
        // returns them along with the others, so when this one fails they cannot be told apart:
        // the sample then stops rather than ask for results it knows it must not ask for.
        var bucklingCaseIds = new HashSet<long>(QueryInfoIds(InformationalElementTypeEnum.LoadCase_Buckling, out bool bucklingCasesListed));
        if (!bucklingCasesListed)
        {
          SampleReport.Fatal("ERROR: The buckling load cases could not be listed. Aborting sample.");
          Console.WriteLine("ERROR: The buckling load cases could not be listed, so they cannot be told apart from");
          Console.WriteLine("       the other load cases. Aborting sample.");
          return;
        }

        int skippedBucklingCases = analysisCaseIds.RemoveAll(bucklingCaseIds.Contains);
        if (skippedBucklingCases > 0)
          Console.WriteLine($"  Buckling load cases skipped: {skippedBucklingCases} " +
                            "(they are read per mode - see the generalized buckling sample).");

        if (analysisCaseIds.Count == 0)
        {
          Console.WriteLine(skippedBucklingCases > 0
            ? "  The only load cases of this model are buckling ones, falling back to the combinations."
            : "  No load case in the model, falling back to the combinations.");
          analysisCaseIds = QueryInfoIds(InformationalElementTypeEnum.Combination, out _);
          analysisCaseKind = "Combination";
        }

        if (analysisCaseIds.Count == 0)
        {
          Console.WriteLine("  This model holds neither a load case nor a combination - no result to read.");
          return;
        }
        Console.WriteLine($"  {analysisCaseKind}s found: {analysisCaseIds.Count}");

        // -----------------------------------------------------------------
        // [5/5] Displacements and rotations, per case / element / node
        // -----------------------------------------------------------------
        Console.WriteLine("[5/5] Reading the nodal displacements ...");

        // The displacement results of one case for the elements named. The answer carries two
        // families: a ResElementLinear per element, and the bare ResNode results the engine reader
        // adds for the mesh nodes of those elements (CSessionImpl::GetNodesDispResults). Both are
        // reported below. Diagnostics are printed only when the call failed, so that the single
        // element requests stay quiet.
        (List<ResElementLinear> Elements, List<ResNode> Nodes) GetDisplacementResults(long caseId, List<long> elementIds)
        {
          var resp = client.GetResults(ResultType.Displacement, caseId, elementIds);
          if (!resp.Details.Success || resp.Details.HasErrors)
          {
            PrintResultDetails(resp.Details, logVerbosityLevel,
              $"GetResults Displacement (case EID={caseId}, {elementIds.Count} element(s))");
            return (new List<ResElementLinear>(), new List<ResNode>());
          }

          return (resp.Data?.OfType<ResElementLinear>().ToList() ?? new List<ResElementLinear>(),
                  resp.Data?.OfType<ResNode>().ToList() ?? new List<ResNode>());
        }

        // Analysis OID -> EID of the linear element that answered under it. Built by the single
        // element requests below and valid for the rest of the session.
        var elementIdByAnalysisOid = new Dictionary<long, long>();
        var identifiedElementIds = new HashSet<long>();

        // An element that answers nothing in a case stays unidentified and is probed again in the
        // next one, which on a model where many elements never answer would cost one round trip
        // per element and per case. The probing is therefore bounded, as it is in the engine-side
        // caller of the same technique.
        int probeBudget = linearElementIds.Count * 2 + 8;
        bool probeBudgetReported = false;

        int reportedCases = 0;
        int reportedElements = 0;
        int reportedNodes = 0;
        int reportedStandaloneNodes = 0;
        bool anyRotationRead = false;

        // One table of nodal results, printed the same way for the nodes of an element and for the
        // bare ResNode results of the case. Returns how many rows carried a result.
        int PrintNodeTable(IReadOnlyList<ResNode> nodes, string indent)
        {
          string header = string.Format("{0,8} {1,10} {2,10} {3,10} {4,13} {5,13} {6,13} {7,13} {8,13} {9,13} {10,13} {11,13}",
            "Node", "X [m]", "Y [m]", "Z [m]",
            "D [m]", "Dx [m]", "Dy [m]", "Dz [m]", "Rx [rad]", "Ry [rad]", "Rz [rad]", "R [rad]");
          Console.WriteLine(indent + header);
          Console.WriteLine(indent + new string('-', header.Length));

          int printed = 0;
          foreach (var node in nodes)
          {
            string nodeId = node.Id?.Value.ToString() ?? "n/a";
            var position = MeshNodePosition(node.Id?.Value);
            string x = (position != null ? position.X.ToString("0.0000") : "n/a").PadLeft(10);
            string y = (position != null ? position.Y.ToString("0.0000") : "n/a").PadLeft(10);
            string z = (position != null ? position.Z.ToString("0.0000") : "n/a").PadLeft(10);

            var displacements = node.ResDisplacements;
            if (displacements == null)
            {
              Console.WriteLine($"{indent}{nodeId,8} {x} {y} {z}   (no displacement result on this node)");
              continue;
            }

            printed++;
            anyRotationRead |= displacements.Rx != 0.0 || displacements.Ry != 0.0 || displacements.Rz != 0.0;
            Console.WriteLine($"{indent}{nodeId,8} {x} {y} {z} " +
              $"{Fmt(displacements.D)} {Fmt(displacements.Dx)} {Fmt(displacements.Dy)} {Fmt(displacements.Dz)} " +
              $"{Fmt(displacements.Rx)} {Fmt(displacements.Ry)} {Fmt(displacements.Rz)} {Fmt(displacements.R)}");
          }

          return printed;
        }

        foreach (long caseId in analysisCaseIds)
        {
          var respCaseObj = client.GetInformationalElementsObject(new List<long> { caseId });
          PrintResultDetails(respCaseObj.Details, logVerbosityLevel, $"GetInformationalElementsObject (case {caseId})");
          var caseObj = respCaseObj.Data?.FirstOrDefault();

          string caseName = caseObj switch
          {
            LoadCase loadCase when !string.IsNullOrWhiteSpace(loadCase.Name) => loadCase.Name,
            null => "(unknown)",
            _ => string.IsNullOrWhiteSpace(caseObj.UserName) ? "(no name)" : caseObj.UserName
          };
          int caseUserId = caseObj?.UserID ?? 0;

          Console.WriteLine();
          Console.WriteLine(minorRule);
          Console.WriteLine($"  {analysisCaseKind} EID={caseId}  -  UserID={caseUserId}  -  Name={caseName}");
          Console.WriteLine(minorRule);

          var resultByElementId = new Dictionary<long, ResElementLinear>();
          var nodeResultById = new SortedDictionary<long, ResNode>();
          var probedNow = new HashSet<long>();

          // Keeps the standalone node results of this case, deduplicated: a node shared by two
          // elements comes back from more than one of the requests below.
          void CollectNodes(IEnumerable<ResNode> nodes)
          {
            foreach (var node in nodes)
            {
              if (node.Id != null)
                nodeResultById[node.Id.Value] = node;
            }
          }

          // An element whose analysis OID is not known yet is asked for on its own: such an answer
          // cannot belong to any other element. An element that produces nothing in this case stays
          // unidentified and is probed again on the next one.
          foreach (long elementId in linearElementIds)
          {
            if (identifiedElementIds.Contains(elementId))
              continue;

            if (probeBudget <= 0)
            {
              if (!probeBudgetReported)
              {
                probeBudgetReported = true;
                Console.WriteLine("    NOTE: some elements returned no result in the cases read first and can no longer be");
                Console.WriteLine("          identified, so they are left out. Ask for them on their own if they matter.");
              }
              break;
            }

            probeBudget--;
            probedNow.Add(elementId);
            var probe = GetDisplacementResults(caseId, new List<long> { elementId });
            CollectNodes(probe.Nodes);

            var own = probe.Elements.FirstOrDefault();
            if (own?.Id == null)
              continue;

            elementIdByAnalysisOid[own.Id.Value] = elementId;
            identifiedElementIds.Add(elementId);
            resultByElementId[elementId] = own;
          }

          // Everything already identified is asked for in a single call and mapped back by its OID.
          var bulkIds = linearElementIds
            .Where(id => identifiedElementIds.Contains(id) && !probedNow.Contains(id))
            .ToList();
          if (bulkIds.Count > 0)
          {
            var bulk = GetDisplacementResults(caseId, bulkIds);
            CollectNodes(bulk.Nodes);

            foreach (var result in bulk.Elements)
            {
              if (result.Id != null && elementIdByAnalysisOid.TryGetValue(result.Id.Value, out long elementId))
                resultByElementId[elementId] = result;
            }
          }

          if (resultByElementId.Count == 0)
          {
            Console.WriteLine("    (no linear element result for this case)");
          }
          else
          {
            reportedCases++;

            foreach (long elementId in linearElementIds)
            {
              Console.WriteLine();

              if (!resultByElementId.TryGetValue(elementId, out var elementResult))
              {
                Console.WriteLine($"    Linear element EID={elementId}  -  (no result in this case)");
                continue;
              }

              var nodes = elementResult.ResNodes?
                .OrderBy(n => n.Id?.Value ?? long.MaxValue)
                .ToList() ?? new List<ResNode>();

              Console.WriteLine($"    Linear element EID={elementId}  -  analysis OID={elementResult.Id?.Value.ToString() ?? "n/a"}" +
                                $"  -  {nodes.Count} node(s)");

              if (nodes.Count == 0)
              {
                Console.WriteLine("      (no node result)");
                continue;
              }

              reportedElements++;
              reportedNodes += PrintNodeTable(nodes, "      ");
            }
          }

          // The same answer also carries the mesh nodes of those elements as results of their own
          // (CSessionImpl::GetNodesDispResults): one ResNode per distinct node, read through the
          // node accessor rather than along the element. They are reported here, after the
          // elements, deduplicated because the per element and the bulk requests overlap on the
          // nodes the elements share.
          var caseNodes = nodeResultById.Values.ToList();
          Console.WriteLine();
          Console.WriteLine($"    Mesh nodes of the linear elements (ResNode results)  -  {caseNodes.Count} node(s)");
          Console.WriteLine("    These are read on the node itself, in the global frame, while the values above follow");
          Console.WriteLine("    the element's own frame - so the components are laid out differently while D and R,");
          Console.WriteLine("    being norms, match node for node.");

          if (caseNodes.Count == 0)
            Console.WriteLine("      (no node result for this case)");
          else
            reportedStandaloneNodes += PrintNodeTable(caseNodes, "      ");
        }

        Console.WriteLine();
        Console.WriteLine(majorRule);
        if (reportedNodes == 0 && reportedStandaloneNodes == 0)
        {
          Console.WriteLine("  Done. No nodal displacement was returned for this model.");
        }
        else
        {
          Console.WriteLine($"  Done. {reportedNodes} node result(s) on {reportedElements} linear element result(s), " +
                            $"plus {reportedStandaloneNodes} mesh node result(s), " +
                            $"over {reportedCases} {analysisCaseKind.ToLowerInvariant()}(s).");
          if (!anyRotationRead)
          {
            Console.WriteLine("  Note. Every rotation read was 0. That is expected when the structure does not twist");
            Console.WriteLine("        the elements about any axis; check the model before reading anything into it.");
          }
        }
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

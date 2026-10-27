using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace Test.API.Client;

/// <summary>
/// Registers an assembly-resolution fallback at module-load time (before Main)
/// so that AD.API.Client.dll and all its transitive dependencies can be
/// found in the AD server bin folder even when the application runs from a
/// different directory.
/// </summary>
internal static class AssemblyResolver
{
  internal static string ServerBinPath { get; private set; } = string.Empty;

  [ModuleInitializer]
  internal static void Initialize()
  {
    ServerBinPath = GetEnvironmentVariable("AD_API_SERVER_BINARY_PATH")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                        "Graitec", "Advance Design", "2027", "Bin") + Path.DirectorySeparatorChar;

    AssemblyLoadContext.Default.Resolving += (context, assemblyName) =>
    {
      string candidatePath = Path.Combine(ServerBinPath, assemblyName.Name + ".dll");
      if (File.Exists(candidatePath))
        return context.LoadFromAssemblyPath(candidatePath);
      return null;
    };
  }

  private static string? GetEnvironmentVariable(string name) =>
      Environment.GetEnvironmentVariable(name) ??
      Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User) ??
      Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine);
}

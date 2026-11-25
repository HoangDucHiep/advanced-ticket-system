using System.Xml.Linq;

namespace TicketSystem.ArchitectureTests;

/// <summary>A project under src/, read directly from its .csproj file.</summary>
public sealed record ProjectInfo(
    string Name,
    IReadOnlySet<string> ProjectReferences,
    IReadOnlySet<string> PackageReferences,
    IReadOnlySet<string> FrameworkReferences);

/// <summary>
/// Reads the solution structure from disk by inspecting project files rather than built
/// assemblies, because the compiler omits unused references from assemblies. The project
/// files represent the intended dependencies.
/// </summary>
public static class Solution
{
    public const string SharedAbstractions = "TicketSystem.Shared.Abstractions";
    public const string SharedInfrastructure = "TicketSystem.Shared.Infrastructure";

    public static readonly string[] Modules =
        ["Scheduling", "Inventory", "Booking", "Payment", "Ticketing", "IdentityAccess"];

    public static string Root { get; } = FindRoot();
    public static IReadOnlyDictionary<string, ProjectInfo> Projects { get; } = LoadProjects();

    public static string Internal(string module) => $"TicketSystem.{module}";
    public static string Contracts(string module) => $"TicketSystem.{module}.Contracts";

    private static string FindRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "TicketSystem.sln")))
                return dir.FullName;
        }

        throw new InvalidOperationException("Could not find TicketSystem.sln from the test execution directory.");
    }

    private static Dictionary<string, ProjectInfo> LoadProjects() =>
        Directory.EnumerateFiles(Path.Combine(Root, "src"), "*.csproj", SearchOption.AllDirectories)
            .Select(Load)
            .ToDictionary(p => p.Name);

    private static ProjectInfo Load(string path)
    {
        var xml = XDocument.Load(path);

        HashSet<string> Includes(string element) =>
            xml.Descendants(element)
                .Select(e => (string?)e.Attribute("Include"))
                .OfType<string>()
                .ToHashSet();

        // Windows .csproj files use '\' as a path separator; Linux (CI) does not.
        var projectReferences = Includes("ProjectReference")
            .Select(include => Path.GetFileNameWithoutExtension(include.Replace('\\', '/')))
            .ToHashSet();

        return new ProjectInfo(
            Path.GetFileNameWithoutExtension(path),
            projectReferences,
            Includes("PackageReference"),
            Includes("FrameworkReference"));
    }
}
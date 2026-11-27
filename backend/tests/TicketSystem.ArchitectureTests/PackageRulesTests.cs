using Shouldly;

namespace TicketSystem.ArchitectureTests;

/// <summary>ADR-0017 / ADR-0018: use cases are called through their handler interface, never through a mediator.</summary>
public sealed class PackageRulesTests
{
    [Fact]
    public void No_mediator_dependency()
    {
        Solution.Projects.Values
            .Where(project => project.PackageReferences.Any(p => p.StartsWith("MediatR", StringComparison.OrdinalIgnoreCase)))
            .Select(project => project.Name)
            .ShouldBeEmpty();

        File.ReadAllText(Path.Combine(Solution.Root, "Directory.Packages.props"))
            .ShouldNotContain("MediatR", Case.Insensitive);
    }
}

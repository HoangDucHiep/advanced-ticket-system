using Shouldly;

namespace TicketSystem.ArchitectureTests;

/// <summary>Dependency rules 1–3 from D9 in docs/design/architecture.md.</summary>
public sealed class DependencyRulesTests
{
    public static TheoryData<string> Modules => new(Solution.Modules);

    private static readonly string[] ForbiddenInContracts =
        ["Microsoft.EntityFrameworkCore", "Dapper", "Npgsql", "Microsoft.AspNetCore"];

    // Allowed module-to-module edges from D9. Every module may also reference IdentityAccess (R8).
    private static readonly HashSet<(string Source, string Target)> AllowedEdges =
    [
        ("Inventory", "Scheduling"),
        ("Booking", "Scheduling"),
        ("Booking", "Inventory"),
        ("Booking", "Payment"),
        ("Ticketing", "Booking"),
        ("Ticketing", "Scheduling"),
    ];

    [Theory]
    [MemberData(nameof(Modules))]
    public void Every_module_has_the_two_projects_required_by_D2(string module)
    {
        Solution.Projects.ShouldContainKey(Solution.Internal(module));
        Solution.Projects.ShouldContainKey(Solution.Contracts(module));
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Rule1_Internal_projects_reference_only_Contracts_and_Shared_projects(string module)
    {
        var allowed = Solution.Modules.Select(Solution.Contracts)
            .Append(Solution.SharedAbstractions)
            .Append(Solution.SharedInfrastructure)
            .ToHashSet();

        Solution.Projects[Solution.Internal(module)].ProjectReferences
            .Where(reference => !allowed.Contains(reference))
            .ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Rule2_Contracts_reference_only_Shared_Abstractions(string module)
    {
        var contracts = Solution.Projects[Solution.Contracts(module)];

        contracts.ProjectReferences.Where(r => r != Solution.SharedAbstractions).ShouldBeEmpty();
        contracts.PackageReferences.Where(IsForbiddenInContracts).ShouldBeEmpty();
        contracts.FrameworkReferences.ShouldNotContain("Microsoft.AspNetCore.App");
    }

    [Fact]
    public void Rule2_Shared_Abstractions_has_no_infrastructure_dependencies()
    {
        // Contracts reference Shared.Abstractions, so they also inherit its dependencies.
        var abstractions = Solution.Projects[Solution.SharedAbstractions];

        abstractions.ProjectReferences.ShouldBeEmpty();
        abstractions.PackageReferences.Where(IsForbiddenInContracts).ShouldBeEmpty();
        abstractions.FrameworkReferences.ShouldNotContain("Microsoft.AspNetCore.App");
    }

    [Fact]
    public void Rule3_Only_module_edges_allowed_by_D9_exist()
    {
        ModuleEdges()
            .Where(edge => edge.Target != "IdentityAccess" && !AllowedEdges.Contains(edge))
            .Select(edge => $"{edge.Source} -> {edge.Target}")
            .ShouldBeEmpty();
    }

    [Fact]
    public void Rule3_The_module_graph_is_acyclic()
    {
        var edges = ModuleEdges().ToLookup(edge => edge.Source, edge => edge.Target);
        var visiting = new HashSet<string>();
        var done = new HashSet<string>();

        bool HasCycleFrom(string module)
        {
            if (done.Contains(module)) return false;
            if (!visiting.Add(module)) return true; // Revisiting a module on the current path means there is a cycle.

            var cycle = edges[module].Any(HasCycleFrom);
            visiting.Remove(module);
            done.Add(module);
            return cycle;
        }

        Solution.Modules.Any(HasCycleFrom).ShouldBeFalse();
    }

    private static bool IsForbiddenInContracts(string package) =>
        ForbiddenInContracts.Any(prefix => package.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<(string Source, string Target)> ModuleEdges() =>
        from source in Solution.Modules
        from reference in Solution.Projects[Solution.Internal(source)].ProjectReferences
        from target in Solution.Modules
        where target != source && reference == Solution.Contracts(target)
        select (source, target);
}
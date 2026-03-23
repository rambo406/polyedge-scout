using PolyEdgeScout.Architecture.Tests.Helpers;

namespace PolyEdgeScout.Architecture.Tests;

public sealed class EntityImmutabilityTests
{
    private static readonly IReadOnlyList<Type> DomainEntities =
        AssemblyHelper.GetPublicTypesInNamespace(
            AssemblyHelper.GetDomainAssembly(),
            "PolyEdgeScout.Domain.Entities");

    [Fact]
    public void DomainEntities_ShouldExist()
    {
        Assert.NotEmpty(DomainEntities);
    }

    [Fact]
    public void AllDomainEntities_ShouldBeSealed()
    {
        var nonSealed = DomainEntities
            .Where(t => t.IsClass && !t.IsSealed)
            .Select(t => t.Name)
            .ToList();

        Assert.True(nonSealed.Count == 0,
            $"The following domain entities are not sealed: {string.Join(", ", nonSealed)}");
    }

    [Fact]
    public void AllDomainEntities_ShouldBeRecords()
    {
        // Market is intentionally a mutable class (external API mapping)
        var allowedNonRecords = new HashSet<string> { "Market" };

        var nonRecords = DomainEntities
            .Where(t => t.IsClass && !AssemblyHelper.IsRecord(t) && !allowedNonRecords.Contains(t.Name))
            .Select(t => t.Name)
            .ToList();

        Assert.True(nonRecords.Count == 0,
            $"The following domain entities are not records: {string.Join(", ", nonRecords)}");
    }
}

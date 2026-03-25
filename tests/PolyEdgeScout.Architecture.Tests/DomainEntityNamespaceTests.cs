using PolyEdgeScout.Architecture.Tests.Helpers;

namespace PolyEdgeScout.Architecture.Tests;

public sealed class DomainEntityNamespaceTests
{
    [Theory]
    [InlineData("Trade")]
    [InlineData("TradeResult")]
    [InlineData("AuditLogEntry")]
    [InlineData("Market")]
    [InlineData("PnlSnapshot")]
    [InlineData("AppStateEntry")]
    public void KnownEntities_ShouldExistInEntitiesNamespace(string entityName)
    {
        var domainAssembly = AssemblyHelper.GetDomainAssembly();
        var type = domainAssembly.GetType($"PolyEdgeScout.Domain.Entities.{entityName}");
        Assert.NotNull(type);
    }

    [Fact]
    public void AllSealedTypes_InDomain_ShouldBeInEntitiesOrValueObjectsNamespace()
    {
        var domainAssembly = AssemblyHelper.GetDomainAssembly();
        var sealedTypes = domainAssembly.GetTypes()
            .Where(t => t.IsPublic && t.IsSealed && !t.IsEnum && !t.IsAbstract)
            .ToList();

        var allowedNamespaces = new HashSet<string>
        {
            "PolyEdgeScout.Domain.Entities",
            "PolyEdgeScout.Domain.ValueObjects",
            "PolyEdgeScout.Domain.Services",
        };

        var misplaced = sealedTypes
            .Where(t => t.Namespace is not null && !allowedNamespaces.Contains(t.Namespace))
            .Select(t => $"{t.FullName}")
            .ToList();

        Assert.True(misplaced.Count == 0,
            $"Sealed types outside of Entities/ValueObjects: {string.Join(", ", misplaced)}");
    }
}

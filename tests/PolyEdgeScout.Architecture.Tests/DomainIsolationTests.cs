using PolyEdgeScout.Architecture.Tests.Helpers;

namespace PolyEdgeScout.Architecture.Tests;

public sealed class DomainIsolationTests
{
    [Fact]
    public void Domain_ShouldHaveNoPolyEdgeScoutDependencies()
    {
        var refs = AssemblyHelper.GetPolyEdgeReferencedAssemblyNames(AssemblyHelper.GetDomainAssembly());
        Assert.True(refs.Count == 0,
            $"Domain should have no PolyEdgeScout dependencies but has: {string.Join(", ", refs)}");
    }

    [Fact]
    public void Domain_ShouldNotReferenceEntityFramework()
    {
        var allRefs = AssemblyHelper.GetDomainAssembly().GetReferencedAssemblies()
            .Where(a => a.Name != null && a.Name.StartsWith("Microsoft.EntityFrameworkCore"))
            .Select(a => a.Name!)
            .ToList();

        Assert.True(allRefs.Count == 0,
            $"Domain should not reference EF Core but references: {string.Join(", ", allRefs)}");
    }
}

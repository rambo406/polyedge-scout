using PolyEdgeScout.Architecture.Tests.Helpers;

namespace PolyEdgeScout.Architecture.Tests;

public sealed class InterfacePlacementTests
{
    [Fact]
    public void AllInterfaces_InApplication_ShouldBeInInterfacesNamespace()
    {
        var appAssembly = AssemblyHelper.GetApplicationAssembly();
        var interfaces = appAssembly.GetTypes()
            .Where(t => t.IsInterface && t.IsPublic)
            .ToList();

        var misplaced = interfaces
            .Where(i => i.Namespace != "PolyEdgeScout.Application.Interfaces")
            .Select(i => $"{i.FullName}")
            .ToList();

        Assert.True(misplaced.Count == 0,
            $"The following Application interfaces are not in the Interfaces namespace: {string.Join(", ", misplaced)}");
    }

    [Fact]
    public void AllInterfaces_InDomain_ShouldBeInInterfacesNamespace()
    {
        var domainAssembly = AssemblyHelper.GetDomainAssembly();
        var interfaces = domainAssembly.GetTypes()
            .Where(t => t.IsInterface && t.IsPublic)
            .ToList();

        var misplaced = interfaces
            .Where(i => i.Namespace != "PolyEdgeScout.Domain.Interfaces")
            .Select(i => $"{i.FullName}")
            .ToList();

        Assert.True(misplaced.Count == 0,
            $"The following Domain interfaces are not in the Interfaces namespace: {string.Join(", ", misplaced)}");
    }
}

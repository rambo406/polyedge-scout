using PolyEdgeScout.Architecture.Tests.Helpers;

namespace PolyEdgeScout.Architecture.Tests;

public sealed class NamingConventionTests
{
    [Fact]
    public void RepositoryInterfaces_ShouldStartWithI()
    {
        var appAssembly = AssemblyHelper.GetApplicationAssembly();
        var repoInterfaces = appAssembly.GetTypes()
            .Where(t => t.IsInterface && t.IsPublic && t.Name.EndsWith("Repository"))
            .ToList();

        Assert.NotEmpty(repoInterfaces);

        var invalid = repoInterfaces.Where(t => !t.Name.StartsWith("I")).Select(t => t.Name).ToList();
        Assert.True(invalid.Count == 0,
            $"Repository interfaces not starting with 'I': {string.Join(", ", invalid)}");
    }

    [Fact]
    public void ServiceInterfaces_ShouldStartWithI()
    {
        var appAssembly = AssemblyHelper.GetApplicationAssembly();
        var serviceInterfaces = appAssembly.GetTypes()
            .Where(t => t.IsInterface && t.IsPublic && t.Name.EndsWith("Service"))
            .ToList();

        Assert.NotEmpty(serviceInterfaces);

        var invalid = serviceInterfaces.Where(t => !t.Name.StartsWith("I")).Select(t => t.Name).ToList();
        Assert.True(invalid.Count == 0,
            $"Service interfaces not starting with 'I': {string.Join(", ", invalid)}");
    }

    [Fact]
    public void DomainEnums_ShouldBeInEnumsNamespace()
    {
        var domainAssembly = AssemblyHelper.GetDomainAssembly();
        var enums = domainAssembly.GetTypes()
            .Where(t => t.IsEnum && t.IsPublic)
            .ToList();

        Assert.NotEmpty(enums);

        var misplaced = enums
            .Where(e => e.Namespace != "PolyEdgeScout.Domain.Enums")
            .Select(e => $"{e.FullName}")
            .ToList();

        Assert.True(misplaced.Count == 0,
            $"Domain enums not in Enums namespace: {string.Join(", ", misplaced)}");
    }
}

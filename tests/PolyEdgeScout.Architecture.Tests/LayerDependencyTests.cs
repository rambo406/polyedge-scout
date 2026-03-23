using PolyEdgeScout.Architecture.Tests.Helpers;

namespace PolyEdgeScout.Architecture.Tests;

public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_ShouldNotReference_Application()
    {
        var refs = AssemblyHelper.GetPolyEdgeReferencedAssemblyNames(AssemblyHelper.GetDomainAssembly());
        Assert.DoesNotContain("PolyEdgeScout.Application", refs);
    }

    [Fact]
    public void Domain_ShouldNotReference_Infrastructure()
    {
        var refs = AssemblyHelper.GetPolyEdgeReferencedAssemblyNames(AssemblyHelper.GetDomainAssembly());
        Assert.DoesNotContain("PolyEdgeScout.Infrastructure", refs);
    }

    [Fact]
    public void Domain_ShouldNotReference_Console()
    {
        var refs = AssemblyHelper.GetPolyEdgeReferencedAssemblyNames(AssemblyHelper.GetDomainAssembly());
        Assert.DoesNotContain("PolyEdgeScout.Console", refs);
    }

    [Fact]
    public void Application_ShouldReference_Domain()
    {
        var refs = AssemblyHelper.GetPolyEdgeReferencedAssemblyNames(AssemblyHelper.GetApplicationAssembly());
        Assert.Contains("PolyEdgeScout.Domain", refs);
    }

    [Fact]
    public void Application_ShouldNotReference_Infrastructure()
    {
        var refs = AssemblyHelper.GetPolyEdgeReferencedAssemblyNames(AssemblyHelper.GetApplicationAssembly());
        Assert.DoesNotContain("PolyEdgeScout.Infrastructure", refs);
    }

    [Fact]
    public void Application_ShouldNotReference_Console()
    {
        var refs = AssemblyHelper.GetPolyEdgeReferencedAssemblyNames(AssemblyHelper.GetApplicationAssembly());
        Assert.DoesNotContain("PolyEdgeScout.Console", refs);
    }

    [Fact]
    public void Infrastructure_ShouldReference_Domain()
    {
        var refs = AssemblyHelper.GetPolyEdgeReferencedAssemblyNames(AssemblyHelper.GetInfrastructureAssembly());
        Assert.Contains("PolyEdgeScout.Domain", refs);
    }

    [Fact]
    public void Infrastructure_ShouldReference_Application()
    {
        var refs = AssemblyHelper.GetPolyEdgeReferencedAssemblyNames(AssemblyHelper.GetInfrastructureAssembly());
        Assert.Contains("PolyEdgeScout.Application", refs);
    }

    [Fact]
    public void Infrastructure_ShouldNotReference_Console()
    {
        var refs = AssemblyHelper.GetPolyEdgeReferencedAssemblyNames(AssemblyHelper.GetInfrastructureAssembly());
        Assert.DoesNotContain("PolyEdgeScout.Console", refs);
    }
}

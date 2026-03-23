using System.Reflection;
using PolyEdgeScout.Application.Interfaces;

namespace PolyEdgeScout.Architecture.Tests;

public sealed class AuditLogConstraintTests
{
    private static readonly MethodInfo[] InterfaceMethods =
        typeof(IAuditLogRepository).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

    [Fact]
    public void IAuditLogRepository_ShouldNotHaveUpdateMethods()
    {
        var updateMethods = InterfaceMethods.Where(m => m.Name.Contains("Update", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.True(updateMethods.Count == 0,
            $"IAuditLogRepository should be append-only but has update methods: {string.Join(", ", updateMethods.Select(m => m.Name))}");
    }

    [Fact]
    public void IAuditLogRepository_ShouldNotHaveDeleteMethods()
    {
        var deleteMethods = InterfaceMethods.Where(m => m.Name.Contains("Delete", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.True(deleteMethods.Count == 0,
            $"IAuditLogRepository should be append-only but has delete methods: {string.Join(", ", deleteMethods.Select(m => m.Name))}");
    }

    [Fact]
    public void IAuditLogRepository_ShouldNotHaveRemoveMethods()
    {
        var removeMethods = InterfaceMethods.Where(m => m.Name.Contains("Remove", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.True(removeMethods.Count == 0,
            $"IAuditLogRepository should be append-only but has remove methods: {string.Join(", ", removeMethods.Select(m => m.Name))}");
    }

    [Fact]
    public void IAuditLogRepository_MethodsShouldOnlyBeAddOrGet()
    {
        var unexpectedMethods = InterfaceMethods
            .Where(m => !m.Name.StartsWith("Add") && !m.Name.StartsWith("Get"))
            .Select(m => m.Name)
            .ToList();

        Assert.True(unexpectedMethods.Count == 0,
            $"IAuditLogRepository has unexpected methods (only Add* and Get* allowed): {string.Join(", ", unexpectedMethods)}");
    }
}

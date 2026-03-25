using System.Reflection;
using PolyEdgeScout.Domain.Entities;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Infrastructure.Persistence;
using PolyEdgeScout.Console.App;

namespace PolyEdgeScout.Architecture.Tests.Helpers;

internal static class AssemblyHelper
{
    public static Assembly GetDomainAssembly() => typeof(Trade).Assembly;
    public static Assembly GetApplicationAssembly() => typeof(IOrderService).Assembly;
    public static Assembly GetInfrastructureAssembly() => typeof(TradingDbContext).Assembly;
    public static Assembly GetConsoleAssembly() => typeof(ViewNavigator).Assembly;

    public static IReadOnlyList<string> GetPolyEdgeReferencedAssemblyNames(Assembly assembly)
    {
        return assembly.GetReferencedAssemblies()
            .Where(a => a.Name != null && a.Name.StartsWith("PolyEdgeScout", StringComparison.Ordinal))
            .Select(a => a.Name!)
            .ToList();
    }

    public static bool IsRecord(Type type)
    {
        // Records have a compiler-generated EqualityContract property
        return type.GetProperty("EqualityContract",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly) is not null;
    }

    public static IReadOnlyList<Type> GetPublicTypesInNamespace(Assembly assembly, string @namespace)
    {
        return assembly.GetTypes()
            .Where(t => t.IsPublic && t.Namespace == @namespace)
            .ToList();
    }
}

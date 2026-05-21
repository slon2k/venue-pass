using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace VenuePass.ArchitectureTests;

public sealed class ModuleBoundaryTests
{
    private static readonly (string AssemblyName, string RootNamespace)[] Modules =
    [
        ("VenuePass.Modules.Events", "VenuePass.Modules.Events"),
        ("VenuePass.Modules.Ticketing", "VenuePass.Modules.Ticketing"),
        ("VenuePass.Modules.Attendance", "VenuePass.Modules.Attendance"),
        ("VenuePass.Modules.Identity", "VenuePass.Modules.Identity")
    ];

    [Fact]
    public void Modules_should_not_depend_on_other_modules()
    {
        foreach ((string assemblyName, string rootNamespace) in Modules)
        {
            string[] otherModuleNamespaces = Modules
                .Where(m => m.RootNamespace != rootNamespace)
                .Select(m => m.RootNamespace)
                .ToArray();

            var result = Types.InAssembly(LoadAssembly(assemblyName))
                .That()
                .ResideInNamespace(rootNamespace)
                .ShouldNot()
                .HaveDependencyOnAny(otherModuleNamespaces)
                .GetResult();

            Assert.True(
                result.IsSuccessful,
                $"Assembly '{assemblyName}' must not depend on other modules.");
        }
    }

    [Fact]
    public void Domain_layer_should_not_depend_on_infrastructure_or_features()
    {
        foreach ((string assemblyName, string rootNamespace) in Modules)
        {
            var result = Types.InAssembly(LoadAssembly(assemblyName))
                .That()
                .ResideInNamespace($"{rootNamespace}.Domain")
                .ShouldNot()
                .HaveDependencyOnAny(
                    $"{rootNamespace}.Infrastructure",
                    $"{rootNamespace}.Features",
                    "Microsoft.EntityFrameworkCore")
                .GetResult();

            Assert.True(
                result.IsSuccessful,
                $"Domain layer in '{assemblyName}' must be infrastructure-free.");
        }
    }

    private static Assembly LoadAssembly(string assemblyName)
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(assembly => assembly.GetName().Name == assemblyName)
            ?? Assembly.Load(assemblyName);
    }
}

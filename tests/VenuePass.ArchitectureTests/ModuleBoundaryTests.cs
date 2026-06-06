using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace VenuePass.ArchitectureTests;

public sealed class ModuleBoundaryTests
{
    private static readonly ModuleDescriptor[] Modules =
    [
        new("VenuePass.Modules.Events", "VenuePass.Modules.Events", "VenuePass.Modules.Events.Contracts"),
        new("VenuePass.Modules.Ticketing", "VenuePass.Modules.Ticketing", "VenuePass.Modules.Ticketing.Contracts"),
        new("VenuePass.Modules.Attendance", "VenuePass.Modules.Attendance"),
        new("VenuePass.Modules.Identity", "VenuePass.Modules.Identity")
    ];

    [Fact]
    public void Modules_should_not_depend_on_other_modules()
    {
        foreach (ModuleDescriptor module in Modules)
        {
            string[] otherModuleAssemblies = Modules
                .Where(m => m.AssemblyName != module.AssemblyName)
                .Select(m => m.AssemblyName)
                .ToArray();

            Assembly assembly = LoadAssembly(module.AssemblyName);
            string[] referencedAssemblies = assembly
                .GetReferencedAssemblies()
                .Select(name => name.Name)
                .OfType<string>()
                .ToArray();

            string[] invalidReferences = referencedAssemblies
                .Intersect(otherModuleAssemblies, StringComparer.Ordinal)
                .ToArray();

            Assert.True(
                invalidReferences.Length == 0,
                $"Assembly '{module.AssemblyName}' must not depend on other module implementations. Found: {string.Join(", ", invalidReferences)}");
        }
    }

    [Fact]
    public void Contracts_should_not_depend_on_module_implementations()
    {
        string[] moduleAssemblies = Modules
            .Select(m => m.AssemblyName)
            .ToArray();

        foreach (ModuleDescriptor module in Modules.Where(m => m.ContractAssemblyName is not null))
        {
            Assembly contractAssembly = LoadAssembly(module.ContractAssemblyName!);
            string[] referencedAssemblies = contractAssembly
                .GetReferencedAssemblies()
                .Select(name => name.Name)
                .OfType<string>()
                .ToArray();

            string[] invalidReferences = referencedAssemblies
                .Intersect(moduleAssemblies, StringComparer.Ordinal)
                .ToArray();

            Assert.True(
                invalidReferences.Length == 0,
                $"Contract assembly '{module.ContractAssemblyName}' must not depend on module implementations. Found: {string.Join(", ", invalidReferences)}");
        }
    }

    [Fact]
    public void Domain_layer_should_not_depend_on_infrastructure_or_features()
    {
        foreach (ModuleDescriptor module in Modules)
        {
            var result = Types.InAssembly(LoadAssembly(module.AssemblyName))
                .That()
                .ResideInNamespace($"{module.RootNamespace}.Domain")
                .ShouldNot()
                .HaveDependencyOnAny(
                    $"{module.RootNamespace}.Infrastructure",
                    $"{module.RootNamespace}.Features",
                    "Microsoft.EntityFrameworkCore")
                .GetResult();

            Assert.True(
                result.IsSuccessful,
                $"Domain layer in '{module.AssemblyName}' must be infrastructure-free.");
        }
    }

    private sealed record ModuleDescriptor(
        string AssemblyName,
        string RootNamespace,
        string? ContractAssemblyName = null);

    private static Assembly LoadAssembly(string assemblyName)
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(assembly => assembly.GetName().Name == assemblyName)
            ?? Assembly.Load(assemblyName);
    }
}

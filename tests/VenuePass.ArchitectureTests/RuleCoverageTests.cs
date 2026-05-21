using NetArchTest.Rules;
using Xunit;

namespace VenuePass.ArchitectureTests
{
    public sealed class RuleCoverageTests
    {
        [Fact]
        public void Module_rule_positive_case_should_pass()
        {
            var result = Types.InAssembly(typeof(RuleCoverage.Positive.ModuleA.Features.PositiveModuleAFeature).Assembly)
                .That()
                .ResideInNamespace("VenuePass.ArchitectureTests.RuleCoverage.Positive.ModuleA")
                .ShouldNot()
                .HaveDependencyOnAny("VenuePass.ArchitectureTests.RuleCoverage.Positive.ModuleB")
                .GetResult();

            Assert.True(result.IsSuccessful);
        }

        [Fact]
        public void Module_rule_negative_case_should_fail()
        {
            var result = Types.InAssembly(typeof(RuleCoverage.Negative.ModuleA.Features.NegativeModuleAFeature).Assembly)
                .That()
                .ResideInNamespace("VenuePass.ArchitectureTests.RuleCoverage.Negative.ModuleA")
                .ShouldNot()
                .HaveDependencyOnAny("VenuePass.ArchitectureTests.RuleCoverage.Negative.ModuleB")
                .GetResult();

            Assert.False(result.IsSuccessful);
        }

        [Fact]
        public void Domain_rule_positive_case_should_pass()
        {
            var result = Types.InAssembly(typeof(RuleCoverage.Positive.Layering.Domain.PositiveDomainType).Assembly)
                .That()
                .ResideInNamespace("VenuePass.ArchitectureTests.RuleCoverage.Positive.Layering.Domain")
                .ShouldNot()
                .HaveDependencyOnAny("VenuePass.ArchitectureTests.RuleCoverage.Positive.Layering.Infrastructure")
                .GetResult();

            Assert.True(result.IsSuccessful);
        }

        [Fact]
        public void Domain_rule_negative_case_should_fail()
        {
            var result = Types.InAssembly(typeof(RuleCoverage.Negative.Layering.Domain.NegativeDomainType).Assembly)
                .That()
                .ResideInNamespace("VenuePass.ArchitectureTests.RuleCoverage.Negative.Layering.Domain")
                .ShouldNot()
                .HaveDependencyOnAny("VenuePass.ArchitectureTests.RuleCoverage.Negative.Layering.Infrastructure")
                .GetResult();

            Assert.False(result.IsSuccessful);
        }
    }
}

namespace VenuePass.ArchitectureTests.RuleCoverage.Positive.ModuleA.Features
{
    public sealed class PositiveModuleAFeature;
}

namespace VenuePass.ArchitectureTests.RuleCoverage.Positive.ModuleB.Infrastructure
{
    public sealed class PositiveModuleBInfrastructure;
}

namespace VenuePass.ArchitectureTests.RuleCoverage.Negative.ModuleB.Infrastructure
{
    public sealed class NegativeModuleBInfrastructure;
}

namespace VenuePass.ArchitectureTests.RuleCoverage.Negative.ModuleA.Features
{
    using VenuePass.ArchitectureTests.RuleCoverage.Negative.ModuleB.Infrastructure;

    public sealed class NegativeModuleAFeature
    {
        private readonly NegativeModuleBInfrastructure _dependency = new();
    }
}

namespace VenuePass.ArchitectureTests.RuleCoverage.Positive.Layering.Domain
{
    public sealed class PositiveDomainType;
}

namespace VenuePass.ArchitectureTests.RuleCoverage.Positive.Layering.Infrastructure
{
    public sealed class PositiveInfrastructureType;
}

namespace VenuePass.ArchitectureTests.RuleCoverage.Negative.Layering.Infrastructure
{
    public sealed class NegativeInfrastructureType;
}

namespace VenuePass.ArchitectureTests.RuleCoverage.Negative.Layering.Domain
{
    using VenuePass.ArchitectureTests.RuleCoverage.Negative.Layering.Infrastructure;

    public sealed class NegativeDomainType
    {
        private readonly NegativeInfrastructureType _dependency = new();
    }
}

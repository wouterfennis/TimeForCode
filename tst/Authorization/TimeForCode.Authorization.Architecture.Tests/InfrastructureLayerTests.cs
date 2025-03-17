using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace TimeForCode.Authorization.Architecture.Tests
{
    [TestClass]
    public class InfrastructureLayerTests : ArchitectureTestBase
    {
        [TestMethod]
        public void Infrastructure_WithReferencesToApiLayer_AreNotAllowed()
        {
            var rule = Types().That().Are(InfrastructureLayer).Should()
                .NotDependOnAny(ApiLayer).Because("Infastructure layer should not have references to api layer");

            Evaluate(rule);
        }

        [TestMethod]
        public void Repositories_ThatExistInInfrastructureLayer_AreAllowed()
        {
            var rule = Classes().That().HaveNameEndingWith("Repository")
                .Should().Be(InfrastructureLayer);

            Evaluate(rule);
        }
    }
}
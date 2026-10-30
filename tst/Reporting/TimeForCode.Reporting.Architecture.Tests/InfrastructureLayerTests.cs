using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace TimeForCode.Reporting.Architecture.Tests
{
    [TestClass]
    public class InfrastructureLayerTests : ArchitectureTestBase
    {
        [TestMethod]
        public void Infrastructure_WithReferencesToApiLayer_AreNotAllowed()
        {
            var rule = Types().That().Are(InfrastructureLayer).Should()
                .NotDependOnAny(ApiLayer).Because("Infrastructure layer should not have references to api layer");

            Evaluate(rule);
        }

        [TestMethod]
        public void DataSources_ThatExistInInfrastructureLayer_AreAllowed()
        {
            var rule = Classes().That().HaveNameEndingWith("DataSource")
                .Should().Be(InfrastructureLayer);

            Evaluate(rule);
        }
    }
}
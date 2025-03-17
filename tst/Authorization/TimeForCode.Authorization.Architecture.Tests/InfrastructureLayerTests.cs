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
    }
}

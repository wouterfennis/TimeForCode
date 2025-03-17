using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace TimeForCode.Authorization.Architecture.Tests
{
    [TestClass]
    public class DomainLayerTests : ArchitectureTestBase
    {
        [TestMethod]
        public void DomainLayer_WithReferencesToApiLayer_AreNotAllowed()
        {
            var rule = Types().That().Are(DomainLayer).Should()
                .NotDependOnAny(ApiLayer).Because("Domain layer should not have references to other layers");

            Evaluate(rule);
        }

        [TestMethod]
        public void DomainLayer_WithReferencesToApplicationLayer_AreNotAllowed()
        {
            var rule = Types().That().Are(DomainLayer).Should()
                .NotDependOnAny(ApplicationLayer).Because("Domain layer should not have references to other layers");

            Evaluate(rule);
        }

        [TestMethod]
        public void DomainLayer_WithReferencesToInfrastructureLayer_AreNotAllowed()
        {
            var rule = Types().That().Are(DomainLayer).Should()
                .NotDependOnAny(InfrastructureLayer).Because("Domain layer should not have references to other layers");

            Evaluate(rule);
        }
    }
}

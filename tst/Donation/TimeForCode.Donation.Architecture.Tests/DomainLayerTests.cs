using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace TimeForCode.Donation.Architecture.Tests
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

        [TestMethod]
        public void DomainLayer_WithReferencesToMongoDbDriver_AreNotAllowed()
        {
            var rule = Types().That().Are(DomainLayer).Should()
                .NotDependOnAnyTypesThat().ResideInNamespaceMatching(@"^MongoDB\.Driver(\..*)?$")
                .Because("Domain may use MongoDB.Bson types and attributes only, see ADR-016");

            Evaluate(rule);
        }

        [TestMethod]
        public void DomainLayer_WithReferencesToWebFrameworkOrMediator_AreNotAllowed()
        {
            var rule = Types().That().Are(DomainLayer).Should()
                .NotDependOnAnyTypesThat().ResideInNamespaceMatching(@"^(Microsoft\.AspNetCore|MediatR)(\..*)?$")
                .Because("Domain must not know the web framework or the mediator");

            Evaluate(rule);
        }
    }
}
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace TimeForCode.Donation.Architecture.Tests
{
    [TestClass]
    public class ApplicationLayerTests : ArchitectureTestBase
    {
        [TestMethod]
        public void ApplicationLayer_WithReferencesToApiLayer_AreNotAllowed()
        {
            var rule = Types().That().Are(ApplicationLayer).Should()
                .NotDependOnAny(ApiLayer).Because("Application layer should not have references to api layer");

            Evaluate(rule);
        }

        [TestMethod]
        public void ApplicationLayer_WithReferencesToInfrastructureLayer_AreNotAllowed()
        {
            var rule = Types().That().Are(ApplicationLayer).Should()
                .NotDependOnAny(InfrastructureLayer).Because("Application layer should not have references to infrastructure layer");

            Evaluate(rule);
        }

        [TestMethod]
        public void CommandHandlers_ThatAreInApplicationLayer_AreAllowed()
        {
            var rule = Types().That().HaveNameEndingWith("Handler").Should()
                .ResideInNamespaceMatching(@".*\.Donation\.Application\..*")
                .Because("Command Handlers should always be in the application layer");

            Evaluate(rule);
        }
    }
}
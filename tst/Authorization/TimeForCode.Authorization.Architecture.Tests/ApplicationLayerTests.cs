using MediatR;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace TimeForCode.Authorization.Architecture.Tests
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
            // Arrange
            var rule = Types().That().ImplementInterface(typeof(IRequestHandler<>)).Should()
               .ResideInNamespace(".*\\.Authorization\\.Application\\..*", useRegularExpressions: true)
               .Because("Command Handlers should always be in the application layer");

            // Assert
            Evaluate(rule);
        }
    }
}
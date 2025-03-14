using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using MediatR;
//add a using directive to ArchUnitNET.Fluent.ArchRuleDefinition to easily define ArchRules
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace TimeForCode.Authorization.Architecture.Tests
{
    [TestClass]
    public class ApplicationLayerTests
    {
        private ArchUnitNET.Domain.Architecture Architecture = default!;

        [TestInitialize]
        public void Initialize()
        {
            Architecture = new ArchLoader().LoadAssemblies(
                System.Reflection.Assembly.Load(typeof(TimeForCode.Authorization.Application.Anchor).Assembly.GetName().Name!)
            ).Build();
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

        private void Evaluate(IArchRule exampleLayerShouldNotAccessForbiddenLayer)
        {
            var result = exampleLayerShouldNotAccessForbiddenLayer.Evaluate(Architecture);

            foreach (var violation in result.Where(x => !x.Passed))
            {
                Console.WriteLine(violation.Description);
            }

            Assert.IsTrue(exampleLayerShouldNotAccessForbiddenLayer.HasNoViolations(Architecture));
        }
    }
}
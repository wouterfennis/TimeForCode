using ArchUnitNET.Loader;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace TimeForCode.Donation.Architecture.Tests
{
    [TestClass]
    public class CrossContextIsolationTests
    {
        private ArchUnitNET.Domain.Architecture Architecture = default!;

        [TestInitialize]
        public void Initialize()
        {
            var loader = new ArchLoader();

            // Load both Authorization and Donation contexts
            Architecture = loader.LoadAssemblies(
                System.Reflection.Assembly.Load(typeof(Authorization.Api.IAnchor).Assembly.GetName().Name!),
                System.Reflection.Assembly.Load(typeof(Authorization.Application.IAnchor).Assembly.GetName().Name!),
                System.Reflection.Assembly.Load(typeof(Authorization.Domain.IAnchor).Assembly.GetName().Name!),
                System.Reflection.Assembly.Load(typeof(Authorization.Infrastructure.IAnchor).Assembly.GetName().Name!),
                System.Reflection.Assembly.Load(typeof(Api.IAnchor).Assembly.GetName().Name!),
                System.Reflection.Assembly.Load(typeof(Application.IAnchor).Assembly.GetName().Name!),
                System.Reflection.Assembly.Load(typeof(Domain.IAnchor).Assembly.GetName().Name!),
                System.Reflection.Assembly.Load(typeof(Infrastructure.IAnchor).Assembly.GetName().Name!)
            ).Build();
        }

        [TestMethod]
        public void AuthorizationContext_ShouldNotDependOnDonationContext()
        {
            var rule = Types().That().ResideInNamespaceMatching(@"^TimeForCode\.Authorization\..*$").Should()
                .NotDependOnAny(Types().That().ResideInNamespaceMatching(@"^TimeForCode\.Donation\..*$"))
                .Because("Authorization bounded context should be isolated from Donation context; they are independent contexts with no cross-dependencies allowed");

            var result = rule.Evaluate(Architecture);

            foreach (var violation in result.Where(x => !x.Passed))
            {
                Console.WriteLine(violation.Description);
            }

            Assert.IsTrue(rule.HasNoViolations(Architecture));
        }

        [TestMethod]
        public void DonationContext_ShouldNotDependOnAuthorizationContext()
        {
            var rule = Types().That().ResideInNamespaceMatching(@"^TimeForCode\.Donation\..*$").Should()
                .NotDependOnAny(Types().That().ResideInNamespaceMatching(@"^TimeForCode\.Authorization\..*$"))
                .Because("Donation bounded context should be isolated from Authorization context; they are independent contexts with no cross-dependencies allowed");

            var result = rule.Evaluate(Architecture);

            foreach (var violation in result.Where(x => !x.Passed))
            {
                Console.WriteLine(violation.Description);
            }

            Assert.IsTrue(rule.HasNoViolations(Architecture));
        }
    }
}
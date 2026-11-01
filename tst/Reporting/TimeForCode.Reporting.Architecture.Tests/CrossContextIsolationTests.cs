using ArchUnitNET.Loader;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace TimeForCode.Reporting.Architecture.Tests
{
    /// <summary>
    /// Reporting reads Donation data over HTTP only (ADR-010, Shared coupling boundary): the one allowed Donation dependency is the generated
    /// client, and nothing in Reporting may reach the Authorization context.
    /// </summary>
    [TestClass]
    public class CrossContextIsolationTests
    {
        private const string ReportingNamespace = @"^TimeForCode\.Reporting\..*$";
        private const string DonationInternals = @"^TimeForCode\.Donation\.(?!Api\.Client(\.|$)).*$";
        private const string DonationApiClient = @"^TimeForCode\.Donation\.Api\.Client(\..*)?$";
        private const string AuthorizationNamespace = @"^TimeForCode\.Authorization\..*$";

        private ArchUnitNET.Domain.Architecture Architecture = default!;

        [TestInitialize]
        public void Initialize()
        {
            Architecture = new ArchLoader().LoadAssemblies(
                typeof(Api.IAnchor).Assembly,
                typeof(Application.IAnchor).Assembly,
                typeof(Domain.IAnchor).Assembly,
                typeof(Infrastructure.IAnchor).Assembly,
                typeof(Donation.Api.IAnchor).Assembly,
                typeof(Donation.Application.IAnchor).Assembly,
                typeof(Donation.Domain.IAnchor).Assembly,
                typeof(Donation.Infrastructure.IAnchor).Assembly,
                typeof(Authorization.Api.IAnchor).Assembly,
                typeof(Authorization.Application.IAnchor).Assembly,
                typeof(Authorization.Domain.IAnchor).Assembly,
                typeof(Authorization.Infrastructure.IAnchor).Assembly
            ).Build();
        }

        [TestMethod]
        public void ReportingContext_ShouldNotDependOnDonationInternals()
        {
            var rule = Types().That().ResideInNamespaceMatching(ReportingNamespace).Should()
                .NotDependOnAnyTypesThat().ResideInNamespaceMatching(DonationInternals)
                .Because("Reporting may only use the generated Donation API client; Donation domain, application, infrastructure and api types stay private to that context");

            Assert.IsTrue(rule.HasNoViolations(Architecture), Describe(rule));
        }

        [TestMethod]
        public void ReportingContext_ShouldNotDependOnAuthorizationContext()
        {
            var rule = Types().That().ResideInNamespaceMatching(ReportingNamespace).Should()
                .NotDependOnAnyTypesThat().ResideInNamespaceMatching(AuthorizationNamespace)
                .Because("Reporting and Authorization are independent contexts");

            Assert.IsTrue(rule.HasNoViolations(Architecture), Describe(rule));
        }

        [TestMethod]
        public void ReportingAllLayersExceptInfrastructure_ShouldNotDependOnTheDonationApiClient()
        {
            var rule = Types().That().ResideInNamespaceMatching(@"^TimeForCode\.Reporting\.(Values|Domain|Commands|Application|Api)(\..*)?$").Should()
                .NotDependOnAnyTypesThat().ResideInNamespaceMatching(DonationApiClient)
                .Because("The Donation client is an infrastructure detail behind the data source interface");

            Assert.IsTrue(rule.HasNoViolations(Architecture), Describe(rule));
        }

        [TestMethod]
        public void DonationAndAuthorizationContexts_ShouldNotDependOnReporting()
        {
            var rule = Types().That().ResideInNamespaceMatching(@"^TimeForCode\.(Donation|Authorization)\..*$").Should()
                .NotDependOnAnyTypesThat().ResideInNamespaceMatching(ReportingNamespace)
                .Because("Reporting is a downstream consumer; the contexts it reads from must not know it");

            Assert.IsTrue(rule.HasNoViolations(Architecture), Describe(rule));
        }

        private string Describe(ArchUnitNET.Fluent.IArchRule rule)
        {
            return string.Join(Environment.NewLine, rule.Evaluate(Architecture).Where(x => !x.Passed).Select(x => x.Description));
        }
    }
}
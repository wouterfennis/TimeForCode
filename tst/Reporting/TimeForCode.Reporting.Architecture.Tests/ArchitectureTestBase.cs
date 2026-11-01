using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace TimeForCode.Reporting.Architecture.Tests
{
    public class ArchitectureTestBase
    {
        protected ArchUnitNET.Domain.Architecture Architecture = default!;

        protected IObjectProvider<IType> ApiLayer = default!;
        protected IObjectProvider<IType> ApplicationLayer = default!;
        protected IObjectProvider<IType> DomainLayer = default!;
        protected IObjectProvider<IType> InfrastructureLayer = default!;

        // Anchored to the Reporting context: the Reporting infrastructure legitimately uses the generated TimeForCode.Donation.Api.Client.
        private const string ApiLayerNamespace = @"^TimeForCode\.Reporting\.Api(\..*)?$";
        private const string ApplicationLayerNamespace = @"^TimeForCode\.Reporting\.Application(\..*)?$";
        private const string DomainLayerNamespace = @"^TimeForCode\.Reporting\.Domain(\..*)?$";
        private const string InfrastructureLayerNamespace = @"^TimeForCode\.Reporting\.Infrastructure(\..*)?$";

        [TestInitialize]
        public void Initialize()
        {
            Architecture = new ArchLoader().LoadAssemblies(
                typeof(Api.IAnchor).Assembly,
                typeof(Application.IAnchor).Assembly,
                typeof(Domain.IAnchor).Assembly,
                typeof(Infrastructure.IAnchor).Assembly
            ).Build();

            ApiLayer = Types().That().ResideInNamespaceMatching(ApiLayerNamespace).As("Api Layer");
            ApplicationLayer = Types().That().ResideInNamespaceMatching(ApplicationLayerNamespace).As("Application Layer");
            DomainLayer = Types().That().ResideInNamespaceMatching(DomainLayerNamespace).As("Domain Layer");
            InfrastructureLayer = Types().That().ResideInNamespaceMatching(InfrastructureLayerNamespace).As("Infrastructure Layer");
        }

        public void Evaluate(IArchRule rule)
        {
            var result = rule.Evaluate(Architecture);
            var violations = result.Where(x => !x.Passed).Select(x => x.Description).ToList();
            Assert.IsTrue(rule.HasNoViolations(Architecture), string.Join(Environment.NewLine, violations));
        }
    }
}
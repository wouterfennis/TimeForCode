using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace TimeForCode.Donation.Architecture.Tests
{
    public class ArchitectureTestBase
    {
        protected ArchUnitNET.Domain.Architecture Architecture = default!;

        protected IObjectProvider<IType> ApiLayer = default!;
        protected IObjectProvider<IType> ApplicationLayer = default!;
        protected IObjectProvider<IType> DomainLayer = default!;
        protected IObjectProvider<IType> InfrastructureLayer = default!;

        private readonly string ApiLayerNamespace = @"^.*\.Api.*$";
        private readonly string ApplicationLayerNamespace = @"^.*\.Application.*$";
        private readonly string DomainLayerNamespace = @"^.*\.Domain.*$";
        private readonly string InfrastructureLayerNamespace = @"^.*\.Infrastructure.*$";

        [TestInitialize]
        public void Initialize()
        {
            Architecture = new ArchLoader().LoadAssemblies(
                typeof(TimeForCode.Donation.Api.Startup).Assembly,
                typeof(TimeForCode.Donation.Application.Interfaces.IGithubRepositoryApiService).Assembly,
                typeof(TimeForCode.Donation.Domain.GithubSnapshot).Assembly,
                typeof(TimeForCode.Donation.Infrastructure.Extensions.ServiceCollectionExtensions).Assembly
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
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
//add a using directive to ArchUnitNET.Fluent.ArchRuleDefinition to easily define ArchRules
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace TimeForCode.Authorization.Architecture.Tests
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
                System.Reflection.Assembly.Load(typeof(Api.Anchor).Assembly.GetName().Name!),
                System.Reflection.Assembly.Load(typeof(Application.Anchor).Assembly.GetName().Name!),
                System.Reflection.Assembly.Load(typeof(Domain.Anchor).Assembly.GetName().Name!),
                System.Reflection.Assembly.Load(typeof(Infrastructure.Anchor).Assembly.GetName().Name!)
            ).Build();

            ApiLayer = Types().That().ResideInNamespace(ApiLayerNamespace, useRegularExpressions: true).As("Api Layer");
            ApplicationLayer = Types().That().ResideInNamespace(ApplicationLayerNamespace, useRegularExpressions: true).As("Application Layer");
            DomainLayer = Types().That().ResideInNamespace(DomainLayerNamespace, useRegularExpressions: true).As("Domain Layer");
            InfrastructureLayer = Types().That().ResideInNamespace(InfrastructureLayerNamespace, useRegularExpressions: true).As("Infrastructure Layer");
        }

        public void Evaluate(IArchRule exampleLayerShouldNotAccessForbiddenLayer)
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
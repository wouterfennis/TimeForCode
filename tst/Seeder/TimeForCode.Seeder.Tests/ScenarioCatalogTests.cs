using AwesomeAssertions;
using System.Text.RegularExpressions;
using TimeForCode.Seeder;

namespace TimeForCode.Seeder.Tests
{
    [TestClass]
    public class ScenarioCatalogTests
    {
        [TestMethod]
        public void All_Order_IsFixed()
        {
            ScenarioCatalog.All.Select(s => s.Name).Should().Equal(
                "donor-organizations",
                "contributor-profiles",
                "project-lifecycle",
                "donations",
                "contributions",
                "anonymised-contributor");
        }

        [TestMethod]
        public void All_ScenarioNames_AreUnique()
        {
            ScenarioCatalog.All.Select(s => s.Name).Should().OnlyHaveUniqueItems();
        }

        [TestMethod]
        public void All_ScenarioPersonas_AreKnownPersonas()
        {
            ScenarioCatalog.All.SelectMany(s => s.Personas).Should().OnlyContain(p => Personas.All.Contains(p));
        }

        [TestMethod]
        public void All_Personas_AreUsedByAScenario()
        {
            Personas.All.Should().OnlyContain(p => ScenarioCatalog.All.Any(s => s.Personas.Contains(p)));
        }

        [TestMethod]
        public void Personas_Names_AreDistinctAndAcceptedByTheMockIdentityProvider()
        {
            Personas.All.Select(p => p.Name.ToLowerInvariant()).Should().OnlyHaveUniqueItems();
            Personas.All.Where(p => p.Role != PersonaRole.Admin)
                .Should().OnlyContain(p => p.Name.Length <= 64 && Regex.IsMatch(p.Name, "^[A-Za-z0-9._ -]+$"));
        }

        [TestMethod]
        public void Personas_Roles_CoverTheRequiredSet()
        {
            Personas.All.Count(p => p.Role == PersonaRole.Admin).Should().Be(1);
            Personas.All.Count(p => p.Role == PersonaRole.Contributor).Should().BeGreaterThanOrEqualTo(2);
            Personas.All.Count(p => p.Role == PersonaRole.Maintainer).Should().BeGreaterThanOrEqualTo(2);
            Personas.All.Count(p => p.Role == PersonaRole.DonorMember).Should().BeGreaterThanOrEqualTo(2);
        }

        [TestMethod]
        public void Personas_Admin_IsOnlyRequestedByTheProjectLifecycle()
        {
            ScenarioCatalog.All.Where(s => s.Personas.Contains(Personas.Admin)).Select(s => s.Name)
                .Should().Equal("project-lifecycle");
        }
    }
}
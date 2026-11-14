using AwesomeAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Infrastructure.Persistence.Database.Admin;

namespace TimeForCode.Authorization.Infrastructure.Tests.Persistence
{
    [TestClass]
    public class AdminCredentialRepositoryTests
    {
        private static string Render(FilterDefinition<AdminCredential> filter) =>
            filter.Render(new RenderArgs<AdminCredential>(BsonSerializer.SerializerRegistry.GetSerializer<AdminCredential>(), BsonSerializer.SerializerRegistry)).ToJson();

        [TestMethod]
        public void AdvanceFilter_PositiveCounter_OnlyMatchesAStoredCounterBelowIt()
        {
            var json = Render(AdminCredentialRepository.AdvanceFilter(7));

            json.Should().Contain("\"SignCount\" : { \"$lt\" : 7 }");
            json.Should().NotContain("$or\" : [{ \"SignCount\" : 0");
        }

        [TestMethod]
        public void AdvanceFilter_ZeroCounter_OnlyMatchesAStoredZero()
        {
            var json = Render(AdminCredentialRepository.AdvanceFilter(0));

            json.Should().Contain("\"SignCount\" : 0");
            json.Should().NotContain("$lt");
        }
    }
}

using AwesomeAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Infrastructure.Persistence.Database;
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

        [TestMethod]
        public async Task TryAdvanceSignCountAsync_StoredCounterBelowPresented_ReturnsTrue()
        {
            bool advanced = await CreateSut(matchedCount: 1).TryAdvanceSignCountAsync(7);

            advanced.Should().BeTrue();
        }

        [TestMethod]
        public async Task TryAdvanceSignCountAsync_NoDocumentMatches_ReturnsFalse()
        {
            bool advanced = await CreateSut(matchedCount: 0).TryAdvanceSignCountAsync(7);

            advanced.Should().BeFalse();
        }

        private static AdminCredentialRepository CreateSut(long matchedCount)
        {
            var collection = new Mock<IMongoCollection<AdminCredential>>();
            collection.Setup(c => c.UpdateOneAsync(It.IsAny<FilterDefinition<AdminCredential>>(), It.IsAny<UpdateDefinition<AdminCredential>>(), It.IsAny<UpdateOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UpdateResult.Acknowledged(matchedCount, matchedCount, null));
            var context = new Mock<IMongoDbContext>();
            context.Setup(c => c.GetCollection<AdminCredential>()).Returns(collection.Object);
            return new AdminCredentialRepository(context.Object);
        }
    }
}
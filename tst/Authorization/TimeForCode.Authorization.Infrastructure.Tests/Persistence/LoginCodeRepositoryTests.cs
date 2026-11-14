using AwesomeAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Infrastructure.Persistence.Database;

namespace TimeForCode.Authorization.Infrastructure.Tests.Persistence
{
    [TestClass]
    public class LoginCodeRepositoryTests
    {
        [TestMethod]
        public void IndexModels_ExpiryIndex_RemovesRecordsAtTheirExpiryTime()
        {
            CreateIndexModel<LoginCode> index = LoginCodeRepository.IndexModels
                .Single(i => i.Options.Name == LoginCodeRepository.ExpiryIndexName);

            index.Options.ExpireAfter.Should().Be(TimeSpan.Zero);
            Render(index).ToJson().Should().Be(new BsonDocument("ExpiresAfter", 1).ToJson());
        }

        [TestMethod]
        public void IndexModels_HashIndex_IsUnique()
        {
            CreateIndexModel<LoginCode> index = LoginCodeRepository.IndexModels
                .Single(i => i.Options.Name == LoginCodeRepository.HashIndexName);

            index.Options.Unique.Should().BeTrue();
            Render(index).ToJson().Should().Be(new BsonDocument("CodeHash", 1).ToJson());
        }

        [TestMethod]
        public void ToBsonDocument_ExpiresAfter_IsStoredAsDate()
        {
            var code = LoginCode.Issue("hash", "user", isNewUser: false, "challenge", new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero));

            code.ToBsonDocument()["ExpiresAfter"].BsonType.Should().Be(BsonType.DateTime);
        }

        private static BsonDocument Render(CreateIndexModel<LoginCode> index)
        {
            var args = new RenderArgs<LoginCode>(BsonSerializer.SerializerRegistry.GetSerializer<LoginCode>(), BsonSerializer.SerializerRegistry);
            return index.Keys.Render(args);
        }
    }
}
using AwesomeAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Infrastructure.Persistence.Database;

namespace TimeForCode.Authorization.Infrastructure.Tests.Persistence
{
    [TestClass]
    public class RefreshTokenRepositoryTests
    {
        [TestMethod]
        public void IndexModels_ExpiryIndex_RemovesRecordsAtTheirExpiryTime()
        {
            CreateIndexModel<RefreshToken> index = RefreshTokenRepository.IndexModels
                .Single(i => i.Options.Name == RefreshTokenRepository.ExpiryIndexName);

            index.Options.ExpireAfter.Should().Be(TimeSpan.Zero);
            Render(index).ToJson().Should().Be(new BsonDocument("ExpiresAfter", 1).ToJson());
        }

        [TestMethod]
        public void IndexModels_TokenHashIndex_IsUnique()
        {
            CreateIndexModel<RefreshToken> index = RefreshTokenRepository.IndexModels
                .Single(i => i.Options.Name == RefreshTokenRepository.TokenHashIndexName);

            index.Options.Unique.Should().BeTrue();
            Render(index).ToJson().Should().Be(new BsonDocument("TokenHash", 1).ToJson());
        }

        [TestMethod]
        public void ToBsonDocument_ExpiresAfter_IsStoredAsDate()
        {
            RefreshToken token = Entity();

            BsonDocument document = token.ToBsonDocument();

            document["ExpiresAfter"].BsonType.Should().Be(BsonType.DateTime);
        }

        [TestMethod]
        public void ToBsonDocument_Token_ContainsOnlyTheHash()
        {
            RefreshToken token = Entity();

            BsonDocument document = token.ToBsonDocument();

            document.Names.Should().Contain("TokenHash");
            document.Names.Should().NotContain("Token");
        }

        private static BsonDocument Render(CreateIndexModel<RefreshToken> index)
        {
            var args = new RenderArgs<RefreshToken>(BsonSerializer.SerializerRegistry.GetSerializer<RefreshToken>(), BsonSerializer.SerializerRegistry);
            return index.Keys.Render(args);
        }

        private static RefreshToken Entity() => new()
        {
            Id = ObjectId.GenerateNewId(),
            TokenHash = "hash",
            UserId = "user-1",
            ExpiresAfter = new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero)
        };
    }
}
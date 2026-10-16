using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Infrastructure.Persistence.Database;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Infrastructure.Tests.Persistence
{
    [TestClass]
    public class StateRepositoryTests
    {
        [TestMethod]
        public void IndexModels_ExpiryIndex_RemovesRecordsAtTheirExpiryTime()
        {
            CreateIndexModel<StateEntry> index = StateRepository.IndexModels
                .Single(i => i.Options.Name == StateRepository.ExpiryIndexName);

            index.Options.ExpireAfter.Should().Be(TimeSpan.Zero);
            Render(index).ToJson().Should().Be(new BsonDocument("ExpiresAfter", 1).ToJson());
        }

        [TestMethod]
        public void IndexModels_KeyIndex_IsUnique()
        {
            CreateIndexModel<StateEntry> index = StateRepository.IndexModels
                .Single(i => i.Options.Name == StateRepository.KeyIndexName);

            index.Options.Unique.Should().BeTrue();
            Render(index).ToJson().Should().Be(new BsonDocument("Key", 1).ToJson());
        }

        [TestMethod]
        public void ToBsonDocument_ExpiresAfter_IsStoredAsDate()
        {
            var entry = new StateEntry
            {
                Id = ObjectId.GenerateNewId(),
                Key = "state",
                IdentityProvider = IdentityProvider.Github,
                RedirectUri = new Uri("https://localhost/"),
                CodeVerifier = "verifier",
                ExpiresAfter = new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero)
            };

            entry.ToBsonDocument()["ExpiresAfter"].BsonType.Should().Be(BsonType.DateTime);
        }

        private static BsonDocument Render(CreateIndexModel<StateEntry> index)
        {
            var args = new RenderArgs<StateEntry>(BsonSerializer.SerializerRegistry.GetSerializer<StateEntry>(), BsonSerializer.SerializerRegistry);
            return index.Keys.Render(args);
        }
    }
}
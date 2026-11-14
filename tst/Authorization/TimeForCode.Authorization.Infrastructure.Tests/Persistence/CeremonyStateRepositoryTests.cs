using AwesomeAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Infrastructure.Persistence.Database.Admin;

namespace TimeForCode.Authorization.Infrastructure.Tests.Persistence
{
    [TestClass]
    public class CeremonyStateRepositoryTests
    {
        [TestMethod]
        public void IndexModels_ExpiryIndex_RemovesRecordsAtTheirExpiryTime()
        {
            CreateIndexModel<CeremonyState> index = CeremonyStateRepository.IndexModels
                .Single(i => i.Options.Name == CeremonyStateRepository.ExpiryIndexName);

            index.Options.ExpireAfter.Should().Be(TimeSpan.Zero);
            Render(index).ToJson().Should().Be(new BsonDocument("ExpiresAfter", 1).ToJson());
        }

        [TestMethod]
        public void IndexModels_HashIndex_IsUnique()
        {
            CreateIndexModel<CeremonyState> index = CeremonyStateRepository.IndexModels
                .Single(i => i.Options.Name == CeremonyStateRepository.HashIndexName);

            index.Options.Unique.Should().BeTrue();
            Render(index).ToJson().Should().Be(new BsonDocument("IdHash", 1).ToJson());
        }

        [TestMethod]
        public void ToBsonDocument_ExpiresAfter_IsStoredAsDate()
        {
            var code = new CeremonyState { Id = ObjectId.GenerateNewId(), IdHash = "hash", Kind = CeremonyKind.Authentication, State = "state", ExpiresAfter = new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero) };

            code.ToBsonDocument()["ExpiresAfter"].BsonType.Should().Be(BsonType.DateTime);
        }

        private static BsonDocument Render(CreateIndexModel<CeremonyState> index)
        {
            var args = new RenderArgs<CeremonyState>(BsonSerializer.SerializerRegistry.GetSerializer<CeremonyState>(), BsonSerializer.SerializerRegistry);
            return index.Keys.Render(args);
        }
    }
}
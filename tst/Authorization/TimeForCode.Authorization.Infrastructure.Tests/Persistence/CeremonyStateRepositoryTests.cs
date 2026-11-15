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

        [TestMethod]
        public async Task AddAsync_InsertsTheCeremonyState()
        {
            var (sut, collection) = Create();
            var state = new CeremonyState { Id = ObjectId.GenerateNewId(), IdHash = "hash", Kind = CeremonyKind.Authentication, State = "state", ExpiresAfter = new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero) };

            await sut.AddAsync(state);

            collection.Verify(c => c.InsertOneAsync(state, null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task TakeAsync_KnownHash_DeletesAndReturnsTheStateInOneStep()
        {
            var (sut, collection) = Create();
            var state = new CeremonyState { Id = ObjectId.GenerateNewId(), IdHash = "hash", Kind = CeremonyKind.Authentication, State = "state", ExpiresAfter = new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero) };
            FilterDefinition<CeremonyState>? filter = null;
            collection.Setup(c => c.FindOneAndDeleteAsync(It.IsAny<FilterDefinition<CeremonyState>>(), It.IsAny<FindOneAndDeleteOptions<CeremonyState, CeremonyState>>(), It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<CeremonyState>, FindOneAndDeleteOptions<CeremonyState, CeremonyState>, CancellationToken>((f, _, _) => filter = f)
                .ReturnsAsync(state);

            CeremonyState? taken = await sut.TakeAsync("hash");

            taken.Should().BeSameAs(state);
            var args = new RenderArgs<CeremonyState>(BsonSerializer.SerializerRegistry.GetSerializer<CeremonyState>(), BsonSerializer.SerializerRegistry);
            filter!.Render(args).ToJson().Should().Be(new BsonDocument("IdHash", "hash").ToJson());
        }

        [TestMethod]
        public void Constructor_CalledTwice_CreatesTheIndexesAtMostOnce()
        {
            var indexes = new Mock<IMongoIndexManager<CeremonyState>>();
            var collection = new Mock<IMongoCollection<CeremonyState>>();
            collection.SetupGet(c => c.Indexes).Returns(indexes.Object);
            var context = new Mock<IMongoDbContext>();
            context.Setup(c => c.GetCollection<CeremonyState>()).Returns(collection.Object);

            _ = new CeremonyStateRepository(context.Object);
            _ = new CeremonyStateRepository(context.Object);

            indexes.Verify(i => i.CreateMany(CeremonyStateRepository.IndexModels, It.IsAny<CancellationToken>()), Times.AtMostOnce);
        }

        private static (CeremonyStateRepository Sut, Mock<IMongoCollection<CeremonyState>> Collection) Create()
        {
            var collection = new Mock<IMongoCollection<CeremonyState>>();
            collection.SetupGet(c => c.Indexes).Returns(new Mock<IMongoIndexManager<CeremonyState>>().Object);
            var context = new Mock<IMongoDbContext>();
            context.Setup(c => c.GetCollection<CeremonyState>()).Returns(collection.Object);
            return (new CeremonyStateRepository(context.Object), collection);
        }

        private static BsonDocument Render(CreateIndexModel<CeremonyState> index)
        {
            var args = new RenderArgs<CeremonyState>(BsonSerializer.SerializerRegistry.GetSerializer<CeremonyState>(), BsonSerializer.SerializerRegistry);
            return index.Keys.Render(args);
        }
    }
}
using AwesomeAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
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

        [TestMethod]
        public async Task AddAsync_InsertsTheLoginCode()
        {
            var (sut, collection) = Create();
            var code = LoginCode.Issue("hash", "user", isNewUser: false, "challenge", new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero));

            await sut.AddAsync(code);

            collection.Verify(c => c.InsertOneAsync(code, null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task TakeAsync_KnownHash_DeletesAndReturnsTheCodeInOneStep()
        {
            var (sut, collection) = Create();
            var code = LoginCode.Issue("hash", "user", isNewUser: false, "challenge", new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero));
            FilterDefinition<LoginCode>? filter = null;
            collection.Setup(c => c.FindOneAndDeleteAsync(It.IsAny<FilterDefinition<LoginCode>>(), It.IsAny<FindOneAndDeleteOptions<LoginCode, LoginCode>>(), It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<LoginCode>, FindOneAndDeleteOptions<LoginCode, LoginCode>, CancellationToken>((f, _, _) => filter = f)
                .ReturnsAsync(code);

            var taken = await sut.TakeAsync("hash");

            taken.Should().BeSameAs(code);
            var args = new RenderArgs<LoginCode>(BsonSerializer.SerializerRegistry.GetSerializer<LoginCode>(), BsonSerializer.SerializerRegistry);
            filter!.Render(args).ToJson().Should().Be(new BsonDocument("CodeHash", "hash").ToJson());
        }

        private static (LoginCodeRepository Sut, Mock<IMongoCollection<LoginCode>> Collection) Create()
        {
            var collection = new Mock<IMongoCollection<LoginCode>>();
            collection.SetupGet(c => c.Indexes).Returns(new Mock<IMongoIndexManager<LoginCode>>().Object);
            var context = new Mock<IMongoDbContext>();
            context.Setup(c => c.GetCollection<LoginCode>()).Returns(collection.Object);
            return (new LoginCodeRepository(context.Object), collection);
        }

        private static BsonDocument Render(CreateIndexModel<LoginCode> index)
        {
            var args = new RenderArgs<LoginCode>(BsonSerializer.SerializerRegistry.GetSerializer<LoginCode>(), BsonSerializer.SerializerRegistry);
            return index.Keys.Render(args);
        }
    }
}
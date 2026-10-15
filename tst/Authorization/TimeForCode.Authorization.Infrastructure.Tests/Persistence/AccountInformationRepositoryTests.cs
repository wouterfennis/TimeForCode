using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Infrastructure.Persistence.Database;

namespace TimeForCode.Authorization.Infrastructure.Tests.Persistence
{
    [TestClass]
    public class AccountInformationRepositoryTests
    {
        [TestMethod]
        public async Task RemoveStoredGitHubAccessTokensAsync_UnsetsTheLegacyFieldOnAccountsThatHaveIt()
        {
            FilterDefinition<AccountInformation>? filter = null;
            UpdateDefinition<AccountInformation>? update = null;
            var collection = new Mock<IMongoCollection<AccountInformation>>();
            collection.Setup(x => x.UpdateManyAsync(It.IsAny<FilterDefinition<AccountInformation>>(), It.IsAny<UpdateDefinition<AccountInformation>>(), It.IsAny<UpdateOptions>(), It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<AccountInformation>, UpdateDefinition<AccountInformation>, UpdateOptions, CancellationToken>((f, u, _, _) => { filter = f; update = u; })
                .ReturnsAsync(new UpdateResult.Acknowledged(3, 3, null));
            var context = new Mock<IMongoDbContext>();
            context.Setup(x => x.GetCollection<AccountInformation>()).Returns(collection.Object);
            var sut = new AccountInformationRepository(context.Object);

            var removed = await sut.RemoveStoredGitHubAccessTokensAsync();

            removed.Should().Be(3);
            var args = new RenderArgs<AccountInformation>(BsonSerializer.SerializerRegistry.GetSerializer<AccountInformation>(), BsonSerializer.SerializerRegistry);
            filter!.Render(args).ToJson().Should().Contain("EncryptedGitHubAccessToken").And.Contain("$exists");
            update!.Render(args).ToJson().Should().Contain("$unset").And.Contain("EncryptedGitHubAccessToken");
        }
    }
}
using AwesomeAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Infrastructure.Persistence.Database;

namespace TimeForCode.Donation.Infrastructure.Tests.Persistence
{
    [TestClass]
    public class ContributionDefinitionsTests
    {
        private static readonly ObjectId ProjectId = new("5f43a0e74b12c84f1b000001");
        private static readonly ObjectId ContributionId = new("5f43a0e74b12c84f1b0000f1");

        [TestMethod]
        public void ProjectFilter_Project_ExcludesDeletedContributions()
        {
            var filter = Render(ContributionDefinitions.ProjectFilter(ProjectId));

            filter.ToJson().Should().Be(new BsonDocument
            {
                { "ProjectId", ProjectId },
                { "DeletedAt", BsonNull.Value }
            }.ToJson());
        }

        [TestMethod]
        public void ContributorsFilter_Contributors_MatchesTheirNonDeletedContributions()
        {
            var otherContributorId = new ObjectId("5f43a0e74b12c84f1b0000c2");

            var filter = Render(ContributionDefinitions.ContributorsFilter([ContributionId, otherContributorId]));

            filter.ToJson().Should().Be(new BsonDocument
            {
                { "ContributorId", new BsonDocument("$in", new BsonArray { ContributionId, otherContributorId }) },
                { "DeletedAt", BsonNull.Value }
            }.ToJson());
        }

        [TestMethod]
        public void LiveContributionFilter_ProjectAndContribution_ExcludesDeletedContributions()
        {
            var filter = Render(ContributionDefinitions.LiveContributionFilter(ProjectId, ContributionId));

            filter.ToJson().Should().Be(new BsonDocument
            {
                { "_id", ContributionId },
                { "ProjectId", ProjectId },
                { "DeletedAt", BsonNull.Value }
            }.ToJson());
        }

        [TestMethod]
        public void VersionFilter_LoadedContribution_OnlyMatchesTheVersionItWasLoadedWith()
        {
            var filter = Render(ContributionDefinitions.VersionFilter(Contribution(version: 4)));

            filter.ToJson().Should().Be(new BsonDocument
            {
                { "_id", ContributionId },
                { "Version", 4 }
            }.ToJson());
        }

        [TestMethod]
        public void Changes_CorrectedContribution_SetsOnlyTheChangedFieldsAndBumpsTheVersion()
        {
            var update = Render(ContributionDefinitions.Changes(Contribution(version: 4)));

            var set = update["$set"].AsBsonDocument;
            set.Names.Should().BeEquivalentTo("Hours", "GithubReference", "History", "DeletedAt", "DeletedByUserId");
            update["$inc"].AsBsonDocument["Version"].AsInt32.Should().Be(1);
        }

        private static Contribution Contribution(int version)
        {
            return new Contribution
            {
                Id = ContributionId,
                ProjectId = ProjectId,
                ContributorId = ObjectId.GenerateNewId(),
                ContributorGitHubLogin = "octocat",
                ContributorAvatarUrl = "https://avatars.githubusercontent.com/u/1",
                Hours = 3,
                LoggedAt = new DateTimeOffset(2024, 3, 1, 9, 0, 0, TimeSpan.Zero),
                Version = version
            };
        }

        private static BsonDocument Render(FilterDefinition<Contribution> filter)
        {
            var registry = BsonSerializer.SerializerRegistry;
            var args = new RenderArgs<Contribution>(registry.GetSerializer<Contribution>(), registry);
            return filter.Render(args).AsBsonDocument;
        }

        private static BsonDocument Render(UpdateDefinition<Contribution> update)
        {
            var registry = BsonSerializer.SerializerRegistry;
            var args = new RenderArgs<Contribution>(registry.GetSerializer<Contribution>(), registry);
            return update.Render(args).AsBsonDocument;
        }
    }
}
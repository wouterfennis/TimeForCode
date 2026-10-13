using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Text.RegularExpressions;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Infrastructure.Persistence.Database;

namespace TimeForCode.Donation.Infrastructure.Tests.Persistence
{
    [TestClass]
    public class ProjectSearchDefinitionsTests
    {
        private const int ActiveStatus = 0;

        [TestMethod]
        public void BuildFilter_NoCriteria_FiltersOnActiveStatusOnly()
        {
            var filter = Render(ProjectSearchDefinitions.BuildFilter(Criteria()));

            filter.ToJson().Should().Be(new BsonDocument("Status", ActiveStatus).ToJson());
        }

        [TestMethod]
        public void BuildFilter_Language_AddsExactLanguageMatch()
        {
            var filter = Render(ProjectSearchDefinitions.BuildFilter(Criteria(language: "C#")));

            filter.ToJson().Should().Be(new BsonDocument
            {
                { "Status", ActiveStatus },
                { "Snapshot.Language", "C#" }
            }.ToJson());
        }

        [TestMethod]
        public void BuildFilter_Tag_AddsTopicMatch()
        {
            var filter = Render(ProjectSearchDefinitions.BuildFilter(Criteria(tag: "gaming")));

            filter.ToJson().Should().Be(new BsonDocument
            {
                { "Status", ActiveStatus },
                { "Snapshot.Topics", "gaming" }
            }.ToJson());
        }

        [TestMethod]
        public void BuildFilter_SearchText_MatchesNameFullNameOrDescriptionCaseInsensitively()
        {
            var filter = Render(ProjectSearchDefinitions.BuildFilter(Criteria(searchText: "rocket")));

            var alternatives = filter["$or"].AsBsonArray.Select(a => a.AsBsonDocument.GetElement(0)).ToList();
            alternatives.Select(a => a.Name).Should().Equal("Snapshot.Name", "Snapshot.FullName", "Snapshot.Description");
            alternatives.Should().OnlyContain(a => a.Value.AsBsonRegularExpression.Pattern == "rocket"
                && a.Value.AsBsonRegularExpression.Options == "i");
            filter["Status"].AsInt32.Should().Be(ActiveStatus);
        }

        [TestMethod]
        public void BuildFilter_SearchTextWithRegexCharacters_TreatsTextAsLiteral()
        {
            const string text = "c++ (a|b).*";

            var filter = Render(ProjectSearchDefinitions.BuildFilter(Criteria(searchText: text)));

            filter["$or"].AsBsonArray
                .Select(a => a.AsBsonDocument.GetElement(0).Value.AsBsonRegularExpression.Pattern)
                .Should().OnlyContain(pattern => pattern == Regex.Escape(text));
        }

        [TestMethod]
        public void BuildFilter_AllCriteria_CombinesThemWithAnd()
        {
            var filter = Render(ProjectSearchDefinitions.BuildFilter(Criteria("planner", "C#", "gaming")));

            filter["Status"].AsInt32.Should().Be(ActiveStatus);
            filter["Snapshot.Language"].AsString.Should().Be("C#");
            filter["Snapshot.Topics"].AsString.Should().Be("gaming");
            filter["$or"].AsBsonArray.Should().HaveCount(3);
        }

        [TestMethod]
        public void IndexModels_CoverStatusLanguageAndTopicFiltersWithTheDefaultSort()
        {
            var indexes = ProjectSearchDefinitions.IndexModels
                .ToDictionary(m => m.Options.Name, m => RenderKeys(m).ToJson());

            indexes.Should().BeEquivalentTo(new Dictionary<string, string>
            {
                ["ix_projects_status_published_at"] = new BsonDocument { { "Status", 1 }, { "PublishedAt", -1 }, { "_id", -1 } }.ToJson(),
                ["ix_projects_status_language_published_at"] = new BsonDocument { { "Status", 1 }, { "Snapshot.Language", 1 }, { "PublishedAt", -1 }, { "_id", -1 } }.ToJson(),
                ["ix_projects_status_topics_published_at"] = new BsonDocument { { "Status", 1 }, { "Snapshot.Topics", 1 }, { "PublishedAt", -1 }, { "_id", -1 } }.ToJson()
            });
        }

        [TestMethod]
        public void QueryOptions_LimitServerExecutionTime()
        {
            ProjectSearchDefinitions.CountOptions().MaxTime.Should().Be(TimeSpan.FromSeconds(5));
            ProjectSearchDefinitions.FindOptions().MaxTime.Should().Be(TimeSpan.FromSeconds(5));
        }

        private static ProjectSearchCriteria Criteria(string? searchText = null, string? language = null, string? tag = null)
        {
            return new ProjectSearchCriteria(searchText, language, tag, 1, 20);
        }

        private static BsonDocument Render(FilterDefinition<Project> filter)
        {
            var args = new RenderArgs<Project>(BsonSerializer.LookupSerializer<Project>(), BsonSerializer.SerializerRegistry);
            return filter.Render(args).AsBsonDocument;
        }

        private static BsonDocument RenderKeys(CreateIndexModel<Project> model)
        {
            var args = new RenderArgs<Project>(BsonSerializer.LookupSerializer<Project>(), BsonSerializer.SerializerRegistry);
            return model.Keys.Render(args);
        }
    }
}
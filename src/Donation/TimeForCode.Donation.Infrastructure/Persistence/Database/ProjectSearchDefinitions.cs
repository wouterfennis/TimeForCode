using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Infrastructure.Persistence.Database
{
    /// <summary>
    /// Filter and index definitions for searching active projects.
    /// </summary>
    internal static class ProjectSearchDefinitions
    {
        internal static IReadOnlyList<CreateIndexModel<Project>> IndexModels { get; } =
        [
            Index("ix_projects_status_published_at"),
            Index("ix_projects_status_language_published_at", "Snapshot.Language"),
            Index("ix_projects_status_topics_published_at", "Snapshot.Topics")
        ];

        private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(5);

        internal static CountOptions CountOptions()
        {
            return new CountOptions { MaxTime = QueryTimeout };
        }

        internal static FindOptions FindOptions()
        {
            return new FindOptions { MaxTime = QueryTimeout };
        }

        internal static FilterDefinition<Project> BuildFilter(ProjectSearchCriteria criteria)
        {
            var builder = Builders<Project>.Filter;
            var filters = new List<FilterDefinition<Project>>
            {
                builder.Eq(p => p.Status, ProjectStatus.Active)
            };

            if (criteria.Language != null)
            {
                filters.Add(builder.Eq(p => p.Snapshot.Language, criteria.Language));
            }

            if (criteria.Tag != null)
            {
                filters.Add(builder.AnyEq(p => p.Snapshot.Topics, criteria.Tag));
            }

            if (criteria.SearchText != null)
            {
                var pattern = new BsonRegularExpression(Regex.Escape(criteria.SearchText), "i");
                filters.Add(builder.Or(
                    builder.Regex(p => p.Snapshot.Name, pattern),
                    builder.Regex(p => p.Snapshot.FullName, pattern),
                    builder.Regex(p => p.Snapshot.Description, pattern)));
            }

            return builder.And(filters);
        }

        private static CreateIndexModel<Project> Index(string name, string? filterField = null)
        {
            var keys = Builders<Project>.IndexKeys.Ascending("Status");
            if (filterField != null)
            {
                keys = keys.Ascending(filterField);
            }

            keys = keys.Descending("PublishedAt").Descending("_id");

            return new CreateIndexModel<Project>(keys, new CreateIndexOptions { Name = name });
        }
    }
}
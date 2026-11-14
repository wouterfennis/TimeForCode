using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Diagnostics.CodeAnalysis;
using TimeForCode.Reporting.Application.Interfaces;
using TimeForCode.Reporting.Domain;
using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Infrastructure.Persistence
{
    /// <summary>
    /// Badge awards in MongoDB. A unique index on recipient type, recipient and tier name allows one award per tier and recipient;
    /// awarding is an upsert that only sets the award date on insert, so requests at the same time store exactly one award and keep the first date.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Repository implementation")]
    internal class BadgeAwardRepository : IBadgeAwardRepository
    {
        private const string CollectionName = "BadgeAwards";
        private static readonly Lock IndexCreationLock = new();
        private static bool _indexesCreated;
        private readonly IMongoCollection<AwardDocument> _collection;

        public BadgeAwardRepository(ReportingMongoContext context)
        {
            _collection = context.GetCollection<AwardDocument>(CollectionName);
            EnsureIndexes(_collection);
        }

        // Runs on the first request that needs the repository. If creating the index fails (database down) that request fails with a 500,
        // the flag stays unset and the next request tries again.
        private static void EnsureIndexes(IMongoCollection<AwardDocument> collection)
        {
            if (_indexesCreated)
            {
                return;
            }

            lock (IndexCreationLock)
            {
                if (_indexesCreated)
                {
                    return;
                }

                collection.Indexes.CreateOne(new CreateIndexModel<AwardDocument>(
                    Builders<AwardDocument>.IndexKeys
                        .Ascending(a => a.RecipientType)
                        .Ascending(a => a.RecipientId)
                        .Ascending(a => a.TierName),
                    new CreateIndexOptions { Name = "ux_badge_awards_type_recipient_tier", Unique = true }));
                _indexesCreated = true;
            }
        }

        public async Task AwardAsync(BadgeAward award, CancellationToken cancellationToken)
        {
            FilterDefinition<AwardDocument> filter = Builders<AwardDocument>.Filter.And(
                Builders<AwardDocument>.Filter.Eq(a => a.RecipientType, award.RecipientType),
                Builders<AwardDocument>.Filter.Eq(a => a.RecipientId, award.RecipientId),
                Builders<AwardDocument>.Filter.Eq(a => a.TierName, award.TierName));

            try
            {
                await _collection.UpdateOneAsync(
                    filter,
                    Builders<AwardDocument>.Update.SetOnInsert(a => a.AwardedAt, award.AwardedAt.UtcDateTime),
                    new UpdateOptions { IsUpsert = true },
                    cancellationToken);
            }
            catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // A request at the same time stored the award between our filter and our insert; the badge exists, which is the goal.
            }
        }

        public async Task<IReadOnlyList<BadgeAward>> ListAsync(BadgeRecipientType recipientType, string recipientId, CancellationToken cancellationToken)
        {
            List<AwardDocument> documents = await _collection
                .Find(Builders<AwardDocument>.Filter.And(
                    Builders<AwardDocument>.Filter.Eq(a => a.RecipientType, recipientType),
                    Builders<AwardDocument>.Filter.Eq(a => a.RecipientId, recipientId)))
                .SortBy(a => a.AwardedAt)
                .ThenBy(a => a.TierName)
                .ToListAsync(cancellationToken);

            return documents
                .Select(d => new BadgeAward(d.TierName, d.RecipientType, d.RecipientId, new DateTimeOffset(DateTime.SpecifyKind(d.AwardedAt, DateTimeKind.Utc))))
                .ToList();
        }

        private sealed class AwardDocument
        {
            [BsonId]
            public ObjectId Id { get; set; }

            [BsonRepresentation(BsonType.String)]
            public BadgeRecipientType RecipientType { get; set; }

            public string RecipientId { get; set; } = string.Empty;

            public string TierName { get; set; } = string.Empty;

            public DateTime AwardedAt { get; set; }
        }
    }
}
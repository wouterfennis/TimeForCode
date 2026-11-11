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
    /// Badge tiers in MongoDB. Unique indexes keep a tier name and a threshold unique per recipient type, so the rules hold
    /// when administrators save at the same time.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Repository implementation")]
    internal class BadgeTierRepository : IBadgeTierRepository
    {
        private const string CollectionName = "BadgeTiers";
        private static readonly object IndexCreationLock = new();
        private static bool _indexesCreated;
        private readonly IMongoCollection<TierDocument> _collection;

        public BadgeTierRepository(ReportingMongoContext context)
        {
            _collection = context.GetCollection<TierDocument>(CollectionName);
            EnsureIndexes(_collection);
        }

        private static void EnsureIndexes(IMongoCollection<TierDocument> collection)
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

                var keys = Builders<TierDocument>.IndexKeys;
                collection.Indexes.CreateMany(
                [
                    new CreateIndexModel<TierDocument>(
                        keys.Ascending(t => t.RecipientType).Ascending(t => t.Name),
                        new CreateIndexOptions { Name = "ux_badge_tiers_type_name", Unique = true }),
                    new CreateIndexModel<TierDocument>(
                        keys.Ascending(t => t.RecipientType).Ascending(t => t.ThresholdHours),
                        new CreateIndexOptions { Name = "ux_badge_tiers_type_threshold", Unique = true })
                ]);
                _indexesCreated = true;
            }
        }

        public async Task<IReadOnlyList<BadgeTier>> ListAsync(BadgeRecipientType? recipientType, CancellationToken cancellationToken)
        {
            FilterDefinition<TierDocument> filter = recipientType is null
                ? Builders<TierDocument>.Filter.Empty
                : Builders<TierDocument>.Filter.Eq(t => t.RecipientType, recipientType.Value);

            List<TierDocument> documents = await _collection
                .Find(filter)
                .SortBy(t => t.RecipientType)
                .ThenBy(t => t.ThresholdHours)
                .ToListAsync(cancellationToken);

            return documents.Select(d => new BadgeTier(d.Name, d.RecipientType, d.ThresholdHours)).ToList();
        }

        public async Task<bool> SaveAsync(BadgeTier tier, CancellationToken cancellationToken)
        {
            FilterDefinition<TierDocument> filter = Builders<TierDocument>.Filter.And(
                Builders<TierDocument>.Filter.Eq(t => t.RecipientType, tier.RecipientType),
                Builders<TierDocument>.Filter.Eq(t => t.Name, tier.Name));

            try
            {
                await _collection.UpdateOneAsync(
                    filter,
                    Builders<TierDocument>.Update.Set(t => t.ThresholdHours, tier.ThresholdHours),
                    new UpdateOptions { IsUpsert = true },
                    cancellationToken);
                return true;
            }
            catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return false;
            }
        }

        public async Task<bool> RemoveAsync(BadgeRecipientType recipientType, string name, CancellationToken cancellationToken)
        {
            DeleteResult result = await _collection.DeleteOneAsync(
                Builders<TierDocument>.Filter.And(
                    Builders<TierDocument>.Filter.Eq(t => t.RecipientType, recipientType),
                    Builders<TierDocument>.Filter.Eq(t => t.Name, name)),
                cancellationToken);

            return result.DeletedCount > 0;
        }

        private sealed class TierDocument
        {
            [BsonId]
            public ObjectId Id { get; set; }

            [BsonRepresentation(BsonType.String)]
            public BadgeRecipientType RecipientType { get; set; }

            public string Name { get; set; } = string.Empty;

            public int ThresholdHours { get; set; }
        }
    }
}
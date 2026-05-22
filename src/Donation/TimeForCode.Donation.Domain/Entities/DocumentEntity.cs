using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TimeForCode.Donation.Domain.Entities
{
    public class DocumentEntity
    {
        [BsonId]
        public required ObjectId Id { get; init; }
    }
}
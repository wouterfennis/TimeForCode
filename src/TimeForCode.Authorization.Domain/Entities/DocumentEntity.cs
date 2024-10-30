using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TimeForCode.Authorization.Domain.Entities
{
    public class DocumentEntity
    {
        [BsonId]
        public required ObjectId Id { get; init; }
    }
}
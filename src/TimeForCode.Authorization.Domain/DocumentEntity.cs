using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace TimeForCode.Authorization.Domain
{
    public class DocumentEntity
    {
        [BsonId]
        public required ObjectId Id { get; init; }
    }
}
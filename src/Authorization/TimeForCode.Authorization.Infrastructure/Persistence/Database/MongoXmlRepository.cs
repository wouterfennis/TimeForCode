using Microsoft.AspNetCore.DataProtection.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Xml.Linq;

namespace TimeForCode.Authorization.Infrastructure.Persistence.Database
{
    /// <summary>
    /// Persists the Data Protection key ring in MongoDB so restarts and multiple instances share the same keys.
    /// </summary>
    public class MongoXmlRepository : IXmlRepository
    {
        private readonly IMongoCollection<DataProtectionKeyDocument> _collection;

        public MongoXmlRepository(IMongoDbContext context)
        {
            _collection = context.GetCollection<DataProtectionKeyDocument>();
        }

        public IReadOnlyCollection<XElement> GetAllElements()
        {
            return _collection.FindSync(FilterDefinition<DataProtectionKeyDocument>.Empty)
                .ToList()
                .Select(d => XElement.Parse(d.Xml))
                .ToList();
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            _collection.InsertOne(new DataProtectionKeyDocument
            {
                Id = ObjectId.GenerateNewId(),
                FriendlyName = friendlyName,
                Xml = element.ToString(SaveOptions.DisableFormatting)
            });
        }
    }
}
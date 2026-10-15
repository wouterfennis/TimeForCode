using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using System.Xml.Linq;
using TimeForCode.Authorization.Infrastructure.Persistence.Database;

namespace TimeForCode.Authorization.Infrastructure.Tests.Persistence
{
    [TestClass]
    public class MongoXmlRepositoryTests
    {
        private Mock<IMongoCollection<DataProtectionKeyDocument>> _collection = null!;
        private MongoXmlRepository _sut = null!;

        [TestInitialize]
        public void Setup()
        {
            _collection = new Mock<IMongoCollection<DataProtectionKeyDocument>>();
            var context = new Mock<IMongoDbContext>();
            context.Setup(x => x.GetCollection<DataProtectionKeyDocument>()).Returns(_collection.Object);
            _sut = new MongoXmlRepository(context.Object);
        }

        [TestMethod]
        public void StoreElement_InsertsTheKeyXmlWithItsFriendlyName()
        {
            DataProtectionKeyDocument? inserted = null;
            _collection.Setup(x => x.InsertOne(It.IsAny<DataProtectionKeyDocument>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()))
                .Callback<DataProtectionKeyDocument, InsertOneOptions, CancellationToken>((d, _, _) => inserted = d);

            _sut.StoreElement(new XElement("key", new XAttribute("id", "1")), "key-1");

            inserted!.FriendlyName.Should().Be("key-1");
            XElement.Parse(inserted.Xml).Attribute("id")!.Value.Should().Be("1");
        }

        [TestMethod]
        public void GetAllElements_ReturnsTheStoredKeyXml()
        {
            var stored = new DataProtectionKeyDocument { Id = ObjectId.GenerateNewId(), FriendlyName = "key-1", Xml = "<key id=\"1\" />" };
            var cursor = new Mock<IAsyncCursor<DataProtectionKeyDocument>>();
            cursor.SetupSequence(x => x.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
            cursor.Setup(x => x.Current).Returns([stored]);
            _collection.Setup(x => x.FindSync(It.IsAny<FilterDefinition<DataProtectionKeyDocument>>(), It.IsAny<FindOptions<DataProtectionKeyDocument, DataProtectionKeyDocument>>(), It.IsAny<CancellationToken>()))
                .Returns(cursor.Object);

            var elements = _sut.GetAllElements();

            elements.Should().ContainSingle().Which.Attribute("id")!.Value.Should().Be("1");
        }
    }
}
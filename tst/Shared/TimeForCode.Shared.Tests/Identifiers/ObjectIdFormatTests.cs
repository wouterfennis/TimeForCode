using AwesomeAssertions;
using MongoDB.Bson;
using TimeForCode.Shared.Identifiers;

namespace TimeForCode.Shared.Tests.Identifiers
{
    [TestClass]
    public class ObjectIdFormatTests
    {
        [TestMethod]
        [DataRow("507f1f77bcf86cd799439011")]
        [DataRow("507F1F77BCF86CD799439011")]
        public void IsValid_TwentyFourHexCharacters_IsTrue(string id)
        {
            ObjectIdFormat.IsValid(id).Should().BeTrue();
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("not-an-identifier")]
        [DataRow("507f1f77bcf86cd79943901")]
        [DataRow("507f1f77bcf86cd7994390111")]
        [DataRow("507f1f77bcf86cd79943901z")]
        [DataRow("507f1f77bcf86cd799439011\n")]
        public void IsValid_AnythingElse_IsFalse(string id)
        {
            ObjectIdFormat.IsValid(id).Should().BeFalse();
        }

        [TestMethod]
        [DataRow("507f1f77bcf86cd799439011")]
        [DataRow("507f1f77bcf86cd79943901z")]
        [DataRow("507f1f77bcf86cd799439011\n")]
        [DataRow(" 507f1f77bcf86cd79943901")]
        public void IsValid_AgreesWithTheMongoParser(string id)
        {
            ObjectIdFormat.IsValid(id).Should().Be(ObjectId.TryParse(id, out _));
        }
    }
}
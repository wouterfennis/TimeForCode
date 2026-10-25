using AwesomeAssertions;
using TimeForCode.Authorization.Application.Services;

namespace TimeForCode.Authorization.Application.Tests.Services
{
    [TestClass]
    public class RefreshTokenHasherTests
    {
        [TestMethod]
        public void Hash_SameInput_ReturnsSameHash()
        {
            RefreshTokenHasher.Hash("abc").Should().Be(RefreshTokenHasher.Hash("abc"));
        }

        [TestMethod]
        public void Hash_DifferentInput_ReturnsDifferentHash()
        {
            RefreshTokenHasher.Hash("abc").Should().NotBe(RefreshTokenHasher.Hash("abd"));
        }

        [TestMethod]
        public void Hash_Input_ReturnsSha256HexDigest()
        {
            RefreshTokenHasher.Hash("abc")
                .Should().Be("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
        }
    }
}
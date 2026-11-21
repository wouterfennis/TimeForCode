using AwesomeAssertions;
using TimeForCode.Frontend.Bff.Sessions;

namespace TimeForCode.Frontend.Bff.Tests
{
    [TestClass]
    public sealed class BrowserSessionOptionsTests
    {
        [TestMethod]
        public void SignInRequestsPerMinute_Default_Is40()
        {
            new BrowserSessionOptions().SignInRequestsPerMinute.Should().Be(40);
        }
    }
}
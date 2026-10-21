using FluentAssertions;

namespace TimeForCode.Donation.Specifications.Mocking
{
    [TestClass]
    public class LiveHostTests
    {
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void ResolveBaseAddress_NotConfigured_ReturnsNull(string? configured)
        {
            LiveHost.ResolveBaseAddress(configured).Should().BeNull();
        }

        [TestMethod]
        public void ResolveBaseAddress_AbsoluteUrl_ReturnsUri()
        {
            LiveHost.ResolveBaseAddress("http://localhost:8080").Should().Be(new Uri("http://localhost:8080"));
        }

        [TestMethod]
        public void ResolveBaseAddress_RelativeUrl_Throws()
        {
            Action act = () => LiveHost.ResolveBaseAddress("localhost:8080/api");

            act.Should().Throw<InvalidOperationException>().WithMessage($"*{LiveHost.BaseUrlVariable}*");
        }

        [TestMethod]
        public void IsLive_TaggedAndConfigured_ReturnsTrue()
        {
            LiveHost.IsLive(["live"], new Uri("http://localhost:8080")).Should().BeTrue();
        }

        [TestMethod]
        public void IsLive_TaggedButNotConfigured_ReturnsFalse()
        {
            LiveHost.IsLive(["live"], null).Should().BeFalse();
        }

        [TestMethod]
        public void UnavailableServices_GetService_ThrowsWithLiveHint()
        {
            Action act = () => LiveHost.UnavailableServices.GetService(typeof(object));

            act.Should().Throw<NotSupportedException>().WithMessage("*@live*");
        }

        [TestMethod]
        public void IsLive_ConfiguredButNotTagged_ReturnsFalse()
        {
            LiveHost.IsLive(["wip"], new Uri("http://localhost:8080")).Should().BeFalse();
        }
    }
}
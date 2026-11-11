using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Net;
using TimeForCode.Reporting.Application.Handlers;
using TimeForCode.Reporting.Application.Interfaces;
using TimeForCode.Reporting.Commands;
using TimeForCode.Reporting.Domain;
using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Application.Tests.Handlers
{
    [TestClass]
    public class BadgeTierHandlersTests
    {
        private Mock<IBadgeTierRepository> _tiers = default!;

        [TestInitialize]
        public void Setup()
        {
            _tiers = new Mock<IBadgeTierRepository>();
        }

        private DefineBadgeTierHandler DefineHandler() => new(_tiers.Object, NullLogger<DefineBadgeTierHandler>.Instance);

        private static DefineBadgeTierCommand Define(string name = "Bronze", int hours = 10, BadgeRecipientType type = BadgeRecipientType.Organization) =>
            new() { RecipientType = type, Name = name, ThresholdHours = hours };

        [TestMethod]
        public async Task Define_ValidTier_SavesAndReturnsIt()
        {
            _tiers.Setup(t => t.SaveAsync(new BadgeTier("Bronze", BadgeRecipientType.Contributor, 5), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await DefineHandler().Handle(Define("Bronze", 5, BadgeRecipientType.Contributor), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Name.Should().Be("Bronze");
            result.Value.RecipientType.Should().Be(BadgeRecipientType.Contributor);
            result.Value.ThresholdHours.Should().Be(5);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-5)]
        public async Task Define_ThresholdNotPositive_IsRejectedWithoutSaving(int hours)
        {
            var result = await DefineHandler().Handle(Define(hours: hours), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(HttpStatusCode.BadRequest);
            _tiers.Verify(t => t.SaveAsync(It.IsAny<BadgeTier>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("   ")]
        public async Task Define_BlankName_IsRejectedWithoutSaving(string name)
        {
            var result = await DefineHandler().Handle(Define(name: name), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(HttpStatusCode.BadRequest);
            _tiers.Verify(t => t.SaveAsync(It.IsAny<BadgeTier>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task Define_ThresholdAlreadyTaken_IsAConflict()
        {
            _tiers.Setup(t => t.SaveAsync(It.IsAny<BadgeTier>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var result = await DefineHandler().Handle(Define("Silver", 10), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [TestMethod]
        public async Task Define_NameIsTrimmed()
        {
            _tiers.Setup(t => t.SaveAsync(new BadgeTier("Bronze", BadgeRecipientType.Organization, 10), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await DefineHandler().Handle(Define("  Bronze  "), CancellationToken.None);

            result.Value.Name.Should().Be("Bronze");
        }

        [TestMethod]
        public async Task Remove_ExistingTier_Succeeds()
        {
            _tiers.Setup(t => t.RemoveAsync(BadgeRecipientType.Organization, "Bronze", It.IsAny<CancellationToken>())).ReturnsAsync(true);
            var sut = new RemoveBadgeTierHandler(_tiers.Object, NullLogger<RemoveBadgeTierHandler>.Instance);

            var result = await sut.Handle(new RemoveBadgeTierCommand { RecipientType = BadgeRecipientType.Organization, Name = "Bronze" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
        }

        [TestMethod]
        public async Task Remove_UnknownTier_IsNotFound()
        {
            _tiers.Setup(t => t.RemoveAsync(It.IsAny<BadgeRecipientType>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            var sut = new RemoveBadgeTierHandler(_tiers.Object, NullLogger<RemoveBadgeTierHandler>.Instance);

            var result = await sut.Handle(new RemoveBadgeTierCommand { RecipientType = BadgeRecipientType.Organization, Name = "Bronze" }, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task List_ReturnsTheTiersOfTheRecipientType()
        {
            _tiers.Setup(t => t.ListAsync(BadgeRecipientType.Contributor, It.IsAny<CancellationToken>()))
                .ReturnsAsync([new BadgeTier("Starter", BadgeRecipientType.Contributor, 5)]);
            var sut = new GetBadgeTiersHandler(_tiers.Object);

            var result = await sut.Handle(new GetBadgeTiersQuery { RecipientType = BadgeRecipientType.Contributor }, CancellationToken.None);

            result.Value.Should().ContainSingle().Which.ThresholdHours.Should().Be(5);
        }
    }
}
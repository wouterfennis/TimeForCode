using AwesomeAssertions;
using TimeForCode.Authorization.Application.Interfaces.Admin;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Infrastructure.Services.Admin;

namespace TimeForCode.Authorization.Infrastructure.Tests.Services
{
    [TestClass]
    public class CeremonyStateStoreTests
    {
        private sealed class ManualTime : TimeProvider
        {
            public DateTimeOffset Now { get; set; } = new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);

            public override DateTimeOffset GetUtcNow() => Now;
        }

        private InMemoryCeremonyStateRepository _repository = null!;
        private ManualTime _time = null!;
        private CeremonyStateStore _sut = null!;

        [TestInitialize]
        public void Setup()
        {
            _repository = new InMemoryCeremonyStateRepository();
            _time = new ManualTime();
            var options = Microsoft.Extensions.Options.Options.Create(new AdminPasskeyOptions
            {
                ServerDomain = "localhost",
                RelyingPartyName = "TimeForCode",
                BootstrapSecret = "secret",
                CeremonyLifetimeInSeconds = 120
            });
            _sut = new CeremonyStateStore(_repository, _time, options);
        }

        [TestMethod]
        public async Task StartAsync_Always_StoresOnlyTheHashOfTheIdWithTheKindAndAShortExpiry()
        {
            var id = await _sut.StartAsync(CeremonyKind.Authentication, "the-state");

            var stored = _repository.Entries.Should().ContainSingle().Subject.Value;
            stored.IdHash.Should().Be(RefreshTokenHasher.Hash(id)).And.NotBe(id);
            stored.Kind.Should().Be(CeremonyKind.Authentication);
            stored.State.Should().Be("the-state");
            stored.ExpiresAfter.Should().Be(_time.Now.AddSeconds(120));
        }

        [TestMethod]
        public async Task StartAsync_TwoCeremonies_GetDifferentIds()
        {
            var first = await _sut.StartAsync(CeremonyKind.Registration, "a");
            var second = await _sut.StartAsync(CeremonyKind.Registration, "b");

            second.Should().NotBe(first);
            first.Length.Should().BeGreaterThanOrEqualTo(32);
        }

        [TestMethod]
        public async Task TakeAsync_KnownCeremonyOfTheRightKind_ReturnsItsState()
        {
            var id = await _sut.StartAsync(CeremonyKind.Registration, "the-state");

            var state = await _sut.TakeAsync(CeremonyKind.Registration, id);

            state.Should().Be("the-state");
        }

        [TestMethod]
        public async Task TakeAsync_TheSameCeremonyTwice_ReturnsNothingTheSecondTime()
        {
            var id = await _sut.StartAsync(CeremonyKind.Authentication, "the-state");
            await _sut.TakeAsync(CeremonyKind.Authentication, id);

            var again = await _sut.TakeAsync(CeremonyKind.Authentication, id);

            again.Should().BeNull("a captured ceremony id cannot be replayed");
        }

        [TestMethod]
        public async Task TakeAsync_CeremonyOfAnotherKind_ReturnsNothingAndConsumesIt()
        {
            var id = await _sut.StartAsync(CeremonyKind.Registration, "the-state");

            var state = await _sut.TakeAsync(CeremonyKind.Authentication, id);

            state.Should().BeNull();
            (await _sut.TakeAsync(CeremonyKind.Registration, id)).Should().BeNull();
        }

        [TestMethod]
        public async Task TakeAsync_ExpiredCeremony_ReturnsNothing()
        {
            var id = await _sut.StartAsync(CeremonyKind.Authentication, "the-state");
            _time.Now = _time.Now.AddSeconds(121);

            var state = await _sut.TakeAsync(CeremonyKind.Authentication, id);

            state.Should().BeNull();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("never-issued")]
        public async Task TakeAsync_UnknownCeremony_ReturnsNothing(string? id)
        {
            var state = await _sut.TakeAsync(CeremonyKind.Authentication, id);

            state.Should().BeNull();
        }
    }
}
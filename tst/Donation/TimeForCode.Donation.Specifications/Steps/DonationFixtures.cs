using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Moq;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Specifications.Mocking;
using TimeForCode.Donation.Specifications.TestBuilder;
using ProjectStatus = TimeForCode.Donation.Values.ProjectStatus;

namespace TimeForCode.Donation.Specifications.Steps
{
    /// <summary>Arrange helpers that several step classes need: the in-memory repositories and the projects and contributors of the scenario.</summary>
    internal class DonationFixtures
    {
        public const string OtherUserId = "507f1f77bcf86cd799439021";
        public const string OtherUserLogin = "hubot";
        public const string OtherUserAvatarUrl = "https://avatars.githubusercontent.com/u/480938?v=4";

        public static readonly DateTimeOffset FirstLoggedAt = new(2024, 2, 1, 9, 0, 0, TimeSpan.Zero);

        private readonly DonationContext _context;
        private readonly IServiceProvider _provider;

        public DonationFixtures(DonationContext context, IServiceProvider provider)
        {
            _context = context;
            _provider = provider;
        }

        public InMemoryContributorRepository Contributors => _provider.GetRequiredService<InMemoryContributorRepository>();

        public InMemoryContributionRepository Contributions => _provider.GetRequiredService<InMemoryContributionRepository>();

        public InMemoryDonationRepository Donations => _provider.GetRequiredService<InMemoryDonationRepository>();

        public InMemoryMaintainerRepository Maintainers => _provider.GetRequiredService<InMemoryMaintainerRepository>();

        public Mock<IProjectRepository> ProjectRepository => _provider.GetRequiredService<Mock<IProjectRepository>>();

        public static ObjectId ObjectIdOf(string id)
        {
            return new ObjectId(id);
        }

        public void PublishActiveProject()
        {
            Project project = ProjectBuilder.BuildActive();
            ProjectRepository.Setup(x => x.GetByIdAsync(project.Id.ToString()))
                .ReturnsAsync(project);
            _context.Catalog.Add(project);

            _context.RegisteredProjectId = project.Id.ToString();
        }

        public void EnsureActiveProjectRegistered()
        {
            if (_context.RegisteredProjectId != null || _context.LifecycleProject != null)
            {
                return;
            }

            PublishActiveProject();
        }

        public void SetupLifecycleProject(ProjectStatus status)
        {
            // A second project of the user needs its own id, or it replaces the first one in the catalog
            string? id = _context.KnownProjects.Any(p => p.PublishedByUserId == Constants.TestUserId)
                ? ProjectBuilder.IdFor(200 + _context.KnownProjects.Count)
                : null;
            Project project = ProjectBuilder.Build(status, Constants.TestUserId, id);
            _context.LifecycleProject = project;
            TrackProject(project);

            ProjectRepository.Setup(x => x.GetByIdAsync(project.Id.ToString()))
                .ReturnsAsync(project);
            ProjectRepository.Setup(x => x.UpdateAsync(It.IsAny<Project>()))
                .Returns(Task.CompletedTask);
        }

        public void TrackProject(Project project)
        {
            _context.KnownProjects.Add(project);
            SetupProjectListing();
        }

        public void SetupProjectListing()
        {
            ProjectRepository.Setup(x => x.GetByPublishedByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string userId, CancellationToken _) => (IReadOnlyList<Project>)_context.KnownProjects.Where(p => p.PublishedByUserId == userId).ToList());
        }

        public Contributor TestUserContributor()
        {
            Contributor? existing = Contributors.Contributors.SingleOrDefault(c => c.PlatformUserId == Constants.TestUserId);
            if (existing != null)
            {
                return existing;
            }

            var contributor = Contributor.Create(Constants.TestUserId, Constants.TestUserLogin, Constants.TestUserAvatarUrl);
            Contributors.Add(contributor);
            return contributor;
        }

        /// <summary>Registers the second contributor of the scenario, or returns it when it is already registered.</summary>
        public Contributor OtherUserContributor()
        {
            Contributor? existing = Contributors.Contributors.SingleOrDefault(c => c.PlatformUserId == OtherUserId);
            if (existing != null)
            {
                return existing;
            }

            var other = Contributor.Create(OtherUserId, OtherUserLogin, OtherUserAvatarUrl);
            Contributors.Add(other);
            return other;
        }

        /// <summary>Logs hours of the contributor on the registered project, one day after the previous contribution; the logged contribution becomes the one the correct and delete steps work on.</summary>
        public void LogHours(Contributor contributor, int hours, Uri? githubReference = null)
        {
            DateTimeOffset nextMoment = FirstLoggedAt.AddDays(Contributions.Contributions.Count);
            var contribution = Contribution.Create(ObjectIdOf(_context.RegisteredProjectId!), contributor, hours, githubReference, nextMoment);
            Contributions.CreateAsync(contribution);
            _context.ContributionId = contribution.Id.ToString();
        }
    }
}
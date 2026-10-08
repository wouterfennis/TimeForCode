using MongoDB.Bson;
using TimeForCode.Donation.Domain.Entities;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Domain
{
    public class Project : DocumentEntity
    {
        public required GithubSnapshot Snapshot { get; set; }
        public required Uri GithubRepositoryUrl { get; init; }
        public required ProjectStatus Status { get; set; }
        public required string PublishedByUserId { get; init; }
        public required DateTimeOffset PublishedAt { get; set; }
        public string? ChangeRequestReason { get; set; }

        public static Project Create(
            GithubSnapshot snapshot,
            Uri githubRepositoryUrl,
            string publishedByUserId,
            DateTimeOffset publishedAt)
        {
            return new Project
            {
                Id = ObjectId.GenerateNewId(),
                Snapshot = snapshot,
                GithubRepositoryUrl = githubRepositoryUrl,
                Status = ProjectStatus.Draft,
                PublishedByUserId = publishedByUserId,
                PublishedAt = publishedAt
            };
        }

        public ProjectTransitionResult SubmitForReview()
        {
            if (Status != ProjectStatus.Draft)
            {
                return InvalidTransition("submitted for review", ProjectStatus.Draft);
            }

            Status = ProjectStatus.PendingApproval;
            ChangeRequestReason = null;
            return ProjectTransitionResult.Success();
        }

        public ProjectTransitionResult Approve()
        {
            if (Status != ProjectStatus.PendingApproval)
            {
                return InvalidTransition("approved", ProjectStatus.PendingApproval);
            }

            Status = ProjectStatus.Active;
            return ProjectTransitionResult.Success();
        }

        public ProjectTransitionResult RequestChanges(string reason)
        {
            if (Status != ProjectStatus.PendingApproval)
            {
                return InvalidTransition("sent back for changes", ProjectStatus.PendingApproval);
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return ProjectTransitionResult.Failure("A reason is required when requesting changes.");
            }

            Status = ProjectStatus.Draft;
            ChangeRequestReason = reason;
            return ProjectTransitionResult.Success();
        }

        public ProjectTransitionResult Archive()
        {
            if (Status != ProjectStatus.Active)
            {
                return InvalidTransition("archived", ProjectStatus.Active);
            }

            Status = ProjectStatus.Archived;
            return ProjectTransitionResult.Success();
        }

        public ProjectTransitionResult Reactivate()
        {
            if (Status != ProjectStatus.Archived)
            {
                return InvalidTransition("re-activated", ProjectStatus.Archived);
            }

            Status = ProjectStatus.Active;
            return ProjectTransitionResult.Success();
        }

        public ProjectTransitionResult EnsureCanReceiveDonations()
        {
            return Status == ProjectStatus.Active
                ? ProjectTransitionResult.Success()
                : ProjectTransitionResult.Failure($"A project in state {Status} cannot receive donations; only Active projects can.");
        }

        private ProjectTransitionResult InvalidTransition(string action, ProjectStatus requiredStatus)
        {
            return ProjectTransitionResult.Failure($"A project can only be {action} from state {requiredStatus}, but it is {Status}.");
        }
    }
}
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using TimeForCode.Donation.Application.Tests.Handlers;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Application.Tests.Domain
{
    [TestClass]
    public class ProjectTests
    {
        [TestMethod]
        public void Create_NewProject_StartsAsDraft()
        {
            var project = BuildProject(ProjectStatus.Draft);

            var created = Project.Create(project.Snapshot, project.GithubRepositoryUrl, "user-1", DateTimeOffset.UtcNow);

            created.Status.Should().Be(ProjectStatus.Draft);
        }

        [TestMethod]
        public void SubmitForReview_Draft_MovesToPendingApproval()
        {
            var project = BuildProject(ProjectStatus.Draft);

            var result = project.SubmitForReview();

            result.IsSuccess.Should().BeTrue();
            project.Status.Should().Be(ProjectStatus.PendingApproval);
        }

        [TestMethod]
        public void Approve_PendingApproval_MovesToActive()
        {
            var project = BuildProject(ProjectStatus.PendingApproval);

            var result = project.Approve();

            result.IsSuccess.Should().BeTrue();
            project.Status.Should().Be(ProjectStatus.Active);
        }

        [TestMethod]
        public void RequestChanges_PendingApproval_ReturnsToDraftAndStoresReason()
        {
            var project = BuildProject(ProjectStatus.PendingApproval);

            var result = project.RequestChanges("Please add a description");

            result.IsSuccess.Should().BeTrue();
            project.Status.Should().Be(ProjectStatus.Draft);
            project.ChangeRequestReason.Should().Be("Please add a description");
        }

        [TestMethod]
        public void RequestChanges_EmptyReason_IsRejected()
        {
            var project = BuildProject(ProjectStatus.PendingApproval);

            var result = project.RequestChanges(" ");

            result.IsSuccess.Should().BeFalse();
            project.Status.Should().Be(ProjectStatus.PendingApproval);
        }

        [TestMethod]
        public void SubmitForReview_AfterChangesRequested_ClearsReason()
        {
            var project = BuildProject(ProjectStatus.PendingApproval);
            project.RequestChanges("Fix it");

            project.SubmitForReview();

            project.ChangeRequestReason.Should().BeNull();
        }

        [TestMethod]
        public void Archive_Active_MovesToArchived()
        {
            var project = BuildProject(ProjectStatus.Active);

            var result = project.Archive();

            result.IsSuccess.Should().BeTrue();
            project.Status.Should().Be(ProjectStatus.Archived);
        }

        [TestMethod]
        public void Reactivate_Archived_MovesToActive()
        {
            var project = BuildProject(ProjectStatus.Archived);

            var result = project.Reactivate();

            result.IsSuccess.Should().BeTrue();
            project.Status.Should().Be(ProjectStatus.Active);
        }

        [TestMethod]
        [DataRow(ProjectStatus.PendingApproval)]
        [DataRow(ProjectStatus.Active)]
        [DataRow(ProjectStatus.Archived)]
        public void SubmitForReview_NotDraft_IsRejected(ProjectStatus status)
        {
            var project = BuildProject(status);

            var result = project.SubmitForReview();

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Draft");
            project.Status.Should().Be(status);
        }

        [TestMethod]
        [DataRow(ProjectStatus.Draft)]
        [DataRow(ProjectStatus.Active)]
        [DataRow(ProjectStatus.Archived)]
        public void Approve_NotPendingApproval_IsRejected(ProjectStatus status)
        {
            var project = BuildProject(status);

            var result = project.Approve();

            result.IsSuccess.Should().BeFalse();
            project.Status.Should().Be(status);
        }

        [TestMethod]
        [DataRow(ProjectStatus.Draft)]
        [DataRow(ProjectStatus.Active)]
        [DataRow(ProjectStatus.Archived)]
        public void RequestChanges_NotPendingApproval_IsRejected(ProjectStatus status)
        {
            var project = BuildProject(status);

            var result = project.RequestChanges("reason");

            result.IsSuccess.Should().BeFalse();
            project.Status.Should().Be(status);
        }

        [TestMethod]
        [DataRow(ProjectStatus.Draft)]
        [DataRow(ProjectStatus.PendingApproval)]
        [DataRow(ProjectStatus.Archived)]
        public void Archive_NotActive_IsRejected(ProjectStatus status)
        {
            var project = BuildProject(status);

            var result = project.Archive();

            result.IsSuccess.Should().BeFalse();
            project.Status.Should().Be(status);
        }

        [TestMethod]
        [DataRow(ProjectStatus.Draft)]
        [DataRow(ProjectStatus.PendingApproval)]
        [DataRow(ProjectStatus.Active)]
        public void Reactivate_NotArchived_IsRejected(ProjectStatus status)
        {
            var project = BuildProject(status);

            var result = project.Reactivate();

            result.IsSuccess.Should().BeFalse();
            project.Status.Should().Be(status);
        }

        [TestMethod]
        public void EnsureCanReceiveDonations_Active_Succeeds()
        {
            BuildProject(ProjectStatus.Active).EnsureCanReceiveDonations().IsSuccess.Should().BeTrue();
        }

        [TestMethod]
        [DataRow(ProjectStatus.Draft)]
        [DataRow(ProjectStatus.PendingApproval)]
        [DataRow(ProjectStatus.Archived)]
        public void EnsureCanReceiveDonations_NotActive_IsRejected(ProjectStatus status)
        {
            BuildProject(status).EnsureCanReceiveDonations().IsSuccess.Should().BeFalse();
        }

        [TestMethod]
        [DataRow(nameof(Project.Status))]
        [DataRow(nameof(Project.ChangeRequestReason))]
        [DataRow(nameof(Project.Snapshot))]
        [DataRow(nameof(Project.PublishedAt))]
        public void LifecycleProperty_FromOutsideTheAggregate_CannotBeAssigned(string propertyName)
        {
            var setter = typeof(Project).GetProperty(propertyName)!.SetMethod;

            (setter == null || !setter.IsPublic || setter.ReturnParameter.GetRequiredCustomModifiers().Length > 0)
                .Should().BeTrue($"{propertyName} may only change through the aggregate's own methods");
        }

        [TestMethod]
        public void BsonRoundTrip_ProjectAfterTransitions_KeepsStatusAndChangeRequestReason()
        {
            var project = BuildProject(ProjectStatus.Draft);
            project.SubmitForReview();
            project.RequestChanges("Add a README");

            var restored = BsonSerializer.Deserialize<Project>(project.ToBson());

            restored.Status.Should().Be(ProjectStatus.Draft);
            restored.ChangeRequestReason.Should().Be("Add a README");
        }

        private static Project BuildProject(ProjectStatus status)
        {
            return new Project
            {
                Id = new MongoDB.Bson.ObjectId("5f43a0e74b12c84f1b000001"),
                Snapshot = new GithubSnapshot
                {
                    Name = "test-repo",
                    FullName = "owner/test-repo",
                    HtmlUrl = "https://github.com/owner/test-repo",
                    Topics = [],
                    DefaultBranch = "main",
                    OwnerLogin = "owner",
                    OwnerAvatarUrl = "https://avatars.githubusercontent.com/u/1",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    PushedAt = DateTimeOffset.UtcNow
                },
                GithubRepositoryUrl = new Uri("https://github.com/owner/test-repo"),
                PublishedByUserId = "user-1",
                PublishedAt = DateTimeOffset.UtcNow
            }.InStatus(status);
        }
    }
}
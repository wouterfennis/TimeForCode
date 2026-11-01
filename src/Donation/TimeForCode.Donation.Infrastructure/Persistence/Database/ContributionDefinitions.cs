using MongoDB.Bson;
using MongoDB.Driver;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Infrastructure.Persistence.Database
{
    /// <summary>
    /// Filter and update definitions of the contribution collection.
    /// </summary>
    internal static class ContributionDefinitions
    {
        /// <summary>
        /// The non-deleted contributions of a project. A missing <c>DeletedAt</c> matches too, so documents saved before deleting existed stay visible.
        /// </summary>
        internal static FilterDefinition<Contribution> ProjectFilter(ObjectId projectId)
        {
            return Builders<Contribution>.Filter.And(
                Builders<Contribution>.Filter.Eq(c => c.ProjectId, projectId),
                Builders<Contribution>.Filter.Eq(c => c.DeletedAt, null));
        }

        /// <summary>
        /// The non-deleted contributions logged by any of the contributors.
        /// </summary>
        internal static FilterDefinition<Contribution> ContributorsFilter(IReadOnlyCollection<ObjectId> contributorIds)
        {
            return Builders<Contribution>.Filter.And(
                Builders<Contribution>.Filter.In(c => c.ContributorId, contributorIds),
                Builders<Contribution>.Filter.Eq(c => c.DeletedAt, null));
        }

        internal static FilterDefinition<Contribution> LiveContributionFilter(ObjectId projectId, ObjectId contributionId)
        {
            return Builders<Contribution>.Filter.And(
                Builders<Contribution>.Filter.Eq(c => c.Id, contributionId),
                ProjectFilter(projectId));
        }

        /// <summary>
        /// Only the version the caller loaded may be changed, so a stale write cannot overwrite a newer correction or lose a history entry.
        /// </summary>
        internal static FilterDefinition<Contribution> VersionFilter(Contribution contribution)
        {
            return Builders<Contribution>.Filter.And(
                Builders<Contribution>.Filter.Eq(c => c.Id, contribution.Id),
                Builders<Contribution>.Filter.Eq(c => c.Version, contribution.Version));
        }

        /// <summary>
        /// Saves what a correction or deletion changes and leaves the rest alone, so the anonymised contributor snapshot is never overwritten.
        /// </summary>
        internal static UpdateDefinition<Contribution> Changes(Contribution contribution)
        {
            return Builders<Contribution>.Update
                .Set(c => c.Hours, contribution.Hours)
                .Set(c => c.GithubReference, contribution.GithubReference)
                .Set(c => c.History, contribution.History)
                .Set(c => c.DeletedAt, contribution.DeletedAt)
                .Set(c => c.DeletedByUserId, contribution.DeletedByUserId)
                .Inc(c => c.Version, 1);
        }
    }
}
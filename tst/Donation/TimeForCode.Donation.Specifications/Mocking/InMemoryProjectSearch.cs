using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Specifications.Mocking
{
    /// <summary>
    /// In-memory stand-in for the repository search; mirrors the documented semantics of the Mongo query.
    /// </summary>
    internal static class InMemoryProjectSearch
    {
        internal static (IReadOnlyList<Project> Projects, int TotalCount) Execute(
            IEnumerable<Project> catalog,
            ProjectSearchCriteria criteria)
        {
            var matches = catalog
                .Where(p => p.Status == ProjectStatus.Active)
                .Where(p => criteria.SearchText == null || ContainsText(p, criteria.SearchText))
                .Where(p => criteria.Language == null || p.Snapshot.Language == criteria.Language)
                .Where(p => criteria.Tag == null || p.Snapshot.Topics.Contains(criteria.Tag))
                .OrderByDescending(p => p.PublishedAt)
                .ThenByDescending(p => p.Id)
                .ToList();

            var page = matches
                .Skip((criteria.PageNumber - 1) * criteria.PageSize)
                .Take(criteria.PageSize)
                .ToList();

            return (page, matches.Count);
        }

        private static bool ContainsText(Project project, string text)
        {
            return Contains(project.Snapshot.Name, text)
                || Contains(project.Snapshot.FullName, text)
                || Contains(project.Snapshot.Description, text);
        }

        private static bool Contains(string? value, string text)
        {
            return value != null && value.Contains(text, StringComparison.OrdinalIgnoreCase);
        }
    }
}
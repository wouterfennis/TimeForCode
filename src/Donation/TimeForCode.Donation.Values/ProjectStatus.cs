namespace TimeForCode.Donation.Values
{
    /// <summary>
    /// Lifecycle state of a project. The numeric values are persisted, so existing values must not change:
    /// <c>Active</c> and <c>Archived</c> keep the values of the former <c>Published</c> and <c>Archived</c> states.
    /// </summary>
    public enum ProjectStatus
    {
        /// <summary>The project is published and visible to everyone.</summary>
        Active = 0,

        /// <summary>The project was archived by its maintainer and is no longer listed.</summary>
        Archived = 1,

        /// <summary>The project is being prepared by its maintainer and is not visible to others.</summary>
        Draft = 2,

        /// <summary>The maintainer submitted the project and an administrator has not reviewed it yet.</summary>
        PendingApproval = 3
    }
}
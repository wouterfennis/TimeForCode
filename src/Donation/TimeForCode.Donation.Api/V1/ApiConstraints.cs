namespace TimeForCode.Donation.Api.V1
{
    /// <summary>
    /// Input limits shared by the Donation API; declared as validation attributes so they appear in the OpenAPI document.
    /// </summary>
    internal static class ApiConstraints
    {
        internal const int MaxPageNumber = 10_000;
        internal const int MaxPageSize = 100;
        internal const int MaxSearchCriterionLength = 100;
        internal const int MaxOrganizationNameLength = 200;
        internal const int MaxEmailLength = 254;
        internal const int MaxUrlLength = 2048;
        internal const int MaxReasonLength = 1000;
        internal const int MaxContributionHours = 10_000;
        internal const string ObjectIdPattern = "^[0-9a-fA-F]{24}$";

        // Names and reasons must contain at least one non-whitespace character; the match must span the whole value.
        internal const string NotBlankPattern = @"[\s\S]*\S[\s\S]*";
    }
}
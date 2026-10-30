using System.Globalization;
using System.Text;
using System.Text.Json;
using TimeForCode.Reporting.Domain;
using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Application.Exports
{
    /// <summary>
    /// The contribution history rendered as a file: its content, media type and extension.
    /// </summary>
    internal record FormattedExport(string Content, string ContentType, string FileExtension);

    /// <summary>
    /// Renders the contribution history in the documented export schema: columns ContributionId, ProjectId, ContributorLogin, Hours,
    /// GithubReference, LoggedAt (CSV) and the same values as camel case fields in a <c>contributions</c> list (JSON). Times are UTC.
    /// </summary>
    internal static class ContributionHistoryFormatter
    {
        private static readonly string[] CsvColumns = ["ContributionId", "ProjectId", "ContributorLogin", "Hours", "GithubReference", "LoggedAt"];

        private static readonly char[] SpreadsheetFormulaStarts = ['=', '+', '-', '@', '\t', '\r'];

        internal static FormattedExport Format(IEnumerable<OrganizationContribution> contributions, ExportFormat format)
        {
            return format == ExportFormat.Csv ? ToCsv(contributions) : ToJson(contributions);
        }

        private static FormattedExport ToCsv(IEnumerable<OrganizationContribution> contributions)
        {
            var csv = new StringBuilder();
            csv.Append(string.Join(',', CsvColumns)).Append("\r\n");

            foreach (OrganizationContribution c in contributions)
            {
                csv.Append(string.Join(',',
                        CsvField(c.Id),
                        CsvField(c.ProjectId),
                        CsvField(c.ContributorLogin),
                        c.Hours.ToString(CultureInfo.InvariantCulture),
                        CsvField(c.GithubReference ?? string.Empty),
                        CsvField(Timestamp(c.LoggedAt))))
                    .Append("\r\n");
            }

            return new FormattedExport(csv.ToString(), "text/csv", "csv");
        }

        private static FormattedExport ToJson(IEnumerable<OrganizationContribution> contributions)
        {
            var document = new
            {
                contributions = contributions.Select(c => new
                {
                    id = c.Id,
                    projectId = c.ProjectId,
                    contributorLogin = c.ContributorLogin,
                    hours = c.Hours,
                    githubReference = c.GithubReference,
                    loggedAt = c.LoggedAt.ToUniversalTime()
                })
            };

            return new FormattedExport(JsonSerializer.Serialize(document), "application/json", "json");
        }

        private static string Timestamp(DateTimeOffset value) =>
            value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

        /// <summary>
        /// A value a spreadsheet would read as a formula gets a leading quote (OWASP CSV injection guidance); values with a separator,
        /// quote or line break are quoted with doubled quotes (RFC 4180).
        /// </summary>
        private static string CsvField(string value)
        {
            if (value.Length > 0 && SpreadsheetFormulaStarts.Contains(value[0]))
            {
                value = "'" + value;
            }

            return value.IndexOfAny([',', '"', '\r', '\n']) >= 0
                ? "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\""
                : value;
        }
    }
}
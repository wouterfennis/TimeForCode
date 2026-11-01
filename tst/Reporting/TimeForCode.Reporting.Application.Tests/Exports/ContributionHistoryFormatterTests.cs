using AwesomeAssertions;
using System.Text.Json;
using TimeForCode.Reporting.Application.Exports;
using TimeForCode.Reporting.Domain;
using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Application.Tests.Exports
{
    [TestClass]
    public class ContributionHistoryFormatterTests
    {
        private const string CsvHeader = "ContributionId,ProjectId,ContributorLogin,Hours,GithubReference,LoggedAt";

        private static OrganizationContribution Contribution(string login = "octocat", string? reference = null, int hours = 4) =>
            new("507f1f77bcf86cd799439031", "507f1f77bcf86cd799439021", login, hours, reference, new DateTimeOffset(2026, 10, 2, 9, 30, 15, TimeSpan.FromHours(2)));

        private static string[] CsvLines(FormattedExport export) =>
            export.Content.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        [TestMethod]
        public void Format_CsvWithoutContributions_ReturnsOnlyTheHeader()
        {
            var export = ContributionHistoryFormatter.Format([], ExportFormat.Csv);

            CsvLines(export).Should().Equal(CsvHeader);
        }

        [TestMethod]
        public void Format_CsvContribution_WritesOneRowInTheDocumentedColumnOrder()
        {
            var export = ContributionHistoryFormatter.Format([Contribution(reference: "https://github.com/acme/app/pull/7")], ExportFormat.Csv);

            CsvLines(export).Should().Equal(
                CsvHeader,
                "507f1f77bcf86cd799439031,507f1f77bcf86cd799439021,octocat,4,https://github.com/acme/app/pull/7,2026-10-02T07:30:15Z");
        }

        [TestMethod]
        public void Format_CsvContributionWithoutReference_LeavesTheColumnEmpty()
        {
            var export = ContributionHistoryFormatter.Format([Contribution()], ExportFormat.Csv);

            CsvLines(export)[1].Should().Be("507f1f77bcf86cd799439031,507f1f77bcf86cd799439021,octocat,4,,2026-10-02T07:30:15Z");
        }

        [TestMethod]
        public void Format_CsvValueWithComma_QuotesTheValue()
        {
            var export = ContributionHistoryFormatter.Format([Contribution(reference: "https://example.org/a,b")], ExportFormat.Csv);

            CsvLines(export)[1].Should().Contain(",\"https://example.org/a,b\",");
        }

        [TestMethod]
        public void Format_CsvValueWithQuote_DoublesTheQuote()
        {
            var export = ContributionHistoryFormatter.Format([Contribution(login: "say \"hi\"")], ExportFormat.Csv);

            CsvLines(export)[1].Should().Contain(",\"say \"\"hi\"\"\",");
        }

        [TestMethod]
        [DataRow("=SUM(A1)")]
        [DataRow("+1")]
        [DataRow("-1")]
        [DataRow("@cmd")]
        public void Format_CsvValueStartingWithAFormulaCharacter_PrefixesAQuoteSoSpreadsheetsDoNotRunIt(string value)
        {
            var export = ContributionHistoryFormatter.Format([Contribution(login: value)], ExportFormat.Csv);

            CsvLines(export)[1].Should().Contain($",'{value},");
        }

        [TestMethod]
        [DataRow(" =SUM(A1)", "' =SUM(A1)")]
        [DataRow("\n=SUM(A1)", "\"'\n=SUM(A1)\"")]
        public void Format_CsvValueWithAFormulaBehindLeadingWhitespace_PrefixesAQuoteToo(string value, string expectedCell)
        {
            var export = ContributionHistoryFormatter.Format([Contribution(login: value)], ExportFormat.Csv);

            export.Content.Should().Contain("," + expectedCell + ",");
        }

        [TestMethod]
        public void Format_Csv_IsATextCsvFile()
        {
            var export = ContributionHistoryFormatter.Format([], ExportFormat.Csv);

            export.ContentType.Should().Be("text/csv");
            export.FileExtension.Should().Be("csv");
        }

        [TestMethod]
        public void Format_Json_WritesTheContributionsWithTheDocumentedFields()
        {
            var export = ContributionHistoryFormatter.Format([Contribution(reference: "https://github.com/acme/app/pull/7")], ExportFormat.Json);

            JsonElement item = JsonDocument.Parse(export.Content).RootElement.GetProperty("contributions").EnumerateArray().Single();
            item.EnumerateObject().Select(p => p.Name).Should().Equal("id", "projectId", "contributorLogin", "hours", "githubReference", "loggedAt");
            item.GetProperty("id").GetString().Should().Be("507f1f77bcf86cd799439031");
            item.GetProperty("hours").GetInt32().Should().Be(4);
            item.GetProperty("githubReference").GetString().Should().Be("https://github.com/acme/app/pull/7");
            item.GetProperty("loggedAt").GetDateTimeOffset().Should().Be(new DateTimeOffset(2026, 10, 2, 7, 30, 15, TimeSpan.Zero));
        }

        [TestMethod]
        public void Format_JsonContributionWithoutReference_WritesNull()
        {
            var export = ContributionHistoryFormatter.Format([Contribution()], ExportFormat.Json);

            JsonDocument.Parse(export.Content).RootElement.GetProperty("contributions")[0].GetProperty("githubReference").ValueKind
                .Should().Be(JsonValueKind.Null);
        }

        [TestMethod]
        public void Format_JsonWithoutContributions_WritesAnEmptyList()
        {
            var export = ContributionHistoryFormatter.Format([], ExportFormat.Json);

            JsonDocument.Parse(export.Content).RootElement.GetProperty("contributions").GetArrayLength().Should().Be(0);
        }

        [TestMethod]
        public void Format_Json_IsAnApplicationJsonFile()
        {
            var export = ContributionHistoryFormatter.Format([], ExportFormat.Json);

            export.ContentType.Should().Be("application/json");
            export.FileExtension.Should().Be("json");
        }
    }
}
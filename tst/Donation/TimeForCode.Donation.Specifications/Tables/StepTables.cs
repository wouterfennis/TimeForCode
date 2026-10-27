using AwesomeAssertions;
using Reqnroll;
using Reqnroll.Assist;

namespace TimeForCode.Donation.Specifications.Tables
{
    /// <summary>Maps the single-row data tables of the table-driven steps onto a DTO.</summary>
    internal static class StepTables
    {
        /// <summary>
        /// Maps the only row of the table onto <typeparamref name="T"/>; a column left out leaves its property at the default (use nullable properties).
        /// Fails on a column without a matching property, so a misspelled header cannot pass silently.
        /// </summary>
        public static T SingleRow<T>(DataTable table)
            where T : class, new()
        {
            var properties = typeof(T).GetProperties().Select(p => p.Name);
            table.Header.Select(h => h.Replace(" ", string.Empty)).Except(properties, StringComparer.OrdinalIgnoreCase)
                .Should().BeEmpty("every column of the table must be one the step understands");
            table.Rows.Should().ContainSingle("the step reads one row");

            return table.CreateInstance<T>();
        }
    }
}
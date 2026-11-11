using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Diagnostics.CodeAnalysis;
using TimeForCode.Reporting.Infrastructure.Options;

namespace TimeForCode.Reporting.Infrastructure.Persistence
{
    /// <summary>
    /// The Reporting database. Created on first use rather than at host start, because the build step that generates the OpenAPI
    /// document starts the host without a database address.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Database connection")]
    internal class ReportingMongoContext
    {
        private readonly IMongoDatabase _database;

        public ReportingMongoContext(IOptions<DbOptions> options)
        {
            DbOptions value = options.Value;
            if (string.IsNullOrWhiteSpace(value.ConnectionString) || string.IsNullOrWhiteSpace(value.DatabaseName))
            {
                throw new InvalidOperationException($"{DbOptions.SectionName}:{nameof(DbOptions.ConnectionString)} and {DbOptions.SectionName}:{nameof(DbOptions.DatabaseName)} must be set.");
            }

            _database = new MongoClient(value.ConnectionString).GetDatabase(value.DatabaseName);
        }

        public IMongoCollection<T> GetCollection<T>(string name) => _database.GetCollection<T>(name);
    }
}
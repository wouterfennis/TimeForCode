using System.Diagnostics.CodeAnalysis;
using TimeForCode.Shared.Api.Hosting;

namespace TimeForCode.Authorization.Api
{
    /// <summary>
    /// Starting point of the Api.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Application entrypoint")]
    internal static class Program
    {
        /// <summary>
        /// Starting point of the Api.
        /// </summary>
        /// <returns>0 if application terminates gracefully, 1 if an unexpected exception occured.</returns>
        public static int Main(string[] args) => ApiHost.Run<Startup>(args);
    }
}
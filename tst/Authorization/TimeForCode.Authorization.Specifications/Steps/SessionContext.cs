namespace TimeForCode.Authorization.Specifications.Steps
{
    /// <summary>
    /// Per-scenario state shared between step classes (Reqnroll context injection).
    /// </summary>
    internal class SessionContext
    {
        public string UserId { get; set; } = "id";

        public HttpResponseMessage? ReadOnlyRequestResponse { get; set; }
    }
}
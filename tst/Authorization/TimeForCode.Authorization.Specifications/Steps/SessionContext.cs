namespace TimeForCode.Authorization.Specifications.Steps
{
    /// <summary>
    /// Per-scenario state shared between step classes (Reqnroll context injection).
    /// </summary>
    internal class SessionContext
    {
        public string UserId { get; set; } = MongoDB.Bson.ObjectId.Empty.ToString();

        public HttpResponseMessage? ReadOnlyRequestResponse { get; set; }

        /// <summary>
        /// The error of the last call that failed, for the steps that check what the user is told.
        /// </summary>
        public Api.Client.ApiException? LastException { get; set; }
    }
}
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

        /// <summary>
        /// The raw response of the last call that was made without the generated client.
        /// </summary>
        public HttpResponseMessage? LastResponse { get; set; }

        /// <summary>
        /// The status code of the last failed call or raw response, whichever the scenario produced.
        /// </summary>
        public int? LastStatusCode => LastException?.StatusCode ?? (int?)LastResponse?.StatusCode;
    }
}
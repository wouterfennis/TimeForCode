namespace TimeForCode.Donation.Domain
{
    /// <summary>
    /// Outcome of a project lifecycle rule check or transition.
    /// </summary>
    public sealed class ProjectTransitionResult
    {
        private ProjectTransitionResult(bool isSuccess, string? errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public bool IsSuccess { get; }
        public string ErrorMessage { get; }

        public static ProjectTransitionResult Success() => new(true, null);

        public static ProjectTransitionResult Failure(string errorMessage) => new(false, errorMessage);
    }
}
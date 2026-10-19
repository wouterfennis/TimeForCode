namespace TimeForCode.Donation.Domain
{
    /// <summary>
    /// Outcome of a donation lifecycle transition.
    /// </summary>
    public sealed class DonationTransitionResult
    {
        private DonationTransitionResult(bool isSuccess, string? errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public bool IsSuccess { get; }
        public string ErrorMessage { get; }

        public static DonationTransitionResult Success() => new(true, null);

        public static DonationTransitionResult Failure(string errorMessage) => new(false, errorMessage);
    }
}
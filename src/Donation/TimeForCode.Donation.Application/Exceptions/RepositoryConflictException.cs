namespace TimeForCode.Donation.Application.Exceptions
{
    public class RepositoryConflictException : Exception
    {
        public RepositoryConflictException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
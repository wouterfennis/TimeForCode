namespace TimeForCode.Seeder
{
    /// <summary>A failure the operator can act on; the message is printed as is, without a stack trace.</summary>
    public sealed class SeederException : Exception
    {
        public SeederException(string message)
            : base(message)
        {
        }

        public SeederException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
namespace TimeForCode.Authorization.Application.Interfaces
{
    public class AccountInformation
    {
        public required int Id { get; init; }
        public required string Login { get; init; }
        public required string NodeId { get; init; }
        public required string AvatarUrl { get; init; }
        public required string Name { get; init; }
        public required string? Company { get; init; }
        public required string Email { get; init; }
    }
}
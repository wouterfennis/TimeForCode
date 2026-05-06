namespace TimeForCode.Authorization.Application.Models
{
    public class RepositoryInfo
    {
        public required string Name { get; init; }
        public string? Description { get; init; }
        public int StarCount { get; init; }
        public string? Language { get; init; }
        public required string Url { get; init; }
    }
}
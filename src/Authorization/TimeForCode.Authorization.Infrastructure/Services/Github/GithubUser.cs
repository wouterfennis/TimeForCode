using MongoDB.Bson;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Infrastructure.Services.Github
{
    internal class GithubUser
    {
        public required uint Id { get; init; }
        public required string Login { get; init; }
        public required string NodeId { get; init; }
        public required string AvatarUrl { get; init; }
        public required string Name { get; init; }
        public required string? Company { get; init; }
        public required string Email { get; init; }

        internal AccountInformation MapToAccountInformation()
        {
            return new AccountInformation
            {
                Id = ObjectId.GenerateNewId(),
                IdentityProviderId = Id.ToString(),
                Login = Login,
                NodeId = NodeId,
                AvatarUrl = AvatarUrl,
                Name = Name,
                Company = Company,
                Email = Email
            };
        }
    }
}
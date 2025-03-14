using System.ComponentModel.DataAnnotations;

namespace TimeForCode.Authorization.Application.Options
{
    public class ExternalIdentityProvider
    {
        [Required]
        public required string LoginHost { get; init; }

        public int? LoginHostPort { get; init; }

        [Required]
        public required string AccessTokenHost { get; init; }

        public int? AccessTokenHostPort { get; init; }

        [Required]
        public required string ClientId { get; init; }

        [Required]
        public required string ClientSecret { get; init; }

        [Required]
        public required string RestApiHost { get; init; }

        public int? RestApiPort { get; init; }

        public required string? MetaDataAddress { get; init; }

        public string? Issuer { get; set; }
    }
}
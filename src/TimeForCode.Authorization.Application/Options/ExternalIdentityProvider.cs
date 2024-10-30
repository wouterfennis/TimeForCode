using System.ComponentModel.DataAnnotations;

namespace TimeForCode.Authorization.Application.Options
{
    public class ExternalIdentityProvider
    {
        [Required]
        public required string Host { get; init; }

        public int? HostPort { get; init; }

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
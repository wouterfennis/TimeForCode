using MongoDB.Bson;

namespace TimeForCode.Authorization.Domain.Entities
{
    /// <summary>
    /// The single, permanently-claimed WebAuthn passkey credential for the admin role.
    /// Own file, own MongoDB collection — no shared base beyond <see cref="DocumentEntity"/>.
    /// </summary>
    public class AdminCredential : DocumentEntity
    {
        /// <summary>
        /// Fixed document id. Every registration attempt writes to this same id so MongoDB's unique
        /// _id constraint atomically arbitrates the "first-claim" race between concurrent registrations.
        /// </summary>
        public static readonly ObjectId SingletonId = ObjectId.Parse("000000000000000000000001");

        public required byte[] CredentialId { get; init; }
        public required byte[] PublicKey { get; init; }
        public required long SignCount { get; set; }
        public required string[] Transports { get; init; }
        public required bool IsUserVerified { get; init; }
        public required bool IsBackupEligible { get; init; }
        public required bool IsBackedUp { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }
    }
}

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TimeForCode.Authorization.Domain.Entities
{
    /// <summary>
    /// Which passkey ceremony a state belongs to. A state of one kind cannot complete the other.
    /// </summary>
    public enum CeremonyKind
    {
        Registration = 0,
        Authentication = 1
    }

    /// <summary>
    /// The server-side state of one passkey ceremony in progress (the challenge the browser must sign). It is used exactly once:
    /// taking it deletes it, so a captured ceremony id and assertion cannot be replayed. Only the hash of the ceremony id is stored.
    /// </summary>
    public class CeremonyState : DocumentEntity
    {
        public required string IdHash { get; init; }

        public required CeremonyKind Kind { get; init; }

        /// <summary>
        /// The (encrypted) state of the passkey library for this ceremony.
        /// </summary>
        public required string State { get; init; }

        [BsonRepresentation(BsonType.DateTime)]
        public required DateTimeOffset ExpiresAfter { get; init; }

        public bool IsExpired(DateTimeOffset referenceDateTime) => referenceDateTime > ExpiresAfter;
    }
}
# ADR-015 — Keep OAuth login state in MongoDB with a TTL index, single-use and PKCE-protected

**Date**: 2026-10-16
**Status**: Accepted

## Context

The OAuth login state (the `state` value and the redirect target) was held in process memory. With more than one instance, or after a restart, a callback could not be matched to its login, and a state could be replayed.

## Decision

Each login stores a state record in the MongoDB collection `StateEntry`: a random key, the identity provider, the redirect URI, a PKCE code verifier and an expiry. The key and the verifier come from `RandomNumberGenerator`. A unique index on the key and a TTL index on the expiry (`ExpireAfter = 0`) let MongoDB remove expired records. The callback consumes the record with an atomic `FindOneAndDelete`, so a state works exactly once, on any instance. The authorize request carries the PKCE `code_challenge` (S256) and the token exchange carries the `code_verifier`. A state record lives 10 minutes. Related: [ADR-002](0002-use-mongodb.md).

## Consequences

**Positive**: Login works across instances and restarts, a replayed or forged state is rejected, and an intercepted authorization code is useless without the verifier.

**Negative**: The code verifier is stored in plain text for at most 10 minutes. A failed token exchange consumes the state, so the user has to log in again. The database is touched twice per login. TTL deletion by MongoDB is not instantaneous, so the expiry is also checked when the record is read.

# ADR-013 — Do not store GitHub access tokens; read public data with application credentials

**Date**: 2026-10-15
**Status**: Accepted

## Context

The login requested GitHub scope `user` (profile write access and email) and stored the resulting OAuth-app token, which never expires, encrypted in the database. It was used only to read public repository data.

## Decision

The login requests no scope (public profile access only) and the GitHub token is discarded after the profile fetch. Public repository data (`GET user/repositories` and `GET user/repositories/{owner}/{repo}`) is read with the application's own GitHub client id and secret (HTTP Basic, only over HTTPS). Tokens stored by earlier versions are removed at startup. Related: [ADR-001](0001-use-internal-jwt.md).

## Consequences

**Positive**: A database leak exposes no GitHub credential, and the platform holds no more GitHub access than it needs.

**Negative**: The email is only known when it is public on GitHub. The list is limited to one page of 100 owned public repositories. Whether the application's rate limit suffices at scale is not verified against real GitHub.

# ADR-004 — Use MediatR for command dispatch

**Date**: 2025
**Status**: Accepted

## Context

The Application layer needs a way to dispatch commands and decouple the API layer from the implementation of business logic without introducing a full event-driven architecture.

## Decision

MediatR is used for in-process command dispatch. Each operation is modelled as a command (e.g. `LoginCommand`) handled by a corresponding handler. No event sourcing or external messaging is used at this stage.

## Consequences

**Positive**: Clean separation between intent (command) and execution (handler). Handlers are independently testable. Easy to add cross-cutting concerns (logging, validation) as pipeline behaviours.

**Negative**: In-process only; does not support distributed scenarios. If distributed commands are needed in future, MediatR would need to be complemented with a message broker.

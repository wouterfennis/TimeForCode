---
paths:
  - "deploy/**"
  - "Dockerfile.*"
  - "docker-compose*.y*ml"
---

# Infrastructure rules

- Treat IaC as production-affecting: never run `az`, `azd`, `bicep` deployments or `podman`/`docker` commands that push, delete or prune without explicit user approval.
- Never write secrets, tokens or connection strings into Bicep parameters, compose files or Dockerfiles; use Key Vault references or env files that are gitignored (`.env.real-github` is the precedent).
- Keep `docs/current/deployment-status.md` and arc42 chapter 07 in step with any change here.
- Prefer a `what-if` / config validation (`bicep build`) to prove a change before asking for review.

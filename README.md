# TimeForCode

[![Continuous Integration](https://github.com/wouterfennis/TimeForCode/actions/workflows/build.yaml/badge.svg)](https://github.com/wouterfennis/TimeForCode/actions/workflows/build.yaml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=wouterfennis_TimeForCode&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=wouterfennis_TimeForCode)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=wouterfennis_TimeForCode&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=wouterfennis_TimeForCode)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=wouterfennis_TimeForCode&metric=coverage)](https://sonarcloud.io/summary/new_code?id=wouterfennis_TimeForCode)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=wouterfennis_TimeForCode&metric=vulnerabilities)](https://sonarcloud.io/summary/new_code?id=wouterfennis_TimeForCode)

**TimeForCode** is a community platform that connects companies and individuals willing to donate developer time to open-source projects. Organizations pledge hours, open-source projects list their needs, and the platform handles matchmaking, tracking, and impact reporting.

> Full documentation lives in [docs/](docs/index.md).

---

## Table of Contents

- [About the Project](#about-the-project)
- [Current Status](#current-status)
- [Tech Stack](#tech-stack)
- [Getting Started](#getting-started)
- [Contributing](#contributing)
- [License](#license)
- [Contact](#contact)

---

## About the Project

TimeForCode makes it easy for companies to give back to open source in a structured, transparent way. Rather than ad-hoc contributions, organizations register their available developer hours, choose open-source projects to support, and contributors log time against those donations. Projects get the help they need; companies get a transparent record of their open-source investment.

This repository contains:

- **Authorization API** — OAuth 2.0 login via GitHub, JWT issuance and refresh.
- **Donation API** — Project registration, donation management, and contribution tracking.
- **Website** — Blazor frontend for donors, contributors, and project maintainers.
- **Infrastructure** — Azure Bicep deployment templates and Docker Compose local setup.
- **Documentation** — Architecture (Arc42), product requirements, and implementation guides.

---

## Current Status

The platform is under active development. The following areas are production-ready or near-complete:

- GitHub OAuth 2.0 authentication with internal JWT issuance and refresh.
- User account creation and linking to external identity providers.

The following areas are in progress:

- Project registration and the donation lifecycle.
- Contributor hour allocation and time tracking.
- Matchmaking between donated hours and project needs.
- Reporting and impact dashboards.

See [docs/current/capability-status.md](docs/current/capability-status.md) for the full implementation status.

---

## Tech Stack

| Concern | Technology |
| --- | --- |
| Frontend | Blazor (.NET) |
| Backend | ASP.NET Core (.NET) |
| Database | MongoDB |
| Authentication | OAuth 2.0 (GitHub), JWT (RSA-signed) |
| Containerisation | Podman, Docker Compose |
| Infrastructure | Azure App Service, Azure Bicep (IaC) |
| CI/CD | GitHub Actions |
| Code quality | SonarCloud |
| API documentation | Swagger / OpenAPI |

---

## Getting Started

### Prerequisites

- [Podman](https://podman.io/docs/installation) 4.x or later (Docker Desktop is not used in this project)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8) (for running tests and local development outside containers)

> **Windows**: After installing Podman, open a new terminal and initialise the Podman machine once:
>
> ```powershell
> podman machine init
> podman machine start
> ```

### Run locally

```powershell
podman compose up --build
```

| Service | URL |
| --- | --- |
| Website | <http://localhost:8083> |
| Authorization API | <http://localhost:8080> |
| Donation API | <http://localhost:8082> |
| Identity Provider Mock | <http://localhost:8081> |

### Verify the stack

A smoke-test script checks every service and walks through the full authentication flow:

```powershell
.\scripts\smoke-test.ps1
```

All 17 checks should report `[PASS]`. The script exits with code `0` on success.

See [docs/current/deployment-status.md](docs/current/deployment-status.md) for environment details and configuration.

---

## Contributing

Contributions are welcome. Please read [CONTRIBUTING.md](CONTRIBUTING.md) and the [Code of Conduct](CODE_OF_CONDUCT.md) before submitting a pull request.

Before submitting:

- Ensure all automated checks and tests pass.
- Document any new functionality or API changes in the relevant `docs/` file.
- Any architectural change must update the relevant [Arc42 section](docs/architecture/arc42/README.md).

---

## License

Distributed under the MIT License. See [LICENSE](LICENSE) for more information.

---

## Contact

Wouter Fennis — [@wouterfennis](https://github.com/wouterfennis)

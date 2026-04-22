# Deployment Status

Status: Current

This document describes how the platform is deployed today, both locally and in the cloud.

---

## Local Development

The full platform runs locally via Docker Compose. The compose file at the repository root starts all services.

```powershell
docker compose up --build
```

### Services and Ports

```mermaid
graph LR
    Browser["Browser"]
    Website["Website\n:8083"]
    AuthAPI["Authorization API\n:8080"]
    DonationAPI["Donation API\n:8082"]
    IdpMock["Identity Provider Mock\n:8081"]
    Mongo[("MongoDB\n:27017")]

    Browser --> Website
    Browser --> IdpMock
    Website --> AuthAPI
    Website --> DonationAPI
    AuthAPI --> IdpMock
    AuthAPI --> Mongo
    DonationAPI --> Mongo
```

The Identity Provider Mock replaces GitHub in local development. The Authorization API is configured to direct OAuth 2.0 requests to the mock instead of `github.com`.

| Service | URL | Notes |
| --- | --- | --- |
| Website | <http://localhost:8083> | Blazor frontend |
| Authorization API | <http://localhost:8080> | OAuth 2.0 + JWT |
| Identity Provider Mock | <http://localhost:8081> | Simulates GitHub OAuth |
| Donation API | <http://localhost:8082> | Project and donation management |
| MongoDB | mongodb://localhost:27017 | Shared database instance |

### Configuration

All services are configured via environment variables defined in `docker-compose.yaml`. Key configuration points:

- **Token signing**: RSA key pair provided as a base64-encoded PKCS#12 certificate via `RsaOptions__Base64Certificate`.
- **Token lifetime**: Access token expires in 60 minutes; refresh token in 7 days.
- **Redirect URIs**: The Authorization API validates redirect URIs against an allowlist.
- **Database**: Each service connects to MongoDB using a connection string from `DbOptions__ConnectionString`.

> Credentials and secrets in `docker-compose.yaml` are for local development only. Do not use them in any other environment.

---

## Cloud Deployment (Azure)

The production environment runs on Microsoft Azure using Azure App Service with container-based deployment.

### Infrastructure

```mermaid
graph LR
    subgraph Azure Subscription
        subgraph Resource Group — App
            AuthAppService["Authorization API\nApp Service"]
        end
        subgraph Resource Group — Infrastructure
            AppServicePlan["App Service Plan"]
        end
    end

    GitHub["GitHub Container Registry\nor ACR"] -->|container image| AuthAppService
    AppServicePlan --> AuthAppService
```

The infrastructure is defined as code using Azure Bicep templates in the `deploy/` folder.

| Template | Purpose |
| --- | --- |
| `deploy/application.bicep` | Top-level orchestration; references modules |
| `deploy/applicationInfrastructure.bicep` | Shared infrastructure (App Service Plan) |
| `deploy/modules/appService.bicep` | App Service definition and settings |
| `deploy/modules/appServicePlan.bicep` | App Service Plan definition |
| `deploy/modules/appServiceSidecar.bicep` | Sidecar container support |

### Deployment Region

- Primary region: West Europe.

### Known Gaps

| Gap | Notes |
| --- | --- |
| Donation API not deployed | Only the Authorization API is currently deployed to Azure |
| Website not deployed to Azure | Only runs locally |
| MongoDB managed service | Local compose uses a dev MongoDB; production database provisioning not yet defined in Bicep |
| No staging environment | Only production is defined |
| No secrets management | Secrets are passed as App Settings; Key Vault integration is not yet configured |

See [Arc42 Section 07 — Deployment View](../architecture/arc42/07-deployment-view.md) for the target deployment architecture.

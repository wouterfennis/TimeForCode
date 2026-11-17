# Arc42 Section 7 - Deployment View

Status: Mixed

This section describes how the system is deployed in its target and current environments.

Published views of the [LikeC4 architecture model](../likec4/README.md): [production deployment](https://wouterfennis.github.io/TimeForCode/architecture-model/#/view/production/), [local development](https://wouterfennis.github.io/TimeForCode/architecture-model/#/view/local/), [target production deployment](https://wouterfennis.github.io/TimeForCode/architecture-model/#/view/target/) and the [delivery pipeline](https://wouterfennis.github.io/TimeForCode/architecture-model/#/view/delivery/).

---

## Local Development

All services run in Docker containers orchestrated by Docker Compose. The Identity Provider Mock replaces GitHub for local OAuth flows.

```mermaid
graph LR
    subgraph Developer Machine
        subgraph Docker Compose Network
            Frontend["Frontend BFF + SPA\n:8084"]
            AuthAPI["Authorization API\n:8080"]
            IdpMock["Identity Provider Mock\n:8081"]
            DonationAPI["Donation API\n:8082"]
            ReportingAPI["Reporting API\n:8083"]
            MongoDB[("MongoDB\n:27017")]
        end
    end

    Browser["Browser"] -->|HTTP :8084| Frontend
    Browser -->|HTTP :8080| AuthAPI
    Browser -->|HTTP :8082| DonationAPI
    Browser -->|HTTP :8083| ReportingAPI
    ReportingAPI -->|HTTP, caller's token| DonationAPI
    AuthAPI -->|HTTP :8081| IdpMock
    DonationAPI -->|HTTP :8081| IdpMock
    Frontend -->|/api/auth/v1/user* and code exchange, refresh, logout| AuthAPI
    Frontend -->|/api/donation/*| DonationAPI
    AuthAPI -->|MongoDB wire protocol| MongoDB
    Frontend -->|sessions, MongoDB wire protocol| MongoDB
    DonationAPI -->|MongoDB wire protocol| MongoDB
```

To start:

```powershell
podman compose up --build
```

---

## Target Cloud Deployment (Azure)

No cloud deployment definition exists at present; it is being rewritten. The full platform should eventually run on Azure with each service as an independent App Service container, a managed MongoDB instance, and secrets stored in Azure Key Vault.

```mermaid
graph LR
    subgraph Azure Subscription
        subgraph Resource Group - Shared Infrastructure
            AppServicePlan["App Service Plan"]
            KeyVault["Azure Key Vault\n(secrets)"]
            CosmosDB[("Azure Cosmos DB\nfor MongoDB\n(managed)")]
        end
        subgraph Resource Group - Application
            FrontendApp["Frontend SPA\nApp Service"]
            AuthApp["Authorization API\nApp Service"]
            DonationApp["Donation API\nApp Service"]
        end
    end

    ACR["Azure Container Registry"] -->|image| FrontendApp
    ACR -->|image| AuthApp
    ACR -->|image| DonationApp
    AppServicePlan -->|hosts| FrontendApp
    AppServicePlan -->|hosts| AuthApp
    AppServicePlan -->|hosts| DonationApp
    AuthApp -->|reads secrets| KeyVault
    DonationApp -->|reads secrets| KeyVault
    AuthApp -->|persistence| CosmosDB
    DonationApp -->|persistence| CosmosDB
```

### Target Infrastructure Requirements

| Component | Technology | Notes |
| --- | --- | --- |
| Frontend | Azure App Service (container) | The BFF serving the SPA, holding the browser session (MongoDB) and forwarding the API routes (one container, `Dockerfile.frontend`) |
| Authorization API | Azure App Service (container) | Not yet deployed |
| Donation API | Azure App Service (container) | Not yet deployed |
| Reporting API | Azure App Service (container) | Not yet deployed; needs the Donation API address (`DonationApiOptions__BaseUri`), the token settings and its own MongoDB database (`DbOptions__ConnectionString`, `DbOptions__DatabaseName`) for badge tiers and awards |
| Database | Azure Cosmos DB for MongoDB API | Drop-in replacement for MongoDB driver |
| Secrets | Azure Key Vault | Client secrets, RSA key, connection strings |
| Container registry | Azure Container Registry | Build and store images via CI/CD |
| TLS | Azure App Service managed certificates | Automatic HTTPS |

---

## Environment Summary

| Environment | Services | Notes |
| --- | --- | --- |
| Local (`docker-compose`) | All services + IdP Mock + MongoDB | Dev only; HTTP; uses mock identity provider |
| Target Production | All services | Deployment definition not yet written |

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
            Frontend["Frontend SPA\nnginx :8084"]
            AuthAPI["Authorization API\n:8080"]
            IdpMock["Identity Provider Mock\n:8081"]
            DonationAPI["Donation API\n:8082"]
            MongoDB[("MongoDB\n:27017")]
        end
    end

    Browser["Browser"] -->|HTTP :8084| Frontend
    Browser -->|HTTP :8080| AuthAPI
    Browser -->|HTTP :8082| DonationAPI
    AuthAPI -->|HTTP :8081| IdpMock
    DonationAPI -->|HTTP :8081| IdpMock
    AuthAPI -->|MongoDB wire protocol| MongoDB
    DonationAPI -->|MongoDB wire protocol| MongoDB
```

To start:

```powershell
podman compose up --build
```

---

## Current Cloud Deployment (Azure)

Only the Authorization API is currently deployed to Azure. The Bicep templates in `deploy/` define the infrastructure.

```mermaid
graph LR
    subgraph Azure Subscription
        subgraph Resource Group - Infrastructure
            AppServicePlan["App Service Plan\n(shared)"]
        end
        subgraph Resource Group - Application
            AuthAppService["Authorization API\nAzure App Service"]
        end
    end

    ContainerRegistry["Container Registry\n(image source)"] -->|pull image| AuthAppService
    AppServicePlan -->|hosts| AuthAppService
```

> **Current**: Only the Authorization API has a Bicep definition. The Donation API and Frontend are not yet configured for Azure deployment.

---

## Target Cloud Deployment (Azure)

The full platform should eventually run on Azure with each service as an independent App Service container, a managed MongoDB instance, and secrets stored in Azure Key Vault.

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
| Frontend | Azure App Service (container) | Static SPA assets served by nginx |
| Authorization API | Azure App Service (container) | Current deployment extended |
| Donation API | Azure App Service (container) | Not yet deployed |
| Database | Azure Cosmos DB for MongoDB API | Drop-in replacement for MongoDB driver |
| Secrets | Azure Key Vault | Client secrets, RSA key, connection strings |
| Container registry | Azure Container Registry | Build and store images via CI/CD |
| TLS | Azure App Service managed certificates | Automatic HTTPS |

---

## Environment Summary

| Environment | Services | Notes |
| --- | --- | --- |
| Local (`docker-compose`) | All services + IdP Mock + MongoDB | Dev only; HTTP; uses mock identity provider |
| Production (Azure) | Authorization API | Only service currently deployed |
| Target Production | All services | Full deployment not yet defined in Bicep |

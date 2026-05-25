# Arc42 Section 7 - Deployment View

Status: Mixed

This section describes how the system is deployed in its target and current environments.

---

## Local Development

All services run in Docker containers orchestrated by Docker Compose. The Identity Provider Mock replaces GitHub for local OAuth flows.

```mermaid
graph LR
    subgraph Developer Machine
        subgraph Docker Compose Network
            Website["Website\nBlazor :8083"]
            AuthAPI["Authorization API\n:8080"]
            IdpMock["Identity Provider Mock\n:8081"]
            DonationAPI["Donation API\n:8082"]
            MongoDB[("MongoDB\n:27017")]
        end
    end

    Browser["Browser"] -->|HTTP :8083| Website
    Website -->|HTTP :8080| AuthAPI
    Website -->|HTTP :8082| DonationAPI
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

> **Current**: Only the Authorization API has a Bicep definition. The Donation API and Website are not yet configured for Azure deployment.

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
            WebsiteApp["Website\nApp Service"]
            AuthApp["Authorization API\nApp Service"]
            DonationApp["Donation API\nApp Service"]
        end
    end

    ACR["Azure Container Registry"] -->|image| WebsiteApp
    ACR -->|image| AuthApp
    ACR -->|image| DonationApp
    AppServicePlan -->|hosts| WebsiteApp
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
| Website | Azure App Service (container) | Static assets + Blazor server |
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

targetScope = 'subscription'
param location string = 'WestEurope'

var parameters = loadJsonContent('parameters.json')

resource rgAuthApp 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: parameters.application.resourceGroup
  location: location
}

module apiAppServiceModule 'modules/appService.bicep' = {
  scope: rgAuthApp
  name: 'apiAppServiceModule'
  params: {
    location: location
    appServicePlanName: parameters.applicationInfrastructure.appServicePlan.name
    appServicePlanResourceGroup: parameters.applicationInfrastructure.resourceGroup
  	appServiceName: parameters.application.authorizationApi.appServiceName
  	imageName: 'ghcr.io/wouterfennis/timeforcode/timeforcode-authorization-api:latest'
    additionalAppSettings: [
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: 'Development'
      }
      {
        name: 'ASPNETCORE_URLS'
        value: 'http://+:80'
      }
      {
        name: 'Logging__LogLevel__Default'
        value: 'Debug'
      }
      {
        name: 'Logging__LogLevel__Microsoft.AspNetCore'
        value: 'Warning'
      }
      {
        name: 'ExternalIdentityProviderOptions__CallbackUri'
        value: 'https://timeforcode-auth-api.azurewebsites.net/api/authentication/callback'
      }
      {
        name: 'ExternalIdentityProviderOptions__Github__LoginHost'
        value: 'localhost'
      }
      {
        name: 'ExternalIdentityProviderOptions__Github__AccessTokenHost'
        value: 'identity-provider-mock'
      }
      {
        name: 'ExternalIdentityProviderOptions__Github__RestApiHost'
        value: 'identity-provider-mock'
      }
      {
        name: 'ExternalIdentityProviderOptions__Github__ClientId'
        value: 'test-client'
      }
      {
        name: 'ExternalIdentityProviderOptions__Github__ClientSecret'
        value: 'test-secret'
      }
      {
        name: 'ExternalIdentityProviderOptions__Github__Issuer'
        value: 'http://localhost:8081'
      }
      {
        name: 'RefreshTokenOptions__DefaultExpirationAfterInDays'
        value: '7'
      }
      {
        name: 'AuthenticationOptions__Issuer'
        value: 'https://timeforcode-auth-api.azurewebsites.net'
      }
      {
        name: 'AuthenticationOptions__Audience'
        value: 'https://timeforcode-website.azurewebsites.net'
      }
      {
        name: 'AuthenticationOptions__TokenExpiresInMinutes'
        value: '60'
      }
      {
        name: 'AuthenticationOptions__DefaultRefreshTokenExpirationAfterInDays'
        value: '7'
      }
      {
        name: 'RsaOptions__Base64Certificate'
        value: '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'
      }
      {
        name: 'DbOptions__ConnectionString'
        value: 'mongodb://root:example@mongo:27017/?authSource=admin'
      }
      {
        name: 'DbOptions__DatabaseName'
        value: 'AuthorizationServer'
      }
    ]
  }
}

module websiteAppServiceModule 'modules/appService.bicep' = {
  scope: rgAuthApp
  name: 'websiteAppServiceModule'
  params: {
    location: location
    appServicePlanName: parameters.applicationInfrastructure.appServicePlan.name
    appServicePlanResourceGroup: parameters.applicationInfrastructure.resourceGroup
  	appServiceName: parameters.application.timeForCodeWebsite.appServiceName
  	imageName: 'ghcr.io/wouterfennis/timeforcode/timeforcode-website:latest'
    additionalAppSettings: [
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: 'Development'
      }
      {
        name: 'ASPNETCORE_URLS'
        value: 'http://+:80'
      }
      {
        name: 'Logging__LogLevel__Default'
        value: 'Debug'
      }
      {
        name: 'Logging__LogLevel__Microsoft.AspNetCore'
        value: 'Warning'
      }
      {
        name: 'AuthorizationServiceOptions__BaseUri'
        value: 'https://timeforcode-auth-api.azurewebsites.net'
      }
      {
        name: 'StorageOptions__StoragePath'
        value: '/storage'
      }
    ]
  }
}

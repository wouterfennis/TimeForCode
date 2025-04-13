@description('The name of the App Service. It should be between 3 and 24 characters and can contain only alphanumeric characters and hyphens.')
param appServiceName string
param imageName string
@description('Additional app settings to configure for the App Service.')
param additionalEnvironmentVariables array = []

resource sidecarContainer 'Microsoft.Web/sites/sitecontainers@2024-04-01' = {
  name: '${appServiceName}/sitecontainers'
  properties: {
    image: imageName
    targetPort: '27017'
    isMain: false
    authType: 'Anonymous'
    volumeMounts: []
    environmentVariables: union([
    ], additionalEnvironmentVariables)
  }
}

@description('The name of the App Service. It should be between 3 and 24 characters and can contain only alphanumeric characters and hyphens.')
param appServiceName string
param sidecarName string
param imageName string
param port string
@description('Additional app settings to configure for the App Service.')
param additionalEnvironmentVariables array = []

resource sidecarContainer 'Microsoft.Web/sites/sitecontainers@2024-04-01' = {
  name: '${appServiceName}/${sidecarName}'
  properties: {
    image: imageName
    targetPort: port
    isMain: false
    authType: 'Anonymous'
    environmentVariables: union([
    ], additionalEnvironmentVariables)
  }
}

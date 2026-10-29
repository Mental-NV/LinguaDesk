targetScope = 'resourceGroup'
@allowed(['Staging', 'Production'])
param environment string
@minValue(1)
@maxValue(50)
param dailyCapMb int = 50
@secure()
@minLength(12)
param providerKey string
param enabled bool = false
// Activation changes only the app; never re-PUT monitoring resources after cap verification.
param reconcileMonitoring bool = true
var configuration = loadJsonContent('./environments.json')[environment]
resource insights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: configuration.insightsName
}
module monitoring 'modules/monitoring.bicep' = if (reconcileMonitoring) {
  name: 'monitoring'
  params: {
    workspaceName: configuration.workspaceName
    insightsName: configuration.insightsName
    location: configuration.location
    dailyCapMb: dailyCapMb
  }
}
module app 'modules/app-service.bicep' = {
  name: 'app-service'
  params: {
    name: configuration.appName
    planName: configuration.planName
    location: configuration.location
    environment: environment
    providerKey: providerKey
    // The module output establishes the creation dependency. An existing-resource
    // reference is evaluated too early on a fresh environment, even with dependsOn.
    monitoringConnection: reconcileMonitoring ? monitoring!.outputs.connectionString : insights.properties.ConnectionString
    enabled: enabled
  }
}
output appName string = app.outputs.appName
output hostname string = app.outputs.hostname
output workspaceName string = configuration.workspaceName
output insightsName string = insights.name
output dailyCapMb int = dailyCapMb

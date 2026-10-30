param workspaceName string
param insightsName string
param location string
@minValue(1)
@maxValue(50)
param dailyCapMb int
resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: workspaceName
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
    // Exact decimal: Bicep does not have floating point literals.
    workspaceCapping: { dailyQuotaGb: json('0.${padLeft(string(dailyCapMb), 3, '0')}') }
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}
resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: insightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: workspace.id
    IngestionMode: 'LogAnalytics'
    DisableLocalAuth: false
  }
}
output connectionString string = insights.properties.ConnectionString
output workspaceName string = workspace.name
output insightsName string = insights.name

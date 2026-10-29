param principalId string
// Identities live in the same RG as their app. This role deliberately grants no
// ManagedIdentity or Authorization writes, so CI cannot rewrite its own OIDC trust.
resource runtimeDeployer 'Microsoft.Authorization/roleDefinitions@2022-04-01' = {
  name: guid(resourceGroup().id, 'LinguaDesk.RuntimeDeployer')
  properties: {
    roleName: 'LinguaDesk Runtime Deployer (${resourceGroup().name})'
    description: 'Manage only LinguaDesk runtime resources and monitoring; no identity, federation or RBAC management.'
    type: 'CustomRole'
    assignableScopes: [resourceGroup().id]
    permissions: [{
      actions: [
        'Microsoft.Resources/subscriptions/resourceGroups/read'
        'Microsoft.Resources/deployments/*'
        'Microsoft.Web/serverfarms/*'
        'Microsoft.Web/sites/*'
        'Microsoft.Insights/components/*'
        'Microsoft.OperationalInsights/workspaces/*'
      ]
      notActions: []
      dataActions: []
      notDataActions: []
    }]
  }
}
resource assignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, principalId, runtimeDeployer.id)
  properties: {
    principalId: principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: runtimeDeployer.id
  }
}

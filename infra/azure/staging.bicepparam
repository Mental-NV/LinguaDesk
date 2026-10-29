using './main.bicep'
param environment = 'Staging'
param dailyCapMb = 50
param providerKey = readEnvironmentVariable('DEEPSEEK_API_KEY')

$resource_group = 'rga7d6f5h38a'
$bicep			= './deploy/basic.bicep'

# Create a new Resource Group
az group create -l westeurope -n $resource_group

# Deploy resources inside the Resource Group
$cmd = "az deployment group create --mode Incremental --resource-group $resource_group --template-file $bicep"

Write-Host $cmd
Invoke-Expression  $cmd

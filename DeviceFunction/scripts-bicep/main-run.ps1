New-AzResourceGroupDeployment `
    -name "Deployment-DeviceOfflineDetection" `
    -ResourceGroupName "rg-DeviceOfflineDetection" `
    -TemplateFile "main.bicep" `
    -TemplateParameterFile "main.parameters.json"

# This command does not work in powershell
# az deployment group create --name Deployment-DeviceOfflineDetection --resource-group rg-DeviceOfflineDetection --template-file main.bicep --parameters @main.parameters.json
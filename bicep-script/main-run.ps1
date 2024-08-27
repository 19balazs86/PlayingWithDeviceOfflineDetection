New-AzResourceGroupDeployment `
    -name "Deployment-Devices" `
    -ResourceGroupName "rg-DeviceOfflineDetection" `
    -TemplateFile "main.bicep" `
    -TemplateParameterFile "main.parameters.json"
# Azure Configuration Retrieval Script
# This script retrieves all necessary configuration values for the Social Graph API

param(
    [Parameter(Mandatory=$false)]
    [string]$Environment = "dev"
)

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Azure Configuration Retrieval" -ForegroundColor Cyan
Write-Host "Environment: $Environment" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$cosmosAccountName = "stunsy-socialgraph"
$resourceGroup = "rg-stunsy-devdb"
$appServiceName = "stunsy-socialgraph-api-dev"

# Check if logged in to Azure
Write-Host "Checking Azure CLI login status..." -ForegroundColor Yellow
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "Not logged in to Azure. Running 'az login'..." -ForegroundColor Red
    az login
    $account = az account show | ConvertFrom-Json
}

Write-Host "Logged in as: $($account.user.name)" -ForegroundColor Green
Write-Host "Subscription: $($account.name)" -ForegroundColor Green
Write-Host ""

# Get Cosmos DB Account Details
Write-Host "Retrieving Cosmos DB configuration..." -ForegroundColor Yellow
$cosmosAccount = az cosmosdb show `
    --name $cosmosAccountName `
    --resource-group $resourceGroup `
    | ConvertFrom-Json

# Get Cosmos DB Keys
$cosmosKeys = az cosmosdb keys list `
    --name $cosmosAccountName `
    --resource-group $resourceGroup `
    --type keys `
    | ConvertFrom-Json

# Get Gremlin Endpoint
$gremlinEndpoint = $cosmosAccount.documentEndpoint -replace "https://", "" -replace "/", ""
$gremlinHostname = $gremlinEndpoint -replace ".documents.", ".gremlin."

# List Gremlin Databases
Write-Host "Retrieving Gremlin databases..." -ForegroundColor Yellow
$databases = az cosmosdb gremlin database list `
    --account-name $cosmosAccountName `
    --resource-group $resourceGroup `
    | ConvertFrom-Json

# Get first database (or you can specify)
$databaseName = $databases[0].name

# List Graphs in the database
$graphs = az cosmosdb gremlin graph list `
    --account-name $cosmosAccountName `
    --resource-group $resourceGroup `
    --database-name $databaseName `
    | ConvertFrom-Json

# Get first graph (or you can specify)
$graphName = $graphs[0].name

# Get App Service Details (for Profile API URL)
Write-Host "Retrieving App Service configuration..." -ForegroundColor Yellow
$appService = az webapp show `
    --name $appServiceName `
    --resource-group "rg-stunsy-dev" `
    2>$null | ConvertFrom-Json

if ($appService) {
    $appServiceUrl = "https://$($appService.defaultHostName)"
} else {
    $appServiceUrl = "https://$appServiceName.azurewebsites.net"
}

# Display Results
Write-Host ""
Write-Host "======================================" -ForegroundColor Green
Write-Host "CONFIGURATION VALUES" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green
Write-Host ""

$config = @{
    "GREMLIN_HOSTNAME" = $gremlinHostname
    "GREMLIN_PORT" = "443"
    "GREMLIN_DATABASE" = $databaseName
    "GREMLIN_CONTAINER" = $graphName
    "GREMLIN_AUTHKEY" = $cosmosKeys.primaryMasterKey
    "GREMLIN_AUTHKEY_SECONDARY" = $cosmosKeys.secondaryMasterKey
    "JWT_SECRET" = "<GENERATE_SECURE_32_CHAR_STRING>"
    "JWT_ISSUER" = "stunsy.com"
    "PROFILE_API_BASE_URL" = $appServiceUrl
}

# Display in table format
Write-Host "GitHub Environment Variables (development):" -ForegroundColor Cyan
Write-Host ""
$config.GetEnumerator() | Sort-Object Name | ForEach-Object {
    $type = if ($_.Key -like "*KEY*" -or $_.Key -like "*SECRET*") { "Secret" } else { "Variable" }
    $displayValue = if ($type -eq "Secret" -and $_.Key -ne "JWT_SECRET") { 
        $_.Value.Substring(0, 10) + "..." + $_.Value.Substring($_.Value.Length - 10)
    } else { 
        $_.Value 
    }
    
    Write-Host ("{0,-30} | {1,-10} | {2}" -f $_.Key, $type, $displayValue) -ForegroundColor $(if ($type -eq "Secret") { "Yellow" } else { "White" })
}

Write-Host ""
Write-Host "======================================" -ForegroundColor Green
Write-Host "EXPORT COMMANDS" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green
Write-Host ""
Write-Host "Copy these to appsettings.Development.json or GitHub Secrets:" -ForegroundColor Cyan
Write-Host ""

foreach ($key in $config.Keys | Sort-Object) {
    Write-Host "`"$key`": `"$($config[$key])`""
}

# Export to JSON file
Write-Host ""
Write-Host "Exporting to config-$Environment.json..." -ForegroundColor Yellow
$config | ConvertTo-Json | Out-File -FilePath "$PSScriptRoot\config-$Environment.json" -Encoding UTF8
Write-Host "Configuration saved to: $PSScriptRoot\config-$Environment.json" -ForegroundColor Green

Write-Host ""
Write-Host "======================================" -ForegroundColor Green
Write-Host "RESOURCE INFORMATION" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green
Write-Host "Cosmos DB Account: $cosmosAccountName" -ForegroundColor White
Write-Host "Resource Group: $resourceGroup" -ForegroundColor White
Write-Host "Subscription: $($account.name)" -ForegroundColor White
Write-Host "Subscription ID: $($account.id)" -ForegroundColor White
Write-Host "Location: $($cosmosAccount.location)" -ForegroundColor White
Write-Host "URI: $($cosmosAccount.documentEndpoint)" -ForegroundColor White
Write-Host ""

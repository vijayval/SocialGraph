# Script to create Azure Service Principal for GitHub Actions

param(
    [Parameter(Mandatory=$false)]
    [string]$AppName = "github-actions-socialgraph-dev",
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroup = "rg-stunsy-dev"
)

Write-Host "Creating Azure Service Principal for GitHub Actions..." -ForegroundColor Cyan
Write-Host ""

# Get subscription ID
$subscriptionId = (az account show --query id -o tsv)
$subscriptionName = (az account show --query name -o tsv)

Write-Host "Subscription: $subscriptionName" -ForegroundColor Green
Write-Host "Subscription ID: $subscriptionId" -ForegroundColor Green
Write-Host "Resource Group: $ResourceGroup" -ForegroundColor Green
Write-Host ""

# Create service principal with contributor role scoped to resource group
Write-Host "Creating service principal..." -ForegroundColor Yellow
$sp = az ad sp create-for-rbac `
    --name $AppName `
    --role Contributor `
    --scopes "/subscriptions/$subscriptionId/resourceGroups/$ResourceGroup" `
    --sdk-auth `
    | ConvertFrom-Json

Write-Host ""
Write-Host "======================================" -ForegroundColor Green
Write-Host "SERVICE PRINCIPAL CREATED" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green
Write-Host ""

# Format the credentials JSON
$credentials = @{
    clientId = $sp.clientId
    clientSecret = $sp.clientSecret
    subscriptionId = $sp.subscriptionId
    tenantId = $sp.tenantId
} | ConvertTo-Json

Write-Host "GitHub Secret Name: AZURE_CREDENTIALS" -ForegroundColor Cyan
Write-Host ""
Write-Host "GitHub Secret Value (copy this entire JSON):" -ForegroundColor Yellow
Write-Host ""
Write-Host $credentials -ForegroundColor White
Write-Host ""

Write-Host "======================================" -ForegroundColor Green
Write-Host "NEXT STEPS" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green
Write-Host "1. Go to GitHub Repository → Settings → Secrets and variables → Actions" -ForegroundColor White
Write-Host "2. Click 'New repository secret'" -ForegroundColor White
Write-Host "3. Name: AZURE_CREDENTIALS" -ForegroundColor White
Write-Host "4. Value: Copy the JSON above" -ForegroundColor White
Write-Host "5. Click 'Add secret'" -ForegroundColor White
Write-Host ""

# Save to file
$credentials | Out-File -FilePath "$PSScriptRoot\azure-credentials.json" -Encoding UTF8
Write-Host "Credentials also saved to: $PSScriptRoot\azure-credentials.json" -ForegroundColor Yellow
Write-Host "WARNING: Delete this file after adding to GitHub!" -ForegroundColor Red
Write-Host ""

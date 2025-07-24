# PowerShell deployment script for Minesweeper application
param(
    [string]$GitRepositoryUrl = "https://github.com/jordjudd/minesweeper.git",
    
    [string]$StackName = "minesweeper-stack",
    [string]$Region = "us-east-1",
    [string]$GitBranch = "main"
)

Write-Host "Deploying Minesweeper application..." -ForegroundColor Green

# Check if AWS CLI is installed
try {
    aws --version | Out-Null
} catch {
    Write-Error "AWS CLI is not installed or not in PATH. Please install AWS CLI first."
    exit 1
}

# Create source bundle
Write-Host "Creating source bundle..." -ForegroundColor Yellow
if (Test-Path "source.zip") {
    Remove-Item "source.zip"
}

# Create zip file with all necessary files
$files = @(
    "*.cs", "*.csproj", "*.cshtml", "Views/**/*", "Controllers/**/*", 
    "Models/**/*", "wwwroot/**/*", "appsettings*.json"
)

# Use PowerShell to create zip (alternative to 7zip)
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zipPath = Join-Path (Get-Location) "source.zip"
$tempDir = Join-Path $env:TEMP "minesweeper-deploy"

if (Test-Path $tempDir) {
    Remove-Item $tempDir -Recurse -Force
}
New-Item -ItemType Directory -Path $tempDir | Out-Null

# Copy files to temp directory
Get-ChildItem -Path . -Include @("*.cs", "*.csproj") -Recurse | Copy-Item -Destination $tempDir
if (Test-Path "Views") { Copy-Item "Views" -Destination $tempDir -Recurse }
if (Test-Path "Controllers") { Copy-Item "Controllers" -Destination $tempDir -Recurse }
if (Test-Path "Models") { Copy-Item "Models" -Destination $tempDir -Recurse }
if (Test-Path "wwwroot") { Copy-Item "wwwroot" -Destination $tempDir -Recurse }

[System.IO.Compression.ZipFile]::CreateFromDirectory($tempDir, $zipPath)
Remove-Item $tempDir -Recurse -Force

Write-Host "Source bundle created: source.zip" -ForegroundColor Green

# Deploy CloudFormation stack
Write-Host "Deploying CloudFormation stack..." -ForegroundColor Yellow

$parameters = @(
    "ParameterKey=GitRepositoryUrl,ParameterValue=$GitRepositoryUrl",
    "ParameterKey=GitBranch,ParameterValue=$GitBranch"
)

aws cloudformation deploy `
    --template-file cloudformation-template.yaml `
    --stack-name $StackName `
    --parameter-overrides $parameters `
    --capabilities CAPABILITY_IAM `
    --region $Region

if ($LASTEXITCODE -eq 0) {
    Write-Host "Deployment successful!" -ForegroundColor Green
    
    # Get the application URL
    $url = aws cloudformation describe-stacks `
        --stack-name $StackName `
        --region $Region `
        --query "Stacks[0].Outputs[?OutputKey=='ApplicationURL'].OutputValue" `
        --output text
    
    Write-Host "Application URL: $url" -ForegroundColor Cyan
} else {
    Write-Error "Deployment failed. Check AWS CloudFormation console for details."
}

Write-Host "Deployment script completed." -ForegroundColor Green
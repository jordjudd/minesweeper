# PowerShell deployment script for Minesweeper application
param(
    [string]$GitRepositoryUrl = "https://github.com/jordjudd/minesweeper.git",
    
    [string]$StackName = "minesweeper-stack",
    [string]$Region = "us-east-1",
    [string]$GitBranch = "main",
    [string]$SolutionStack = ""
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

# Check for available solution stacks if not specified
if ([string]::IsNullOrEmpty($SolutionStack)) {
    Write-Host "Finding available .NET solution stacks..." -ForegroundColor Yellow
    try {
        $availableStacks = aws elasticbeanstalk list-available-solution-stacks --region $Region --query "SolutionStacks[?contains(@, '.NET') || contains(@, 'dotnet')]" --output text
        if ($availableStacks) {
            $stackArray = $availableStacks -split "`n" | Where-Object { $_ -match "\.NET" }
            if ($stackArray.Count -gt 0) {
                $SolutionStack = $stackArray[0].Trim()
                Write-Host "Using solution stack: $SolutionStack" -ForegroundColor Green
            }
        }
    } catch {
        Write-Warning "Could not auto-detect solution stack. Using default."
    }
}

# Deploy CloudFormation stack first
Write-Host "Deploying CloudFormation stack..." -ForegroundColor Yellow

$parameters = @(
    "ParameterKey=GitRepositoryUrl,ParameterValue=$GitRepositoryUrl",
    "ParameterKey=GitBranch,ParameterValue=$GitBranch"
)

if (![string]::IsNullOrEmpty($SolutionStack)) {
    $parameters += "ParameterKey=SolutionStackName,ParameterValue=$SolutionStack"
}

aws cloudformation deploy `
    --template-file cloudformation-template.yaml `
    --stack-name $StackName `
    --parameter-overrides $parameters `
    --capabilities CAPABILITY_IAM `
    --region $Region

if ($LASTEXITCODE -ne 0) {
    Write-Error "CloudFormation deployment failed. Check AWS CloudFormation console for details."
    exit 1
}

Write-Host "CloudFormation stack deployed successfully!" -ForegroundColor Green

# Get the S3 bucket name from CloudFormation outputs
Write-Host "Getting S3 bucket name..." -ForegroundColor Yellow
$bucketName = aws cloudformation describe-stacks `
    --stack-name $StackName `
    --region $Region `
    --query "Stacks[0].Outputs[?OutputKey=='SourceBucket'].OutputValue" `
    --output text

if ([string]::IsNullOrEmpty($bucketName)) {
    Write-Error "Could not retrieve S3 bucket name from CloudFormation stack."
    exit 1
}

Write-Host "S3 Bucket: $bucketName" -ForegroundColor Green

# Upload source bundle to S3
Write-Host "Uploading source bundle to S3..." -ForegroundColor Yellow
aws s3 cp source.zip "s3://$bucketName/source.zip" --region $Region

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to upload source bundle to S3."
    exit 1
}

Write-Host "Source bundle uploaded successfully!" -ForegroundColor Green

# Create application version
Write-Host "Creating Elastic Beanstalk application version..." -ForegroundColor Yellow
$versionLabel = "v$(Get-Date -Format 'yyyyMMdd-HHmmss')"

aws elasticbeanstalk create-application-version `
    --application-name "minesweeper-app" `
    --version-label $versionLabel `
    --source-bundle S3Bucket=$bucketName,S3Key=source.zip `
    --region $Region

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create application version."
    exit 1
}

# Deploy the new version to the environment
Write-Host "Deploying application version to environment..." -ForegroundColor Yellow
aws elasticbeanstalk update-environment `
    --application-name "minesweeper-app" `
    --environment-name "minesweeper-env" `
    --version-label $versionLabel `
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
    Write-Host "Note: It may take a few minutes for the new version to be fully deployed." -ForegroundColor Yellow
} else {
    Write-Error "Application deployment failed. Check Elastic Beanstalk console for details."
}

Write-Host "Deployment script completed." -ForegroundColor Green
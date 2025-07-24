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

# Build and publish the application
Write-Host "Building and publishing .NET application..." -ForegroundColor Yellow

# Clean previous builds
if (Test-Path "bin") { Remove-Item "bin" -Recurse -Force }
if (Test-Path "obj") { Remove-Item "obj" -Recurse -Force }
if (Test-Path "publish") { Remove-Item "publish" -Recurse -Force }

# Restore dependencies
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to restore NuGet packages."
    exit 1
}

# Build the application
Write-Host "Building application..." -ForegroundColor Yellow
dotnet build --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to build application."
    exit 1
}

# Publish the application
Write-Host "Publishing application..." -ForegroundColor Yellow
dotnet publish --configuration Release --output publish --no-build
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to publish application."
    exit 1
}

# Create source bundle from published output
Write-Host "Creating source bundle..." -ForegroundColor Yellow
if (Test-Path "source.zip") {
    Remove-Item "source.zip"
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zipPath = Join-Path (Get-Location) "source.zip"

# Create zip from published files
[System.IO.Compression.ZipFile]::CreateFromDirectory("publish", $zipPath)

Write-Host "Source bundle created: source.zip" -ForegroundColor Green

# Check if we need to clean up existing S3 bucket
$expectedBucketName = "minesweeper-app-source-$((aws sts get-caller-identity --query Account --output text))-$Region"
Write-Host "Checking for existing S3 bucket: $expectedBucketName" -ForegroundColor Yellow

try {
    $bucketExists = aws s3api head-bucket --bucket $expectedBucketName 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Found existing S3 bucket. Emptying it..." -ForegroundColor Yellow
        aws s3 rm "s3://$expectedBucketName" --recursive --region $Region
    }
} catch {
    # Bucket doesn't exist, which is fine
}

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
    
    # Get all stack outputs
    Write-Host "`n=== DEPLOYMENT INFORMATION ===" -ForegroundColor Cyan
    
    $outputs = aws cloudformation describe-stacks `
        --stack-name $StackName `
        --region $Region `
        --query "Stacks[0].Outputs" `
        --output json | ConvertFrom-Json
    
    foreach ($output in $outputs) {
        $key = $output.OutputKey
        $value = $output.OutputValue
        $description = $output.Description
        
        Write-Host "$key`: $value" -ForegroundColor Green
        Write-Host "  Description: $description" -ForegroundColor Gray
        Write-Host ""
    }
    
    # Highlight the most important information
    $url = ($outputs | Where-Object { $_.OutputKey -eq "ApplicationURL" }).OutputValue
    Write-Host "🎮 PLAY MINESWEEPER: $url" -ForegroundColor Yellow -BackgroundColor DarkBlue
    Write-Host ""
    Write-Host "Note: It may take a few minutes for the new version to be fully deployed." -ForegroundColor Yellow
    Write-Host "Check the Elastic Beanstalk console for deployment status." -ForegroundColor Yellow
} else {
    Write-Error "Application deployment failed. Check Elastic Beanstalk console for details."
}

# Cleanup
Write-Host "Cleaning up temporary files..." -ForegroundColor Yellow
if (Test-Path "publish") { Remove-Item "publish" -Recurse -Force }
if (Test-Path "source.zip") { Remove-Item "source.zip" }

Write-Host "Deployment script completed." -ForegroundColor Green
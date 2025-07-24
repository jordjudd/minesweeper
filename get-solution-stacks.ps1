# PowerShell script to find available Elastic Beanstalk solution stacks for .NET
param(
    [string]$Region = "us-east-1"
)

Write-Host "Finding available .NET solution stacks in region: $Region" -ForegroundColor Green

try {
    # Get all solution stacks and filter for .NET
    $stacks = aws elasticbeanstalk list-available-solution-stacks --region $Region --query "SolutionStacks[?contains(@, '.NET') || contains(@, 'dotnet')]" --output table
    
    if ($stacks) {
        Write-Host "Available .NET Solution Stacks:" -ForegroundColor Yellow
        Write-Host $stacks
    } else {
        Write-Host "No .NET solution stacks found. Showing all available stacks:" -ForegroundColor Yellow
        aws elasticbeanstalk list-available-solution-stacks --region $Region --query "SolutionStacks" --output table
    }
} catch {
    Write-Error "Failed to retrieve solution stacks. Make sure AWS CLI is configured and you have proper permissions."
}

Write-Host "`nTo use a specific solution stack, update the SolutionStackName parameter in cloudformation-template.yaml" -ForegroundColor Cyan
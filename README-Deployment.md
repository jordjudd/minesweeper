# Minesweeper Deployment Guide

This guide explains how to deploy the Minesweeper application to AWS Elastic Beanstalk using CloudFormation.

## Prerequisites

1. **AWS CLI** installed and configured with appropriate credentials
2. **Git repository** containing your application code
3. **PowerShell** (for Windows deployment script)

## Deployment Options

### Option 1: Using PowerShell Script (Recommended)

1. Ensure your code is committed to a Git repository
2. Run the deployment script:

```powershell
.\deploy.ps1 -GitRepositoryUrl "https://github.com/jordjudd/minesweeper.git"
```

Optional parameters:
- `-StackName`: CloudFormation stack name (default: "minesweeper-stack")
- `-Region`: AWS region (default: "us-east-1")
- `-GitBranch`: Git branch to deploy (default: "main")

### Option 2: Manual CloudFormation Deployment

1. Create a source bundle:
```bash
zip -r source.zip . -x "*.git*" "*.vs*" "bin/*" "obj/*"
```

2. Deploy using AWS CLI:
```bash
aws cloudformation deploy \
  --template-file cloudformation-template.yaml \
  --stack-name minesweeper-stack \
  --parameter-overrides GitRepositoryUrl=https://github.com/jordjudd/minesweeper.git \
  --capabilities CAPABILITY_IAM
```

## Architecture

The CloudFormation template creates:

- **Elastic Beanstalk Application**: Hosts your .NET application
- **Elastic Beanstalk Environment**: Single-instance environment for cost efficiency
- **IAM Roles**: Proper permissions for EB service and EC2 instances
- **S3 Bucket**: Private bucket for source code storage (no public access)
- **Security**: All S3 buckets are private with encryption enabled

## Security Features

- ✅ No public S3 buckets
- ✅ S3 bucket encryption enabled
- ✅ Public access blocked on all buckets
- ✅ IAM roles with minimal required permissions
- ✅ HTTPS redirection enabled

## Configuration

The application is configured for:
- **.NET 8.0** runtime
- **Production** environment
- **Enhanced health monitoring**
- **Session state** using in-memory storage
- **t3.micro** instance type (cost-effective)

## Monitoring

After deployment, you can monitor your application through:
- AWS Elastic Beanstalk console
- CloudWatch logs and metrics
- Application health dashboard

## Costs

Estimated monthly costs (us-east-1):
- t3.micro instance: ~$8.50/month
- S3 storage: <$1/month
- Data transfer: Variable based on usage

## Troubleshooting

1. **Deployment fails**: Check CloudFormation events in AWS console
2. **Application won't start**: Check Elastic Beanstalk logs
3. **502/503 errors**: Verify .NET runtime version compatibility

## Cleanup

To remove all resources:
```bash
aws cloudformation delete-stack --stack-name minesweeper-stack
```

Note: The S3 bucket may need to be emptied manually before stack deletion.
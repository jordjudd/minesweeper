# Minesweeper Game - AWS Deployment

A modern web-based implementation of the classic Minesweeper game built with ASP.NET Core, designed for deployment on AWS using CloudFormation.

## Features

- **Multiple Difficulty Levels**: Easy (9x9, 10 mines), Medium (16x16, 40 mines), Hard (16x30, 99 mines)
- **Custom Game Mode**: Create your own board size and mine count
- **Classic Minesweeper Rules**: Numbers show adjacent mine counts, cascade reveals empty areas
- **Flag Counter**: Track how many flags you've placed
- **Mine Reveal on Loss**: All mines are shown when you lose
- **Responsive Design**: Works on desktop and mobile devices

## AWS Deployment using CloudFormation

This application is designed to be deployed on AWS Elastic Beanstalk using the included CloudFormation template.

### Prerequisites

1. **AWS Account** with appropriate permissions
2. **AWS CLI** installed and configured
3. **PowerShell** (for Windows) or **Bash** (for Linux/Mac)
4. **.NET 9.0 SDK** installed locally

### Quick Deployment

#### Option 1: Using PowerShell Script (Recommended)

1. **Clone the repository**:
   ```bash
   git clone <your-repository-url>
   cd minesweeper
   ```

2. **Configure AWS CLI** (if not already done):
   ```bash
   aws configure
   ```

3. **Run the deployment script**:
   ```powershell
   .\deploy.ps1
   ```

   Or with custom parameters:
   ```powershell
   .\deploy.ps1 -StackName "my-minesweeper" -Region "us-west-2" -GitRepositoryUrl "https://github.com/yourusername/minesweeper.git"
   ```

#### Option 2: Manual CloudFormation Deployment

1. **Deploy the CloudFormation stack**:
   ```bash
   aws cloudformation deploy \
     --template-file cloudformation-template.yaml \
     --stack-name minesweeper-stack \
     --parameter-overrides \
       GitRepositoryUrl=https://github.com/yourusername/minesweeper.git \
       GitBranch=main \
     --capabilities CAPABILITY_IAM \
     --region us-east-1
   ```

2. **Build and publish the application**:
   ```bash
   dotnet publish -c Release -o publish
   ```

3. **Create source bundle**:
   ```bash
   cd publish
   zip -r ../source.zip .
   cd ..
   ```

4. **Upload to S3 and deploy**:
   ```bash
   # Get S3 bucket name from CloudFormation outputs
   BUCKET_NAME=$(aws cloudformation describe-stacks \
     --stack-name minesweeper-stack \
     --query "Stacks[0].Outputs[?OutputKey=='SourceBucket'].OutputValue" \
     --output text)
   
   # Upload source bundle
   aws s3 cp source.zip s3://$BUCKET_NAME/source.zip
   
   # Create application version
   VERSION_LABEL="v$(date +%Y%m%d-%H%M%S)"
   aws elasticbeanstalk create-application-version \
     --application-name minesweeper-app \
     --version-label $VERSION_LABEL \
     --source-bundle S3Bucket=$BUCKET_NAME,S3Key=source.zip
   
   # Deploy to environment
   aws elasticbeanstalk update-environment \
     --application-name minesweeper-app \
     --environment-name minesweeper-env \
     --version-label $VERSION_LABEL
   ```

### CloudFormation Template Parameters

The `cloudformation-template.yaml` supports the following parameters:

| Parameter | Default | Description |
|-----------|---------|-------------|
| `ApplicationName` | `minesweeper-app` | Name of the Elastic Beanstalk application |
| `EnvironmentName` | `minesweeper-env` | Name of the Elastic Beanstalk environment |
| `GitRepositoryUrl` | `https://github.com/jordjudd/minesweeper.git` | HTTPS URL of your Git repository |
| `GitBranch` | `main` | Git branch to deploy from |
| `InstanceType` | `t3.micro` | EC2 instance type (t3.micro, t3.small, t3.medium, t3.large) |
| `SolutionStackName` | `64bit Amazon Linux 2023 v3.5.1 running .NET 9` | Elastic Beanstalk solution stack |

### AWS Resources Created

The CloudFormation template creates the following AWS resources:

- **Elastic Beanstalk Application** - Hosts the .NET application
- **Elastic Beanstalk Environment** - Single-instance environment for the app
- **IAM Roles** - Service role and instance profile with required permissions
- **S3 Bucket** - Private bucket for storing application source bundles
- **Configuration Template** - Defines environment settings and .NET configuration

### Deployment Outputs

After successful deployment, the CloudFormation stack provides these outputs:

- **ApplicationURL** - Direct URL to access your Minesweeper game
- **SourceBucket** - S3 bucket name for future deployments
- **ApplicationName** - Elastic Beanstalk application name
- **EnvironmentName** - Elastic Beanstalk environment name

### Updating the Application

To deploy updates to your application:

1. **Using the PowerShell script**:
   ```powershell
   .\deploy.ps1 -StackName "your-existing-stack-name"
   ```

2. **Manual update**:
   ```bash
   # Build and create new source bundle
   dotnet publish -c Release -o publish
   cd publish && zip -r ../source.zip . && cd ..
   
   # Upload and deploy new version
   aws s3 cp source.zip s3://your-bucket-name/source.zip
   VERSION_LABEL="v$(date +%Y%m%d-%H%M%S)"
   aws elasticbeanstalk create-application-version \
     --application-name minesweeper-app \
     --version-label $VERSION_LABEL \
     --source-bundle S3Bucket=your-bucket-name,S3Key=source.zip
   aws elasticbeanstalk update-environment \
     --application-name minesweeper-app \
     --environment-name minesweeper-env \
     --version-label $VERSION_LABEL
   ```

### Monitoring and Troubleshooting

- **Elastic Beanstalk Console**: Monitor application health and logs
- **CloudWatch Logs**: View application logs and system metrics
- **Health Dashboard**: Check environment health and recent events

### Cost Considerations

- **t3.micro instance**: Eligible for AWS Free Tier (750 hours/month)
- **S3 storage**: Minimal cost for source bundles
- **Data transfer**: Standard AWS data transfer rates apply

### Cleanup

To remove all AWS resources:

```bash
aws cloudformation delete-stack --stack-name minesweeper-stack
```

**Note**: This will permanently delete all resources including the S3 bucket and any stored application versions.
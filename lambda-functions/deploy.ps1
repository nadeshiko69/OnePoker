# Lambda Function Deployment Script (PowerShell)
# Usage: .\deploy.ps1 [function-name]

param(
    [Parameter(Mandatory=$true)]
    [string]$FunctionName
)

if ([string]::IsNullOrEmpty($FunctionName)) {
    Write-Host "Usage: .\deploy.ps1 [function-name]"
    Write-Host "Available functions:"
    Write-Host "  - create_room"
    Write-Host "  - join_room"
    Write-Host "  - cancel_room"
    Write-Host "  - register_user"
    Write-Host "  - set-phase-transition"
    Write-Host "  - login_user"
    Write-Host "  - check-match"
    Write-Host "  - start-game"
    Write-Host "  - get-game-state"
    Write-Host "  - update-game-state"
    Write-Host "  - update-game-action"
    Write-Host "  - bet-action"
    Write-Host "  - use-skill"
    Write-Host "  - next-round"
    Write-Host ""
    Write-Host "Special commands:"
    Write-Host "  - all          : Deploy all functions"
    Write-Host "  - game-functions: Deploy game-related functions only"
    Write-Host "  - list         : List all available function directories"
    exit 1
}

# Handle special commands
if ($FunctionName -eq "list") {
    Write-Host "Available function directories:"
    Get-ChildItem -Directory | ForEach-Object {
        Write-Host "  - $($_.Name)"
    }
    exit 0
}

if ($FunctionName -eq "all") {
    Write-Host "Deploying all Lambda functions..."
    $allFunctions = Get-ChildItem -Directory | Where-Object { $_.Name -ne "shared_deps" } | ForEach-Object { $_.Name }
    foreach ($func in $allFunctions) {
        Write-Host "`n=== Deploying $func ===" -ForegroundColor Yellow
        & $PSScriptRoot\deploy.ps1 $func
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Failed to deploy $func" -ForegroundColor Red
            exit 1
        }
    }
    Write-Host "`nAll functions deployed successfully!" -ForegroundColor Green
    exit 0
}

if ($FunctionName -eq "game-functions") {
    Write-Host "Deploying game-related Lambda functions..."
    $gameFunctions = @("start-game", "get-game-state", "bet-action", "use-skill", "next-round")
    foreach ($func in $gameFunctions) {
        if (Test-Path $func) {
            Write-Host "`n=== Deploying $func ===" -ForegroundColor Yellow
            & $PSScriptRoot\deploy.ps1 $func
            if ($LASTEXITCODE -ne 0) {
                Write-Host "Failed to deploy $func" -ForegroundColor Red
                exit 1
            }
        } else {
            Write-Host "Function directory '$func' not found, skipping..." -ForegroundColor Yellow
        }
    }
    Write-Host "`nGame functions deployed successfully!" -ForegroundColor Green
    exit 0
}

# Check if function directory exists
if (-not (Test-Path $FunctionName)) {
    Write-Host "Error: Function directory '$FunctionName' not found"
    Write-Host "Use '.\deploy.ps1 list' to see available functions"
    exit 1
}

Write-Host "Deploying: $FunctionName" -ForegroundColor Cyan

# Check if lambda_function.py exists
if (-not (Test-Path "$FunctionName/lambda_function.py")) {
    Write-Host "Error: lambda_function.py not found in $FunctionName directory" -ForegroundColor Red
    exit 1
}

# Create temporary directory for packaging
$tempDir = "temp_${FunctionName}_package"
if (Test-Path $tempDir) {
    Remove-Item -Path $tempDir -Recurse -Force
}
New-Item -ItemType Directory -Path $tempDir | Out-Null

try {
    # Copy function files to temp directory
    Write-Host "Copying function files..." -ForegroundColor Yellow
    Copy-Item -Path "$FunctionName/*" -Destination $tempDir -Recurse -Force

    # Copy shared dependencies to temp directory (excluding __pycache__ and bin)
    if (Test-Path "shared_deps") {
        Write-Host "Copying shared dependencies..." -ForegroundColor Yellow
        Get-ChildItem -Path "shared_deps" -Exclude "__pycache__", "bin" | Copy-Item -Destination $tempDir -Recurse -Force
    }

    # Create ZIP file from temp directory
    Write-Host "Creating ZIP file..." -ForegroundColor Yellow
    Compress-Archive -Path "$tempDir/*" -DestinationPath "${FunctionName}.zip" -Force
    
    # Show ZIP file size
    $zipFile = Get-Item "${FunctionName}.zip"
    Write-Host "ZIP file size: $([math]::Round($zipFile.Length / 1KB, 2)) KB" -ForegroundColor Cyan

    # Deploy to AWS Lambda
    Write-Host "Deploying to AWS Lambda..." -ForegroundColor Yellow
    $deployResult = aws lambda update-function-code --function-name "$FunctionName" --zip-file "fileb://${FunctionName}.zip" --output json 2>&1

    if ($LASTEXITCODE -eq 0) {
        Write-Host "Deployment successful: $FunctionName" -ForegroundColor Green
        
        # Parse and show deployment info
        try {
            $deployInfo = $deployResult | ConvertFrom-Json
            Write-Host "  Function ARN: $($deployInfo.FunctionArn)" -ForegroundColor Gray
            Write-Host "  Last Modified: $($deployInfo.LastModified)" -ForegroundColor Gray
            Write-Host "  Code Size: $([math]::Round($deployInfo.CodeSize / 1KB, 2)) KB" -ForegroundColor Gray
        } catch {
            Write-Host "  Deployment completed (could not parse response)" -ForegroundColor Gray
        }
    } else {
        Write-Host "Deployment failed: $FunctionName" -ForegroundColor Red
        Write-Host "Error output: $deployResult" -ForegroundColor Red
        exit 1
    }
}
finally {
    # Clean up temporary files
    Write-Host "Cleaning up temporary files..." -ForegroundColor Gray
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force
    }
    if (Test-Path "${FunctionName}.zip") {
        Remove-Item -Path "${FunctionName}.zip" -Force
    }
    Write-Host "Cleanup completed." -ForegroundColor Gray
} 
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
    exit 1
}

# Check if function directory exists
if (-not (Test-Path $FunctionName)) {
    Write-Host "Error: Function directory '$FunctionName' not found"
    exit 1
}

Write-Host "Deploying: $FunctionName"

# Create temporary directory for packaging
$tempDir = "temp_${FunctionName}_package"
if (Test-Path $tempDir) {
    Remove-Item -Path $tempDir -Recurse -Force
}
New-Item -ItemType Directory -Path $tempDir | Out-Null

try {
    # Copy function files to temp directory
    Write-Host "Copying function files..."
    Copy-Item -Path "$FunctionName/*" -Destination $tempDir -Recurse -Force

    # Copy shared dependencies to temp directory (excluding __pycache__ and bin)
    if (Test-Path "shared_deps") {
        Write-Host "Copying shared dependencies..."
        Get-ChildItem -Path "shared_deps" -Exclude "__pycache__", "bin" | Copy-Item -Destination $tempDir -Recurse -Force
    }

    # Create ZIP file from temp directory
    Write-Host "Creating ZIP file..."
    Compress-Archive -Path "$tempDir/*" -DestinationPath "${FunctionName}.zip" -Force

    # Deploy to AWS Lambda
    Write-Host "Deploying to AWS Lambda..."
    aws lambda update-function-code --function-name "$FunctionName" --zip-file "fileb://${FunctionName}.zip" --output text

    if ($LASTEXITCODE -eq 0) {
        Write-Host "Deployment successful: $FunctionName"
    } else {
        Write-Host "Deployment failed: $FunctionName"
        exit 1
    }
}
finally {
    # Clean up temporary files
    Write-Host "Cleaning up temporary files..."
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force
    }
    if (Test-Path "${FunctionName}.zip") {
        Remove-Item -Path "${FunctionName}.zip" -Force
    }
} 
# Lambda Functions Batch Deployment Script (PowerShell)
# Usage: .\deploy-all.ps1

Write-Host "=== OnePoker Lambda Functions Batch Deployment ===" -ForegroundColor Green

# Install dependencies in lambda-functions root directory
Write-Host "Installing dependencies in lambda-functions root..." -ForegroundColor Yellow
if (Test-Path "requirements.txt") {
    Write-Host "Installing dependencies..."
    py -m pip install -r requirements.txt -t ./shared_deps
    Write-Host "Dependencies installed successfully" -ForegroundColor Green
} else {
    Write-Host "Warning: requirements.txt not found" -ForegroundColor Yellow
}

# Deploy all functions
Write-Host ""
Write-Host "Deploying all functions..." -ForegroundColor Yellow

.\deploy.ps1 create_room
.\deploy.ps1 join_room
.\deploy.ps1 check-match
.\deploy.ps1 start-game
.\deploy.ps1 get-game-state
.\deploy.ps1 update-game-state
.\deploy.ps1 cancel_room
.\deploy.ps1 register_user
.\deploy.ps1 login_user
.\deploy.ps1 update-game-action

Write-Host ""
Write-Host "Batch deployment completed!" -ForegroundColor Green
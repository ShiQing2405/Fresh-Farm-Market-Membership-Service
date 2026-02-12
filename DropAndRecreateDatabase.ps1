# Drop and Recreate Database Script
# This script drops the FreshFarmMarketDb database and reapplies all migrations

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Drop and Recreate Database Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Navigate to project directory
$projectPath = "Fresh Farm Market Membership Service"
Write-Host "Navigating to project directory..." -ForegroundColor Yellow
Set-Location $projectPath

Write-Host ""
Write-Host "Step 1: Dropping the database..." -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Gray

# Drop the database
$dropResult = dotnet ef database drop --force 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Database dropped successfully!" -ForegroundColor Green
} else {
    Write-Host "Note: Database may not exist or already dropped." -ForegroundColor Yellow
    Write-Host $dropResult -ForegroundColor Gray
}

Write-Host ""
Write-Host "Step 2: Applying all migrations..." -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Gray

# Apply all migrations
$updateResult = dotnet ef database update 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "? All migrations applied successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Database recreated with all tables including:" -ForegroundColor Green
    Write-Host "  - AspNetUsers" -ForegroundColor Green
    Write-Host "  - AspNetRoles" -ForegroundColor Green
    Write-Host "  - AspNetUserRoles" -ForegroundColor Green
    Write-Host "  - AspNetUserClaims" -ForegroundColor Green
    Write-Host "  - AspNetUserLogins" -ForegroundColor Green
    Write-Host "  - AspNetUserTokens" -ForegroundColor Green
    Write-Host "  - AspNetRoleClaims" -ForegroundColor Green
    Write-Host "  - AuditLogs" -ForegroundColor Green
    Write-Host "  - PasswordHistories" -ForegroundColor Green
} else {
    Write-Host "? Error applying migrations!" -ForegroundColor Red
    Write-Host $updateResult -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Database recreation completed!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "You can now run your application." -ForegroundColor Green

# Return to original directory
Set-Location ..

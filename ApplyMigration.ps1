# Run this script to apply the database migration that removes the Members table

Write-Host "Applying migration to remove Members table..." -ForegroundColor Cyan

# Navigate to project directory and run migration
Push-Location "Fresh Farm Market Membership Service"
dotnet ef database update
Pop-Location

Write-Host "Migration complete! The Members table has been removed from the database." -ForegroundColor Green
Write-Host ""
Write-Host "Now you only have one set of email fields in AspNetUsers table:" -ForegroundColor Yellow
Write-Host "  - Email: The actual email address" -ForegroundColor White
Write-Host "  - NormalizedEmail: Uppercase version used for lookups/indexing" -ForegroundColor White

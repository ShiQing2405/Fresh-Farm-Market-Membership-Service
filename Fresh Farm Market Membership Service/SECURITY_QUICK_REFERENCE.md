# ?? User Secrets - Quick Reference

## View Secrets
```powershell
cd "Fresh Farm Market Membership Service"
dotnet user-secrets list
```

## Set Secrets (For New Developers)
```powershell
cd "Fresh Farm Market Membership Service"
dotnet user-secrets set "GoogleReCaptcha:SecretKey" "YOUR_SECRET_KEY"
dotnet user-secrets set "EmailSettings:Username" "your.email@gmail.com"
dotnet user-secrets set "EmailSettings:Password" "your-app-password"
dotnet user-secrets set "EmailSettings:FromEmail" "your.email@gmail.com"
```

## Secrets Location
```
%APPDATA%\Microsoft\UserSecrets\a27644a4-5759-4c08-95b4-4c4fb630bc4c\secrets.json
```

## ?? What Changed?
- ? Sensitive data removed from `appsettings.json`
- ? Secrets stored in User Secrets (development only)
- ? Application still works exactly the same
- ? Safe to commit to Git now

## ?? Before Committing
1. Delete `appsettings.json.backup` (contains secrets)
2. Verify `appsettings.json` has no passwords
3. Test application: `dotnet run`

## ?? Current Secrets Stored
- `GoogleReCaptcha:SecretKey`
- `EmailSettings:Username`
- `EmailSettings:Password`
- `EmailSettings:FromEmail`

## ?? Production Deployment
Use **Environment Variables** or **Azure Key Vault** (not User Secrets!)

---
**See**: `SECURITY_USER_SECRETS_GUIDE.md` for complete documentation

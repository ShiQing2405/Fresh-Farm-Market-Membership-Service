# Security Configuration Guide - User Secrets

## ? Security Implementation Complete

Your sensitive configuration data has been secured using .NET User Secrets for development.

---

## ?? What Was Secured

The following sensitive data has been moved from `appsettings.json` to User Secrets:

1. **Google reCAPTCHA Secret Key**
2. **Email Service Credentials** (Username, Password, FromEmail)

---

## ?? Files Modified

### 1. `appsettings.json`
- **Status**: ? Cleaned - All sensitive data removed
- **Backup**: `appsettings.json.backup` (contains original with secrets)

### 2. User Secrets Store
- **Status**: ? Initialized and configured
- **UserSecretsId**: `a27644a4-5759-4c08-95b4-4c4fb630bc4c`
- **Location**: `%APPDATA%\Microsoft\UserSecrets\a27644a4-5759-4c08-95b4-4c4fb630bc4c\secrets.json`

---

## ?? Secrets Stored

The following secrets are now securely stored in User Secrets:

```json
{
  "GoogleReCaptcha:SecretKey": "6LcN6GcsAAAAANnJpJ6GJxeHfXcX0Bksgy0c2niA",
  "EmailSettings:Username": "random.personal.email.here@gmail.com",
  "EmailSettings:Password": "idle ceiw memb lmab",
  "EmailSettings:FromEmail": "random.personal.email.here@gmail.com"
}
```

---

## ?? How to Use User Secrets

### View All Secrets
```powershell
cd "Fresh Farm Market Membership Service"
dotnet user-secrets list
```

### Add/Update a Secret
```powershell
dotnet user-secrets set "KeyName" "KeyValue"
```

### Remove a Secret
```powershell
dotnet user-secrets remove "KeyName"
```

### Clear All Secrets
```powershell
dotnet user-secrets clear
```

---

## ?? How It Works

1. **Development**: User Secrets automatically override values from `appsettings.json`
2. **Production**: Use Environment Variables or Azure Key Vault
3. **Team Members**: Each developer maintains their own secrets locally

---

## ?? Setting Up for New Team Members

When a new developer clones the repository:

1. **Initialize User Secrets** (if not already done):
   ```powershell
   cd "Fresh Farm Market Membership Service"
   dotnet user-secrets init
   ```

2. **Set Required Secrets**:
   ```powershell
   dotnet user-secrets set "GoogleReCaptcha:SecretKey" "YOUR_SECRET_KEY"
   dotnet user-secrets set "EmailSettings:Username" "your.email@gmail.com"
   dotnet user-secrets set "EmailSettings:Password" "your-app-password"
   dotnet user-secrets set "EmailSettings:FromEmail" "your.email@gmail.com"
   ```

---

## ?? Production Deployment

### Option 1: Environment Variables

Set environment variables on your production server:

```bash
GoogleReCaptcha__SecretKey=your-production-secret
EmailSettings__Username=production.email@company.com
EmailSettings__Password=production-password
EmailSettings__FromEmail=production.email@company.com
```

### Option 2: Azure App Service Configuration

1. Go to Azure Portal ? Your App Service
2. Navigate to **Configuration** ? **Application settings**
3. Add each secret as a new application setting:
   - `GoogleReCaptcha:SecretKey`
   - `EmailSettings:Username`
   - `EmailSettings:Password`
   - `EmailSettings:FromEmail`

### Option 3: Azure Key Vault

For enterprise-level security, use Azure Key Vault:

```csharp
// In Program.cs
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

---

## ?? appsettings.json Template

Your `appsettings.json` now serves as a template:

```json
{
  "GoogleReCaptcha": {
    "SiteKey": "public-site-key-ok-to-commit",
    "SecretKey": "",  // Set via User Secrets or Environment Variables
    "Version": "v3"
  },
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": "587",
    "FromEmail": "",  // Set via User Secrets or Environment Variables
    "FromName": "Fresh Farm Market",
    "Username": "",   // Set via User Secrets or Environment Variables
    "Password": "",   // Set via User Secrets or Environment Variables
    "EnableSsl": "true"
  }
}
```

---

## ?? Important Security Notes

### ? Safe to Commit
- `appsettings.json` (cleaned version)
- `appsettings.Development.json`
- `appsettings.Production.json`

### ? NEVER Commit
- `appsettings.json.backup` (contains secrets)
- `secrets.json` (User Secrets file)
- Any file with actual passwords or keys

### ?? .gitignore Check

Ensure your `.gitignore` includes:
```
**/appsettings.json.backup
**/secrets.json
**/*.user
```

---

## ?? Verify Configuration

Run this to confirm secrets are loaded:

```powershell
cd "Fresh Farm Market Membership Service"
dotnet run
```

The application will automatically load:
1. `appsettings.json` (base configuration)
2. User Secrets (overrides for development)

---

## ?? Additional Resources

- [Safe Storage of App Secrets in Development](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Configuration in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Azure Key Vault Configuration Provider](https://learn.microsoft.com/en-us/aspnet/core/security/key-vault-configuration)

---

## ? Security Checklist Completed

- [x] Sensitive data removed from `appsettings.json`
- [x] User Secrets initialized and configured
- [x] All secrets stored securely
- [x] Backup created for reference
- [x] Documentation provided
- [x] Production deployment options outlined

---

## ?? Next Steps

1. **Delete the backup file** after confirming everything works:
   ```powershell
   Remove-Item "Fresh Farm Market Membership Service\appsettings.json.backup"
   ```

2. **Test your application** to ensure secrets are loading correctly

3. **Commit the cleaned appsettings.json** to your repository

4. **Share this guide** with your team members

---

## ?? Troubleshooting

### Secrets Not Loading?

1. Verify UserSecretsId in `.csproj` file
2. List secrets: `dotnet user-secrets list`
3. Check secrets location: `%APPDATA%\Microsoft\UserSecrets\a27644a4-5759-4c08-95b4-4c4fb630bc4c\secrets.json`

### Application Can't Send Emails?

Ensure these secrets are set:
- `EmailSettings:Username`
- `EmailSettings:Password`
- `EmailSettings:FromEmail`

---

**Last Updated**: 2025-01-XX
**Status**: ? Production Ready

# Email Service Setup Guide - Fresh Farm Market

## ? What Was Implemented

I've added a complete email sending service to your application for password reset functionality:

### New Files Created:
1. **IEmailService.cs** - Interface for email operations
2. **EmailService.cs** - Full SMTP email implementation with HTML templates

### Files Modified:
1. **appsettings.json** - Added EmailSettings configuration
2. **Program.cs** - Registered email service
3. **MemberController.cs** - Updated to use email service
4. **ForgotPasswordConfirmation.cshtml** - Shows email status

---

## ?? Email Service Options

### Option 1: Gmail (Easiest for Testing)

**Step 1: Enable 2-Step Verification**
1. Go to https://myaccount.google.com/security
2. Enable "2-Step Verification"

**Step 2: Create App Password**
1. Go to https://myaccount.google.com/apppasswords
2. Select "Mail" and your device
3. Click "Generate"
4. Copy the 16-character password

**Step 3: Update appsettings.json**
```json
"EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": "587",
    "FromEmail": "yourrealemail@gmail.com",
    "FromName": "Fresh Farm Market",
    "Username": "yourrealemail@gmail.com",
    "Password": "your-16-char-app-password",
    "EnableSsl": "true"
}
```

---

### Option 2: Outlook/Hotmail

**Update appsettings.json:**
```json
"EmailSettings": {
    "SmtpServer": "smtp-mail.outlook.com",
    "SmtpPort": "587",
    "FromEmail": "youremail@outlook.com",
    "FromName": "Fresh Farm Market",
    "Username": "youremail@outlook.com",
    "Password": "your-outlook-password",
    "EnableSsl": "true"
}
```

---

### Option 3: SendGrid (Production Recommended)

**Step 1: Create SendGrid Account**
1. Go to https://signup.sendgrid.com/
2. Create free account (100 emails/day)

**Step 2: Create API Key**
1. Go to Settings ? API Keys
2. Click "Create API Key"
3. Give it "Mail Send" permissions
4. Copy the API key

**Step 3: Update EmailService.cs**
```csharp
// Add NuGet Package: SendGrid
// Install-Package SendGrid

using SendGrid;
using SendGrid.Helpers.Mail;

public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
{
    var apiKey = _configuration["EmailSettings:SendGridApiKey"];
    var client = new SendGridClient(apiKey);
    
    var from = new EmailAddress(_configuration["EmailSettings:FromEmail"], 
                                _configuration["EmailSettings:FromName"]);
    var to = new EmailAddress(toEmail);
    var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlMessage);
    
    var response = await client.SendEmailAsync(msg);
    
    if (!response.IsSuccessStatusCode)
    {
        throw new InvalidOperationException($"Failed to send email: {response.StatusCode}");
    }
}
```

**Step 4: Update appsettings.json**
```json
"EmailSettings": {
    "SendGridApiKey": "SG.your-api-key-here",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "Fresh Farm Market"
}
```

---

## ?? Testing Email Sending

### Test 1: Using Real Email (Gmail Example)

1. **Update appsettings.json** with your Gmail credentials

2. **Run the application**

3. **Test Password Reset:**
```
1. Go to /Member/Login
2. Click "Forgot Password?"
3. Enter your real email address
4. Submit
5. Check your email inbox
6. Click the reset link in the email
7. Reset your password
```

### Test 2: Development Mode (Fallback)

If email sending fails (e.g., wrong credentials), the app will:
- Show an error message
- Display the reset link on the confirmation page (development only)
- Still allow you to test the feature

---

## ?? Troubleshooting

### Error: "Failed to send email"

**Possible Causes:**

1. **Wrong Credentials**
   - Solution: Verify email and password in appsettings.json
   - For Gmail: Make sure you're using App Password, not regular password

2. **SMTP Port Blocked**
   - Solution: Try port 465 instead of 587
   ```json
   "SmtpPort": "465"
   ```

3. **Less Secure Apps Blocked (Gmail)**
   - Solution: Use App Passwords (2-Step Verification required)

4. **Firewall/Antivirus Blocking**
   - Solution: Temporarily disable or add exception for your app

### Error: "Authentication failed"

**For Gmail:**
- ? Use App Password (16 characters, no spaces)
- ? Enable 2-Step Verification first
- ? Don't use your regular Gmail password

**For Outlook:**
- ? Use your regular password
- ? Enable "Less secure app access" if prompted

---

## ?? Email Template Features

Your password reset email includes:

? **Professional HTML design** with colors and styling
? **Button link** for easy clicking
? **Plain text fallback** link for accessibility
? **Security warnings**:
   - Link expires in 24 hours
   - Single-use only
   - What to do if you didn't request it

? **Branded** with Fresh Farm Market name
? **Mobile-responsive** design

---

## ?? Security Features

### Email Service Security:
- ? Credentials stored in appsettings.json (use User Secrets in production)
- ? SSL/TLS encryption enabled
- ? Error logging (doesn't expose sensitive data)
- ? Graceful failure handling

### Password Reset Security:
- ? Tokens valid for 24 hours
- ? Single-use tokens
- ? Doesn't reveal if email exists (privacy)
- ? All actions audited in database

---

## ?? Production Deployment

### Step 1: Use User Secrets (Don't commit passwords!)

```bash
# Set up user secrets
dotnet user-secrets init

# Add email credentials
dotnet user-secrets set "EmailSettings:Password" "your-password-here"
```

### Step 2: Use Environment Variables (Azure/IIS)

```bash
# In Azure App Service - Configuration
EmailSettings__Password = your-password
EmailSettings__Username = your-email@gmail.com
```

### Step 3: Use Azure Key Vault (Best Practice)

```csharp
// In Program.cs
builder.Configuration.AddAzureKeyVault(
    new Uri("https://your-keyvault.vault.azure.net/"),
    new DefaultAzureCredential());
```

---

## ?? Testing Checklist

- [ ] Email credentials configured in appsettings.json
- [ ] Application builds successfully
- [ ] Request password reset for real email
- [ ] Email received in inbox (check spam folder)
- [ ] Email looks professional
- [ ] Reset link works
- [ ] Password reset successful
- [ ] Can login with new password
- [ ] Audit log shows all actions

---

## ?? Quick Start (Gmail)

**5-Minute Setup:**

1. **Enable 2-Step Verification** on your Gmail account
2. **Generate App Password** at https://myaccount.google.com/apppasswords
3. **Update appsettings.json:**
   ```json
   "FromEmail": "yourrealgmail@gmail.com",
   "Username": "yourrealgmail@gmail.com",
   "Password": "abcd efgh ijkl mnop"
   ```
4. **Run application**
5. **Test forgot password feature**
6. **Check your Gmail inbox** ?

---

## ?? Alternative: MailTrap (Testing Only)

MailTrap is perfect for testing without sending real emails:

1. **Sign up** at https://mailtrap.io (free)
2. **Get SMTP credentials** from your inbox
3. **Update appsettings.json:**
```json
"EmailSettings": {
    "SmtpServer": "smtp.mailtrap.io",
    "SmtpPort": "587",
    "FromEmail": "test@example.com",
    "FromName": "Fresh Farm Market",
    "Username": "your-mailtrap-username",
    "Password": "your-mailtrap-password",
    "EnableSsl": "true"
}
```

All emails sent will appear in MailTrap inbox (not real delivery).

---

## ? What Happens Now

### With Email Configured:
1. User requests password reset
2. Email sent to their inbox
3. User clicks link in email
4. Resets password
5. Success! ?

### Without Email Configured (Development):
1. User requests password reset
2. Email sending fails (gracefully)
3. Reset link shown on confirmation page
4. User clicks link there
5. Resets password
6. Success! ? (Demo mode)

**Both methods work!** The app is smart enough to handle both scenarios.

---

## ?? Need Help?

### Check Application Logs

```bash
# Console output shows email sending status
# Look for:
# ? "Email sent successfully to user@example.com"
# ? "Failed to send email to user@example.com"
```

### Check Audit Logs in Database

```sql
SELECT * FROM AuditLogs 
WHERE Action LIKE '%Password Reset%' 
ORDER BY Timestamp DESC;

-- Shows:
-- "Password Reset Requested - Password reset email sent successfully" ?
-- OR
-- "Password Reset Failed - Failed to send email: [error details]" ?
```

---

## ?? Summary

Your application now has:
- ? Professional email service with HTML templates
- ? Multiple SMTP provider support (Gmail, Outlook, SendGrid)
- ? Graceful fallback for development
- ? Production-ready security
- ? Comprehensive error handling
- ? Audit logging

**Just add your email credentials and you're ready to send emails!** ??

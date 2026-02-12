# reCAPTCHA v3 Implementation Guide

## Overview
This application now uses Google reCAPTCHA v3 to protect the Login and Registration forms from bots and automated attacks.

## Configuration

### Keys Used
- **Site Key (Public)**: `6LcN6GcsAAAAAGht2pfExckXOcCQwzQ390oTsqFA`
- **Secret Key (Private)**: `6LcN6GcsAAAAANnJpJ6GJxeHfXcX0Bksgy0c2niA`

These keys are configured in `appsettings.json`:
```json
"GoogleReCaptcha": {
    "SiteKey": "6LcN6GcsAAAAAGht2pfExckXOcCQwzQ390oTsqFA",
    "SecretKey": "6LcN6GcsAAAAANnJpJ6GJxeHfXcX0Bksgy0c2niA",
    "Version": "v3"
}
```

## How reCAPTCHA v3 Works

Unlike reCAPTCHA v2 (checkbox), v3 is **invisible** and runs in the background. It:
1. Analyzes user behavior on your site
2. Generates a score from 0.0 (bot) to 1.0 (human)
3. Sends the score to your server for verification

## Implementation Details

### 1. NuGet Package
The application uses the `reCAPTCHA.AspNetCore` package (version 3.0.10) which handles:
- Token validation
- Score evaluation
- Communication with Google's reCAPTCHA API

### 2. Service Registration (Program.cs)
```csharp
builder.Services.Configure<RecaptchaSettings>(builder.Configuration.GetSection("GoogleReCaptcha"));
builder.Services.AddTransient<IRecaptchaService, RecaptchaService>();
```

### 3. Frontend Implementation

#### Login Page (`Views/Member/Login.cshtml`)
```javascript
<script src="https://www.google.com/recaptcha/api.js?render=6LcN6GcsAAAAAGht2pfExckXOcCQwzQ390oTsqFA"></script>
<script>
    grecaptcha.ready(function() {
        var form = document.querySelector('form');
        
        form.addEventListener('submit', function(e) {
            e.preventDefault();
            
            grecaptcha.execute('6LcN6GcsAAAAAGht2pfExckXOcCQwzQ390oTsqFA', {action: 'login'}).then(function(token) {
                // Add token to form
                var input = document.createElement('input');
                input.type = 'hidden';
                input.name = 'g-recaptcha-response';
                input.value = token;
                form.appendChild(input);
                
                // Submit the form
                form.submit();
            });
        });
    });
</script>
```

#### Registration Page (`Views/Home/Registration.cshtml`)
Similar implementation with `action: 'register'`

### 4. Backend Validation (MemberController.cs)

Both `Login` and `Register` actions validate the reCAPTCHA:

```csharp
// Validate reCAPTCHA
var recaptcha = await _recaptchaService.Validate(Request);
if (!recaptcha.success || recaptcha.score < 0.5)
{
    ModelState.AddModelError(string.Empty, "reCAPTCHA validation failed. Please try again.");
    return View(model);
}
```

**Score Threshold**: 0.5
- Scores >= 0.5 are considered human
- Scores < 0.5 are rejected as potential bots

## Testing the Implementation

### Test the Login Page
1. Navigate to `/Member/Login`
2. Enter valid credentials
3. Click "Login"
4. The form will automatically:
   - Execute reCAPTCHA in the background
   - Get a token from Google
   - Submit the form with the token

### Test the Registration Page
1. Navigate to `/Member/Register`
2. Fill in the registration form
3. Click "Register"
4. Same reCAPTCHA process happens automatically

### Expected Behavior
- **Valid User**: Form submits normally, no visible reCAPTCHA challenge
- **Bot/Suspicious**: Form submission is blocked with error message
- **Network Issues**: If Google's API is unreachable, validation will fail

## Troubleshooting

### Common Issues

1. **"reCAPTCHA validation failed"**
   - Check if the keys are correct in `appsettings.json`
   - Ensure your domain is registered with these keys in Google reCAPTCHA console
   - Check browser console for JavaScript errors

2. **Keys Not Working**
   - Verify domain is whitelisted in Google reCAPTCHA Admin Console
   - Ensure you're using v3 keys (not v2)
   - Check if localhost is allowed for testing

3. **Score Always Below Threshold**
   - Adjust the threshold in controller (currently 0.5)
   - Test with normal user behavior (not automation)
   - Check reCAPTCHA admin console for score analytics

## Security Notes

?? **Important Security Considerations:**

1. **Key Management**: 
   - Never commit secret keys to public repositories
   - Use environment variables or Azure Key Vault in production
   - Keep `appsettings.json` out of version control

2. **Domain Restrictions**:
   - Register your production domain in Google reCAPTCHA console
   - Restrict keys to specific domains for security

3. **Score Tuning**:
   - Monitor score distribution in Google's admin console
   - Adjust threshold (0.5) based on your needs:
     - Higher (0.7): More strict, may block some humans
     - Lower (0.3): More lenient, may allow some bots

## Google reCAPTCHA Admin Console

Access your reCAPTCHA dashboard at:
https://www.google.com/recaptcha/admin

Here you can:
- View analytics and score distribution
- Monitor blocked requests
- Add/remove domains
- Regenerate keys if compromised

## Production Checklist

Before deploying to production:

- [ ] Update keys in `appsettings.json` or use environment variables
- [ ] Register production domain in Google reCAPTCHA console
- [ ] Test on production domain (keys may not work on localhost)
- [ ] Monitor scores and adjust threshold if needed
- [ ] Set up proper error logging for failed validations
- [ ] Consider adding rate limiting as additional protection

## Additional Protection

reCAPTCHA v3 is part of a defense-in-depth strategy. This app also includes:

- Account lockout after 3 failed login attempts
- Two-factor authentication (2FA)
- Password complexity requirements
- Session management and security stamps
- Audit logging

All these work together to provide comprehensive security.

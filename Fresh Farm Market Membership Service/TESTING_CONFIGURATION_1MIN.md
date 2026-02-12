# Testing Configuration - 1 Minute Timeouts

## Summary of Changes

All security timeout settings have been adjusted to **1 minute** for easier testing and demonstration purposes.

## Updated Settings

### 1. Session Timeout ??
**Location:** `Program.cs`
```csharp
options.ExpireTimeSpan = TimeSpan.FromMinutes(1); // Session timeout - Changed to 1 minute
```
- **Previous:** 30 minutes
- **Current:** 1 minute
- **Effect:** User sessions expire after 1 minute of inactivity

### 2. Session Idle Timeout ??
**Location:** `Program.cs`
```csharp
options.IdleTimeout = TimeSpan.FromMinutes(1);
```
- **Previous:** 30 minutes
- **Current:** 1 minute
- **Effect:** Session data expires after 1 minute of inactivity

### 3. Account Lockout Duration ??
**Location:** `Program.cs`
```csharp
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
```
- **Previous:** 5 minutes
- **Current:** 1 minute
- **Effect:** After 3 failed login attempts, account is locked for 1 minute

### 4. Minimum Password Age ??
**Location:** `MemberController.cs` - `ChangePassword` action
```csharp
if (timeSinceLastChange.TotalMinutes < 1)
{
    var secondsRemaining = (int)(60 - timeSinceLastChange.TotalSeconds);
    ModelState.AddModelError(string.Empty, 
        $"You can only change your password once every 1 minute. Please wait {secondsRemaining} more seconds.");
    return View(model);
}
```
- **Previous:** 5 minutes
- **Current:** 1 minute
- **Effect:** Users must wait 1 minute between password changes

### 5. Account Locked View Message ??
**Location:** `Views/Member/AccountLocked.cshtml`
```html
<p>Please try again in 1 minute.</p>
```
- **Previous:** "Please try again in 5 minutes."
- **Current:** "Please try again in 1 minute."

## Testing Guide

### Test 1: Account Lockout (1 minute)
1. Go to Login page
2. Enter incorrect password 3 times
3. Account will be locked for 1 minute
4. Wait 1 minute
5. Try logging in again - should work

### Test 2: Session Timeout (1 minute)
1. Log in successfully
2. Wait 1 minute without any activity
3. Try to navigate to a protected page
4. You'll be redirected to login (session expired)

### Test 3: Minimum Password Age (1 minute)
1. Log in successfully
2. Go to "Change Password"
3. Change your password
4. Immediately try to change password again
5. You'll get error: "You can only change your password once every 1 minute"
6. Wait 1 minute
7. Try again - should work

## Security Settings Still Active

### These settings remain unchanged:
- ? **Max Failed Login Attempts:** 3 attempts before lockout
- ? **Password Complexity:** 
  - Minimum 12 characters
  - Requires uppercase, lowercase, digit, and special character
- ? **Password History:** Cannot reuse last 2 passwords
- ? **Password Expiry:** 90 days (not changed for testing)
- ? **Two-Factor Authentication (2FA):** Enabled
- ? **reCAPTCHA v3:** Enabled on Login and Register
- ? **Anti-CSRF Tokens:** Enabled on all forms
- ? **Session Fixation Prevention:** Enabled
- ? **Concurrent Session Prevention:** Enabled
- ? **Audit Logging:** All activities logged

## Reverting to Production Settings

For production deployment, change these back to recommended values in `Program.cs` and `MemberController.cs`:

```csharp
// Session timeout: 30 minutes (or as per requirements)
options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
options.IdleTimeout = TimeSpan.FromMinutes(30);

// Account lockout: 5-15 minutes (industry standard)
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);

// Minimum password age: 1 day (24 hours) is recommended
if (timeSinceLastChange.TotalHours < 24)
```

## Notes for Demonstration

?? **Important:** These 1-minute timeouts are specifically for testing/demonstration purposes. 

**Why 1 minute is useful for testing:**
- Faster to demonstrate security features
- Don't have to wait 5-30 minutes during testing
- Can quickly show account lockout and session expiry
- Easier to verify all security mechanisms work correctly

**Production Recommendations:**
- Session Timeout: 15-30 minutes
- Account Lockout: 5-15 minutes
- Minimum Password Age: 1 day (24 hours)
- Maximum Password Age: 90 days

## Quick Test Checklist

Use this checklist to verify all security features:

- [ ] Register new account (reCAPTCHA validation)
- [ ] Login with correct credentials
- [ ] Wait 1 minute - verify session timeout
- [ ] Login with wrong password 3 times - verify lockout
- [ ] Wait 1 minute - verify lockout release
- [ ] Change password successfully
- [ ] Try to change password immediately - verify minimum age restriction
- [ ] Wait 1 minute - change password again successfully
- [ ] Try to reuse old password - verify password history enforcement
- [ ] Enable 2FA
- [ ] Logout and login with 2FA
- [ ] Check audit logs in database

## Configuration Files Modified

1. ? `Program.cs` - Session and lockout timeouts
2. ? `MemberController.cs` - Minimum password age
3. ? `Views/Member/AccountLocked.cshtml` - User-facing message

Build Status: ? **Successful**

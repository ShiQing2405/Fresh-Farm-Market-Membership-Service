# Account Lockout Debugging Guide - "Only 2 Attempts" Issue

## ?? PROBLEM IDENTIFIED

You're experiencing **only 2 failed attempts before lockout** instead of the configured 3 attempts.

## ?? ROOT CAUSE

The issue is in the login flow logic in `MemberController.cs`. Here's what's happening:

### Current Flow (Lines 253-283):

```csharp
// Attempt sign in with lockout enabled
var result = await _signInManager.PasswordSignInAsync(
    model.Email, 
    model.Password, 
    isPersistent: model.RememberMe, 
    lockoutOnFailure: true);  // ? This increments counter

// ...

if (result.IsLockedOut)
{
    // Account is locked (shows on 3rd attempt)
    return View("AccountLocked");
}

// Increment failed login count
await _userManager.AccessFailedAsync(user);  // ? This increments AGAIN!
var failedCount = await _userManager.GetAccessFailedCountAsync(user);

ModelState.AddModelError(string.Empty, $"Invalid login attempt. {3 - failedCount} attempts remaining.");
```

### The Problem:
1. **`PasswordSignInAsync` with `lockoutOnFailure: true`** already increments `AccessFailedCount`
2. **`AccessFailedAsync`** increments it AGAIN
3. **Result:** Double counting! Each failed attempt counts as 2

### Why You See Only 2 Attempts:
```
Attempt 1: Counter goes from 0 ? 2 (instead of 1)
           Message: "1 attempts remaining" (3 - 2 = 1)
           
Attempt 2: Counter goes from 2 ? 4 (instead of 3)
           But MaxFailedAccessAttempts = 3
           So: LOCKED!
```

---

## ? SOLUTION: Remove Duplicate Counter

### Fix the Login Method

Update `MemberController.cs` (Lines 253-283):

**Remove lines 276-277:**
```csharp
// ? REMOVE THESE LINES:
await _userManager.AccessFailedAsync(user);
var failedCount = await _userManager.GetAccessFailedCountAsync(user);
```

**Replace with:**
```csharp
// ? JUST GET THE COUNT (don't increment again):
var failedCount = await _userManager.GetAccessFailedCountAsync(user);
```

---

## ?? COMPLETE FIXED CODE

Replace the entire Login POST method with this:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
{
    // Validate reCAPTCHA
    var recaptcha = await _recaptchaService.Validate(Request);
    if (!recaptcha.success || recaptcha.score < 0.5)
    {
        ModelState.AddModelError(string.Empty, "reCAPTCHA validation failed. Please try again.");
        return View(model);
    }

    ViewData["ReturnUrl"] = returnUrl;

    if (!ModelState.IsValid)
        return View(model);

    var user = await _userManager.FindByEmailAsync(model.Email);
    if (user == null)
    {
        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        await _auditLogService.LogAsync("", model.Email, "Login Failed", "User not found");
        return View(model);
    }

    // Check if account is locked
    if (await _userManager.IsLockedOutAsync(user))
    {
        await _auditLogService.LogAsync(user.Id, user.Email, "Login Failed", "Account locked");
        return View("AccountLocked");
    }

    // Check password expiry
    if (user.PasswordExpiryDate.HasValue && user.PasswordExpiryDate.Value < DateTime.UtcNow)
    {
        await _auditLogService.LogAsync(user.Id, user.Email, "Login Failed", "Password expired");
        TempData["Message"] = "Your password has expired. Please reset your password.";
        return RedirectToAction("ChangePassword");
    }

    // Sign-out any existing session to prevent session fixation
    await _signInManager.SignOutAsync();

    // Attempt sign in with lockout enabled
    var result = await _signInManager.PasswordSignInAsync(
        model.Email, 
        model.Password, 
        isPersistent: model.RememberMe, 
        lockoutOnFailure: true);  // ? This handles the counter

    if (result.RequiresTwoFactor)
    {
        return RedirectToAction("Verify2fa", new { returnUrl });
    }

    if (result.Succeeded)
    {
        // Reset counter on successful login
        await _userManager.ResetAccessFailedCountAsync(user);
        
        // Update security stamp to invalidate other sessions
        await _userManager.UpdateSecurityStampAsync(user);
        
        await _auditLogService.LogAsync(user.Id, user.Email, "Login Success", "User logged in successfully");

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        
        return RedirectToAction("Index", "Home");
    }

    if (result.IsLockedOut)
    {
        await _auditLogService.LogAsync(user.Id, user.Email, "Login Failed", "Account locked due to multiple failed attempts");
        return View("AccountLocked");
    }

    // ? FIXED: Just GET the count (PasswordSignInAsync already incremented it)
    var failedCount = await _userManager.GetAccessFailedCountAsync(user);
    
    await _auditLogService.LogAsync(user.Id, user.Email, "Login Failed", $"Invalid credentials - Attempt {failedCount}");

    ModelState.AddModelError(string.Empty, $"Invalid login attempt. {3 - failedCount} attempts remaining.");
    return View(model);
}
```

---

## ?? HOW IT SHOULD WORK NOW

### Correct Flow (After Fix):

```
Attempt 1: Wrong password
           ?
           PasswordSignInAsync increments: 0 ? 1
           ?
           GetAccessFailedCountAsync: 1
           ?
           Message: "2 attempts remaining" (3 - 1 = 2) ?

Attempt 2: Wrong password
           ?
           PasswordSignInAsync increments: 1 ? 2
           ?
           GetAccessFailedCountAsync: 2
           ?
           Message: "1 attempts remaining" (3 - 2 = 1) ?

Attempt 3: Wrong password
           ?
           PasswordSignInAsync increments: 2 ? 3
           ?
           MaxFailedAccessAttempts (3) reached!
           ?
           result.IsLockedOut = true
           ?
           Show "AccountLocked.cshtml" ?
```

---

## ?? TESTING AFTER FIX

### Test Procedure:

1. **Restart your application** (to apply the code change)

2. **Create a test account or use existing**

3. **Reset lockout** (if already locked):
   ```sql
   UPDATE AspNetUsers 
   SET AccessFailedCount = 0, 
       LockoutEnd = NULL 
   WHERE Email = 'test@example.com';
   ```

4. **Test the 3 attempts:**

   **Attempt 1:**
   - Enter wrong password
   - ? Should see: "Invalid login attempt. **2 attempts remaining.**"
   
   **Attempt 2:**
   - Enter wrong password
   - ? Should see: "Invalid login attempt. **1 attempts remaining.**"
   
   **Attempt 3:**
   - Enter wrong password
   - ? Should be redirected to "Account Locked" page

5. **Wait 1 minute**

6. **Try to login with correct password**
   - ? Should succeed

---

## ?? DATABASE VERIFICATION

Check the counter in the database:

```sql
-- Check current status
SELECT 
    Email,
    AccessFailedCount,
    LockoutEnd,
    CASE 
        WHEN LockoutEnd IS NULL THEN 'Not Locked'
        WHEN LockoutEnd > GETUTCDATE() THEN 'LOCKED'
        ELSE 'Unlocked'
    END AS Status
FROM AspNetUsers
WHERE Email = 'test@example.com';
```

**After each failed attempt, check:**

```sql
-- Should increment by 1 each time (not 2!)
-- Attempt 1: AccessFailedCount = 1
-- Attempt 2: AccessFailedCount = 2
-- Attempt 3: AccessFailedCount = 3, LockoutEnd = (future time)
```

---

## ?? AUDIT LOG VERIFICATION

Check that attempts are logged correctly:

```sql
SELECT 
    Timestamp,
    UserEmail,
    Action,
    Details
FROM AuditLogs
WHERE UserEmail = 'test@example.com'
  AND Action = 'Login Failed'
ORDER BY Timestamp DESC;
```

**Should show:**
```
Details
-----------------------
Invalid credentials - Attempt 1
Invalid credentials - Attempt 2
Invalid credentials - Attempt 3
Account locked due to multiple failed attempts
```

---

## ?? ALTERNATIVE: Add Explicit Reset on Success

For extra clarity, you can also explicitly reset the counter on successful login:

```csharp
if (result.Succeeded)
{
    // ? Explicitly reset failed attempts counter
    await _userManager.ResetAccessFailedCountAsync(user);
    
    // Update security stamp to invalidate other sessions
    await _userManager.UpdateSecurityStampAsync(user);
    
    await _auditLogService.LogAsync(user.Id, user.Email, "Login Success", 
        "User logged in successfully");

    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
    {
        return Redirect(returnUrl);
    }
    
    return RedirectToAction("Index", "Home");
}
```

---

## ? SUMMARY OF CHANGES

| File | Line | Change | Reason |
|------|------|--------|--------|
| `MemberController.cs` | 276 | ? Remove `await _userManager.AccessFailedAsync(user);` | Duplicate counter |
| `MemberController.cs` | 277 | ? Keep `var failedCount = await _userManager.GetAccessFailedCountAsync(user);` | Just read count |
| `MemberController.cs` | 261 | ? Add `await _userManager.ResetAccessFailedCountAsync(user);` | Explicit reset (optional) |

---

## ?? WHY THIS HAPPENED

**ASP.NET Core Identity's `PasswordSignInAsync`** with `lockoutOnFailure: true` **automatically**:
1. ? Increments `AccessFailedCount` on failure
2. ? Checks if count >= `MaxFailedAccessAttempts`
3. ? Sets `LockoutEnd` if threshold reached
4. ? Resets counter on successful login

**You don't need to manually call `AccessFailedAsync`!**

---

## ?? REFERENCES

**Microsoft Documentation:**
- [PasswordSignInAsync](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.signinmanager-1.passwordsigninasync)
- [Account Lockout](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration#lockout)

**Your Documentation:**
- `SESSION_LOGIN_VERIFICATION.md` - Account lockout section
- `ACCOUNT_POLICIES_VERIFICATION.md` - Automatic recovery

---

## ?? IMPORTANT NOTE

After making this change:
1. ? Restart your application
2. ? Clear any existing lockouts in database
3. ? Test all 3 attempts
4. ? Update your documentation if needed

---

**Status After Fix:** ? **Will work correctly with 3 attempts**

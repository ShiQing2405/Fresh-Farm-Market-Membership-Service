# Quick Demo Guide for Tutor

## ?? Quick Feature Demonstration

### 1. Registration (4% + 10% + 6%)
**URL:** `/Member/Register`

**Demo Steps:**
1. Fill all fields with test data
2. Try weak password ? See red warning
3. Create strong password ? See green "STRONG" indicator
4. Upload .JPG photo
5. Submit ? Auto-logged in
6. Check database:
   - Password is hashed (AspNetUsers.PasswordHash)
   - Credit card is encrypted (AspNetUsers.EncryptedCreditCardNo)

**Key Features:**
- ? All 9 required fields present
- ? Duplicate email prevention
- ? Client-side password strength indicator
- ? Server-side validation
- ? Data encryption & password hashing
- ? Google reCAPTCHA v3 (invisible)

---

### 2. Session Management (10%)
**Demo Steps:**
1. Login as User A in Chrome
2. Open Incognito/New Browser
3. Login as User A again
4. Return to Chrome ? Should be logged out (security stamp invalidation)

**Key Features:**
- ? Multi-login detection
- ? 30-minute session timeout
- ? Automatic redirect after timeout
- ? Secure cookies (HttpOnly, Secure, SameSite)

---

### 3. Login/Logout with Audit (10%)
**URL:** `/Member/Login`

**Demo Steps:**
1. Try login with wrong password 3 times ? Account locks
2. See "Account Locked" page (5-minute lockout)
3. Check AuditLogs table ? All attempts logged with IP
4. Login successfully
5. Check AuditLogs ? Success entry with timestamp
6. Logout ? Check AuditLogs ? Logout entry

**Key Features:**
- ? Rate limiting (3 failed attempts)
- ? Automatic unlock after 5 minutes
- ? Audit logging for all actions
- ? IP address captured
- ? Remaining attempts shown to user

---

### 4. Google reCAPTCHA v3 (5%)
**Demo:**
1. Open browser DevTools (F12)
2. Go to Network tab
3. Submit login or registration form
4. See POST to `www.google.com/recaptcha/api/siteverify`
5. Server validates score (threshold: 0.5)

**Key Features:**
- ? Invisible - no user interaction
- ? On both Login and Registration
- ? Server-side validation
- ? Score-based (v3)

---

### 5. Input Validation (15%)
**Demo:**

**SQL Injection Prevention:**
- Try: `' OR '1'='1` in email field ? Fails validation
- Using EF Core parameterized queries

**XSS Prevention:**
- Try: `<script>alert('XSS')</script>` in About Me
- HTML encoded, displays as text

**CSRF Prevention:**
- Remove `@Html.AntiForgeryToken()` from form
- Submit ? 400 Bad Request

**Validation Examples:**
- Email: `invalid-email` ? Format error
- Phone: `abc123` ? Must be numeric
- Credit Card: `123` ? Must be 12-19 digits
- Password: `weak` ? Complexity error

**Key Features:**
- ? Client-side (jQuery Validation)
- ? Server-side (DataAnnotations)
- ? SQL Injection prevented (EF Core)
- ? XSS prevented (HTML encoding)
- ? CSRF tokens on all forms

---

### 6. Custom Error Pages (5%)
**Demo:**
- Navigate to `/nonexistent` ? 404 Page
- Navigate to `/Error/403` ? 403 Forbidden
- Navigate to `/Error/500` ? 500 Server Error

**Key Features:**
- ? Graceful error handling
- ? Custom branded error pages
- ? Proper HTTP status codes

---

### 7. Password Management (10%)
**Demo:**

**Change Password:**
1. URL: `/Member/ChangePassword`
2. Try changing immediately after previous change ? Error (5-min rule)
3. Try reusing old password ? Error (password history)
4. Use new strong password ? Success

**Reset Password:**
1. URL: `/Member/ForgotPassword`
2. Enter email ? Token generated
3. Click reset link (shown in TempData for demo)
4. Try reusing old password ? Error
5. Enter new password ? Success

**Password Expiry:**
- After 90 days, user prompted to change password on login

**Key Features:**
- ? Minimum password age (5 minutes)
- ? Maximum password age (90 days)
- ? Password history (prevents reuse of last 2)
- ? Secure token-based reset
- ? Automatic account unlock (5 min)

---

### 8. Two-Factor Authentication (5%)
**Demo:**

**Enable 2FA:**
1. Login ? Click "Enable 2FA"
2. Scan QR code with Google Authenticator
3. Enter 6-digit code ? Enabled

**Login with 2FA:**
1. Logout
2. Login with email/password
3. Redirected to `/Member/Verify2fa`
4. Enter 6-digit code from app
5. Success

**Key Features:**
- ? TOTP (Time-based One-Time Password)
- ? QR code generation
- ? Manual key entry option
- ? "Remember this device" option
- ? Audit logging

---

## ?? Database Verification

### Check Encryption & Hashing:
```sql
-- View encrypted credit card
SELECT Email, EncryptedCreditCardNo FROM AspNetUsers;

-- View password hash (should be long hash string)
SELECT Email, PasswordHash FROM AspNetUsers;

-- View audit logs
SELECT * FROM AuditLogs ORDER BY Timestamp DESC;

-- View password history
SELECT * FROM PasswordHistories ORDER BY CreatedAt DESC;

-- Check lockout status
SELECT Email, LockoutEnd, AccessFailedCount FROM AspNetUsers;
```

---

## ?? Grading Quick Reference

| Feature | Location | Points | Demo Time |
|---------|----------|--------|-----------|
| Registration | `/Member/Register` | 4% | 2 min |
| Password Strength | Registration page | 10% | 1 min |
| Data Security | Database check | 6% | 2 min |
| Session Mgmt | Multi-browser test | 10% | 3 min |
| Login/Audit | `/Member/Login` + DB | 10% | 3 min |
| reCAPTCHA | DevTools Network tab | 5% | 1 min |
| Input Validation | Try invalid inputs | 15% | 5 min |
| Error Pages | Navigate to errors | 5% | 1 min |
| Password Mgmt | Change/Reset password | 10% | 4 min |
| 2FA | Enable & verify | 5% | 3 min |
| **TOTAL** | | **80%** | **25 min** |

---

## ?? Test Credentials

After first registration, you can use:
- **Email:** test@example.com
- **Password:** Test@123456789 (or whatever you registered with)

---

## ?? Known Limitations (Out of Scope)

1. **Email Sending:** Reset password link shown in TempData (not sent via email)
   - In production, integrate with SendGrid/SMTP
   
2. **SMS 2FA:** Only authenticator app-based 2FA implemented
   - SMS option not required by assignment

3. **GitHub Testing:** Requires manual setup
   - Push to GitHub
   - Enable Actions
   - Run CodeQL analysis

---

## ?? Quick Notes for Tutor

1. **reCAPTCHA Keys:** Using Google's test keys (always pass)
   - Replace with real keys for production

2. **Database:** LocalDB (auto-created on first run)
   - Connection string in appsettings.json

3. **Migrations:** Already applied
   - If needed: `dotnet ef database update`

4. **Build:** 
   ```bash
   dotnet build
   dotnet run
   ```

5. **Default URL:** https://localhost:7XXX (HTTPS)

---

## ? Pre-Demo Checklist

- [ ] Database migrated (`dotnet ef database update`)
- [ ] Application builds successfully
- [ ] Test registration with strong password
- [ ] Verify database shows encrypted/hashed data
- [ ] Create test account for login demo
- [ ] Have Google Authenticator app ready (for 2FA)
- [ ] Open multiple browsers (for session test)
- [ ] Open SQL Server Management Studio (optional)

---

**Total Implementation:** 80% of assignment requirements  
**Demo Duration:** ~25 minutes  
**Status:** ? Ready for Submission

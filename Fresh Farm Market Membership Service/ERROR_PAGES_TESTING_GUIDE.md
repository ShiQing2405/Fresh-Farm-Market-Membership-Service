# Error Pages Testing Guide

## ?? Overview

Your application has the following custom error pages:
1. **404 - Not Found** (`Views/Error/NotFound.cshtml`)
2. **403 - Forbidden** (`Views/Error/Forbidden.cshtml`)
3. **500 - Server Error** (`Views/Error/ServerError.cshtml`)
4. **Generic Error** (`Views/Error/Error.cshtml`)
5. **Access Denied** (`Views/Member/AccessDenied.cshtml`)
6. **Account Locked** (`Views/Member/AccountLocked.cshtml`)

---

## ?? METHOD 1: Direct URL Navigation (Easiest)

### Test 404 - Not Found
Simply navigate to a URL that doesn't exist:

```
https://localhost:5001/this-page-does-not-exist
https://localhost:5001/Member/NonExistentAction
https://localhost:5001/FakeController/FakeAction
```

**Expected Result:** Shows custom 404 page with "Go to Home" button

---

### Test 403 - Forbidden
Navigate directly to the error endpoint:

```
https://localhost:5001/Error/403
```

**Expected Result:** Shows custom 403 page with "Access Forbidden" message

---

### Test 500 - Server Error
Navigate directly to the error endpoint:

```
https://localhost:5001/Error/500
```

**Expected Result:** Shows custom 500 page with "Internal Server Error" message

---

### Test Generic Error
Navigate directly to the error endpoint:

```
https://localhost:5001/Error
```

**Expected Result:** Shows generic error page

---

### Test Access Denied
Try to access a protected page without being logged in:

1. **Logout** (if logged in)
2. Navigate to:
```
https://localhost:5001/Home/Index
```
or
```
https://localhost:5001/Member/ChangePassword
```

**Expected Result:** Redirected to `/Member/AccessDenied` (or `/Member/Login` depending on configuration)

---

### Test Account Locked
1. Go to login page: `https://localhost:5001/Member/Login`
2. Enter wrong password **3 times**
3. **Expected Result:** Redirected to `/Member/AccountLocked`

---

## ?? METHOD 2: Create Test Controller (Temporary)

Add a temporary test controller to easily trigger different errors:

### Step 1: Create TestErrorController.cs

Create file: `Controllers/TestErrorController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;

namespace Fresh_Farm_Market_Membership_Service.Controllers
{
    // ?? ONLY FOR TESTING - Remove before production!
    public class TestErrorController : Controller
    {
        // Test 404
        [Route("test/404")]
        public IActionResult Test404()
        {
            return NotFound(); // Triggers 404 error page
        }

        // Test 403
        [Route("test/403")]
        public IActionResult Test403()
        {
            return StatusCode(403); // Triggers 403 error page
        }

        // Test 500
        [Route("test/500")]
        public IActionResult Test500()
        {
            return StatusCode(500); // Triggers 500 error page
        }

        // Test Exception (triggers 500)
        [Route("test/exception")]
        public IActionResult TestException()
        {
            throw new Exception("This is a test exception to trigger 500 error page");
        }

        // Test all error codes
        [Route("test/error/{code}")]
        public IActionResult TestError(int code)
        {
            return StatusCode(code);
        }
    }
}
```

### Step 2: Test the Error Pages

Run your application and navigate to:

```
https://localhost:5001/test/404       ? 404 Not Found page
https://localhost:5001/test/403       ? 403 Forbidden page
https://localhost:5001/test/500       ? 500 Server Error page
https://localhost:5001/test/exception ? 500 Server Error page (via exception)
https://localhost:5001/test/error/400 ? Test any error code
```

### Step 3: Remove Before Production
?? **Important:** Delete `TestErrorController.cs` before deploying to production!

---

## ?? METHOD 3: Temporarily Modify Existing Controller

### Test 500 Error by Throwing Exception

#### Option A: Add Test Action to HomeController

Temporarily add to `HomeController.cs`:

```csharp
// ?? TEMPORARY TEST METHOD - Remove after testing
[Route("test-error")]
public IActionResult TestError()
{
    throw new Exception("Testing 500 error page");
}
```

Navigate to: `https://localhost:5001/test-error`

**Expected:** Shows custom 500 error page

#### Option B: Break an Existing Action

Temporarily modify `HomeController.cs` ? `Index` action:

```csharp
[Authorize]
public async Task<IActionResult> Index()
{
    // ?? TEMPORARY: Throw exception to test error page
    throw new Exception("Test exception");
    
    // ... rest of code
}
```

Navigate to homepage after login ? Should show 500 error page

**Remember to revert this change after testing!**

---

## ?? METHOD 4: Browser Developer Tools

### Test 404 with DevTools

1. Open browser DevTools (F12)
2. Go to **Network** tab
3. Navigate to a non-existent URL
4. Check the response:
   - **Status Code:** 404
   - **Response:** Should show your custom HTML error page

### Test Other Status Codes

Use browser console to manually trigger status codes:

```javascript
// In browser console
fetch('/nonexistent').then(r => console.log(r.status)); // 404
```

---

## ?? METHOD 5: Test with Postman or curl

### Using curl (Command Line)

```bash
# Test 404
curl -I https://localhost:5001/nonexistent

# Test 403
curl -I https://localhost:5001/Error/403

# Test 500
curl -I https://localhost:5001/Error/500
```

### Using Postman

1. Create a new request
2. Set URL to: `https://localhost:5001/nonexistent`
3. Send request
4. Check:
   - **Status Code:** 404
   - **Response Body:** Your custom error page HTML

---

## ?? METHOD 6: Database Manipulation (For Account Locked)

### Force Account Lockout

Instead of trying wrong password 3 times, directly update database:

```sql
-- Force account lockout
UPDATE AspNetUsers 
SET LockoutEnd = DATEADD(MINUTE, 5, GETUTCDATE()),
    AccessFailedCount = 3
WHERE Email = 'test@example.com';
```

Now try to login ? Should show "Account Locked" page

---

## ?? METHOD 7: Integration Testing (Automated)

Create automated tests to verify error pages:

### Create Test File: `Tests/ErrorPagesTests.cs`

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

public class ErrorPagesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ErrorPagesTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Test404_ReturnsCustomErrorPage()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/nonexistent-page");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("404", content);
        Assert.Contains("Page Not Found", content);
    }

    [Fact]
    public async Task Test403_ReturnsCustomErrorPage()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Error/403");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("403", content);
        Assert.Contains("Forbidden", content);
    }
}
```

---

## ?? COMPLETE TESTING CHECKLIST

### ? 404 - Not Found
- [ ] Navigate to `/nonexistent-page`
- [ ] Check displays "404" and "Page Not Found"
- [ ] Check "Go to Home" button works
- [ ] Verify in Network tab: Status code 404

### ? 403 - Forbidden
- [ ] Navigate to `/Error/403`
- [ ] Check displays "403" and "Access Forbidden"
- [ ] Check "Go to Home" and "Login" buttons work

### ? 500 - Server Error
- [ ] Navigate to `/Error/500`
- [ ] Check displays "500" and "Internal Server Error"
- [ ] Check "Go to Home" button works

### ? Generic Error
- [ ] Navigate to `/Error`
- [ ] Check displays generic error message
- [ ] Check "Go to Home" button works

### ? Access Denied
- [ ] Logout
- [ ] Try to access `/Home/Index`
- [ ] Check redirected to login or access denied page

### ? Account Locked
- [ ] Enter wrong password 3 times on login
- [ ] Check displays "Account Locked" message
- [ ] Check message says "Please try again in 1 minute"
- [ ] Wait 1 minute
- [ ] Verify can login again

---

## ?? RECOMMENDED TESTING SEQUENCE

### Quick Test (5 minutes)

1. **Test 404:**
   - Navigate to `https://localhost:5001/fake-page`
   - ? See custom 404 page

2. **Test 403:**
   - Navigate to `https://localhost:5001/Error/403`
   - ? See custom 403 page

3. **Test 500:**
   - Navigate to `https://localhost:5001/Error/500`
   - ? See custom 500 page

4. **Test Access Denied:**
   - Logout ? Try to access `/Home/Index`
   - ? Redirected to login page

5. **Test Account Locked:**
   - Enter wrong password 3 times
   - ? See "Account Locked" page

### Complete Test (15 minutes)

Use the **Complete Testing Checklist** above to test all scenarios.

---

## ?? TROUBLESHOOTING

### Error Pages Not Showing?

**Problem:** Still seeing default ASP.NET error pages

**Solution:** Check `Program.cs` configuration:

```csharp
// Should have this configuration
app.UseStatusCodePagesWithReExecute("/Error/{0}");

// In production:
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error/Index");
}
```

**To Test in Development Mode:**

Temporarily change `Program.cs`:

```csharp
// Force production error handling
if (true) // Change this line temporarily
{
    app.UseExceptionHandler("/Error/Index");
    app.UseHsts();
}
```

---

### 404 Not Triggering Custom Page?

**Check ErrorController.cs:**

```csharp
[Route("Error/{statusCode}")]
public IActionResult Index(int statusCode)
{
    ViewData["StatusCode"] = statusCode;
    
    return statusCode switch
    {
        404 => View("NotFound"),     // ? Correct
        403 => View("Forbidden"),
        500 => View("ServerError"),
        _ => View("Error")
    };
}
```

---

### Exception Not Showing 500 Page?

**In Development:** You'll see detailed error page (by design)

**To Test 500 Page in Development:**

Add to `Program.cs`:

```csharp
// Temporarily force production error handling
app.UseExceptionHandler("/Error/Index");
// Comment out: app.UseDeveloperExceptionPage();
```

---

## ?? SCREENSHOT CHECKLIST FOR TUTOR

Take screenshots of each error page for demonstration:

1. ? 404 page with "Page Not Found"
2. ? 403 page with "Access Forbidden"
3. ? 500 page with "Internal Server Error"
4. ? Account Locked page
5. ? Browser DevTools showing 404 status code

---

## ?? DEMO SCRIPT FOR TUTOR

### 1. Show 404 Page (30 seconds)
```
Tutor: "Show me your 404 error page"
You: *Types: localhost:5001/nonexistent*
     *Shows custom 404 page*
     *Clicks "Go to Home" button*
     *Returns to homepage*
```

### 2. Show 403 Page (20 seconds)
```
Tutor: "Show me your 403 error page"
You: *Types: localhost:5001/Error/403*
     *Shows custom 403 page*
```

### 3. Show 500 Page (20 seconds)
```
Tutor: "Show me your 500 error page"
You: *Types: localhost:5001/Error/500*
     *Shows custom 500 page*
```

### 4. Show Account Lockout (1 minute)
```
Tutor: "Show me account lockout"
You: *Logout*
     *Enter wrong password 3 times*
     *Shows "Account Locked" page*
     *Point out: "Please try again in 1 minute"*
```

### 5. Show Error Handling in Code (30 seconds)
```
Tutor: "Show me the error handling code"
You: *Open Program.cs*
     *Show: app.UseStatusCodePagesWithReExecute("/Error/{0}")*
     *Open ErrorController.cs*
     *Show switch statement routing to different error pages*
```

---

## ?? QUICK TIPS

### Best Testing Method
For quick demonstration: **Method 1 (Direct URL Navigation)** is fastest

### Most Impressive for Tutor
Show **Method 4 (Browser DevTools)** to demonstrate:
- HTTP status codes
- Network traffic
- Professional debugging approach

### Most Thorough
Use **Complete Testing Checklist** to verify all error pages work correctly

---

## ?? REMEMBER

Before demonstration:
1. ? Test all error pages work
2. ? Remove any test controllers
3. ? Check error messages are user-friendly
4. ? Verify "Go to Home" buttons work
5. ? Ensure proper HTTP status codes returned

---

## ?? RELATED DOCUMENTATION

- `INPUT_VALIDATION_ERROR_HANDLING_VERIFICATION.md` - Complete error handling verification
- `DEMO_GUIDE_FOR_TUTOR.md` - Full demonstration guide
- `ASSIGNMENT_IMPLEMENTATION_COMPLETE.md` - All features overview

---

**Testing Status:** ? **READY FOR TESTING**
**Estimated Time:** 5-15 minutes for complete testing
**Difficulty:** ????? (Easy)

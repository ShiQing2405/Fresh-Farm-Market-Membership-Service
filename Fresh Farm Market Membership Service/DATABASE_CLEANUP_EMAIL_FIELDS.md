# Email Fields - Database Cleanup

## Problem Identified

Your database had **duplicate email storage** across two tables:

### Before Cleanup:
1. **`Members` table** (Legacy/Unused)
   - `Email` field
   - This table was from an earlier implementation
   - **Not being used** by the application

2. **`AspNetUsers` table** (ASP.NET Identity - Active)
   - `Email` field - Stores the actual email address
   - `NormalizedEmail` field - Stores UPPERCASE version for efficient lookups
   - **Currently in use** by the application

## Solution Applied

? **Removed the unused `Members` table completely**

### Changes Made:

1. **Deleted `Member.cs` model** - No longer needed
2. **Updated `ApplicationDbContext.cs`** - Removed `DbSet<Member> Members`
3. **Created migration** - `RemoveMembersTable` to drop the table from database

## Current Database Structure (After Fix)

### AspNetUsers Table (ASP.NET Identity)
Now you only have **ONE** set of user data with two email-related fields:

| Field | Purpose | Example |
|-------|---------|---------|
| `Email` | Stores the actual email address | `john.doe@example.com` |
| `NormalizedEmail` | Uppercase version for fast lookups | `JOHN.DOE@EXAMPLE.COM` |

### Why Two Email Fields in AspNetUsers?

This is by **design** in ASP.NET Identity for performance:

1. **`Email`** - User-friendly format for display
   - Shows to users as they typed it
   - Example: `John.Doe@Example.com`

2. **`NormalizedEmail`** - Used for database queries
   - Always uppercase for case-insensitive searches
   - Indexed for fast lookups
   - Example: `JOHN.DOE@EXAMPLE.COM`

### Benefits:
- ? Fast email lookups (case-insensitive)
- ? Prevents duplicate emails with different cases
- ? Maintains user's original formatting
- ? Database index on normalized field for performance

## Applying the Migration

### Option 1: Run PowerShell Script
```powershell
.\ApplyMigration.ps1
```

### Option 2: Manual Command
Open Package Manager Console in Visual Studio:
```
Update-Database
```

### Option 3: Using .NET CLI
Navigate to the project folder and run:
```bash
cd "Fresh Farm Market Membership Service"
dotnet ef database update
```

## Verification

After applying the migration:

### Check Database
1. Open SQL Server Object Explorer in Visual Studio
2. Expand your database (`FreshFarmMarketDb`)
3. Verify:
   - ? `AspNetUsers` table exists (with Email and NormalizedEmail)
   - ? `Members` table is gone

### Query to Verify Users
```sql
SELECT Id, UserName, Email, NormalizedEmail, FullName 
FROM AspNetUsers
```

## How Email is Used in the Application

### Registration (MemberController.cs)
```csharp
var user = new ApplicationUser
{
    UserName = model.Email,  // Uses email as username
    Email = model.Email       // Stores email
    // NormalizedEmail is set automatically by Identity
};
```

### Login (MemberController.cs)
```csharp
// Finds user by email (uses NormalizedEmail for lookup)
var user = await _userManager.FindByEmailAsync(model.Email);
```

### Display (Home/Index.cshtml)
```razor
<p><strong>Email:</strong> @ViewBag.Email</p>  <!-- Shows the Email field -->
```

## Important Notes

?? **All existing user accounts are in `AspNetUsers` table**
- Your registered users are safe
- They use the Identity system (AspNetUsers)
- The Members table was empty or had old test data

? **No data loss**
- All active user data is in AspNetUsers
- Only the unused Members table is removed

## Technical Details

### ApplicationUser Inheritance
```csharp
public class ApplicationUser : IdentityUser
```

By inheriting from `IdentityUser`, you automatically get:
- `Email` - string
- `NormalizedEmail` - string
- `UserName` - string
- `NormalizedUserName` - string
- `PasswordHash` - string
- `SecurityStamp` - string
- Plus many other Identity features (2FA, lockout, etc.)

### Custom Fields Added
Your custom fields on top of Identity:
- `FullName`
- `EncryptedCreditCardNo`
- `Gender`
- `MobileNo`
- `DeliveryAddress`
- `PhotoPath`
- `AboutMe`
- `CreatedAt`
- `LastPasswordChangedDate`
- `PasswordExpiryDate`

## Summary

? **Before:** Two tables, duplicate email fields, confusion
? `Members` table with Email
? `AspNetUsers` table with Email + NormalizedEmail

? **After:** One table, clean structure, no duplication
? `AspNetUsers` table with Email + NormalizedEmail (standard Identity pattern)

## Migration Files

- **Migration:** `Migrations/[timestamp]_RemoveMembersTable.cs`
- **Script:** `ApplyMigration.ps1` (helper script)

## Related Documentation

- `IDENTITY_IMPLEMENTATION_GUIDE.md` - Complete Identity setup
- `TESTING_CONFIGURATION_1MIN.md` - Testing configuration
- `RECAPTCHA_IMPLEMENTATION.md` - reCAPTCHA setup

---

**Status:** ? Code updated, migration created, ready to apply
**Action Required:** Run the migration to update your database

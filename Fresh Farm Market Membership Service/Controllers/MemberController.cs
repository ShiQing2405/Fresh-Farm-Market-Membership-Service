using Microsoft.AspNetCore.Mvc;
using Fresh_Farm_Market_Membership_Service.Models;
using Fresh_Farm_Market_Membership_Service.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using reCAPTCHA.AspNetCore;
using System.Text.Encodings.Web;

namespace Fresh_Farm_Market_Membership_Service.Controllers
{
    public class MemberController : Controller
    {
        private readonly IDataProtector _protector;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IAuditLogService _auditLogService;
        private readonly IPasswordHistoryService _passwordHistoryService;
        private readonly IRecaptchaService _recaptchaService;
        private readonly UrlEncoder _urlEncoder;
        private readonly IEmailService _emailService;

        public MemberController(
            IDataProtectionProvider provider,
            ApplicationDbContext context,
            IWebHostEnvironment env,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IAuditLogService auditLogService,
            IPasswordHistoryService passwordHistoryService,
            IRecaptchaService recaptchaService,
            UrlEncoder urlEncoder,
            IEmailService emailService)
        {
            _protector = provider.CreateProtector("CreditCardProtector");
            _context = context;
            _env = env;
            _userManager = userManager;
            _signInManager = signInManager;
            _auditLogService = auditLogService;
            _passwordHistoryService = passwordHistoryService;
            _recaptchaService = recaptchaService;
            _urlEncoder = urlEncoder;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Validate reCAPTCHA
            var recaptcha = await _recaptchaService.Validate(Request);
            if (!recaptcha.success || recaptcha.score < 0.5)
            {
                ModelState.AddModelError(string.Empty, "reCAPTCHA validation failed. Please try again.");
                return View(model);
            }

            if (!ModelState.IsValid)
                return View(model);

            // Check for duplicate email (prevents race condition)
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "This email address is already registered. Please use a different email or try logging in.");
                await _auditLogService.LogAsync("", model.Email, "Registration Failed", "Duplicate email attempt");
                return View(model);
            }

            // Handle photo upload
            string? photoPath = null;
            if (model.Photo != null && model.Photo.Length > 0)
            {
                var ext = Path.GetExtension(model.Photo.FileName).ToLowerInvariant();
                if (ext != ".jpg")
                {
                    ModelState.AddModelError("Photo", "Only .JPG files are allowed.");
                    return View(model);
                }

                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + ext;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Photo.CopyToAsync(stream);
                }

                photoPath = "/uploads/" + uniqueFileName;
            }

            // Encrypt credit card number
            string encryptedCreditCard = _protector.Protect(model.CreditCardNo);

            // Create user with Identity
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = HtmlEncoder.Default.Encode(model.FullName),
                EncryptedCreditCardNo = encryptedCreditCard,
                Gender = model.Gender,
                MobileNo = model.MobileNo,
                DeliveryAddress = HtmlEncoder.Default.Encode(model.DeliveryAddress),
                PhotoPath = photoPath,
                AboutMe = model.AboutMe != null ? HtmlEncoder.Default.Encode(model.AboutMe) : null,
                CreatedAt = DateTime.UtcNow,
                LastPasswordChangedDate = DateTime.UtcNow,
                PasswordExpiryDate = DateTime.UtcNow.AddDays(90) // 90 day max password age
            };

            try
            {
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Store password history
                    var passwordHash = _userManager.PasswordHasher.HashPassword(user, model.Password);
                    await _passwordHistoryService.AddPasswordHistoryAsync(user.Id, passwordHash);

                    await _auditLogService.LogAsync(user.Id, user.Email, "Registration Success", "User registered successfully");

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                // Handle Identity errors (including duplicate username/email if race condition occurs)
                foreach (var error in result.Errors)
                {
                    if (error.Code == "DuplicateUserName" || error.Code == "DuplicateEmail")
                    {
                        ModelState.AddModelError("Email", "This email address is already registered. Please use a different email or try logging in.");
                        await _auditLogService.LogAsync("", model.Email, "Registration Failed", $"Duplicate detected by Identity: {error.Description}");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
            catch (DbUpdateException ex)
            {
                // Handle database constraint violations (e.g., unique email constraint)
                if (ex.InnerException?.Message.Contains("IX_AspNetUsers_NormalizedEmail") == true ||
                    ex.InnerException?.Message.Contains("duplicate") == true)
                {
                    ModelState.AddModelError("Email", "This email address is already registered. Please use a different email or try logging in.");
                    await _auditLogService.LogAsync("", model.Email, "Registration Failed", "Database constraint violation - duplicate email");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "An error occurred while creating your account. Please try again.");
                    await _auditLogService.LogAsync("", model.Email, "Registration Failed", $"Database error: {ex.Message}");
                }
                
                // Clean up uploaded photo if user creation failed
                if (!string.IsNullOrEmpty(photoPath))
                {
                    var fullPath = Path.Combine(_env.WebRootPath, photoPath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle any other unexpected errors
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                await _auditLogService.LogAsync("", model.Email, "Registration Failed", $"Unexpected error: {ex.Message}");
                
                // Clean up uploaded photo if user creation failed
                if (!string.IsNullOrEmpty(photoPath))
                {
                    var fullPath = Path.Combine(_env.WebRootPath, photoPath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

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
                lockoutOnFailure: true);

            if (result.RequiresTwoFactor)
            {
                return RedirectToAction("Verify2fa", new { returnUrl });
            }

            if (result.Succeeded)
            {
                // Reset access failed count on successful login
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

            // Get failed login count (PasswordSignInAsync already incremented it)
            var failedCount = await _userManager.GetAccessFailedCountAsync(user);
            
            await _auditLogService.LogAsync(user.Id, user.Email, "Login Failed", $"Invalid credentials - Attempt {failedCount}");

            ModelState.AddModelError(string.Empty, $"Invalid login attempt. {3 - failedCount} attempts remaining.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await _auditLogService.LogAsync(user.Id, user.Email, "Logout", "User logged out");
            }

            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            // Check minimum password age (can't change within 1 minute)
            if (user.LastPasswordChangedDate.HasValue)
            {
                var timeSinceLastChange = DateTime.UtcNow - user.LastPasswordChangedDate.Value;
                if (timeSinceLastChange.TotalMinutes < 1)
                {
                    var secondsRemaining = (int)(60 - timeSinceLastChange.TotalSeconds);
                    ModelState.AddModelError(string.Empty, 
                        $"You can only change your password once every 1 minute. Please wait {secondsRemaining} more seconds.");
                    return View(model);
                }
            }

            // Check password reuse
            if (await _passwordHistoryService.IsPasswordReusedAsync(user.Id, model.NewPassword))
            {
                ModelState.AddModelError(string.Empty, 
                    "You cannot reuse any of your last 2 passwords. Please choose a different password.");
                return View(model);
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                // Update password tracking
                user.LastPasswordChangedDate = DateTime.UtcNow;
                user.PasswordExpiryDate = DateTime.UtcNow.AddDays(90);
                await _userManager.UpdateAsync(user);

                // Store in password history
                var newPasswordHash = _userManager.PasswordHasher.HashPassword(user, model.NewPassword);
                await _passwordHistoryService.AddPasswordHistoryAsync(user.Id, newPasswordHash);

                await _auditLogService.LogAsync(user.Id, user.Email, "Password Changed", "Password changed successfully");

                await _signInManager.RefreshSignInAsync(user);
                TempData["Message"] = "Password changed successfully.";
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user doesn't exist (security best practice)
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action("ResetPassword", "Member",
                new { token, email = user.Email }, Request.Scheme);

            // Send email with reset link
            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email, callbackUrl!);
                await _auditLogService.LogAsync(user.Id, user.Email, "Password Reset Requested", 
                    "Password reset email sent successfully");
            }
            catch (Exception ex)
            {
                // Log error and show user-friendly message
                await _auditLogService.LogAsync(user.Id, user.Email, "Password Reset Failed", 
                    $"Failed to send email: {ex.Message}");
                
                // Show error message to user
                TempData["EmailError"] = "Unable to send password reset email. Please contact support or try again later.";
            }

            return RedirectToAction("ForgotPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string? token = null, string? email = null)
        {
            if (token == null || email == null)
            {
                return RedirectToAction("Error", "Home");
            }

            var model = new ResetPasswordViewModel { Token = token, Email = email };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return RedirectToAction("ResetPasswordConfirmation");
            }

            // Check password reuse
            if (await _passwordHistoryService.IsPasswordReusedAsync(user.Id, model.Password))
            {
                ModelState.AddModelError(string.Empty, 
                    "You cannot reuse any of your last 2 passwords. Please choose a different password.");
                return View(model);
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

            if (result.Succeeded)
            {
                // Update password tracking
                user.LastPasswordChangedDate = DateTime.UtcNow;
                user.PasswordExpiryDate = DateTime.UtcNow.AddDays(90);
                await _userManager.UpdateAsync(user);

                // Store in password history
                var passwordHash = _userManager.PasswordHasher.HashPassword(user, model.Password);
                await _passwordHistoryService.AddPasswordHistoryAsync(user.Id, passwordHash);

                await _auditLogService.LogAsync(user.Id, user.Email, "Password Reset", "Password reset successfully");

                return RedirectToAction("ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Enable2fa()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            if (await _userManager.GetTwoFactorEnabledAsync(user))
            {
                return RedirectToAction("Index", "Home");
            }

            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            var model = new Enable2faViewModel
            {
                SharedKey = FormatKey(unformattedKey!),
                QrCodeUrl = GenerateQrCodeUri(user.Email!, unformattedKey!)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Enable2fa(Enable2faViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            var verificationCode = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
            var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

            if (!is2faTokenValid)
            {
                ModelState.AddModelError("Code", "Verification code is invalid.");
                return View(model);
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            await _auditLogService.LogAsync(user.Id, user.Email, "2FA Enabled", "Two-factor authentication enabled");

            TempData["Message"] = "Two-factor authentication has been enabled.";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Verify2fa(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify2fa(Verify2faViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var authenticatorCode = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
                authenticatorCode, model.RememberDevice, rememberClient: model.RememberDevice);

            if (result.Succeeded)
            {
                await _auditLogService.LogAsync(user.Id, user.Email, "2FA Login Success", "Logged in with 2FA");

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                await _auditLogService.LogAsync(user.Id, user.Email, "2FA Login Failed", "Account locked");
                return View("AccountLocked");
            }

            ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
            return View(model);
        }

        public IActionResult AccountLocked()
        {
            return View();
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        // API endpoint to check if email is already registered (for client-side validation)
        [HttpGet]
        public async Task<IActionResult> CheckEmailAvailability(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { available = false, message = "Email is required" });
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                return Json(new { available = false, message = "This email is already registered" });
            }

            return Json(new { available = true, message = "Email is available" });
        }

        private string FormatKey(string unformattedKey)
        {
            var result = new System.Text.StringBuilder();
            int currentPosition = 0;
            while (currentPosition + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
                currentPosition += 4;
            }
            if (currentPosition < unformattedKey.Length)
            {
                result.Append(unformattedKey.AsSpan(currentPosition));
            }

            return result.ToString().ToLowerInvariant();
        }

        private string GenerateQrCodeUri(string email, string unformattedKey)
        {
            return string.Format(
                "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6",
                _urlEncoder.Encode("FreshFarmMarket"),
                _urlEncoder.Encode(email),
                unformattedKey);
        }
    }
}

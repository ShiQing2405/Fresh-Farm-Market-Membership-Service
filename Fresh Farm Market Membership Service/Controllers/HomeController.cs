using Microsoft.AspNetCore.DataProtection;
using Fresh_Farm_Market_Membership_Service.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IDataProtector _protector;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(
        ILogger<HomeController> logger,
        ApplicationDbContext context,
        IDataProtectionProvider provider,
        UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _context = context;
        _protector = provider.CreateProtector("CreditCardProtector");
        _userManager = userManager;
    }

    public IActionResult Registration()
    {
        return View();
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login", "Member");

        // Decrypt credit card number
        string decryptedCreditCard = _protector.Unprotect(user.EncryptedCreditCardNo);

        // Pass data to view using ViewBag
        ViewBag.FullName = user.FullName;
        ViewBag.CreditCardNo = decryptedCreditCard;
        ViewBag.Gender = user.Gender;
        ViewBag.MobileNo = user.MobileNo;
        ViewBag.DeliveryAddress = user.DeliveryAddress;
        ViewBag.Email = user.Email;
        ViewBag.PhotoPath = user.PhotoPath;
        ViewBag.AboutMe = user.AboutMe;

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

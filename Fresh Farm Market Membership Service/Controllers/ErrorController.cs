using Microsoft.AspNetCore.Mvc;

namespace Fresh_Farm_Market_Membership_Service.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/{statusCode}")]
        public IActionResult Index(int statusCode)
        {
            ViewData["StatusCode"] = statusCode;
            
            return statusCode switch
            {
                404 => View("NotFound"),
                403 => View("Forbidden"),
                500 => View("ServerError"),
                _ => View("Error")
            };
        }

        [Route("Error")]
        public IActionResult Index()
        {
            return View("Error");
        }
    }
}

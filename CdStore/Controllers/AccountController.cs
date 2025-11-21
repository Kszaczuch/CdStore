using Microsoft.AspNetCore.Mvc;

namespace CdStore.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

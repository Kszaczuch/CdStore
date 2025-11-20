using System.Diagnostics;
using CdStore.Models;
using CdStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace CdStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            var albumy = _context.Albumy.ToList();
            return View(albumy);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Koszyk()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

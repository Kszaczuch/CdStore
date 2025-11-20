using System.Diagnostics;
using CdStore.Models;
using CdStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace CdStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly CartService _cartService;
        private const string CartCookieName = "CartId";

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, CartService cartService)
        {
            _logger = logger;
            _context = context;
            _cartService = cartService;
        }

        private string GetOrCreateCartId()
        {
            if (Request.Cookies.TryGetValue(CartCookieName, out var id) && !string.IsNullOrEmpty(id))
                return id;
            var newId = Guid.NewGuid().ToString();
            Response.Cookies.Append(CartCookieName, newId, new CookieOptions { HttpOnly = true, IsEssential = true });
            return newId;
        }

        public IActionResult Index()
        {
            var albumy = _context.Albumy.ToList();
            var cartId = GetOrCreateCartId();
            var cartItems = _cartService.GetCartItems(cartId);
            ViewBag.CartIds = cartItems;
            return View(albumy);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Koszyk()
        {
            var cartId = GetOrCreateCartId();
            var ids = _cartService.GetCartItems(cartId);
            var produkty = _context.Albumy.Where(a => ids.Contains(a.Id)).ToList();
            return View(produkty);
        }

        [HttpPost]
        public IActionResult AddToCart(int albumId)
        {
            var cartId = GetOrCreateCartId();
            var added = _cartService.Add(cartId, albumId);
            return Json(new { success = added });
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int albumId)
        {
            var cartId = GetOrCreateCartId();
            _cartService.Remove(cartId, albumId);
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult ClearCart()
        {
            var cartId = GetOrCreateCartId();
            _cartService.Clear(cartId);
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult Buy()
        {
            var cartId = GetOrCreateCartId();
            _cartService?.Clear(cartId);
            return Json(new { success = true });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

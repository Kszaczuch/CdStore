using CdStore.Models;
using CdStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace CdStore.Controllers
{
    [Authorize]
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
            if (User?.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                    return userId;
            }

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

        [Authorize(Roles = "Admin")]
        public IActionResult Privacy(int? id)
        {
            var albumy = _context.Albumy.ToList();
            if (id.HasValue)
            {
                var selected = _context.Albumy.Find(id.Value);
                ViewBag.SelectedAlbum = selected;
            }
            return View(albumy);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveProductForm(Album model)
        {
            if (model == null) return RedirectToAction("Privacy");

            if (model.Id == 0)
            {
                _context.Albumy.Add(model);
            }
            else
            {
                var existing = _context.Albumy.Find(model.Id);
                if (existing != null)
                {
                    existing.Tytul = model.Tytul;
                    existing.Artysta = model.Artysta;
                    existing.Cena = model.Cena;
                    existing.OkladkaLink = model.OkladkaLink;
                    _context.Albumy.Update(existing);
                }
            }

            _context.SaveChanges();
            return RedirectToAction("Privacy");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteProductForm(int id)
        {
            var album = _context.Albumy.Find(id);
            if (album != null)
            {
                _context.Albumy.Remove(album);
                _context.SaveChanges();
            }
            return RedirectToAction("Privacy");
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

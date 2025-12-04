using CdStore.Models;
using CdStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Globalization;
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



        [AllowAnonymous]
        public IActionResult Index(IndexHomeVm model )
        {

            var query = _context.Albumy.Include(a => a.Kategoria).AsQueryable();

            if (model.kategoriaId.HasValue && model.kategoriaId.Value != 0)
            {
                query = query.Where(a => a.KategoriaId == model.kategoriaId.Value);
            }


            if (!string.IsNullOrEmpty(model.availability))
            {
                if (model.availability == "in")
                {
                    query = query.Where(a => a.IloscNaStanie > 0);
                }
                else if (model.availability == "out")
                {
                    query = query.Where(a => a.IloscNaStanie == 0);
                }
            }

            if (string.IsNullOrEmpty(model.sort)) model.sort = "name_asc";
            switch (model.sort)
            {
                case "price_desc":
                    query = query.OrderByDescending(a => a.Cena);
                    break;
                case "name_desc":
                    query = query.OrderByDescending(a => a.Tytul);
                    break;
                case "price_asc":
                    query = query.OrderBy(a => a.Cena);
                    break;
                default:
                    query = query.OrderBy(a => a.Tytul);
                    break;
            }

            var albumy = query.ToList();

            var categories = _context.Kategorie.OrderBy(c => c.Nazwa).ToList();
            ViewBag.Categories = categories;
            ViewBag.SelectedCategoryId = model.kategoriaId;
            ViewBag.SelectedAvailability = string.IsNullOrEmpty(model.availability) ? "all" : model.availability;
            ViewBag.SelectedSort = model.sort;

            var cartId = GetOrCreateCartId();
            var cartItems = _cartService.GetCartItems(cartId);
            ViewBag.CartIds = cartItems;
            model.Albums = albumy;
            return View(model);
        }

        [AllowAnonymous]
        public IActionResult Detale(int id)
        {
            var album = _context.Albumy.Find(id);
            if (album == null) return NotFound();
            var cartId = GetOrCreateCartId();
            var cartItems = _cartService.GetCartItems(cartId);
            ViewBag.CartIds = cartItems;
            return View(album);
        }

        [AllowAnonymous]
        public IActionResult Regulamin()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult PolitykaPrywatnosci()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Privacy(int? id)
        {
            var albumy = _context.Albumy.Include(a => a.Kategoria).ToList();

            var categories = _context.Kategorie.OrderBy(c => c.Nazwa).ToList();
            ViewBag.Categories = categories;

            if (id.HasValue)
            {
                var selected = _context.Albumy.Include(a => a.Kategoria).FirstOrDefault(a => a.Id == id.Value);
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
            if (!string.IsNullOrEmpty(model.Opis))
            {
                model.Opis = model.Opis.Replace("\r\n", "\n");
            }

            decimal parsedCena = model.Cena;
            var cenaStr = Request.Form["Cena"].ToString();
            if (!string.IsNullOrWhiteSpace(cenaStr))
            {
                if (!decimal.TryParse(cenaStr, NumberStyles.Number, CultureInfo.InvariantCulture, out parsedCena))
                {
                    decimal.TryParse(cenaStr, NumberStyles.Number, CultureInfo.CurrentCulture, out parsedCena);
                }
            }
            model.Cena = parsedCena;

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
                    existing.Opis = model.Opis;
                    existing.IloscNaStanie = model.IloscNaStanie;
                    existing.KategoriaId = model.KategoriaId;
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
            var ids = _cartService.GetCartItems(cartId);
            if (ids == null || !ids.Any())
                return Json(new { success = false, message = "Koszyk jest pusty." });

            var albumy = _context.Albumy.Where(a => ids.Contains(a.Id)).ToList();
            var outOfStock = albumy.Where(a => a.IloscNaStanie <= 0).ToList();

            if (outOfStock.Any())
            {
                var names = outOfStock.Select(a => $"{a.Tytul} - {a.Artysta}");
                return Json(new { success = false, message = "Brak dostêpnoœci: " + string.Join(", ", names) });
            }

            foreach (var a in albumy)
            {
                if (a.IloscNaStanie > 0)
                    a.IloscNaStanie--;
            }

            _context.SaveChanges();

            _cartService.Clear(cartId);
            return Json(new { success = true });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

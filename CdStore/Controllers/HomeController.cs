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

        private bool IsCurrentUserBlocked()
        {
            if (!(User?.Identity?.IsAuthenticated == true)) return false;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return false;
            var u = _context.Users.Find(userId);
            return u?.IsBlocked ?? false;
        }

        [AllowAnonymous]
        public IActionResult Index(IndexHomeVm model)
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

            var favoriteIds = new List<int>();
            if (User?.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    favoriteIds = _context.Favorites
                        .Where(f => f.UserId == userId)
                        .Select(f => f.AlbumId)
                        .ToList();
                }
            }
            ViewBag.FavoriteIds = favoriteIds;

            model.Albums = albumy;
            return View(model);
        }

        [AllowAnonymous]
        public IActionResult Detale(int id)
        {
            var album = _context.Albumy
                .Include(a => a.Kategoria)
                .FirstOrDefault(a => a.Id == id);
            if (album == null) return NotFound();
            var cartId = GetOrCreateCartId();
            var cartItems = _cartService.GetCartItems(cartId);
            ViewBag.CartIds = cartItems;

            var favoriteIds = new List<int>();
            if (User?.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    favoriteIds = _context.Favorites
                        .Where(f => f.UserId == userId)
                        .Select(f => f.AlbumId)
                        .ToList();
                }
            }
            ViewBag.FavoriteIds = favoriteIds;

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

        [Authorize(Roles = "Admin")]
        public IActionResult Gatunki(int? id)
        {
            var categories = _context.Kategorie.OrderBy(c => c.Nazwa).ToList();
            if (id.HasValue)
            {
                var selected = _context.Kategorie.Find(id.Value);
                ViewBag.SelectedCategory = selected;
            }
            return View(categories);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveCategoryForm(CdStore.Models.Kategoria model)
        {
            if (model == null) return RedirectToAction("Gatunki");
            if (string.IsNullOrWhiteSpace(model.Nazwa))
            {
                return RedirectToAction("Gatunki");
            }

            if (model.Id == 0)
            {
                _context.Kategorie.Add(model);
            }
            else
            {
                var existing = _context.Kategorie.Find(model.Id);
                if (existing != null)
                {
                    existing.Nazwa = model.Nazwa;
                    _context.Kategorie.Update(existing);
                }
            }

            _context.SaveChanges();
            return RedirectToAction("Gatunki");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCategoryForm(int id)
        {
            var cat = _context.Kategorie.Find(id);
            if (cat != null)
            {
                _context.Kategorie.Remove(cat);
                _context.SaveChanges();
            }
            return RedirectToAction("Gatunki");
        }

        public IActionResult Koszyk()
        {
            var cartId = GetOrCreateCartId();
            var cartDetailed = _cartService.GetCartItemsDetailed(cartId);
            var ids = cartDetailed.Select(ci => ci.AlbumId).ToList();
            var produkty = _context.Albumy.Where(a => ids.Contains(a.Id)).ToList();

            var quantities = cartDetailed.ToDictionary(ci => ci.AlbumId, ci => ci.Quantity);

            ViewBag.CartQuantities = quantities;
            ViewBag.IsBlocked = IsCurrentUserBlocked();
            ViewBag.ErrorMessage = TempData["Error"] as string;

            return View(produkty);
        }

        [HttpPost]
        public IActionResult AddToCart(int albumId, int quantity = 1)
        {
            if (User?.Identity?.IsAuthenticated == true && IsCurrentUserBlocked())
            {
                return Json(new { success = false, message = "Twoje konto jest zablokowane. Nie mo¿esz dodawaæ produktów do koszyka." });
            }

            var cartId = GetOrCreateCartId();
            var added = _cartService.Add(cartId, albumId, quantity);
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
            if (User?.Identity?.IsAuthenticated == true && IsCurrentUserBlocked())
            {
                return Json(new { success = false, message = "Twoje konto jest zablokowane. Nie mo¿esz realizowaæ zakupów." });
            }

            var cartId = GetOrCreateCartId();

            var cartDetailed = _cartService.GetCartItemsDetailed(cartId);

            if (cartDetailed == null || !cartDetailed.Any())
                return Json(new { success = false, message = "Koszyk jest pusty." });

            var albumIds = cartDetailed.Select(ci => ci.AlbumId).ToList();
            var albumy = _context.Albumy.Where(a => albumIds.Contains(a.Id)).ToList();

            var outOfStock = new List<string>();
            foreach (var ci in cartDetailed)
            {
                var alb = albumy.FirstOrDefault(a => a.Id == ci.AlbumId);
                if (alb == null) { outOfStock.Add($"#{ci.AlbumId} (brak produktu)"); continue; }
                if (ci.Quantity <= 0) { outOfStock.Add($"{alb.Tytul} - nieprawid³owa iloœæ"); continue; }
                if (ci.Quantity > 5) { outOfStock.Add($"{alb.Tytul} - wiêcej ni¿ 5"); continue; }
                if (ci.Quantity > alb.IloscNaStanie) { outOfStock.Add($"{alb.Tytul} - dostêpne: {alb.IloscNaStanie}"); }
            }

            if (outOfStock.Any())
            {
                return Json(new { success = false, message = "Problem z dostêpnoœci¹: " + string.Join(", ", outOfStock) });
            }

            foreach (var ci in cartDetailed)
            {
                var alb = albumy.FirstOrDefault(a => a.Id == ci.AlbumId);
                if (alb != null && alb.IloscNaStanie >= ci.Quantity)
                {
                    alb.IloscNaStanie -= ci.Quantity;
                }
            }

            _context.SaveChanges();

            _cartService.Clear(cartId);
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult UpdateCartQuantity(int albumId, int quantity)
        {
            var cartId = GetOrCreateCartId();
            var album = _context.Albumy.Find(albumId);
            if (album == null)
                return Json(new { success = false, message = "Produkt nie istnieje." });

            var maxAllowed = Math.Min(5, album.IloscNaStanie);
            if (quantity < 1) quantity = 1;
            if (quantity > maxAllowed) quantity = maxAllowed;

            var ok = _cartService.SetQuantity(cartId, albumId, quantity);
            if (!ok)
                return Json(new { success = false, message = "Nie uda³o siê zaktualizowaæ koszyka." });

            var cartDetailed = _cartService.GetCartItemsDetailed(cartId);
            var albumIds = cartDetailed.Select(ci => ci.AlbumId).ToList();
            var albums = _context.Albumy.Where(a => albumIds.Contains(a.Id)).ToList();

            var subtotal = album.Cena * quantity;
            var total = albums.Sum(a =>
            {
                var q = cartDetailed.FirstOrDefault(ci => ci.AlbumId == a.Id)?.Quantity ?? 1;
                return a.Cena * q;
            });

            return Json(new
            {
                success = true,
                quantity = quantity,
                subtotal = subtotal.ToString("C"),
                total = total.ToString("C")
            });
        }

        public IActionResult Favorites()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var favoriteIds = _context.Favorites
                .Where(f => f.UserId == userId)
                .Select(f => f.AlbumId)
                .ToList();

            var albums = _context.Albumy
                .Include(a => a.Kategoria)
                .Where(a => favoriteIds.Contains(a.Id))
                .ToList();

            var cartId = GetOrCreateCartId();
            var cartItems = _cartService.GetCartItems(cartId);
            ViewBag.CartIds = cartItems;
            ViewBag.FavoriteIds = favoriteIds;

            return View(albums);
        }

        [HttpPost]
        public IActionResult AddToFavorites(int albumId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Json(new { success = false });

            if (!_context.Albumy.Any(a => a.Id == albumId)) return Json(new { success = false });

            var exists = _context.Favorites.Any(f => f.UserId == userId && f.AlbumId == albumId);
            if (!exists)
            {
                _context.Favorites.Add(new Favorite { UserId = userId, AlbumId = albumId });
                _context.SaveChanges();
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult RemoveFromFavorites(int albumId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Json(new { success = false });

            var fav = _context.Favorites.FirstOrDefault(f => f.UserId == userId && f.AlbumId == albumId);
            if (fav != null)
            {
                _context.Favorites.Remove(fav);
                _context.SaveChanges();
            }

            return Json(new { success = true });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
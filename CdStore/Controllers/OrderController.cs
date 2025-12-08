using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CdStore.Models;
using CdStore.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using CdStore.ViewModels;

namespace CdStore.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CartService _cartService;

        public OrderController(ApplicationDbContext context, CartService cartService)
        {
            _context = context;
            _cartService = cartService;
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        public IActionResult Checkout()
        {
            var userId = GetUserId();
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);

            var cartId = userId ?? Request.Cookies["CartId"];
            var ids = _cartService.GetCartItems(cartId);

            var items = _context.Albumy
                .Where(a => ids.Contains(a.Id))
                .ToList();

            var vm = new CheckoutVm()
            {
                FirstName = string.Empty,// user.FirstName,
                LastName = string.Empty, //user.LastName,
                Address = string.Empty,//user.Address,
                Phone = user.PhoneNumber,
                Email = user.Email,
                CartItems = items,
                Total = items.Sum(x => x.Cena)
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout(CheckoutVm model)
        {
            // Reload cart items to redisplay form
            var cartId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Request.Cookies["CartId"];
            var ids = _cartService.GetCartItems(cartId);
            model.CartItems = _context.Albumy.Where(a => ids.Contains(a.Id)).ToList();
            model.Total = model.CartItems.Sum(x => x.Cena);


            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartId2 = userId ?? Request.Cookies["CartId"];
            var ids2 = _cartService.GetCartItems(cartId2);
            var cartItems = _context.Albumy.Where(a => ids2.Contains(a.Id)).ToList();

            if (!cartItems.Any())
                return RedirectToAction("Koszyk", "Home");

            // create Order
            var order = new Order
            {
                UserId = userId,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Address = model.Address,
                Phone = model.Phone,
                Email = model.Email,
                CreatedAt = DateTime.UtcNow,
                IsPaid = false,
                Total = cartItems.Sum(x => x.Cena)
            };

            foreach (var album in cartItems)
            {
                order.Items.Add(new OrderItem
                {
                    AlbumId = album.Id,
                    Quantity = 1,
                    UnitPrice = album.Cena
                });

                album.IloscNaStanie--;
            }

            _context.Orders.Add(order);
            _context.SaveChanges();

            _cartService.Clear(cartId2);

            return RedirectToAction("OrderSummary", new { id = order.Id });
        }


        public IActionResult OrderSummary(int id)
        {
            var order = _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Album)
                .Include(o => o.Receipt)
                .FirstOrDefault(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }


        [HttpPost]
        public IActionResult Pay(int id, string method = "Card")
        {
            var order = _context.Orders
                .Include(o => o.Receipt)
                .FirstOrDefault(o => o.Id == id);

            if (order == null) return NotFound();
            if (order.IsPaid) return Json(new { success = false, msg = "Order already paid." });

            // mark as paid
            order.IsPaid = true;

            // create receipt
            var receipt = new Receipt
            {
                OrderId = order.Id,
                Number = $"R-{order.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                PaymentMethod = Enum.Parse<PaymentMethod>(method)
            };

            _context.Receipts.Add(receipt);
            _context.SaveChanges();

            return Json(new { success = true });
        }
    }
}

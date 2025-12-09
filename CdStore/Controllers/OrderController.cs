using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CdStore.Models;
using CdStore.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using CdStore.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;


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

        private bool IsUserBlocked(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return false;
            var u = _context.Users.Find(userId);
            return u?.IsBlocked ?? false;
        }

        public IActionResult Checkout()
        {
            var userId = GetUserId();

            if (!string.IsNullOrEmpty(userId) && IsUserBlocked(userId))
            {
                TempData["Error"] = "Twoje konto jest zablokowane. Nie możesz składać zamówień.";
                return RedirectToAction("Koszyk", "Home");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);

            var cartId = userId ?? Request.Cookies["CartId"];
            var ids = _cartService.GetCartItems(cartId);

            var items = _context.Albumy
                .Where(a => ids.Contains(a.Id))
                .ToList();

            var firstName = string.Empty;
            var lastName = string.Empty;
            if (user != null && !string.IsNullOrWhiteSpace(user.FullName))
            {
                var parts = user.FullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                firstName = parts.Length > 0 ? parts[0] : string.Empty;
                lastName = parts.Length > 1 ? parts[1] : string.Empty;
            }

            var vm = new CheckoutVm()
            {

                FirstName = firstName,
                LastName = lastName,
                Address = user?.DeliveryAddress ?? string.Empty,
                Phone = user?.PhoneNumber,
                Email = user?.Email,
                CartItems = items,
                Total = items.Sum(x => x.Cena)
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout(CheckoutVm model)
        {
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

            var currentUserId = GetUserId();
            if (!User.IsInRole("Admin") && order.UserId != currentUserId)
                return NotFound();

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

            if (!string.IsNullOrEmpty(order.UserId) && IsUserBlocked(order.UserId))
            {
                return Json(new { success = false, msg = "Konto właściciela zamówienia jest zablokowane. Płatność niemożliwa." });
            }


            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(currentUserId) && IsUserBlocked(currentUserId))
            {
                return Json(new { success = false, msg = "Twoje konto jest zablokowane. Nie możesz dokonywać płatności." });
            }

            order.IsPaid = true;

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

        [Authorize(Roles = "Admin")]
        public IActionResult AllOrders()
        {
            var orders = _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Album)
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            return View(orders);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangeStatus(int id, string status)
        {
            var order = _context.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null) return NotFound();

            if (string.IsNullOrWhiteSpace(status))
            {
                TempData["Error"] = "Nieprawidłowy status.";
                return RedirectToAction("AllOrders");
            }

            if (!TryMapStringToStatus(status, out var newStatus))
            {
                TempData["Error"] = "Nieprawidłowy status.";
                return RedirectToAction("AllOrders");
            }

            order.Status = newStatus;
            _context.SaveChanges();

            return RedirectToAction("AllOrders");
        }

        private bool TryMapStringToStatus(string s, out OrderStatus status)
        {
            status = OrderStatus.Oczekujace;
            if (string.IsNullOrWhiteSpace(s)) return false;

            switch (s.Trim().ToLowerInvariant())
            {
                case "oczekujace":
                case "oczekujące":
                    status = OrderStatus.Oczekujace;
                    return true;
                case "wyslane":
                case "wysłane":
                    status = OrderStatus.Wyslane;
                    return true;
                case "dostarczone":
                    status = OrderStatus.Dostarczone;
                    return true;
                default:
                    return false;
            }
        }


        [HttpGet]
        public IActionResult DownloadReceipt(int id)
        {
            var order = _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Album)
                .Include(o => o.Receipt)
                .FirstOrDefault(o => o.Id == id);

            if (order == null) return NotFound();

            var userId = GetUserId();
            if (!User.IsInRole("Admin") && order.UserId != userId)
                return Forbid();

            var receipt = order.Receipt;
            if (receipt == null) return NotFound();

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text($"Paragon {receipt.Number}")
                        .FontSize(16)
                        .SemiBold();

                    page.Content().Column(column =>
                    {
                        column.Item().Text($"Data wystawienia: {receipt.IssuedAt.ToLocalTime():g}");
                        column.Item().Text($"Metoda płatności: {receipt.PaymentMethod}");
                        column.Item().PaddingVertical(5).LineHorizontal(1);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(); // nazwa
                                columns.ConstantColumn(60); // ilość
                                columns.ConstantColumn(90); // cena jedn.
                                columns.ConstantColumn(90); // razem
                            });

                            // nagłówki
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Produkt").SemiBold();
                                header.Cell().Element(CellStyle).AlignCenter().Text("Ilość").SemiBold();
                                header.Cell().Element(CellStyle).AlignRight().Text("Cena jedn.").SemiBold();
                                header.Cell().Element(CellStyle).AlignRight().Text("Razem").SemiBold();
                            });

                            foreach (var it in order.Items)
                            {
                                var title = $"{it.Album?.Tytul ?? "[brak]"} – {it.Album?.Artysta ?? ""}";
                                table.Cell().Element(CellStyle).Text(title);
                                table.Cell().Element(CellStyle).AlignCenter().Text(it.Quantity.ToString());
                                table.Cell().Element(CellStyle).AlignRight().Text(it.UnitPrice.ToString("C"));
                                table.Cell().Element(CellStyle).AlignRight().Text((it.UnitPrice * it.Quantity).ToString("C"));
                            }

                            // stopka tabeli
                            table.Footer(footer =>
                            {
                                footer.Cell().ColumnSpan(3).AlignRight().Text("Razem:").SemiBold();
                                footer.Cell().AlignRight().Text(order.Total.ToString("C")).SemiBold();
                            });

                            static IContainer CellStyle(IContainer container)
                            {
                                return container.PaddingVertical(5).PaddingLeft(2).PaddingRight(2);
                            }
                        });

                        column.Item().PaddingTop(10).AlignRight().Text("Dziękujemy za zakupy!").Italic();
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.CurrentPageNumber();
                            text.Span(" / ");
                            text.TotalPages();
                        });
                });
            });

            var pdfBytes = doc.GeneratePdf();
            var fileName = $"Paragon_{receipt.Number}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
    }
}
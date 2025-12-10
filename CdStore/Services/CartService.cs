using CdStore.Models;
using Microsoft.EntityFrameworkCore;

namespace CdStore.Services
{
    public class CartService
    {
        private readonly ApplicationDbContext _db;

        public CartService(ApplicationDbContext db)
        {
            _db = db;
        }

        public bool Add(string cartId, int albumId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(cartId)) return false;
            if (quantity <= 0) return false;

            var album = _db.Albumy.Find(albumId);
            if (album == null) return false;

            var maxAllowed = Math.Min(5, album.IloscNaStanie);
            if (maxAllowed <= 0) return false;

            var existing = _db.CartItems.FirstOrDefault(ci => ci.CartId == cartId && ci.AlbumId == albumId);
            if (existing == null)
            {
                var qty = Math.Min(quantity, maxAllowed);
                _db.CartItems.Add(new CartItem { CartId = cartId, AlbumId = albumId, Quantity = qty });
            }
            else
            {
                var newQty = existing.Quantity + quantity;
                existing.Quantity = Math.Min(newQty, maxAllowed);
                _db.CartItems.Update(existing);
            }

            _db.SaveChanges();
            return true;
        }

        public void Remove(string cartId, int albumId)
        {
            if (string.IsNullOrEmpty(cartId)) return;
            var items = _db.CartItems.Where(ci => ci.CartId == cartId && ci.AlbumId == albumId).ToList();
            if (!items.Any()) return;
            _db.CartItems.RemoveRange(items);
            _db.SaveChanges();
        }

        public List<int> GetCartItems(string cartId)
        {
            if (string.IsNullOrEmpty(cartId)) return new List<int>();
            return _db.CartItems
                      .Where(ci => ci.CartId == cartId && ci.Quantity > 0)
                      .Select(ci => ci.AlbumId)
                      .Distinct()
                      .ToList();
        }

        public List<CartItem> GetCartItemsDetailed(string cartId)
        {
            if (string.IsNullOrEmpty(cartId)) return new List<CartItem>();

            var items = _db.CartItems
                           .Where(ci => ci.CartId == cartId)
                           .ToList();

            var invalid = items.Where(i => i.Quantity <= 0).ToList();
            if (invalid.Any())
            {
                _db.CartItems.RemoveRange(invalid);
                _db.SaveChanges();

                items = _db.CartItems
                           .Where(ci => ci.CartId == cartId)
                           .ToList();
            }

            return items;
        }

        public bool SetQuantity(string cartId, int albumId, int quantity)
        {
            if (string.IsNullOrEmpty(cartId)) return false;

            var item = _db.CartItems.FirstOrDefault(ci => ci.CartId == cartId && ci.AlbumId == albumId);
            if (item == null) return false;

            var album = _db.Albumy.Find(albumId);
            if (album == null) return false;

            var maxAllowed = Math.Min(5, album.IloscNaStanie);

            if (quantity <= 0)
            {
                _db.CartItems.Remove(item);
            }
            else
            {
                item.Quantity = Math.Min(quantity, Math.Max(1, maxAllowed));
                _db.CartItems.Update(item);
            }

            _db.SaveChanges();
            return true;
        }

        public void Clear(string cartId)
        {
            if (string.IsNullOrEmpty(cartId)) return;
            var items = _db.CartItems.Where(ci => ci.CartId == cartId).ToList();
            if (!items.Any()) return;
            _db.CartItems.RemoveRange(items);
            _db.SaveChanges();
        }
    }
}
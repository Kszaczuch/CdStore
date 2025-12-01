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

        public bool Add(string cartId, int albumId)
        {
            if (string.IsNullOrEmpty(cartId)) return false;

            var exists = _db.CartItems.Any(ci => ci.CartId == cartId && ci.AlbumId == albumId);
            if (exists) return false;

            _db.CartItems.Add(new CartItem { CartId = cartId, AlbumId = albumId });
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
                      .Where(ci => ci.CartId == cartId)
                      .Select(ci => ci.AlbumId)
                      .Distinct()
                      .ToList();
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
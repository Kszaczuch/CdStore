using System.Collections.Concurrent;

namespace CdStore.Services
{
    public class CartService
    {
        private readonly ConcurrentDictionary<string, List<int>> _carts = new();

        public bool Add(string cartId, int albumId)
        {
            var list = _carts.GetOrAdd(cartId, _ => new List<int>());
            lock (list)
            {
                if (!list.Contains(albumId))
                {
                    list.Add(albumId);
                    return true;
                }
                return false;
            }
        }

        public void Remove(string cartId, int albumId)
        {
            if (_carts.TryGetValue(cartId, out var list))
            {
                lock (list)
                {
                    list.Remove(albumId);
                }
            }
        }

        public List<int> GetCartItems(string cartId)
        {
            if (string.IsNullOrEmpty(cartId)) return new List<int>();
            if (_carts.TryGetValue(cartId, out var list))
            {
                lock (list)
                {
                    return list.ToList();
                }
            }
            return new List<int>();
        }

        public void Clear(string cartId)
        {
            _carts.TryRemove(cartId, out _);
        }
    }
}

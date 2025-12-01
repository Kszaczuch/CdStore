using System;

namespace CdStore.Models
{
    public class Favorite
    {
        public int Id { get; set; }

        public string UserId { get; set; }
        public Users User { get; set; }

        public int AlbumId { get; set; }
        public Album Album { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
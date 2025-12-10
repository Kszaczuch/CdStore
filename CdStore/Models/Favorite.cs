using System;
using System.ComponentModel.DataAnnotations;

namespace CdStore.Models
{
    public class Favorite
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(450)]
        public string UserId { get; set; }
        public Users User { get; set; }

        [Required]
        public int AlbumId { get; set; }
        public Album Album { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
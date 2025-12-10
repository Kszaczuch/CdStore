using System.ComponentModel.DataAnnotations;

namespace CdStore.Models
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string CartId { get; set; } = null!;

        [Required]
        public int AlbumId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;
    }
}

using System.ComponentModel.DataAnnotations;

namespace CdStore.Models
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string CartId { get; set; } = null!;

        [Required]
        public int AlbumId { get; set; }

        public int Quantity { get; set; } = 1;
    }
}

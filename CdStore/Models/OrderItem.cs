using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CdStore.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }
        public Order Order { get; set; }

        [Required]
        public int AlbumId { get; set; }
        public Album Album { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        [Range(0, 100000)]
        public decimal UnitPrice { get; set; }

        [NotMapped]
        public decimal SubTotal => UnitPrice * Quantity;
    }
}
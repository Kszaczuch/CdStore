using System;

namespace CdStore.Models
{
    public class Receipt
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; }

        public string NumerParagonu { get; set; }

        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

        public decimal Total { get; set; }
    }
}
using System;
using System.ComponentModel.DataAnnotations;

namespace CdStore.Models
{
    public enum PaymentMethod
    {
        Cash,
        Card,
        BankTransfer,
        PayPal,
        Blik
    }

    public class Receipt
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }
        public Order Order { get; set; }

        [Required]
        [MaxLength(100)]
        public string Number { get; set; }

        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

        public PaymentMethod PaymentMethod { get; set; }
    }
}
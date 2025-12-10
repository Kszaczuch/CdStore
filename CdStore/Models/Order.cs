using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CdStore.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        public Users User { get; set; }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }

        [Required]
        [MaxLength(300)]
        public string Address { get; set; }

        [Required]
        [Phone]
        [MaxLength(30)]
        public string Phone { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsPaid { get; set; }

        [Range(0, 1000000)]
        public decimal Total { get; set; }

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
        public Receipt Receipt { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Oczekujace;
        public DateTime? DeliveryDate { get; set; }

    }
}
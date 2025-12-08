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
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsPaid { get; set; }

        public decimal Total { get; set; }

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
        public Receipt Receipt { get; set; }

    }
}
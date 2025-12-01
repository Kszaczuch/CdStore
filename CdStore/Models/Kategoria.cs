using System.ComponentModel.DataAnnotations;

namespace CdStore.Models
{
    public class Kategoria
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nazwa { get; set; }

        public string Opis { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace CdStore.Models
{
    public class Album
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Tytul { get; set; }

        [Required]
        [MaxLength(150)]
        public string Artysta { get; set; }

        [Range(0, 100000)]
        public decimal Cena { get; set; }

        [MaxLength(500)]
        [Url]
        public string OkladkaLink { get; set; }

        [Range(0, int.MaxValue)]
        public int IloscNaStanie { get; set; }

        [MaxLength(2000)]
        public string Opis { get; set; }

        public int? KategoriaId { get; set; }
        public Kategoria Kategoria { get; set; }
    }
}

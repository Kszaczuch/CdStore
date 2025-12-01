using System.ComponentModel.DataAnnotations;

namespace CdStore.Models
{
    public class Album
    {
        public int Id { get; set; }
        [Required]
        public string Tytul { get; set; }
        [Required]
        public string Artysta { get; set; }
        public decimal Cena { get; set; }
        public string OkladkaLink { get; set; }
        public int IloscNaStanie { get; set; }
        public string Opis { get; set; }
        public int? KategoriaId { get; set; }
        public Kategoria Kategoria { get; set; }
    }
}

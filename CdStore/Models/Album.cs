namespace CdStore.Models
{
    public class Album
    {
        public int Id { get; set; }
        public string Tytul { get; set; }
        public string Artysta { get; set; }
        public decimal Cena { get; set; }
        public string OkladkaLink { get; set; }
    }
}

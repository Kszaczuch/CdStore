namespace CdStore.Models
{
    public class IndexHomeVm
    {

        public IEnumerable<CdStore.Models.Album> Albums { get; set; }
        public int? kategoriaId { get; set; }
        public string availability {get; set; }
        public string sort { get; set; }
    }
}

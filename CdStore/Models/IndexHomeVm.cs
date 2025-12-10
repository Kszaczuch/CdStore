using System.ComponentModel.DataAnnotations;

namespace CdStore.Models
{
    public class IndexHomeVm
    {

        public IEnumerable<CdStore.Models.Album> Albums { get; set; }
        public int? kategoriaId { get; set; }

        [MaxLength(50)]
        public string availability {get; set; }

        [MaxLength(50)]
        public string sort { get; set; }
    }
}

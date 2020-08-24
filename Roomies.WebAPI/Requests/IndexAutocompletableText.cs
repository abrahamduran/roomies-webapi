using System.ComponentModel.DataAnnotations;

namespace Roomies.WebAPI.Requests
{
    public class IndexAutocompletableText
    {
        [Required]
        [MaxLength(30)]
        public string Text { get; set; }
        [Required]
        public IndexAutocompletableField Field { get; set; }
    }

    public enum IndexAutocompletableField
    {
        BusinessName, ItemName
    }

    public enum AutocompletableField
    {
        All, BusinessName, ItemName
    }
}

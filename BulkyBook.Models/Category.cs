using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BulkyBook.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [DisplayName("Display Order")]
        [Range(1,50,ErrorMessage ="The Display Order should be 1 to 50.")]
        public int DisplayOrder { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}

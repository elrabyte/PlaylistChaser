using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlaylistChaser.Model
{
    public class Source
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string? IconHtml { get; set; }
        public string? ColorHex { get; set; }

        [NotMapped]
        public string DisplayName => (IconHtml + " " ?? "") + Name;
    }
}
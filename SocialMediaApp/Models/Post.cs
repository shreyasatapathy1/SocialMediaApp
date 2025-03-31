using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocialMediaApp.Models
{
    public class Post
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [MaxLength(1000)]
        public string? TextContent { get; set; }

        public string? MediaUrl { get; set; } // Image or video path

        [Required]
        [MaxLength(10)]
        public string MediaType { get; set; } = "none"; // "image", "video", or "none"

        public DateTime PostedAt { get; set; } = DateTime.Now;
        //public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

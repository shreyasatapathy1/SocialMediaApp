using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SocialMediaApp.Models.ViewModel
{
    public class PostViewModel
    {
        [MaxLength(1000)]
        public string? TextContent { get; set; }

        public IFormFile? MediaFile { get; set; } // Accepts image/video
    }
}

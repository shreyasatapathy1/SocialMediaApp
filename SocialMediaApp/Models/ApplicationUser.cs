using Microsoft.AspNetCore.Identity;

namespace SocialMediaApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? ProfilePictureUrl { get; set; }
    }
}

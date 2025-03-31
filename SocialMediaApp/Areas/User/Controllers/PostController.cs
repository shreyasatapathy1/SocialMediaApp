using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using SocialMediaApp.Data;
using SocialMediaApp.Models;
using SocialMediaApp.Models.ViewModel;
using Microsoft.AspNetCore.Identity;

namespace SocialMediaApp.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class PostController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public PostController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TextContent,MediaFile")] PostViewModel model)
        {
            Console.WriteLine("🚨 Create Action HIT");
            Console.WriteLine($"📎 TextContent: {model.TextContent}");
            Console.WriteLine($"📎 MediaFile: {model.MediaFile?.FileName}");
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            string? mediaUrl = null;
            string mediaType = "none"; // default to none

            if (model.MediaFile != null && model.MediaFile.Length > 0)
            {
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsDir))
                    Directory.CreateDirectory(uploadsDir);

                var fileName = Guid.NewGuid() + Path.GetExtension(model.MediaFile.FileName);
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.MediaFile.CopyToAsync(stream);
                }

                mediaUrl = "/uploads/" + fileName;

                // 🛡️ Make sure extension exists
                var ext = Path.GetExtension(fileName)?.ToLower();

                if (ext == ".mp4" || ext == ".webm")
                    mediaType = "video";
                else if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".gif")
                    mediaType = "image";
                else
                    mediaType = "none"; // fallback
            }

            Console.WriteLine($"📎 MediaType: {mediaType}");
            Console.WriteLine($"📎 MediaUrl: {mediaUrl}");
            Console.WriteLine($"📎 TextContent: {model.TextContent}");

            var post = new Post
            {
                TextContent = model.TextContent ?? "",
                MediaUrl = mediaUrl ?? "",
                MediaType = mediaType ?? "none",
                UserId = user.Id,
                PostedAt = DateTime.Now
            };


            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Post created successfully!";
            return RedirectToAction("MyPosts");
        }

        [HttpGet]
        public async Task<IActionResult> MyPosts()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var posts = _context.Posts
                        .Where(p => p.UserId == user.Id)
                        .OrderByDescending(p => p.PostedAt)
                        .ToList();

            return View(posts);
        }
    }
}

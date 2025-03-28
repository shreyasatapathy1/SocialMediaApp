using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using SocialMediaApp.Models; // If ApplicationUser is here

namespace SocialMediaApp.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _webHostEnvironment = webHostEnvironment;
        }

        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public string ProfilePictureUrl { get; set; } // ✅ For showing in Razor view

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }

            [Display(Name = "Profile Picture")]
            public IFormFile ProfilePicture { get; set; } // ✅ For upload
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Username = userName;
            ProfilePictureUrl = user.ProfilePictureUrl; // ✅ Load for view

            Input = new InputModel
            {
                PhoneNumber = phoneNumber
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            // ✅ Profile Picture Validation
            if (Input.ProfilePicture != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(Input.ProfilePicture.FileName).ToLowerInvariant();
                var fileSize = Input.ProfilePicture.Length;

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("Input.ProfilePicture", "Only JPG, PNG, JPEG, and GIF files are allowed.");
                    await LoadAsync(user);
                    return Page();
                }

                if (fileSize > 2 * 1024 * 1024) // 2MB max
                {
                    ModelState.AddModelError("Input.ProfilePicture", "File size must not exceed 2 MB.");
                    await LoadAsync(user);
                    return Page();
                }

                // Save the file to wwwroot/images/profile/
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profile");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.ProfilePicture.CopyToAsync(stream);
                }

                // Save URL in DB
                user.GetType().GetProperty("ProfilePictureUrl")?.SetValue(user, $"/images/profile/{uniqueFileName}");
                await _userManager.UpdateAsync(user);
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }


        public async Task<IActionResult> OnPostDeleteProfilePictureAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var fileName = Path.GetFileName(user.ProfilePictureUrl ?? "");
            if (!string.IsNullOrEmpty(fileName))
            {
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "profile", fileName);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            user.ProfilePictureUrl = null;
            await _userManager.UpdateAsync(user);
            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Profile picture removed.";
            return RedirectToPage();
        }

    }

}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SocialMediaApp.Data;
using SocialMediaApp.Models;
using System.Linq;
using System.Threading.Tasks;
using SocialMediaApp.Models.ViewModel;

namespace SocialMediaApp.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> ChatList()
        {
            var userId = _userManager.GetUserId(User);

            var friends = await _context.FriendRequests
                .Where(r => r.Status == FriendRequestStatus.Accepted &&
                            (r.SenderId == userId || r.ReceiverId == userId))
                .Include(r => r.Sender)
                .Include(r => r.Receiver)
                .ToListAsync();

            var chatUsers = friends.Select(r =>
            {
                var user = r.SenderId == userId ? r.Receiver : r.Sender;
                return new UserSearchViewModel
                {
                    UserId = user.Id,
                    UserName = user.Name ?? user.UserName,
                    Email = user.Email,
                    ProfilePictureUrl = user.ProfilePictureUrl
                };
            }).DistinctBy(u => u.UserId).ToList(); // Optional: prevent duplicate rows

            return View(chatUsers);
        }


        public async Task<IActionResult> ChatRoom(string userId)
        {
            var currentUserId = _userManager.GetUserId(User);
            var friend = await _userManager.FindByIdAsync(userId);

            if (friend == null)
            {
                return NotFound();
            }

            var messages = await _context.Messages
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                            (m.SenderId == userId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            var viewModel = new ChatRoomViewModel
            {
                FriendUserId = friend.Id, // <-- This must not be null!
                FriendUserName = friend.UserName,
                Messages = messages.Select(m => new ChatMessageViewModel
                {
                    Content = m.Content,
                    SentAt = m.SentAt,
                    IsSentByCurrentUser = m.SenderId == currentUserId
                }).ToList()
            };

            return View("Chat", viewModel);
        }



        [HttpPost]
        public async Task<IActionResult> SendMessage(string receiverId, string content)
        {
            var senderId = _userManager.GetUserId(User);

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}

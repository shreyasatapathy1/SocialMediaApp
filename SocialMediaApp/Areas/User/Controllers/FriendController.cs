// FIXED: Complete FriendController.cs, Search.cshtml, and ViewModel integration
// Includes dynamic Instagram-like friend interaction via AJAX

// --- UPDATED FriendController.cs ---
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialMediaApp.Data;
using SocialMediaApp.Models;
using SocialMediaApp.Models.ViewModel;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SocialMediaApp.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class FriendController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FriendController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Search(string query)
        {
            var currentUserId = _userManager.GetUserId(User);

            var users = await _context.Users
                .Where(u => u.Id != currentUserId &&
                            (u.UserName.Contains(query) || u.Email.Contains(query)))
                .ToListAsync();

            var requests = await _context.FriendRequests
    .Where(r =>
        (r.SenderId == currentUserId || r.ReceiverId == currentUserId) &&
         r.Status != FriendRequestStatus.Declined) // ✅ Add this filter
    .ToListAsync();


            var result = users.Select(user =>
            {
                var request = requests.FirstOrDefault(r =>
                    (r.SenderId == currentUserId && r.ReceiverId == user.Id) ||
                    (r.SenderId == user.Id && r.ReceiverId == currentUserId));

                bool isSender = request?.SenderId == currentUserId;

                return new UserSearchViewModel
                {
                    UserId = user.Id,
                    UserName = user.Name ?? user.UserName,
                    Email = user.Email,
                    RequestId = request?.Id,
                    Status = request?.Status,
                    IsSender = isSender,
                    IsBlocked = request?.Status == FriendRequestStatus.Blocked
                };
            }).ToList();

            return View(result);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendRequest(string receiverId)
        {
            if (string.IsNullOrEmpty(receiverId))
                return BadRequest();

            var senderId = _userManager.GetUserId(User);

            var existingRequest = await _context.FriendRequests
                .FirstOrDefaultAsync(r =>
                    (r.SenderId == senderId && r.ReceiverId == receiverId) ||
                    (r.SenderId == receiverId && r.ReceiverId == senderId));

            if (existingRequest != null)
            {
                if (existingRequest.Status == FriendRequestStatus.Declined)
                {
                    _context.FriendRequests.Remove(existingRequest);
                    await _context.SaveChangesAsync(); // 💥 you missed this save
                }
                else
                {
                    return Ok(); // Don't re-send if not Declined
                }
            }


            var newRequest = new FriendRequest
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Status = FriendRequestStatus.Pending,
                RequestedAt = DateTime.Now
            };

            _context.FriendRequests.Add(newRequest);
            await _context.SaveChangesAsync();

            return Ok();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptRequest(int id)
        {
            var request = await _context.FriendRequests.FindAsync(id);
            if (request != null && request.Status == FriendRequestStatus.Pending)
            {
                request.Status = FriendRequestStatus.Accepted;
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineRequest(int id)
        {
            var request = await _context.FriendRequests.FindAsync(id);
            if (request != null && request.Status == FriendRequestStatus.Pending)
            {
                request.Status = FriendRequestStatus.Declined;
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unfriend(string userId)
        {
            var currentUserId = _userManager.GetUserId(User);

            var friendship = await _context.FriendRequests.FirstOrDefaultAsync(
                r => (r.SenderId == currentUserId && r.ReceiverId == userId ||
                      r.SenderId == userId && r.ReceiverId == currentUserId) &&
                      r.Status == FriendRequestStatus.Accepted);

            if (friendship != null)
            {
                _context.FriendRequests.Remove(friendship);
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockUser(string userIdToBlock)
        {
            var currentUserId = _userManager.GetUserId(User);

            var request = await _context.FriendRequests.FirstOrDefaultAsync(
                r => (r.SenderId == currentUserId && r.ReceiverId == userIdToBlock) ||
                     (r.SenderId == userIdToBlock && r.ReceiverId == currentUserId));

            if (request != null)
            {
                request.Status = FriendRequestStatus.Blocked;
            }
            else
            {
                request = new FriendRequest
                {
                    SenderId = currentUserId,
                    ReceiverId = userIdToBlock,
                    Status = FriendRequestStatus.Blocked,
                    RequestedAt = DateTime.Now
                };
                _context.FriendRequests.Add(request);
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        public async Task<IActionResult> MyFriends()
        {
            var currentUserId = _userManager.GetUserId(User);

            // Get accepted friends
            var friends = await _context.FriendRequests
                .Where(r => (r.SenderId == currentUserId || r.ReceiverId == currentUserId)
                         && r.Status == FriendRequestStatus.Accepted && !r.IsBlocked)
                .Include(r => r.Sender)
                .Include(r => r.Receiver)
                .ToListAsync();

            var friendUsers = friends.Select(r =>
            {
                var user = r.SenderId == currentUserId ? r.Receiver : r.Sender;
                return new UserSearchViewModel
                {
                    UserId = user.Id,
                    UserName = user.Name ?? user.UserName,
                    Email = user.Email
                };
            }).ToList();

            // Get received requests
            var requests = await _context.FriendRequests
                .Where(r => r.ReceiverId == currentUserId && r.Status == FriendRequestStatus.Pending)
                .Include(r => r.Sender)
                .ToListAsync();

            ViewBag.ReceivedRequests = requests;

            return View(friendUsers);
        }


    }
}

using System.ComponentModel.DataAnnotations;

namespace SocialMediaApp.Models
{
    public enum FriendRequestStatus
    {
        Pending = 0,
        Accepted = 1,
        Declined = 2,
        Blocked = 3
    }



    public class FriendRequest
    {
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; }
        public ApplicationUser Sender { get; set; }

        [Required]
        public string ReceiverId { get; set; }
        public ApplicationUser Receiver { get; set; }

        public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;

        public DateTime RequestedAt { get; set; }

        public bool IsBlocked { get; set; } = false; 
    }
}

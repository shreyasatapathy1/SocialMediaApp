namespace SocialMediaApp.Models.ViewModel
{
    public class UserSearchViewModel
    {
        public string UserId { get; set; } // required for sending requests
        public string UserName { get; set; } // for displaying
        public string Email { get; set; }
        public string? ProfilePictureUrl { get; set; }

        public bool IsFriend { get; set; } = false;
        public bool IsRequestSent { get; set; } = false;
        public bool IsRequestReceived { get; set; } = false;

        //public string Status { get; set; }

        public FriendRequestStatus? Status { get; set; } // null if no request
        public bool IsSender { get; set; } // true if current user sent it
        public bool IsBlocked { get; set; }
        public int? RequestId { get; set; }
    }
}

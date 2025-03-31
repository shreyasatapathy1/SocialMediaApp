using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SocialMediaApp.Data;
using SocialMediaApp.Models;

namespace SocialMediaApp.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"User Connected: {Context.UserIdentifier}");
            return base.OnConnectedAsync();
        }

        public async Task SendMessage(string senderId, string receiverId, string content)
        {
            Console.WriteLine(" SendMessage Invoked");
            Console.WriteLine($" SenderId: {senderId}");
            Console.WriteLine($" ReceiverId: {receiverId}");
            Console.WriteLine($" Content: {content}");

            try
            {
                var senderExists = await _context.Users.AnyAsync(u => u.Id == senderId);
                var receiverExists = await _context.Users.AnyAsync(u => u.Id == receiverId);

                if (!senderExists || !receiverExists)
                {
                    Console.WriteLine($" Invalid Sender or Receiver. SenderExists: {senderExists}, ReceiverExists: {receiverExists}");
                    return;
                }

                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Content = content,
                    SentAt = DateTime.Now
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync(); // 💥 This line throws FK error if IDs mismatch

                await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, content, message.Id);
                await Clients.User(senderId).SendAsync("ReceiveMessage", senderId, content, message.Id);


            }
            catch (Exception ex)
            {
                Console.WriteLine($" Exception in SendMessage: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($" Inner: {ex.InnerException.Message}");
            }
        }

        public async Task AddReaction(int messageId, string emoji)
        {
            Console.WriteLine($"AddReaction called for MessageID={messageId} with Emoji={emoji}");

            var message = await _context.Messages.FindAsync(messageId);
            if (message == null)
            {
                Console.WriteLine(" Message not found in DB.");
                return;
            }

            message.Reaction = emoji;
            await _context.SaveChangesAsync();

            Console.WriteLine(" Reaction saved to DB.");

            await Clients.User(message.SenderId).SendAsync("ReceiveReaction", messageId, emoji);
            await Clients.User(message.ReceiverId).SendAsync("ReceiveReaction", messageId, emoji);
        }





    }
}

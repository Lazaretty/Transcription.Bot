
using Transcription.Common.Enums;

namespace Transcription.DAL.Models;

public class ChatState
{
    public long ChatStateId { get; set; }
    public long UserChatId { get; set; }

    public ChatSate State { get; set; }

    public User User { get; set; }
}
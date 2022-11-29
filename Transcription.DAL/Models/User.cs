namespace Transcription.DAL.Models;

public class User
{
    public long UserId { get; set; }
    public long UserChatId { get; set; }

    public string ApiKey { get; set; }
    
    public DateTimeOffset? LastUpdate { get; set; }

    public bool IsActive { get; set; }

    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string UserName { get; set; }

    public int Balance { get; set; }

    public ChatState ChatState { get; set; }
}
namespace Transcription.DAL.Models;

public class YandexRequest
{
    public long UserChatId { get; set; }

    public string YandexRequestId { get; set; }

    public bool IsDone { get; set; }
    
    public DateTimeOffset? CreateTime { get; set; }
    
    public DateTimeOffset? UpdateTime { get; set; }
}
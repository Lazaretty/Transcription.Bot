using Newtonsoft.Json;

namespace Trascribition.Models
{


    public class Alternative
    {
        public List<Word> words { get; set; }
        public string text { get; set; }
        public int confidence { get; set; }
    }

    public class Chunk
    {
        public List<Alternative> alternatives { get; set; }
        public string channelTag { get; set; }
    }

    public class Response
    {
        [JsonProperty("@type")] public string type { get; set; }
        public List<Chunk> chunks { get; set; }
    }

    public class SpeechToTextResponse
    {
        public bool done { get; set; }
        public Response response { get; set; }
        public string id { get; set; }
        public DateTime createdAt { get; set; }
        public string createdBy { get; set; }
        public DateTime modifiedAt { get; set; }
    }

    public class Word
    {
        public string startTime { get; set; }
        public string endTime { get; set; }
        public string word { get; set; }
        public int confidence { get; set; }
    }
}
namespace Trascribition.Models
{
    public class Audio
    {
        public string uri { get; set; }
    }

    public class Config
    {
        public Specification specification { get; set; }
    }

    public class SpeechToTextRequest
    {
        public Config config { get; set; }
        public Audio audio { get; set; }
    }

    public class Specification
    {
        public string languageCode { get; set; }
        public string model { get; set; }
        public string profanityFilter { get; set; }
        public string literature_text { get; set; }
        public string audioEncoding { get; set; }
        public string sampleRateHertz { get; set; }
        public int audioChannelCount { get; set; }
    }
}
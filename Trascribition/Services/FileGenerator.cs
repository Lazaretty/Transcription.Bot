using Trascribition.Models;

namespace Trascribition.Services;

public static class FileGenerator
{
    public static string GetTextFromResponse(this SpeechToTextResponse response)
    {
        var result = string.Empty;

        foreach (var chunk in response.response.chunks)
        {
            result += chunk.channelTag + " : " + $"{chunk.alternatives[0].text}" + $" ({chunk.alternatives[0].words.First().startTime} - {chunk.alternatives[0].words.Last().endTime})" + Environment.NewLine ;
        }

        return result;
    }
}
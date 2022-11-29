using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Newtonsoft.Json;
using Trascribition.Models;

namespace Trascribition;

public class YandexConnector
{
    private readonly HttpClient _client;
    
    private readonly AmazonS3Client s3client;
    
    private readonly string AWS_SECRET_KEY = "YCNihuk33tUsnCyUrLFEZXDJPBXGMrNMcSFpurfg";
    private readonly string AWS_ACCESS_KEY = "YCAJEbV9URJZG_g03Flz2_wcf";
    private readonly string _bucketName = "my-bucet";
    
    public YandexConnector()
    {
        _client = new HttpClient();
        
        _client.DefaultRequestHeaders.Add("Authorization", "Api-Key AQVNytekyHwKvHxiLFLsdfNDQ3Y68MyqZzLwpQBa");
        
        AmazonS3Config configsS3 = new AmazonS3Config
        {
            //ServiceURL = "https://storage.yandexcloud.net/"
            ServiceURL = "https://s3.yandexcloud.net",
            //RegionEndpoint = RegionEndpoint.USEast1
        };
        s3client = new AmazonS3Client(AWS_ACCESS_KEY, AWS_SECRET_KEY, configsS3);
    }

    public async Task<SpeechToTextRequestResponse> SendSpeechToTextRequest(string fileURl)
    {
        var request = new SpeechToTextRequest()
        {
            config = new Config()
            {
                specification = new Specification()
                {
                    languageCode= "ru-RU",
                    model= "general",
                    profanityFilter= "false",
                    literature_text= "true",
                    audioEncoding= "LINEAR16_PCM",
                    sampleRateHertz= "48000",
                    audioChannelCount=  2
                }
            },
            audio = new Audio()
            {
                uri = fileURl
            }
        };

        var responseMessage = await _client.PostAsync(new Uri("https://transcribe.api.cloud.yandex.net/speech/stt/v2/longRunningRecognize"), new StringContent(JsonConvert.SerializeObject(request)));

        var responseString = await responseMessage.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<SpeechToTextRequestResponse>(responseString);
    }

    public async Task<SpeechToTextResponse> TryGetToTextResponse(string id)
    {
        var responseMessage = await _client.GetAsync(new Uri($"https://operation.api.cloud.yandex.net/operations/{id}"));

        var responseString = await responseMessage.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<SpeechToTextResponse>(responseString);
    }
    
    public async Task SendAudioInObjectStorage(string fileName)
    {
        try
        {
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = Path.GetFileName(fileName),
                InputStream = File.OpenRead(fileName)
            };

//            if (contentLength.HasValue)
  //              request.Headers.ContentLength = contentLength.Value;

            await s3client.PutObjectAsync(request);
        }
        catch (Exception exception)
        {
            //_logger.LogError(exception.ToString());
            throw exception;
        }
    }
    
    public string GetFileURL(string fileName)
    {
        return s3client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = fileName,
            Expires = DateTime.Now.AddDays(10)
        });
    }
}
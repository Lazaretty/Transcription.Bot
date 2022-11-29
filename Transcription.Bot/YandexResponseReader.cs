using Telegram.Bot;
using Telegram.Bot.Types.InputFiles;
using Transcription.DAL.Repositories;
using Trascribition;
using Trascribition.Models;
using Trascribition.Services;
using File = System.IO.File;

namespace Transcription.Bot;

public class YandexResponseReader : BackgroundService
{
    private readonly ILogger<YandexResponseReader> _logger;
    private readonly IServiceProvider _services;
    private readonly TelegramConfiguration _botConfig;
    
    public YandexResponseReader(ILogger<YandexResponseReader> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _services = serviceProvider;
        _botConfig = configuration.GetSection("BotConfiguration").Get<TelegramConfiguration>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _services.CreateScope();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
        var userRepository = scope.ServiceProvider.GetRequiredService<UserRepository>();
        var chatStateRepository = scope.ServiceProvider.GetRequiredService<ChatStateRepository>();
        var yandexRequestRepository = scope.ServiceProvider.GetRequiredService<YandexRequestRepository>();
        var yandexConnector = new YandexConnector();
        
        

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                foreach (var yandexRequest in await yandexRequestRepository.GetAllActiveAsync())
                {
                    var resp = await yandexConnector.TryGetToTextResponse(yandexRequest.YandexRequestId);

                    if(resp.done)
                    {
                        var trascribition = resp.GetTextFromResponse();
                        
                        await File.WriteAllTextAsync(  @$"{AppDomain.CurrentDomain.BaseDirectory}Files{Path.DirectorySeparatorChar}{yandexRequest.UserChatId}{Path.DirectorySeparatorChar}result.txt", trascribition);

                        await using var fs = File.OpenRead(@$"{AppDomain.CurrentDomain.BaseDirectory}Files{Path.DirectorySeparatorChar}{yandexRequest.UserChatId}{Path.DirectorySeparatorChar}result.txt");
                        {
                            await botClient.SendDocumentAsync(yandexRequest.UserChatId,
                                new InputOnlineFile(fs, "result.txt"));
                            fs.Close();
                        }
        
                        Directory.Delete( @$"{AppDomain.CurrentDomain.BaseDirectory}Files{Path.DirectorySeparatorChar}{yandexRequest.UserChatId}", true);

                        yandexRequest.IsDone = true;
                        
                        await yandexRequestRepository.Update(yandexRequest);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, $"[{GetType().Name}]");
            }
            
            await Task.Delay(10_000, stoppingToken);
        }
    }
}
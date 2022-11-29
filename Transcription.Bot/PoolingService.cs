using Telegram.Bot;
using Telegram.Bot.Types;
using Transcription.DAL.Repositories;
using Trascribition.Models;
using Trascribition.Services;

namespace Transcription.Bot;

public class PoolingService : BackgroundService
{
    private readonly ILogger<PoolingService> _logger;
    private readonly IServiceProvider _services;
    private readonly TelegramConfiguration _botConfig;
    
    public PoolingService(ILogger<PoolingService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _services = serviceProvider;
        _botConfig = configuration.GetSection("BotConfiguration").Get<TelegramConfiguration>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(10_000);

        using var scope = _services.CreateScope();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
        
        await botClient.DeleteWebhookAsync(cancellationToken: stoppingToken);
        
        var offset = -1;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var innerScope = _services.CreateScope();

                var userRepository = scope.ServiceProvider.GetRequiredService<UserRepository>();
                var chatStateRepository = scope.ServiceProvider.GetRequiredService<ChatStateRepository>();
                var yandexRequestRepository = scope.ServiceProvider.GetRequiredService<YandexRequestRepository>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<HandleUpdateService>>();


                var handleService = new HandleUpdateService(botClient,userRepository,chatStateRepository, yandexRequestRepository, logger);
                
                Update[] updates;
                
                if(offset == -1)
                {
                    updates = await botClient.GetUpdatesAsync(cancellationToken: stoppingToken);
                }
                else
                {
                    updates = await botClient.GetUpdatesAsync(cancellationToken: stoppingToken, offset: offset + 1);
                }
                
                foreach (var update in updates)
                {
                    await handleService.EchoAsync(update);
                    offset = update.Id;
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, $"[{GetType().Name}]");
            }
            
            await Task.Delay(1_000, stoppingToken);
        }
    }
}
using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using Transcription.Common.Enums;
using Transcription.DAL.Models;
using Transcription.DAL.Repositories;
using File = System.IO.File;
using User = Telegram.Bot.Types.User;

namespace Trascribition.Services;

public class HandleUpdateService
{
    private readonly ITelegramBotClient _botClient;

    private readonly UserRepository _userRepository;

    private readonly ChatStateRepository _chatStateRepository;

    private readonly YandexRequestRepository _yandexRequestRepository;
    private readonly ILogger<HandleUpdateService> _logger;

    public HandleUpdateService(ITelegramBotClient botClient, UserRepository userRepository, ChatStateRepository chatStateRepository, YandexRequestRepository yandexRequestRepository, ILogger<HandleUpdateService> logger)//, ILogger<HandleUpdateService> logger)
    {
        _botClient = botClient;
        _userRepository = userRepository;
        _chatStateRepository = chatStateRepository;
        _yandexRequestRepository = yandexRequestRepository;
        _logger = logger;
    }

    public async Task EchoAsync(Update update)
    {
        var handler = update.Type switch
        {
            // UpdateType.Unknown:
            // UpdateType.ChannelPost:
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            // UpdateType.Poll:
            UpdateType.Message            => BotOnMessageReceived(update.Message!),
            _                             => UnknownUpdateHandlerAsync(update)
        };

        try
        {
            await handler;
        }
        catch (Exception exception)
        {
            await HandleErrorAsync(_botClient, exception);
        }
    }

    private async Task BotOnMessageReceived(Message message)
    {
        //_logger.LogInformation("Receive message type: {MessageType}", message.Type);
        if (message.Type != MessageType.Text)
        {
            //return;
            if (message.Audio is not null)
            {
                var file = await _botClient.GetFileAsync(message.Audio.FileId);
                
                if (file.FilePath.EndsWith(".m4a"))
                {
                    Directory.CreateDirectory($"{AppDomain.CurrentDomain.BaseDirectory}/Files/{message.Chat.Id}");
                    
                    var files = Directory.GetFiles($"{AppDomain.CurrentDomain.BaseDirectory}/Files/{message.Chat.Id}");
                    
                    if (files.Length == 2)
                    {
                        await _botClient.SendTextMessageAsync(chatId: message.Chat.Id, @$"В очереди на загрузку уже находится 2 файла. Отправьте комманду /run чтобы транскрибать их. Или удалите файлы /clearStorage, чтобы повторить загрузку");
                        return;
                    }

                    var fileName = message.Audio.FileName +"-"+  Guid.NewGuid().ToString() + ".m4a";
                    
                    var fileStream = File.Create(@$"{AppDomain.CurrentDomain.BaseDirectory}Files{Path.DirectorySeparatorChar}{message.Chat.Id}{Path.DirectorySeparatorChar}{fileName}");

                    await _botClient.DownloadFileAsync(file.FilePath, fileStream);
                
                    fileStream.Close();
                    await fileStream.DisposeAsync();

                    await _botClient.SendTextMessageAsync(chatId: message.Chat.Id, $"{message.Audio.FileName} сохранен.");
                    
                    if (files.Length == 1)
                    {
                        await _botClient.SendTextMessageAsync(chatId: message.Chat.Id, @$"Сохранено 2 файла. Отправьте комманду /run чтобы запустить транскрибацию"); 
                    }
                    
                }
            }
            
            return;
        }

        var action = message.Text!.Split(' ')[0] switch
        {
            "/start"   => OnStart(_botClient, message),
            "/run"   =>   OnRun(_botClient, message),
            "/clearStorage"   =>   OnClearStorage(_botClient, message),
            "/storageInfo"   =>   OnStorageInfo(_botClient, message),
            "/usage"   =>   Usage(_botClient, message),
            "/topup"   =>   OnTopUp(_botClient, message),
            "/balance"   =>   OnBalance(_botClient, message),
            _           => Help(_botClient, message)
        };
        Message sentMessage = await action;
        //_logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);

        // Send inline keyboard
        // You can process responses in BotOnCallbackQueryReceived handle
    }

    async Task<Message> OnStart(ITelegramBotClient bot, Message message)
    {
        var welcomeMessage = "👋 Привет, этот бот может транскрибировать аудио файлы." + Environment.NewLine + Environment.NewLine
            + "🤖 Для перевода речи в тест сервис использует технологию *SpeechKit* - разработка Yandex. " + "Ей доверяют транскрибацию телефонных разговоров для оценки качества работы call-центров такие компании как : *Тинькофф*, *ВТБ*, *Lamoda* и так далее. " + Environment.NewLine + Environment.NewLine
            + "Для того чтобы транскрибировать звонок из Zoom - необходимо изменить настройки Zoom:" + Environment.NewLine
            +"1️⃣"+" Зайти в настройки ZOOM." + Environment.NewLine
            + "2️⃣" +@" Перейти во вкладку *""Запись""*." + Environment.NewLine
            + "3️⃣"+ @" Поставить галочку в *""Записать отдельное аудио для каждого участника""*.";
        
        await bot.SendPhotoAsync(chatId: message.Chat.Id,
            new InputOnlineFile(File.OpenRead(@$"{AppDomain.CurrentDomain.BaseDirectory}Examples{Path.DirectorySeparatorChar}Init2.jpg")),
            caption: welcomeMessage,
            parseMode: ParseMode.Markdown);

        if ((!await _userRepository.IsUserExists(message.Chat.Id)))
        {
            await _userRepository.Insert(new Transcription.DAL.Models.User()
            {
                UserChatId = message.Chat.Id,
                IsActive = true,
                ApiKey = string.Empty,
                FirstName = message.From.FirstName,
                LastName = message.From.LastName,
                UserName = message.From.Username,
                Balance = 0,
                ChatState = new ChatState()
                {
                    UserChatId = message.Chat.Id,
                    State = ChatSate.Configuration
                }
            });
        }
        
        return await Help(bot, message);
    }

    async Task<Message> OnRun(ITelegramBotClient bot, Message message)
    {
        if (await _userRepository.IsUserExists(message.Chat.Id))
        {
            var user = await _userRepository.GetAsync(message.Chat.Id);

            if (user.Balance < 500)
            {
                return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
                    $"Недостаточно средств 🥲" + Environment.NewLine + "Стоимость одной транскрибации *500* рублей" +
                    Environment.NewLine +$"Текущий баланс *{user.Balance}*",
                    parseMode: ParseMode.Markdown);
            }

            user.Balance -= 500;

            await _userRepository.Update(user);
            
            if (!Directory.Exists(@$"{AppDomain.CurrentDomain.BaseDirectory}Files{Path.DirectorySeparatorChar}{message.Chat.Id}"))
            {
                return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
                    $"Недостаточно файлов для транскрибации : {2}");
            }

            var files = Directory.GetFiles($"{AppDomain.CurrentDomain.BaseDirectory}/Files/{message.Chat.Id}");

            if (files.Length < 2)
            {
                return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
                    $"Недостаточно файлов для транскрибации : {2 - files.Length}");
            }

            if (files.Length > 2)
            {
                Directory.Delete(@$"{AppDomain.CurrentDomain.BaseDirectory}Files{Path.DirectorySeparatorChar}{message.Chat.Id}", true);
                return await bot.SendTextMessageAsync(chatId: message.Chat.Id, $"Загрузите файлы повторно");
            }
            
            var connector = new YandexConnector();

            var convertor = new Converter();
            var resulFileName =
                @$"{AppDomain.CurrentDomain.BaseDirectory}Files{Path.DirectorySeparatorChar}{message.Chat.Id}{Path.DirectorySeparatorChar}result.wav-{Guid.NewGuid()}.wav";
            
            var isWindows = System.Runtime.InteropServices.RuntimeInformation
                .IsOSPlatform(OSPlatform.Windows);

            var validationMessage = isWindows
                ? convertor.GenerateLPCM(files[0], files[1], resulFileName)
                : await convertor.GenerateLPCMUnix(files[0], files[1], resulFileName);
            
            if (!string.IsNullOrEmpty(validationMessage))
            {
                return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
                    $"{validationMessage}. Воспользуйтесь /clearStorage и загрузите файлы повторно");
            }

            await bot.SendTextMessageAsync(chatId: message.Chat.Id,
                "Транскрибация запущена. Процесс может занять до нескольких минут.");
            
            var shortFileName = Path.GetFileName(resulFileName);

            await connector.SendAudioInObjectStorage(resulFileName);

            //var url = connector.GetFileURL(shortFileName);
            var url = $"https://storage.yandexcloud.net/my-bucet/{shortFileName}";

            var response = await connector.SendSpeechToTextRequest(url);

            await _yandexRequestRepository.Insert(new YandexRequest()
            {
                UserChatId = message.Chat.Id,
                IsDone = false,
                YandexRequestId = response.id
            });

            return await Task.FromResult(new Message());

            await Task.Delay(10_000);

            var resp = await connector.TryGetToTextResponse(response.id);

            var trascribition = resp.GetTextFromResponse();

            await File.WriteAllTextAsync(@$"{AppDomain.CurrentDomain.BaseDirectory}Files{Path.DirectorySeparatorChar}{message.Chat.Id}{Path.DirectorySeparatorChar}result.txt",
                trascribition);

            await using var fs =
                File.OpenRead(@$"{AppDomain.CurrentDomain.BaseDirectory}Files{Path.DirectorySeparatorChar}{message.Chat.Id}{Path.DirectorySeparatorChar}result.txt");
            {
                await bot.SendDocumentAsync(message.Chat.Id,
                    new InputOnlineFile(fs, "result.txt"));
                fs.Close();
            }

            Directory.Delete(@$"{AppDomain.CurrentDomain.BaseDirectory}Files{Path.DirectorySeparatorChar}{message.Chat.Id}", true);

            return await Task.FromResult(new Message());

            //return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
            //  trascribition);
        }
        
        return await Task.FromResult(new Message());
    }

    async Task<Message> OnClearStorage(ITelegramBotClient bot, Message message)
    {
        Directory.Delete( @$"{AppDomain.CurrentDomain.BaseDirectory}Files{Path.DirectorySeparatorChar}{message.Chat.Id}", true);

        return await bot.SendTextMessageAsync(chatId: message.Chat.Id, $"Файлы из очереди на транскрибацию очищены");
    }

    async Task<Message> OnStorageInfo(ITelegramBotClient bot, Message message)
    {
        if (!Directory.Exists(@$"{AppDomain.CurrentDomain.BaseDirectory}Files{Path.DirectorySeparatorChar}{message.Chat.Id}"))
        {
            return await bot.SendTextMessageAsync(chatId: message.Chat.Id, $"Нет файлов в очереди на транскрибацию.");
        }

        var files = Directory.GetFiles($"{AppDomain.CurrentDomain.BaseDirectory}/Files/{message.Chat.Id}");

        if (!files.Any())
        {
            return await bot.SendTextMessageAsync(chatId: message.Chat.Id, $"Нет файлов в очереди на транскрибацию.");
        }

        var fileNames = files.Select(x => Path.GetFileName(x).Split('-')[0]).ToList();
        
        return await bot.SendTextMessageAsync(chatId: message.Chat.Id, $"Файлы в очереди на транскрибацию: {string.Join(Environment.NewLine, fileNames)}");
    }
    
    async Task<Message> Usage(ITelegramBotClient bot, Message message)
    {
        await Usage2(bot, message);

        await bot.SendMediaGroupAsync(chatId: message.Chat.Id, new[]
        {
            new InputMediaPhoto(new InputMedia(File.OpenRead(@$"{AppDomain.CurrentDomain.BaseDirectory}Examples{Path.DirectorySeparatorChar}Usage1.jpg"),"Usage1.jpg")),
            new InputMediaPhoto(new InputMedia(File.OpenRead(@$"{AppDomain.CurrentDomain.BaseDirectory}Examples{Path.DirectorySeparatorChar}Usage2.jpg"), "Usage2.jpg")),
            new InputMediaPhoto(new InputMedia(File.OpenRead(@$"{AppDomain.CurrentDomain.BaseDirectory}Examples{Path.DirectorySeparatorChar}Usage3.jpg"), "Usage3.jpg")),
            new InputMediaPhoto(new InputMedia(File.OpenRead(@$"{AppDomain.CurrentDomain.BaseDirectory}Examples{Path.DirectorySeparatorChar}Usage4.jpg"), "Usage4.jpg")),
            new InputMediaPhoto(new InputMedia(File.OpenRead(@$"{AppDomain.CurrentDomain.BaseDirectory}Examples{Path.DirectorySeparatorChar}Usage5.jpg"), "Usage5.jpg")),
        });
        
        await bot.SendMediaGroupAsync(chatId: message.Chat.Id, new[]
        {
            new InputMediaAudio(new InputMedia(File.OpenRead(@$"{AppDomain.CurrentDomain.BaseDirectory}Examples{Path.DirectorySeparatorChar}audioDmitrySkvortsov21018473429.m4a"),"audioDmitrySkvortsov21018473429.m4a")),
            new InputMediaAudio(new InputMedia(File.OpenRead(@$"{AppDomain.CurrentDomain.BaseDirectory}Examples{Path.DirectorySeparatorChar}audioiPhone11018473429.m4a"), "audioiPhone11018473429.m4a")),
            //new InputMediaAudio(new InputMedia(File.OpenRead(@$"{AppDomain.CurrentDomain.BaseDirectory}Examples{Path.DirectorySeparatorChar}result.txt"), "result.txt"))
        });
        
        await bot.SendMediaGroupAsync(chatId: message.Chat.Id, new[]
        {
           new InputMediaDocument(new InputMedia(File.OpenRead(@$"{AppDomain.CurrentDomain.BaseDirectory}Examples{Path.DirectorySeparatorChar}result.txt"), "result.txt"))
        });
        
        return await bot.SendTextMessageAsync(message.Chat.Id, "По вопросам работы бота писать @skvrtsv");
    }

    public async Task<Message> OnTopUp(ITelegramBotClient bot, Message message)
    {
        return await bot.SendTextMessageAsync(message.Chat.Id, "Оплата по карте онлайн еще в разработке 🥲 " + Environment.NewLine 
            +"Пополнить баланс можно только переводом на карту "+ Environment.NewLine +" *4274 3200 6020 3554*" + Environment.NewLine 
            + "После оплаты прислать скрин @skvrtsv", ParseMode.Markdown);
    }
    
    public async Task<Message> OnBalance(ITelegramBotClient bot, Message message)
    {
        if (await _userRepository.IsUserExists(message.Chat.Id))
        {
            var user = await _userRepository.GetAsync(message.Chat.Id);
            
            return await bot.SendTextMessageAsync(message.Chat.Id, $"Текущий баланс : *{user.Balance}*", ParseMode.Markdown);
        }

        return await Task.FromResult(new Message());
    }

    async Task<Message> Help(ITelegramBotClient bot, Message message)
    {
        var commands = "Список доступных команд:" + Environment.NewLine
                                                  + "/run - запуск транскрибации из аудиофайлов" + Environment.NewLine
                                                  + "/clearStorage - отчистка очереди файлов" + Environment.NewLine
                                                  + "/storageInfo - получить имена файлов из очереди на транскрибацию" +
                                                  Environment.NewLine
                                                  + "/usage - FAQ" + Environment.NewLine 
                                                  + "/topup - поплнить баланс" + Environment.NewLine 
                                                  + "/balance - получить текущий баланс";
        // + "По вопросам работы бота писать @skvrtsv";

        return await bot.SendTextMessageAsync(message.Chat.Id, commands);
    }
    
    async Task<Message> Usage2(ITelegramBotClient bot, Message message)
    {
        var welcomeMessage =  "Для успешной транскрибации необходимо чтобы голос каждого участника был записан в отдельный файл. Для этого: " + Environment. NewLine +
                                "1️⃣"+" Зайти в настройки ZOOM." + Environment.NewLine
                             + "2️⃣" +@" Перейти во вкладку *""Запись""*." + Environment.NewLine
                             + "3️⃣"+ @" Поставить галочку в *""Записать отдельное аудио для каждого участника""*.";
        
        await bot.SendPhotoAsync(chatId: message.Chat.Id,
            new InputOnlineFile(File.OpenRead(@$"{AppDomain.CurrentDomain.BaseDirectory}Examples{Path.DirectorySeparatorChar}Init2.jpg")),
            caption: welcomeMessage,
            parseMode: ParseMode.Markdown);
    
        var usage =
            "Для успешной транскрибации необходимо начать запись когда ВСЕ участники будут подключены к конференции." +
            Environment.NewLine
            +  "1️⃣"+" После звонка, необходимо перейти в папку, которая указана в настройках" + Environment.NewLine
            + "2️⃣" + " Перейти в папку, соответсвующую последнего звонку" + Environment.NewLine
            + "3️⃣"+ @" Перейти в папку ""Audio Records"" " + Environment.NewLine
            + "4️⃣"+ " Отравить в бота 2 файла, которые соотвествуют интервьюеру и интервьюируемый" + Environment.NewLine + Environment.NewLine +
            "⬇️⬇️⬇Ниже приведен пример транскрибации из 2 аудио файлов записанных в zoom конференции ⬇️⬇️⬇️"; 
        return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
            text: usage,
            replyMarkup: new ReplyKeyboardRemove());
    }
    
    #region Inline Mode

    #endregion

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        //_logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    public async Task HandleErrorAsync(ITelegramBotClient bot, Exception exception)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        await bot.SendTextMessageAsync("669363145", ErrorMessage);
        
        _logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);
    }
}
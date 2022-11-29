using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Telegram.Bot;
using Transcription.DAL;
using Trascribition.Models;
using Trascribition.Services;

namespace Transcription.Bot
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var botConfig = Configuration.GetSection("BotConfiguration").Get<TelegramConfiguration>();

            services.AddScoped<HandleUpdateService>();
            //services.AddHostedService<ConfigureWebhook>();
            services.AddHostedService<PoolingService>();
            services.AddHostedService<YandexResponseReader>();

            services.AddDALServices(Configuration);
            
            /*
            services.AddHttpClient("tgwebhook")
                .AddTypedClient<ITelegramBotClient>(httpClient =>
                    new TelegramBotClient(botConfig.BotToken, httpClient));
            */
            services.AddHttpClient("telegram_bot_client")
                .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
                {
                    TelegramBotClientOptions options = new(botConfig.BotToken);
                    return new TelegramBotClient(options, httpClient);
                });

            services.AddLogging();

            services.AddControllers();
            
            services.AddHealthChecks();

        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
            IHostApplicationLifetime applicationLifetime)
        {
            app.UseHealthChecks("/healthCheck", new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    await context.Response.WriteAsync(
                        JsonConvert.SerializeObject(
                            new
                            {
                                result = "wb.bot is running"
                            }));
                }
            });
            
            app.UseRouting();
            app.UseCors();
            
            #region Migrate
            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetService<WbContext>())
                {
                    context?.Database.Migrate();
                }
            }
            #endregion
            
            app.UseEndpoints(endpoints =>
            {
               
            });
        }
    }
}
using Microsoft.EntityFrameworkCore;
using Transcription.DAL.Configuration;
using Transcription.DAL.Models;

namespace Transcription.DAL
{
    public class WbContext : DbContext
    {
        public WbContext(DbContextOptions options)
            : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        
        public DbSet<ChatState> ChatStates { get; set; }
        
        public DbSet<YandexRequest> YandexRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfiguration(new UserConfiguration());
            builder.ApplyConfiguration(new ChatStatesConfiguration());
            builder.ApplyConfiguration(new YandexRequestConfiguration());
        }
    }
}


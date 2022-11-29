using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transcription.DAL.Models;

namespace Transcription.DAL.Configuration;

public class ChatStatesConfiguration : IEntityTypeConfiguration<ChatState>
{
    public void Configure(EntityTypeBuilder<ChatState> builder)
    {
        builder.HasIndex(x => x.ChatStateId)
            .IsUnique();
        
        builder.Property(x => x.ChatStateId)
            .ValueGeneratedOnAdd();

        builder.HasOne(x => x.User)
            .WithOne(x => x.ChatState)
            .HasForeignKey<ChatState>(x => x.ChatStateId);
    }
}
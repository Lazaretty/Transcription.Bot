using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transcription.DAL.Models;

namespace Transcription.DAL.Configuration;

public class YandexRequestConfiguration : IEntityTypeConfiguration<YandexRequest>
{
    public void Configure(EntityTypeBuilder<YandexRequest> builder)
    {
    }
}
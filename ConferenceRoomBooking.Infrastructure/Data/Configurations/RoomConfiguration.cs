using ConferenceRoomBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConferenceRoomBooking.Infrastructure.Data.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<ConferenceRoom>
{
    public void Configure(EntityTypeBuilder<ConferenceRoom> builder)
    {
        builder.ToTable("Rooms");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Capacity)
            .IsRequired();

        builder.Property(r => r.BaseHourlyRate)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(r => r.IsDeleted)
            .HasDefaultValue(false);

        builder.HasMany(r => r.AvailableServices)
            .WithMany()
            .UsingEntity(j => j.ToTable("RoomServices"));

        builder.HasQueryFilter(r => !r.IsDeleted);

        builder.HasIndex(r => r.Name);
    }
}
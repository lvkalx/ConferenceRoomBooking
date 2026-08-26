using ConferenceRoomBooking.Domain.Entities;
using ConferenceRoomBooking.Infrastructure.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBooking.Infrastructure.Tests.Data.Configurations;

/// <summary>
/// Verifies the EF Core model metadata produced by IEntityTypeConfiguration classes
/// (BookingConfiguration, RoomConfiguration, ServiceConfiguration).
/// This inspects the built <see cref="IModel"/> directly rather than talking to a real
/// database, so it stays a pure unit test of the mapping configuration itself.
/// </summary>
public class EntityConfigurationTests
{
    #region Room mapping

    [Fact]
    public void RoomConfiguration_MapsToRoomsTableWithExpectedKey()
    {
        using var context = InMemoryDbContextFactory.Create();

        var entity = context.Model.FindEntityType(typeof(ConferenceRoom))!;

        entity.GetAnnotation("Relational:TableName").Value.Should().Be("Rooms");
        entity.FindPrimaryKey()!.Properties
            .Should().ContainSingle(p => p.Name == nameof(ConferenceRoom.Id));
    }

    [Fact]
    public void RoomConfiguration_NameProperty_IsRequiredWithMaxLength100()
    {
        using var context = InMemoryDbContextFactory.Create();

        var entity = context.Model.FindEntityType(typeof(ConferenceRoom))!;
        var name = entity.FindProperty(nameof(ConferenceRoom.Name))!;

        name.IsNullable.Should().BeFalse();
        name.GetMaxLength().Should().Be(100);
    }

    [Fact]
    public void RoomConfiguration_BaseHourlyRate_HasDecimalColumnType()
    {
        using var context = InMemoryDbContextFactory.Create();

        var entity = context.Model.FindEntityType(typeof(ConferenceRoom))!;
        var rate = entity.FindProperty(nameof(ConferenceRoom.BaseHourlyRate))!;

        rate.GetAnnotation("Relational:ColumnType").Value.Should().Be("decimal(18,2)");
        rate.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void RoomConfiguration_IsDeleted_DefaultsToFalse()
    {
        using var context = InMemoryDbContextFactory.Create();

        var entity = context.Model.FindEntityType(typeof(ConferenceRoom))!;
        var isDeleted = entity.FindProperty(nameof(ConferenceRoom.IsDeleted))!;

        isDeleted.GetAnnotation("Relational:DefaultValue").Value.Should().Be(false);
    }

    [Fact]
    public void RoomConfiguration_HasQueryFilterForSoftDelete()
    {
        using var context = InMemoryDbContextFactory.Create();

        var entity = context.Model.FindEntityType(typeof(ConferenceRoom))!;

        entity.GetQueryFilter().Should().NotBeNull();
    }

    [Fact]
    public void RoomConfiguration_HasIndexOnName()
    {
        using var context = InMemoryDbContextFactory.Create();

        var entity = context.Model.FindEntityType(typeof(ConferenceRoom))!;

        entity.GetIndexes().Should().Contain(i =>
            i.Properties.Count == 1 &&
            i.Properties[0].Name == nameof(ConferenceRoom.Name));
    }

    #endregion

    #region Booking mapping

    [Fact]
    public void BookingConfiguration_MapsToBookingsTableWithExpectedKey()
    {
        using var context = InMemoryDbContextFactory.Create();

        var entity = context.Model.FindEntityType(typeof(Booking))!;

        entity.GetAnnotation("Relational:TableName").Value.Should().Be("Bookings");
        entity.FindPrimaryKey()!.Properties
            .Should().ContainSingle(p => p.Name == nameof(Booking.Id));
    }

    [Fact]
    public void BookingConfiguration_TotalPrice_HasDecimalColumnType()
    {
        using var context = InMemoryDbContextFactory.Create();

        var entity = context.Model.FindEntityType(typeof(Booking))!;
        var totalPrice = entity.FindProperty(nameof(Booking.TotalPrice))!;

        totalPrice.GetAnnotation("Relational:ColumnType").Value
            .Should().Be("decimal(18,2)");
    }

    [Fact]
    public void BookingConfiguration_Status_IsStoredAsStringWithMaxLength20()
    {
        using var context = InMemoryDbContextFactory.Create();

        var entity = context.Model.FindEntityType(typeof(Booking))!;
        var status = entity.FindProperty(nameof(Booking.Status))!;

        status.GetProviderClrType().Should().Be(typeof(string));
        status.GetMaxLength().Should().Be(20);
    }

    [Fact]
    public void BookingConfiguration_StartAndEndTime_AreRequired()
    {
        using var context = InMemoryDbContextFactory.Create();

        var entity = context.Model.FindEntityType(typeof(Booking))!;

        entity.FindProperty(nameof(Booking.StartTime))!.IsNullable.Should().BeFalse();
        entity.FindProperty(nameof(Booking.EndTime))!.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void BookingConfiguration_RoomForeignKey_UsesRestrictDeleteBehavior()
    {
        using var context = InMemoryDbContextFactory.Create();

        var entity = context.Model.FindEntityType(typeof(Booking))!;
        var fk = entity.GetForeignKeys()
            .Should()
            .ContainSingle(f => f.PrincipalEntityType.ClrType == typeof(ConferenceRoom))
            .Subject;

        fk.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
    }

    [Fact]
    public void BookingConfiguration_HasCompositeIndexOnRoomIdStartTimeEndTime()
    {
        using var context = InMemoryDbContextFactory.Create();

        var entity = context.Model.FindEntityType(typeof(Booking))!;

        entity.GetIndexes().Should().Contain(i =>
            i.Properties.Select(p => p.Name).SequenceEqual(new[]
            {
                nameof(Booking.RoomId),
                nameof(Booking.StartTime),
                nameof(Booking.EndTime)
            }));
    }

    #endregion

    #region Service mapping

    [Fact]
    public void ServiceConfiguration_MapsToServicesTableWithExpectedKey()
    {
        using var context = InMemoryDbContextFactory.Create();

        var entity = context.Model.FindEntityType(typeof(Service))!;

        entity.GetAnnotation("Relational:TableName").Value.Should().Be("Services");
        entity.FindPrimaryKey()!.Properties
            .Should().ContainSingle(p => p.Name == nameof(Service.Id));
    }

    [Fact]
    public void ServiceConfiguration_NameProperty_IsRequiredWithMaxLength50()
    {
        using var context = InMemoryDbContextFactory.Create();

        var entity = context.Model.FindEntityType(typeof(Service))!;
        var name = entity.FindProperty(nameof(Service.Name))!;

        name.IsNullable.Should().BeFalse();
        name.GetMaxLength().Should().Be(50);
    }

    [Fact]
    public void ServiceConfiguration_Price_HasDecimalColumnTypeAndIsRequired()
    {
        using var context = InMemoryDbContextFactory.Create();

        var entity = context.Model.FindEntityType(typeof(Service))!;
        var price = entity.FindProperty(nameof(Service.Price))!;

        price.GetAnnotation("Relational:ColumnType").Value
            .Should().Be("decimal(18,2)");

        price.IsNullable.Should().BeFalse();
    }

    #endregion
}
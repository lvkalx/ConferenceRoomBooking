using ConferenceRoomBooking.Domain.Entities;
using FluentAssertions;

namespace ConferenceRoomBooking.Domain.Tests.Entities;

public class ServiceTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesService()
    {
        var service = new Service("Projector", 25.5m);

        service.Id.Should().NotBeEmpty();
        service.Name.Should().Be("Projector");
        service.Price.Should().Be(25.5m);
    }

    [Fact]
    public void Constructor_GeneratesUniqueId()
    {
        var service1 = new Service("Wi-Fi", 5m);
        var service2 = new Service("Wi-Fi", 5m);

        service1.Id.Should().NotBe(service2.Id);
    }

    [Fact]
    public void Constructor_TrimsName()
    {
        var service = new Service("  Sound System  ", 15m);

        service.Name.Should().Be("Sound System");
    }

    [Fact]
    public void Constructor_WithZeroPrice_IsAllowed()
    {
        var service = new Service("Free Water", 0m);

        service.Price.Should().Be(0m);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? name)
    {
        var act = () => new Service(name!, 10m);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Назва послуги є обов'язковою*");
    }

    [Fact]
    public void Constructor_WithNegativePrice_ThrowsArgumentException()
    {
        var act = () => new Service("Projector", -1m);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*не може бути від'ємною*");
    }

    [Fact]
    public void SetName_WithValidValue_UpdatesName()
    {
        var service = new Service("Projector", 10m);

        service.SetName("New Projector");

        service.Name.Should().Be("New Projector");
    }

    [Fact]
    public void SetName_WithInvalidValue_ThrowsArgumentException()
    {
        var service = new Service("Projector", 10m);

        var act = () => service.SetName("   ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetPrice_WithValidValue_UpdatesPrice()
    {
        var service = new Service("Projector", 10m);

        service.SetPrice(99.99m);

        service.Price.Should().Be(99.99m);
    }

    [Fact]
    public void SetPrice_WithNegativeValue_ThrowsArgumentException()
    {
        var service = new Service("Projector", 10m);

        var act = () => service.SetPrice(-0.01m);

        act.Should().Throw<ArgumentException>();
    }
}
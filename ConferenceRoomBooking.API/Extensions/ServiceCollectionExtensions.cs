using ConferenceRoomBooking.Application.Interfaces.Repositories;
using ConferenceRoomBooking.Application.Interfaces.Services;
using ConferenceRoomBooking.Application.Mapping;
using ConferenceRoomBooking.Application.Services;
using ConferenceRoomBooking.Application.Validators;
using ConferenceRoomBooking.Infrastructure.Data.Repositories;
using ConferenceRoomBooking.Infrastructure.Reports;
using FluentValidation;
using FluentValidation.AspNetCore;

namespace ConferenceRoomBooking.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IPricingService, PricingService>();

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IReportService, ReportService>();

        return services;
    }

    public static IServiceCollection AddMappingAndValidation(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => { }, typeof(MappingProfile));

        services.AddValidatorsFromAssemblyContaining<CreateRoomDtoValidator>();
        services.AddFluentValidationAutoValidation();

        return services;
    }
}
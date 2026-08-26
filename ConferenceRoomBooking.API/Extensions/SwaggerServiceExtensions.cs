using Microsoft.OpenApi.Models;
using System.Reflection;

namespace ConferenceRoomBooking.API.Extensions;

public static class SwaggerServiceExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Conference Room Booking API",
                Version = "v1",
                Description = "API для управління конференц-залами, бронюванням та розрахунком вартості оренди."
            });

            // Підключення XML-коментарів (<summary> над контролерами/методами) у Swagger UI
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        });

        return services;
    }
}
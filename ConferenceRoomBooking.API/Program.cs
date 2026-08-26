using ConferenceRoomBooking.API.Extensions;
using ConferenceRoomBooking.Infrastructure.Data;
using ConferenceRoomBooking.Infrastructure.Data.Seed;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBooking.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerDocumentation();

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddApplicationServices();
            builder.Services.AddInfrastructureServices();
            builder.Services.AddMappingAndValidation();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                using var scope = app.Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await DbSeeder.SeedAsync(dbContext);
            }

            // √лобальний обробник вин€тк≥в Ч реЇструЇтьс€ першим у пайплайн≥,
            // щоб перехоплювати помилки з ус≥х наступних middleware
            app.UseGlobalExceptionHandling();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            await app.RunAsync();
        }
    }
}
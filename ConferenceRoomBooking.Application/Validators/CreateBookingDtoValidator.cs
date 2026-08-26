using ConferenceRoomBooking.Application.DTOs.Bookings;
using FluentValidation;

namespace ConferenceRoomBooking.Application.Validators;

public class CreateBookingDtoValidator : AbstractValidator<CreateBookingDto>
{
    public CreateBookingDtoValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("ID залу є обов'язковим.");

        RuleFor(x => x.StartTime)
            .GreaterThan(DateTime.UtcNow).WithMessage("Час початку бронювання має бути в майбутньому.");

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime).WithMessage("Час завершення має бути пізніше часу початку.");

        RuleFor(x => x)
            .Must(x => (x.EndTime - x.StartTime).TotalMinutes >= 30)
            .WithMessage("Мінімальна тривалість бронювання — 30 хвилин.")
            .Must(x => (x.EndTime - x.StartTime).TotalHours <= 12)
            .WithMessage("Максимальна тривалість бронювання — 12 годин.");

        RuleForEach(x => x.SelectedServiceIds)
            .NotEmpty();
    }
}
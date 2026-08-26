using ConferenceRoomBooking.Application.DTOs.Rooms;
using FluentValidation;

namespace ConferenceRoomBooking.Application.Validators;

public class ReplaceRoomDtoValidator : AbstractValidator<ReplaceRoomDto>
{
    public ReplaceRoomDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Назва залу є обов'язковою.")
            .MaximumLength(100);

        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Місткість залу має бути більшою за нуль.")
            .LessThanOrEqualTo(1000);

        RuleFor(x => x.BaseHourlyRate)
            .GreaterThan(0).WithMessage("Базова вартість оренди має бути більшою за нуль.");

        RuleForEach(x => x.Services).SetValidator(new CreateServiceDtoValidator());
    }
}
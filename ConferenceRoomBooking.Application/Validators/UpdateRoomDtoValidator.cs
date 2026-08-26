using ConferenceRoomBooking.Application.DTOs.Rooms;
using FluentValidation;

namespace ConferenceRoomBooking.Application.Validators;

public class UpdateRoomDtoValidator : AbstractValidator<UpdateRoomDto>
{
    public UpdateRoomDtoValidator()
    {
        // Поля опційні — валідуємо лише те, що клієнт дійсно передав (не null).
        // Якщо поле присутнє, до нього застосовуються ті самі обмеження, що й у Create/Replace.

        When(x => x.Name is not null, () =>
        {
            RuleFor(x => x.Name!)
                .NotEmpty().WithMessage("Назва залу не може бути порожньою.")
                .MaximumLength(100);
        });

        When(x => x.Capacity.HasValue, () =>
        {
            RuleFor(x => x.Capacity!.Value)
                .GreaterThan(0).WithMessage("Місткість залу має бути більшою за нуль.")
                .LessThanOrEqualTo(1000);
        });

        When(x => x.BaseHourlyRate.HasValue, () =>
        {
            RuleFor(x => x.BaseHourlyRate!.Value)
                .GreaterThan(0).WithMessage("Базова вартість оренди має бути більшою за нуль.");
        });

        RuleForEach(x => x.ServicesToAdd).SetValidator(new CreateServiceDtoValidator());

        RuleForEach(x => x.ServiceIdsToRemove)
            .NotEqual(Guid.Empty).WithMessage("Ідентифікатор послуги для видалення не може бути порожнім Guid.");
    }
}
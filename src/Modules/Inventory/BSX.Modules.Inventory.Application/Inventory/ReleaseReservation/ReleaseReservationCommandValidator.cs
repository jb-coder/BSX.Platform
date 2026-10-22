using FluentValidation;

namespace BSX.Modules.Inventory.Application.Inventory.ReleaseReservation;

/// <summary>Validates <see cref="ReleaseReservationCommand"/> (shape only; the domain validates quantities).</summary>
public sealed class ReleaseReservationCommandValidator : AbstractValidator<ReleaseReservationCommand>
{
    /// <summary>Initializes a new instance of the <see cref="ReleaseReservationCommandValidator"/> class.</summary>
    public ReleaseReservationCommandValidator()
    {
        RuleFor(command => command.ProductId).NotEmpty();
        RuleFor(command => command.WarehouseId).NotEmpty();
    }
}

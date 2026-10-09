using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Users.UpdateUser;

/// <summary>Handles <see cref="UpdateUserCommand"/>.</summary>
public sealed class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initializes a new instance of the <see cref="UpdateUserCommandHandler"/> class.</summary>
    public UpdateUserCommandHandler(IUserRepository users, IUnitOfWork unitOfWork)
    {
        _users = users;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result> HandleAsync(UpdateUserCommand request, CancellationToken cancellationToken = default)
    {
        if (request.UserId == Guid.Empty)
        {
            return Result.Failure(IdentityApplicationErrors.InvalidUserId);
        }

        var nameResult = PersonName.Create(request.Name);

        if (nameResult.IsFailure)
        {
            return Result.Failure(nameResult.Error);
        }

        var emailResult = Email.Create(request.Email);

        if (emailResult.IsFailure)
        {
            return Result.Failure(emailResult.Error);
        }

        User? user = await _users.GetByIdAsync(UserId.From(request.UserId), cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure(IdentityApplicationErrors.UserNotFound);
        }

        if (user.Email != emailResult.Value
            && await _users.ExistsByEmailAsync(emailResult.Value, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure(IdentityApplicationErrors.UserEmailAlreadyExists);
        }

        Result changeName = user.ChangeName(nameResult.Value);

        if (changeName.IsFailure)
        {
            return changeName;
        }

        Result changeEmail = user.ChangeEmail(emailResult.Value);

        if (changeEmail.IsFailure)
        {
            return changeEmail;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}

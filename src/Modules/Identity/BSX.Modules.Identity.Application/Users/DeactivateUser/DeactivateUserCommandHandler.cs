using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Users.DeactivateUser;

/// <summary>Handles <see cref="DeactivateUserCommand"/>.</summary>
public sealed class DeactivateUserCommandHandler : ICommandHandler<DeactivateUserCommand>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initializes a new instance of the <see cref="DeactivateUserCommandHandler"/> class.</summary>
    public DeactivateUserCommandHandler(IUserRepository users, IUnitOfWork unitOfWork)
    {
        _users = users;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result> HandleAsync(DeactivateUserCommand request, CancellationToken cancellationToken = default)
    {
        if (request.UserId == Guid.Empty)
        {
            return Result.Failure(IdentityApplicationErrors.InvalidUserId);
        }

        User? user = await _users.GetByIdAsync(UserId.From(request.UserId), cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure(IdentityApplicationErrors.UserNotFound);
        }

        Result result = user.Deactivate();

        if (result.IsFailure)
        {
            return result;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}

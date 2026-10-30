using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Users.UnlockUser;

/// <summary>Handles <see cref="UnlockUserCommand"/>.</summary>
public sealed class UnlockUserCommandHandler : ICommandHandler<UnlockUserCommand>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initializes a new instance of the <see cref="UnlockUserCommandHandler"/> class.</summary>
    public UnlockUserCommandHandler(IUserRepository users, IUnitOfWork unitOfWork)
    {
        _users = users;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result> HandleAsync(UnlockUserCommand request, CancellationToken cancellationToken = default)
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

        // Unlock clears the counter and lockout window and rotates the security stamp, so sessions
        // and refresh tokens issued before the lockout cannot be revived.
        Result result = user.Unlock();

        if (result.IsFailure)
        {
            return result;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}

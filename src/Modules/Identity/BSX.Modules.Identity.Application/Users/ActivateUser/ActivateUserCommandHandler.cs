using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Users.ActivateUser;

/// <summary>Handles <see cref="ActivateUserCommand"/>.</summary>
public sealed class ActivateUserCommandHandler : ICommandHandler<ActivateUserCommand>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="ActivateUserCommandHandler"/> class.</summary>
    public ActivateUserCommandHandler(IUserRepository users, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<Result> HandleAsync(ActivateUserCommand request, CancellationToken cancellationToken = default)
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

        Result result = user.Activate(_timeProvider.GetUtcNow());

        if (result.IsFailure)
        {
            return result;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}

using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Dtos;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Application.Mappings;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Users.GetUserById;

/// <summary>Handles <see cref="GetUserByIdQuery"/>.</summary>
public sealed class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDto>
{
    private readonly IUserRepository _users;

    /// <summary>Initializes a new instance of the <see cref="GetUserByIdQueryHandler"/> class.</summary>
    public GetUserByIdQueryHandler(IUserRepository users) => _users = users;

    /// <inheritdoc />
    public async Task<Result<UserDto>> HandleAsync(GetUserByIdQuery request, CancellationToken cancellationToken = default)
    {
        if (request.UserId == Guid.Empty)
        {
            return Result.Failure<UserDto>(IdentityApplicationErrors.InvalidUserId);
        }

        User? user = await _users.GetByIdAsync(UserId.From(request.UserId), cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure<UserDto>(IdentityApplicationErrors.UserNotFound);
        }

        return Result.Success(user.ToDto());
    }
}

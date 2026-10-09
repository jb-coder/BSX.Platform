using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Users.RegisterUser;

/// <summary>Handles <see cref="RegisterUserCommand"/>.</summary>
public sealed class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, RegisterUserResponse>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="RegisterUserCommandHandler"/> class.</summary>
    public RegisterUserCommandHandler(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<Result<RegisterUserResponse>> HandleAsync(RegisterUserCommand request, CancellationToken cancellationToken = default)
    {
        var emailResult = Email.Create(request.Email);

        if (emailResult.IsFailure)
        {
            return Result.Failure<RegisterUserResponse>(emailResult.Error);
        }

        var nameResult = PersonName.Create(request.Name);

        if (nameResult.IsFailure)
        {
            return Result.Failure<RegisterUserResponse>(nameResult.Error);
        }

        if (await _users.ExistsByEmailAsync(emailResult.Value, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<RegisterUserResponse>(IdentityApplicationErrors.UserEmailAlreadyExists);
        }

        var userResult = User.Register(UserId.New(), emailResult.Value, nameResult.Value, _timeProvider.GetUtcNow());

        if (userResult.IsFailure)
        {
            return Result.Failure<RegisterUserResponse>(userResult.Error);
        }

        var user = userResult.Value;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.SetPassword(_passwordHasher.Hash(request.Password), _timeProvider.GetUtcNow());
        }

        await _users.AddAsync(user, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(new RegisterUserResponse(user.Id.Value));
    }
}

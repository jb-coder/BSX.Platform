using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.Fakes;

/// <summary>In-memory <see cref="IUserRepository"/> for handler tests.</summary>
internal sealed class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = [];

    public IReadOnlyCollection<User> Users => _users;

    public void Seed(params User[] users) => _users.AddRange(users);

    public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_users.Find(user => user.Id == id));

    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
        => Task.FromResult(_users.Find(user => user.Email == email));

    public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
        => Task.FromResult(_users.Exists(user => user.Email == email));

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }
}

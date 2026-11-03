using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Identity.Infrastructure.Repositories;

internal sealed class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _context;

    public UserRepository(IdentityDbContext context) => _context = context;

    public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
        => _context.Users
            .Include(user => user.PlatformRoleIds)
            .Include(user => user.Credentials)
            .Include(user => user.PasswordResetToken)
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
        => _context.Users
            .Include(user => user.PlatformRoleIds)
            .Include(user => user.Credentials)
            .Include(user => user.PasswordResetToken)
            .FirstOrDefaultAsync(user => user.Email == email, cancellationToken);

    public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
        => _context.Users.AnyAsync(user => user.Email == email, cancellationToken);

    public Task<User?> FindByPasswordResetTokenAsync(PasswordResetTokenHash hash, CancellationToken cancellationToken = default)
        => _context.Users
            .Include(user => user.PlatformRoleIds)
            .Include(user => user.Credentials)
            .Include(user => user.PasswordResetToken)
            .FirstOrDefaultAsync(
                user => user.PasswordResetToken != null && user.PasswordResetToken.Hash == hash,
                cancellationToken);

    public Task<bool> ExistsOtherActiveUserWithPlatformRoleAsync(
        RoleId roleId,
        UserId excludedUserId,
        CancellationToken cancellationToken = default)
        => _context.Users.AnyAsync(
            user => user.Status == UserStatus.Active
                && user.Id != excludedUserId
                && user.PlatformRoleIds.Any(platformRoleId => platformRoleId.Value == roleId.Value),
            cancellationToken);

    public Task TrackPlatformRoleChangesAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        foreach (var entry in _context.ChangeTracker.Entries()
            .Where(entry => entry.Metadata.Name.Contains("PlatformRoleIds", StringComparison.Ordinal)
                && entry.State == EntityState.Modified))
        {
            entry.State = EntityState.Added;
        }

        return Task.CompletedTask;
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken).ConfigureAwait(false);
        await TrackPlatformRoleChangesAsync(user, cancellationToken).ConfigureAwait(false);
    }
}

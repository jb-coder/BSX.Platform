using BSX.Modules.Identity.Application.Dtos;
using BSX.Modules.Identity.Domain.Users;

namespace BSX.Modules.Identity.Application.Mappings;

/// <summary>Maps the <see cref="User"/> aggregate to read-model DTOs.</summary>
public static class UserMappings
{
    /// <summary>Maps a user to its full projection.</summary>
    /// <param name="user">The user aggregate.</param>
    public static UserDto ToDto(this User user) => new(
        user.Id.Value,
        user.Email.Value,
        user.Name.Value,
        user.Status.ToString(),
        user.IsMfaEnabled,
        user.PlatformRoleIds.Select(roleId => roleId.Value).ToArray(),
        user.RegisteredOnUtc);

    /// <summary>Maps a user to its summary projection.</summary>
    /// <param name="user">The user aggregate.</param>
    public static UserSummaryDto ToSummary(this User user) => new(
        user.Id.Value,
        user.Email.Value,
        user.Name.Value,
        user.Status.ToString());
}

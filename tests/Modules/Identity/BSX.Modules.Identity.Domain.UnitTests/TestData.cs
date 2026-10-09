using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Domain.UnitTests;

/// <summary>Shared test data builders for the Identity domain.</summary>
internal static class TestData
{
    internal static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    internal static Email CreateEmail(string value = "user@example.com") => Email.Create(value).Value;

    internal static PersonName CreateName(string value = "Test User") => PersonName.Create(value).Value;

    internal static PasswordHash CreatePasswordHash(string value = "hash") => PasswordHash.From(value, "test");

    internal static Permission CreatePermission(string code = "crm.customers.read") => Permission.Create(code).Value;

    internal static User CreateActiveUser(string email = "user@example.com")
    {
        User user = User.Register(UserId.New(), CreateEmail(email), CreateName(), Now).Value;
        user.SetPassword(CreatePasswordHash(), Now);
        user.Activate(Now);
        user.ClearDomainEvents();
        return user;
    }
}

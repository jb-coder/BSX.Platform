using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.Users.Credentials;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.Modules.Identity.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Modules.Identity.Infrastructure.Persistence;
using BSX.Modules.Identity.Infrastructure.Repositories;
using BSX.Modules.Identity.Infrastructure.Security;

namespace BSX.Modules.Identity.Infrastructure.IntegrationTests;

public sealed class PasswordResetTests
{
    [Fact]
    public async Task Should_PersistHash_AndCompleteReset()
    {
        await using SqliteIdentityDatabase database = await SqliteIdentityDatabase.CreateAsync();
        var users = new UserRepository(database.Context);
        var unitOfWork = new IdentityUnitOfWork(database.Context, new RecordingDomainEventDispatcher());
        var tokenHasher = new PasswordResetTokenHasher();

        User user = TestData.CreateUser("reset@example.com");
        await users.AddAsync(user);
        await unitOfWork.SaveChangesAsync();

        var hash = tokenHasher.Hash("reset-token");
        user.RequestPasswordReset(PasswordResetTokenId.New(), hash, TestData.Now, TimeSpan.FromMinutes(30));
        await unitOfWork.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        // Only the hash is stored and the user is discoverable by the token hash.
        User? found = await users.FindByPasswordResetTokenAsync(tokenHasher.Hash("reset-token"));
        found.Should().NotBeNull();
        found!.PasswordResetToken!.Hash.Value.Should().NotContain("reset-token");

        var newHash = new PasswordHasher().Hash("NewPassw0rd!");
        found.CompletePasswordReset(tokenHasher.Hash("reset-token"), newHash, TestData.Now.AddMinutes(1));
        await unitOfWork.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        User? reloaded = await users.GetByIdAsync(user.Id);
        reloaded!.PasswordResetToken!.IsUsed.Should().BeTrue();

        PasswordCredential credential = reloaded.Credentials.OfType<PasswordCredential>().Single();
        credential.Hash.Hash.Should().Be(newHash.Hash);
        credential.Hash.Algorithm.Should().Be(newHash.Algorithm);
    }
}

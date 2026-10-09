using BSX.Modules.Identity.Domain.DomainEvents;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.Modules.Identity.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Identity.Infrastructure.IntegrationTests;

public sealed class DomainEventDispatchTests
{
    [Fact]
    public async Task Should_CollectPersistAndDispatchDomainEvents()
    {
        await using SqliteIdentityDatabase database = await SqliteIdentityDatabase.CreateAsync();
        var dispatcher = new RecordingDomainEventDispatcher();
        var unitOfWork = new IdentityUnitOfWork(database.Context, dispatcher);

        User user = User.Register(UserId.New(), TestData.CreateEmail(), TestData.CreateName(), TestData.Now).Value;
        await database.Context.Users.AddAsync(user);

        await unitOfWork.SaveChangesAndDispatchAsync();

        (await database.Context.Users.CountAsync()).Should().Be(1);
        dispatcher.Dispatched.Should().ContainSingle(domainEvent => domainEvent is UserRegistered);
        user.DomainEvents.Should().BeEmpty();
    }
}

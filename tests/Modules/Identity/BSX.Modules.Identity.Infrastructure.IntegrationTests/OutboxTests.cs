using BSX.Modules.Identity.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Modules.Identity.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Identity.Infrastructure.IntegrationTests;

public sealed class OutboxTests
{
    [Fact]
    public async Task Should_PersistIntegrationEvent_ToOutbox()
    {
        await using SqliteIdentityDatabase database = await SqliteIdentityDatabase.CreateAsync();
        var writer = new OutboxWriter(database.Context, TimeProvider.System);

        writer.Enqueue(new TestIntegrationEvent(Guid.NewGuid(), TestData.Now));
        await database.Context.SaveChangesAsync();

        List<OutboxMessage> messages = await database.Context.OutboxMessages.ToListAsync();

        messages.Should().ContainSingle();
        messages[0].Kind.Should().Be(OutboxMessageStatus.IntegrationKind);
        messages[0].Status.Should().Be(OutboxMessageStatus.Pending);
        messages[0].Payload.Should().NotBeNullOrWhiteSpace();
    }
}

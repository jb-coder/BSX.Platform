using BSX.BuildingBlocks.DependencyInjection;
using BSX.BuildingBlocks.Events;
using BSX.BuildingBlocks.Persistence.Outbox;
using BSX.Modules.Identity.Domain.DomainEvents;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Modules.Identity.Infrastructure.Persistence;
using BSX.Persistence.Outbox;
using BSX.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BSX.Modules.Identity.Infrastructure.IntegrationTests;

/// <summary>
/// Verifies ADR-009 durability: events are persisted atomically with aggregate state, claimed and
/// dispatched exactly once, retried, dead-lettered and recoverable after a crash.
/// </summary>
public sealed class OutboxDurabilityTests
{
    [Fact]
    public async Task Should_PersistDomainEvent_AtomicallyWithAggregateState()
    {
        await using SqliteIdentityDatabase database = await SqliteIdentityDatabase.CreateAsync(outbox: true);
        User user = TestData.CreateUser();

        await database.Context.Users.AddAsync(user);
        await database.Context.SaveChangesAsync();

        List<OutboxMessage> messages = await database.Context.OutboxMessages.ToListAsync();

        messages.Should().NotBeEmpty();
        OutboxMessage registered = messages.Should().ContainSingle(message => message.EventType.Contains(nameof(UserRegistered))).Which;
        registered.Kind.Should().Be(OutboxKinds.Domain);
        registered.Status.Should().Be(OutboxMessageStatus.Pending);
        registered.Payload.Should().Contain(user.Id.Value.ToString());
    }

    [Fact]
    public async Task Should_RollBackOutboxRow_WhenTheTransactionRollsBack()
    {
        await using SqliteIdentityDatabase database = await SqliteIdentityDatabase.CreateAsync(outbox: true);
        await using var transaction = await database.Context.Database.BeginTransactionAsync();

        await database.Context.Users.AddAsync(TestData.CreateUser());
        await database.Context.SaveChangesAsync();
        await transaction.RollbackAsync();

        (await database.Context.Users.CountAsync()).Should().Be(0);
        (await database.Context.OutboxMessages.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Should_ClaimDispatchAndMarkProcessed_ExactlyOnce()
    {
        await using SqliteIdentityDatabase database = await SqliteIdentityDatabase.CreateAsync(outbox: true);
        User user = TestData.CreateUser();
        await database.Context.Users.AddAsync(user);
        await database.Context.SaveChangesAsync();

        UserRegistered domainEvent = user.DomainEvents.OfType<UserRegistered>().Single();
        var handler = new CountingUserRegisteredHandler();
        using ServiceProvider provider = CreateDispatcherProvider(database, handler);
        IDomainEventDispatcher dispatcher = provider.CreateScope().ServiceProvider
            .GetRequiredService<IDomainEventDispatcher>();

        await dispatcher.DispatchAsync([domainEvent]);
        await dispatcher.DispatchAsync([domainEvent]);

        handler.Count.Should().Be(1);
        OutboxMessage message = await database.Context.OutboxMessages
            .AsNoTracking()
            .SingleAsync(candidate => candidate.EventId == domainEvent.EventId);
        message.Status.Should().Be(OutboxMessageStatus.Processed);
        message.ProcessedOnUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_RecoverAndDeserializeAPendingDomainEvent()
    {
        await using SqliteIdentityDatabase database = await SqliteIdentityDatabase.CreateAsync(outbox: true);
        User user = TestData.CreateUser();
        await database.Context.Users.AddAsync(user);
        await database.Context.SaveChangesAsync();

        var store = CreateStore(database);
        IReadOnlyList<OutboxEnvelope> pending = await store.ReadPendingAsync(10);

        OutboxEnvelope envelope = pending
            .Should().ContainSingle(candidate => candidate.EventType.Contains(nameof(UserRegistered))).Which;
        IDomainEvent? recovered = DomainEventSerializer.Deserialize(envelope);
        recovered.Should().BeOfType<UserRegistered>();
        ((UserRegistered)recovered!).Email.Should().Be(user.Email.Value);
        recovered!.EventId.Should().Be(envelope.EventId);
    }

    [Fact]
    public async Task Should_RequeueAStaleProcessingClaim()
    {
        var time = new MutableTimeProvider(DateTimeOffset.UtcNow);
        await using SqliteIdentityDatabase database = await SqliteIdentityDatabase.CreateAsync(
            outbox: true,
            timeProvider: time);
        User user = TestData.CreateUser();
        await database.Context.Users.AddAsync(user);
        await database.Context.SaveChangesAsync();

        UserRegistered domainEvent = user.DomainEvents.OfType<UserRegistered>().Single();
        var store = new EfOutboxStore<IdentityDbContext>(database.Context, time, Options.Create(new OutboxOptions()));

        (await store.ClaimAsync([domainEvent.EventId])).Should().ContainSingle();

        time.UtcNow = time.UtcNow.AddMinutes(10);
        int requeued = await store.RequeueStaleAsync(TimeSpan.FromMinutes(5));

        requeued.Should().Be(1);
        OutboxMessage message = await database.Context.OutboxMessages
            .AsNoTracking()
            .SingleAsync(candidate => candidate.EventId == domainEvent.EventId);
        message.Status.Should().Be(OutboxMessageStatus.Pending);
    }

    [Fact]
    public async Task Should_DeadLetter_WhenTheHandlerKeepsFailing()
    {
        await using SqliteIdentityDatabase database = await SqliteIdentityDatabase.CreateAsync(
            outbox: true,
            outboxOptions: new OutboxOptions { MaxRetryCount = 1 });
        User user = TestData.CreateUser();
        await database.Context.Users.AddAsync(user);
        await database.Context.SaveChangesAsync();

        UserRegistered domainEvent = user.DomainEvents.OfType<UserRegistered>().Single();
        var handler = new FailingUserRegisteredHandler();
        var options = new OutboxOptions { MaxRetryCount = 1 };
        using ServiceProvider provider = CreateDispatcherProvider(database, handler, options);
        IDomainEventDispatcher dispatcher = provider.CreateScope().ServiceProvider
            .GetRequiredService<IDomainEventDispatcher>();

        await dispatcher.DispatchAsync([domainEvent]);

        handler.Count.Should().Be(1);
        OutboxMessage message = await database.Context.OutboxMessages
            .AsNoTracking()
            .SingleAsync(candidate => candidate.EventId == domainEvent.EventId);
        message.Status.Should().Be(OutboxMessageStatus.DeadLettered);
        message.RetryCount.Should().Be(1);
        message.LastError.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Should_NotRedispatch_WhileTheRetryBackoffIsPending()
    {
        await using SqliteIdentityDatabase database = await SqliteIdentityDatabase.CreateAsync(
            outbox: true,
            outboxOptions: new OutboxOptions { MaxRetryCount = 10, BaseBackoff = TimeSpan.FromHours(1) });
        User user = TestData.CreateUser();
        await database.Context.Users.AddAsync(user);
        await database.Context.SaveChangesAsync();

        UserRegistered domainEvent = user.DomainEvents.OfType<UserRegistered>().Single();
        var handler = new FailingUserRegisteredHandler();
        var options = new OutboxOptions { MaxRetryCount = 10, BaseBackoff = TimeSpan.FromHours(1) };
        using ServiceProvider provider = CreateDispatcherProvider(database, handler, options);
        IDomainEventDispatcher dispatcher = provider.CreateScope().ServiceProvider
            .GetRequiredService<IDomainEventDispatcher>();

        await dispatcher.DispatchAsync([domainEvent]);
        await dispatcher.DispatchAsync([domainEvent]);

        handler.Count.Should().Be(1);
        OutboxMessage message = await database.Context.OutboxMessages
            .AsNoTracking()
            .SingleAsync(candidate => candidate.EventId == domainEvent.EventId);
        message.Status.Should().Be(OutboxMessageStatus.Failed);
        message.RetryCount.Should().Be(1);
        message.NextAttemptOnUtc.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    private static EfOutboxStore<IdentityDbContext> CreateStore(
        SqliteIdentityDatabase database,
        OutboxOptions? options = null)
        => new(database.Context, TimeProvider.System, Options.Create(options ?? new OutboxOptions()));

    private static ServiceProvider CreateDispatcherProvider(
        SqliteIdentityDatabase database,
        IDomainEventHandler<UserRegistered> handler,
        OutboxOptions? options = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDomainEventHandler<UserRegistered>>(handler);
        services.AddSingleton<IOutboxStore>(CreateStore(database, options));
        services.AddBuildingBlocks();
        return services.BuildServiceProvider();
    }

    private sealed class CountingUserRegisteredHandler : IDomainEventHandler<UserRegistered>
    {
        public int Count { get; private set; }

        public Task HandleAsync(UserRegistered domainEvent, CancellationToken cancellationToken = default)
        {
            Count++;
            return Task.CompletedTask;
        }
    }

    private sealed class FailingUserRegisteredHandler : IDomainEventHandler<UserRegistered>
    {
        public int Count { get; private set; }

        public Task HandleAsync(UserRegistered domainEvent, CancellationToken cancellationToken = default)
        {
            Count++;
            throw new InvalidOperationException("Simulated handler failure.");
        }
    }
}

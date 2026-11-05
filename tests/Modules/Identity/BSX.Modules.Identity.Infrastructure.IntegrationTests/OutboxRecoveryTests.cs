using System.Text.Json;
using BSX.BuildingBlocks.DependencyInjection;
using BSX.BuildingBlocks.Events;
using BSX.BuildingBlocks.Persistence.Outbox;
using BSX.Modules.Identity.Domain.DomainEvents;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.Modules.Identity.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Modules.Identity.Infrastructure.Persistence;
using BSX.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BSX.Modules.Identity.Infrastructure.IntegrationTests;

/// <summary>
/// Verifies the recovery loop requeues stale claims on every pass and isolates poison envelopes so
/// they cannot stall healthy rows or other module stores (ADR-009).
/// </summary>
public sealed class OutboxRecoveryTests
{
    [Fact]
    public async Task Should_RequeueStaleClaim_AfterTheFirstPass_WhenProcessingIsStale()
    {
        var time = new MutableTimeProvider(TestData.Now);
        await using SqliteIdentityDatabase database = await SqliteIdentityDatabase.CreateAsync(timeProvider: time);
        OutboxOptions options = CreateOptions();
        var store = new EfOutboxStore<IdentityDbContext>(database.Context, time, Options.Create(options));

        (OutboxMessage row, Guid eventId) = CreateHealthyDomainRow(time.UtcNow);
        await database.Context.OutboxMessages.AddAsync(row);
        await database.Context.SaveChangesAsync();

        var dispatcher = new RecordingDomainEventDispatcher();
        using ServiceProvider provider = CreateProvider(dispatcher, store);
        OutboxRecoveryService service = CreateService(provider, options);

        // Simulate a worker that claims the row and then crashes before processing it.
        (await store.ClaimAsync([eventId])).Should().ContainSingle();

        await service.ProcessOnceAsync(CancellationToken.None);

        (await ReadRowAsync(database, eventId)).Status.Should().Be(OutboxMessageStatus.Processing);
        dispatcher.Dispatched.Should().BeEmpty();

        // The claim goes stale only after the first pass has already completed.
        time.UtcNow = time.UtcNow.Add(options.StaleProcessingAfter).Add(TimeSpan.FromMinutes(1));

        await service.ProcessOnceAsync(CancellationToken.None);

        (await ReadRowAsync(database, eventId)).Status.Should().Be(OutboxMessageStatus.Pending);
        dispatcher.Dispatched.Should().ContainSingle(domainEvent => domainEvent.EventId == eventId);
    }

    [Fact]
    public async Task Should_IsolateMalformedPayload_WhenHealthyRowsExist()
    {
        var time = new MutableTimeProvider(TestData.Now);
        await using SqliteIdentityDatabase database = await SqliteIdentityDatabase.CreateAsync(timeProvider: time);
        OutboxOptions options = CreateOptions();
        var store = new EfOutboxStore<IdentityDbContext>(database.Context, time, Options.Create(options));

        // The poison row is older, so it would sit at the queue head if it were allowed to block.
        (OutboxMessage malformed, Guid malformedEventId) = CreateMalformedDomainRow(time.UtcNow.AddMinutes(-10));
        (OutboxMessage healthy, Guid healthyEventId) = CreateHealthyDomainRow(time.UtcNow);

        await database.Context.OutboxMessages.AddRangeAsync(malformed, healthy);
        await database.Context.SaveChangesAsync();

        using ServiceProvider provider = CreateProvider(dispatcher: null, store);
        OutboxRecoveryService service = CreateService(provider, options);

        Func<Task> act = () => service.ProcessOnceAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();

        OutboxMessage malformedRow = await ReadRowAsync(database, malformedEventId);
        malformedRow.Status.Should().BeOneOf(OutboxMessageStatus.Failed, OutboxMessageStatus.DeadLettered);
        malformedRow.RetryCount.Should().BeGreaterThanOrEqualTo(1);
        malformedRow.LastError.Should().NotBeNullOrWhiteSpace();

        OutboxMessage healthyRow = await ReadRowAsync(database, healthyEventId);
        healthyRow.Status.Should().Be(OutboxMessageStatus.Processed);
    }

    [Fact]
    public async Task Should_IsolateMalformedPayload_AcrossStores_WhenHealthyRowsExist()
    {
        var time = new MutableTimeProvider(TestData.Now);
        await using SqliteIdentityDatabase poisonedDatabase = await SqliteIdentityDatabase.CreateAsync(timeProvider: time);
        await using SqliteIdentityDatabase healthyDatabase = await SqliteIdentityDatabase.CreateAsync(timeProvider: time);
        OutboxOptions options = CreateOptions();
        var poisonedStore = new EfOutboxStore<IdentityDbContext>(poisonedDatabase.Context, time, Options.Create(options));
        var healthyStore = new EfOutboxStore<IdentityDbContext>(healthyDatabase.Context, time, Options.Create(options));

        (OutboxMessage malformed, Guid malformedEventId) = CreateMalformedDomainRow(time.UtcNow);
        (OutboxMessage healthy, Guid healthyEventId) = CreateHealthyDomainRow(time.UtcNow);

        await poisonedDatabase.Context.OutboxMessages.AddAsync(malformed);
        await poisonedDatabase.Context.SaveChangesAsync();
        await healthyDatabase.Context.OutboxMessages.AddAsync(healthy);
        await healthyDatabase.Context.SaveChangesAsync();

        using ServiceProvider provider = CreateProvider(dispatcher: null, poisonedStore, healthyStore);
        OutboxRecoveryService service = CreateService(provider, options);

        Func<Task> act = () => service.ProcessOnceAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();

        OutboxMessage malformedRow = await ReadRowAsync(poisonedDatabase, malformedEventId);
        malformedRow.Status.Should().BeOneOf(OutboxMessageStatus.Failed, OutboxMessageStatus.DeadLettered);
        malformedRow.RetryCount.Should().BeGreaterThanOrEqualTo(1);
        malformedRow.LastError.Should().NotBeNullOrWhiteSpace();

        OutboxMessage healthyRow = await ReadRowAsync(healthyDatabase, healthyEventId);
        healthyRow.Status.Should().Be(OutboxMessageStatus.Processed);
    }

    private static OutboxOptions CreateOptions() => new()
    {
        StaleProcessingAfter = TimeSpan.FromMinutes(5),
        MaxRetryCount = 10,
        BaseBackoff = TimeSpan.FromSeconds(5),
        BatchSize = 100,
    };

    private static (OutboxMessage Row, Guid EventId) CreateHealthyDomainRow(DateTimeOffset occurredOnUtc)
    {
        Guid eventId = Guid.NewGuid();
        var domainEvent = new UserRegistered(UserId.New(), "healthy@example.com")
        {
            EventId = eventId,
            OccurredOnUtc = occurredOnUtc,
        };

        string payload = JsonSerializer.Serialize(domainEvent, typeof(UserRegistered));

        return (
            new OutboxMessage(
                Guid.NewGuid(),
                eventId,
                OutboxKinds.Domain,
                typeof(UserRegistered).FullName!,
                payload,
                occurredOnUtc,
                occurredOnUtc),
            eventId);
    }

    private static (OutboxMessage Row, Guid EventId) CreateMalformedDomainRow(DateTimeOffset occurredOnUtc)
    {
        Guid eventId = Guid.NewGuid();

        return (
            new OutboxMessage(
                Guid.NewGuid(),
                eventId,
                OutboxKinds.Domain,
                typeof(UserRegistered).FullName!,
                "{not-json",
                occurredOnUtc,
                occurredOnUtc),
            eventId);
    }

    private static ServiceProvider CreateProvider(
        IDomainEventDispatcher? dispatcher,
        params IOutboxStore[] stores)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        if (dispatcher is not null)
        {
            services.AddSingleton(dispatcher);
        }

        services.AddBuildingBlocks();

        foreach (IOutboxStore store in stores)
        {
            services.AddSingleton(store);
        }

        return services.BuildServiceProvider();
    }

    private static OutboxRecoveryService CreateService(ServiceProvider provider, OutboxOptions options)
        => new(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options),
            NullLogger<OutboxRecoveryService>.Instance);

    private static Task<OutboxMessage> ReadRowAsync(SqliteIdentityDatabase database, Guid eventId)
        => database.Context.OutboxMessages
            .AsNoTracking()
            .SingleAsync(message => message.EventId == eventId);
}

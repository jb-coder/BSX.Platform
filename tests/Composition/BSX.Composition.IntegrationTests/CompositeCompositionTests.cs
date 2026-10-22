using BSX.BuildingBlocks.DependencyInjection;
using BSX.BuildingBlocks.Persistence;
using BSX.Composition.IntegrationTests.TestDoubles;
using BSX.Modules.Crm.Application.Abstractions;
using BSX.Modules.Crm.Application.Customers.CreateCustomer;
using BSX.Modules.Crm.Application.DependencyInjection;
using BSX.Modules.Crm.Domain.DomainEvents;
using BSX.Modules.Crm.Infrastructure.DependencyInjection;
using BSX.Modules.Crm.Infrastructure.Persistence;
using BSX.Modules.Crm.Infrastructure.Persistence.Interceptors;
using BSX.Modules.Crm.Infrastructure.Repositories;
using BSX.Modules.Identity.Application.DependencyInjection;
using BSX.Modules.Identity.Application.Users.RegisterUser;
using BSX.Modules.Identity.Domain.DomainEvents;
using BSX.Modules.Identity.Infrastructure.DependencyInjection;
using BSX.Modules.Identity.Infrastructure.Persistence;
using BSX.Modules.Identity.Infrastructure.Persistence.Interceptors;
using BSX.Modules.Identity.Infrastructure.Repositories;
using BSX.Modules.Sales.Application.DependencyInjection;
using BSX.Modules.Sales.Infrastructure.DependencyInjection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BSX.Composition.IntegrationTests;

public sealed class CompositeCompositionTests
{
    [Fact]
    public async Task Should_PersistIdentityAndCrm_AndDispatchEventsExactlyOnce()
    {
        Guid tenantId = Guid.NewGuid();
        var dispatcher = new RecordingDomainEventDispatcher();
        var currentUser = new TestCurrentUser(Guid.NewGuid());

        await using var identityConnection = new SqliteConnection("DataSource=:memory:");
        await identityConnection.OpenAsync();
        var identityOptions = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlite(identityConnection)
            .AddInterceptors(new AuditingSaveChangesInterceptor(TimeProvider.System, currentUser))
            .Options;
        await using var identityContext = new IdentityDbContext(identityOptions);
        await identityContext.Database.EnsureCreatedAsync();

        await using var crmConnection = new SqliteConnection("DataSource=:memory:");
        await crmConnection.OpenAsync();
        var crmOptions = new DbContextOptionsBuilder<CrmDbContext>()
            .UseSqlite(crmConnection)
            .AddInterceptors(new CrmAuditingSaveChangesInterceptor(TimeProvider.System, currentUser))
            .Options;
        await using var crmContext = new CrmDbContext(crmOptions, new TestCrmTenantScope(tenantId));
        await crmContext.Database.EnsureCreatedAsync();

        var identityUnitOfWork = new IdentityUnitOfWork(identityContext, dispatcher);
        var crmUnitOfWork = new CrmUnitOfWork(crmContext, dispatcher);
        var composite = new CompositeUnitOfWork([identityUnitOfWork, crmUnitOfWork], dispatcher);

        await using IDatabaseTransaction transaction = await composite.BeginTransactionAsync();

        var identityHandler = new RegisterUserCommandHandler(
            new UserRepository(identityContext),
            new FakePasswordHasher(),
            composite,
            TimeProvider.System);
        var crmHandler = new CreateCustomerCommandHandler(
            new CustomerRepository(crmContext),
            composite,
            new CrmTenantResolver(new TestTenantContext(tenantId)));

        var identityResult = await identityHandler.HandleAsync(
            new RegisterUserCommand("user@example.com", "Test User", "Passw0rd!"));
        var crmResult = await crmHandler.HandleAsync(
            new CreateCustomerCommand("CUST-001", "Acme Corporation", null));

        identityResult.IsSuccess.Should().BeTrue();
        crmResult.IsSuccess.Should().BeTrue();

        await composite.SaveChangesAndDispatchAsync();

        (await identityContext.Users.CountAsync()).Should().Be(1);
        (await crmContext.Customers.CountAsync()).Should().Be(1);
        dispatcher.Dispatched.OfType<UserRegistered>().Should().ContainSingle();
        dispatcher.Dispatched.OfType<CustomerCreated>().Should().ContainSingle();
        (await identityContext.Users.SingleAsync()).DomainEvents.Should().BeEmpty();
        (await crmContext.Customers.SingleAsync()).DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_ResolveCompositeUnitOfWork_WhenAllModulesAreRegistered()
    {
        var services = new ServiceCollection();
        services.AddIdentityApplication();
        services.AddIdentityInfrastructure("Host=localhost;Port=5432;Database=bsx_identity;Username=postgres;Password=postgres");
        services.AddCrmApplication();
        services.AddCrmInfrastructure("Host=localhost;Port=5432;Database=bsx_crm;Username=postgres;Password=postgres");
        services.AddSalesApplication();
        services.AddSalesInfrastructure("Host=localhost;Port=5432;Database=bsx_sales;Username=postgres;Password=postgres");
        services.AddCompositeUnitOfWork();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IUnitOfWork>().Should().BeOfType<CompositeUnitOfWork>();

        IModuleUnitOfWork[] modules = scope.ServiceProvider.GetServices<IModuleUnitOfWork>().ToArray();
        modules.Should().HaveCount(3);
        modules.Select(module => module.ModuleName).Should().BeEquivalentTo(["Identity", "Crm", "Sales"]);
    }
}

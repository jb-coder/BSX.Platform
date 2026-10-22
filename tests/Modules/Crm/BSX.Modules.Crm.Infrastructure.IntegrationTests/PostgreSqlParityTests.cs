using System.Data.Common;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Modules.Crm.Infrastructure.Persistence;
using BSX.Modules.Crm.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BSX.Modules.Crm.Infrastructure.IntegrationTests;

/// <summary>
/// Provider parity: validates the CRM schema, constraints, tenant filters, search, soft delete and
/// optimistic concurrency against a real PostgreSQL instance (the production provider, ADR-004).
/// </summary>
public sealed class PostgreSqlParityTests
{
    [DockerAvailableFact]
    public async Task Migrations_Should_Apply_AndCreateTheCrmSchema()
    {
        PostgreSqlCrmEnvironment environment = await PostgreSqlCrmEnvironment.GetAsync();
        await using CrmDbContext context = environment.CreateContext(Guid.NewGuid());

        (await context.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
        (await context.Database.GetAppliedMigrationsAsync()).Should().Contain("20261020145553_InitialCrm");

        DbConnection connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT count(*)
            FROM information_schema.tables
            WHERE table_schema = 'crm'
              AND table_name IN ('customers', 'customer_addresses', 'customer_contacts', 'organizations')
            """;

        Convert.ToInt32(await command.ExecuteScalarAsync()).Should().Be(4);
    }

    [DockerAvailableFact]
    public async Task UniqueIndex_Should_EnforceCodePerTenant_AndAllowReuseAfterSoftDelete()
    {
        PostgreSqlCrmEnvironment environment = await PostgreSqlCrmEnvironment.GetAsync();
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        string code = NewCode();

        await AddCustomerAsync(environment, tenantA, code);
        await AddCustomerAsync(environment, tenantB, code);

        await using (CrmDbContext duplicate = environment.CreateContext(tenantA))
        {
            await duplicate.Customers.AddAsync(TestData.CreateCustomer(tenantId: tenantA, code: code));
            Func<Task> act = () => duplicate.SaveChangesAsync();

            PostgresException exception = await CatchPostgresExceptionAsync(act);
            exception.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
            exception.ConstraintName.Should().Be("IX_customers_TenantId_Code");
        }

        await using (CrmDbContext softDelete = environment.CreateContext(tenantA))
        {
            Customer existing = await softDelete.Customers.SingleAsync(customer => customer.Code == TestData.Code(code));
            softDelete.Customers.Remove(existing);
            await softDelete.SaveChangesAsync();
        }

        await using (CrmDbContext reuse = environment.CreateContext(tenantA))
        {
            await reuse.Customers.AddAsync(TestData.CreateCustomer(tenantId: tenantA, code: code));
            Func<Task> act = () => reuse.SaveChangesAsync();

            await act.Should().NotThrowAsync();
        }
    }

    [DockerAvailableFact]
    public async Task TenantFilters_Should_BeFailClosed_AndSystemScopeShouldSeeAllTenants()
    {
        PostgreSqlCrmEnvironment environment = await PostgreSqlCrmEnvironment.GetAsync();
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        string codeA = NewCode();
        string codeB = NewCode();

        await AddCustomerAsync(environment, tenantA, codeA);
        await AddCustomerAsync(environment, tenantB, codeB);

        await using (CrmDbContext scoped = environment.CreateContext(tenantA))
        {
            (await scoped.Customers.CountAsync(customer => customer.Code == TestData.Code(codeA))).Should().Be(1);
            (await scoped.Customers.CountAsync(customer => customer.Code == TestData.Code(codeB))).Should().Be(0);
        }

        await using (CrmDbContext missingTenant = environment.CreateContext(null))
        {
            (await missingTenant.Customers
                .CountAsync(customer => customer.Code == TestData.Code(codeA))).Should().Be(0);
        }

        await using (CrmDbContext system = environment.CreateContext(null, isSystem: true))
        {
            (await system.Customers.CountAsync(customer =>
                customer.Code == TestData.Code(codeA) || customer.Code == TestData.Code(codeB))).Should().Be(2);
        }
    }

    [DockerAvailableFact]
    public async Task CheckConstraint_Should_RejectTenantKeyMismatch()
    {
        PostgreSqlCrmEnvironment environment = await PostgreSqlCrmEnvironment.GetAsync();
        Guid tenantId = Guid.NewGuid();
        Guid wrongTenantKey = Guid.NewGuid();
        string code = NewCode();

        await using CrmDbContext context = environment.CreateContext(tenantId);

        Func<Task> act = () => context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO crm."customers"
                ("Id", "TenantId", "Code", "Name", "Status", "CreatedOnUtc", "IsDeleted", "TenantKey", "Version")
            VALUES ({0}, {1}, {2}, {3}, {4}, {5}, false, {6}, 1)
            """,
            Guid.NewGuid(),
            tenantId,
            code,
            "Constraint Customer",
            "Active",
            DateTimeOffset.UtcNow,
            wrongTenantKey);

        PostgresException exception = await CatchPostgresExceptionAsync(act);
        exception.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        exception.ConstraintName.Should().Be("CK_customers_tenant_key");
    }

    [DockerAvailableFact]
    public async Task Search_Should_BeCaseInsensitive_AndPartial()
    {
        PostgreSqlCrmEnvironment environment = await PostgreSqlCrmEnvironment.GetAsync();
        Guid tenantId = Guid.NewGuid();
        string code = NewCode();

        await using (CrmDbContext seed = environment.CreateContext(tenantId))
        {
            await seed.Customers.AddRangeAsync(
                TestData.CreateCustomer(tenantId: tenantId, code: code, name: "Acme Corporation"),
                TestData.CreateCustomer(tenantId: tenantId, code: NewCode(), name: "Globex"));
            await seed.SaveChangesAsync();
        }

        await using CrmDbContext context = environment.CreateContext(tenantId);
        var repository = new CustomerReadRepository(context);

        var byName = await repository.ListAsync(tenantId, 1, 20, "ACME");
        byName.Items.Should().ContainSingle(item => item.Code == code.ToUpperInvariant());

        var byCode = await repository.ListAsync(tenantId, 1, 20, code.ToLowerInvariant());
        byCode.Items.Should().ContainSingle(item => item.Code == code.ToUpperInvariant());
    }

    [DockerAvailableFact]
    public async Task SoftDelete_Should_RetainTheRow_AndExcludeItFromQueries()
    {
        PostgreSqlCrmEnvironment environment = await PostgreSqlCrmEnvironment.GetAsync();
        Guid tenantId = Guid.NewGuid();
        string code = NewCode();

        await using (CrmDbContext seed = environment.CreateContext(tenantId))
        {
            Customer customer = TestData.CreateCustomer(tenantId: tenantId, code: code);
            await seed.Customers.AddAsync(customer);
            await seed.SaveChangesAsync();
            seed.Customers.Remove(customer);
            await seed.SaveChangesAsync();
        }

        await using CrmDbContext context = environment.CreateContext(tenantId);
        (await context.Customers.CountAsync(customer => customer.Code == TestData.Code(code))).Should().Be(0);

        Customer deleted = await context.Customers
            .IgnoreQueryFilters()
            .SingleAsync(customer => customer.Code == TestData.Code(code));
        context.Entry(deleted).Property("IsDeleted").CurrentValue.Should().Be(true);
    }

    [DockerAvailableFact]
    public async Task Concurrency_Should_DetectAStaleUpdate_AndPreserveTheWinner()
    {
        PostgreSqlCrmEnvironment environment = await PostgreSqlCrmEnvironment.GetAsync();
        Guid tenantId = Guid.NewGuid();
        string code = NewCode();

        await AddCustomerAsync(environment, tenantId, code);

        await using CrmDbContext first = environment.CreateContext(tenantId);
        await using CrmDbContext second = environment.CreateContext(tenantId);

        Customer firstCopy = await first.Customers.SingleAsync(customer => customer.Code == TestData.Code(code));
        Customer secondCopy = await second.Customers.SingleAsync(customer => customer.Code == TestData.Code(code));

        firstCopy.ChangeName(TestData.Name("Postgres First")).IsSuccess.Should().BeTrue();
        await first.SaveChangesAsync();

        secondCopy.ChangeName(TestData.Name("Postgres Second")).IsSuccess.Should().BeTrue();
        var unitOfWork = new CrmUnitOfWork(second, new RecordingDomainEventDispatcher());

        Func<Task> act = () => unitOfWork.SaveChangesAsync();
        await act.Should().ThrowAsync<ConcurrencyConflictException>();

        second.ChangeTracker.Clear();
        Customer persisted = await second.Customers.SingleAsync(customer => customer.Code == TestData.Code(code));
        persisted.Name.Value.Should().Be("Postgres First");
    }

    private static async Task AddCustomerAsync(
        PostgreSqlCrmEnvironment environment,
        Guid tenantId,
        string code)
    {
        await using CrmDbContext context = environment.CreateContext(tenantId);
        await context.Customers.AddAsync(TestData.CreateCustomer(tenantId: tenantId, code: code));
        await context.SaveChangesAsync();
    }

    private static async Task<PostgresException> CatchPostgresExceptionAsync(Func<Task> action)
    {
        Exception exception = (await action.Should().ThrowAsync<Exception>()).Which;
        PostgresException? postgres = exception as PostgresException ?? exception.InnerException as PostgresException;

        postgres.Should().NotBeNull("the failure must be raised by PostgreSQL");
        return postgres!;
    }

    private static string NewCode() => $"PG-{Guid.NewGuid():N}"[..20];
}

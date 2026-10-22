using System.Data.Common;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.Opportunities;
using BSX.Modules.Sales.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Modules.Sales.Infrastructure.Persistence;
using BSX.Modules.Sales.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BSX.Modules.Sales.Infrastructure.IntegrationTests;

/// <summary>
/// Provider parity: validates the Sales schema, constraints, tenant filters, search, soft delete and
/// optimistic concurrency against a real PostgreSQL instance (ADR-004).
/// </summary>
public sealed class PostgreSqlParityTests
{
    [DockerAvailableFact]
    public async Task Migrations_Should_Apply_AndCreateTheSalesSchema()
    {
        PostgreSqlSalesEnvironment environment = await PostgreSqlSalesEnvironment.GetAsync();
        await using SalesDbContext context = environment.CreateContext(Guid.NewGuid());

        (await context.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
        (await context.Database.GetAppliedMigrationsAsync()).Should().Contain("20261022113527_InitialSales");

        DbConnection connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT count(*)
            FROM information_schema.tables
            WHERE table_schema = 'sales'
              AND table_name IN ('leads', 'opportunities')
            """;

        Convert.ToInt32(await command.ExecuteScalarAsync()).Should().Be(2);
    }

    [DockerAvailableFact]
    public async Task UniqueIndex_Should_EnforceOneOpportunityPerTenantAndLead_AndAllowReuseAfterSoftDelete()
    {
        PostgreSqlSalesEnvironment environment = await PostgreSqlSalesEnvironment.GetAsync();
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        Guid leadId = Guid.NewGuid();
        Guid customerId = Guid.NewGuid();
        Lead lead = TestData.CreateQualifiedLead(id: leadId, tenantId: tenantA, customerId: customerId);
        Opportunity first = TestData.CreateOpportunity(lead: lead, customerId: customerId);

        await using (SalesDbContext seed = environment.CreateContext(tenantA))
        {
            await seed.Leads.AddAsync(lead);
            await seed.Opportunities.AddAsync(first);
            await seed.SaveChangesAsync();
        }

        await using (SalesDbContext duplicate = environment.CreateContext(tenantA))
        {
            await duplicate.Opportunities.AddAsync(TestData.CreateOpportunity(lead: lead, customerId: customerId));
            Func<Task> act = () => duplicate.SaveChangesAsync();

            PostgresException exception = await CatchPostgresExceptionAsync(act);
            exception.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
            exception.ConstraintName.Should().Be("IX_opportunities_TenantId_LeadId");
        }

        Lead tenantBLead = TestData.CreateQualifiedLead(id: leadId, tenantId: tenantB, customerId: customerId);
        await using (SalesDbContext otherTenant = environment.CreateContext(tenantB))
        {
            await otherTenant.Opportunities.AddAsync(
                TestData.CreateOpportunity(lead: tenantBLead, customerId: customerId));
            Func<Task> act = () => otherTenant.SaveChangesAsync();

            await act.Should().NotThrowAsync();
        }

        await using (SalesDbContext softDelete = environment.CreateContext(tenantA))
        {
            Opportunity existing = await softDelete.Opportunities.SingleAsync(
                opportunity => opportunity.LeadId == lead.Id);
            softDelete.Opportunities.Remove(existing);
            await softDelete.SaveChangesAsync();
        }

        await using (SalesDbContext reuse = environment.CreateContext(tenantA))
        {
            await reuse.Opportunities.AddAsync(TestData.CreateOpportunity(lead: lead, customerId: customerId));
            Func<Task> act = () => reuse.SaveChangesAsync();

            await act.Should().NotThrowAsync();
        }
    }

    [DockerAvailableFact]
    public async Task TenantFilters_Should_BeFailClosed_AndSystemScopeShouldSeeAllTenants()
    {
        PostgreSqlSalesEnvironment environment = await PostgreSqlSalesEnvironment.GetAsync();
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        string titleA = NewTitle("Alpha");
        string titleB = NewTitle("Beta");

        await using (SalesDbContext seed = environment.CreateContext(null, isSystem: true))
        {
            await seed.Leads.AddRangeAsync(
                TestData.CreateLead(tenantId: tenantA, title: titleA),
                TestData.CreateLead(tenantId: tenantB, title: titleB));
            await seed.SaveChangesAsync();
        }

        await using (SalesDbContext scoped = environment.CreateContext(tenantA))
        {
            (await scoped.Leads.CountAsync(lead => lead.Title == TestData.LeadTitleOf(titleA))).Should().Be(1);
            (await scoped.Leads.CountAsync(lead => lead.Title == TestData.LeadTitleOf(titleB))).Should().Be(0);
        }

        await using (SalesDbContext missingTenant = environment.CreateContext(null))
        {
            (await missingTenant.Leads.CountAsync()).Should().Be(0);
        }

        await using (SalesDbContext system = environment.CreateContext(null, isSystem: true))
        {
            (await system.Leads.CountAsync(lead =>
                lead.Title == TestData.LeadTitleOf(titleA)
                || lead.Title == TestData.LeadTitleOf(titleB))).Should().Be(2);
        }
    }

    [DockerAvailableFact]
    public async Task CheckConstraint_Should_RejectTenantKeyMismatch()
    {
        PostgreSqlSalesEnvironment environment = await PostgreSqlSalesEnvironment.GetAsync();
        Guid tenantId = Guid.NewGuid();
        Guid wrongTenantKey = Guid.NewGuid();

        await using SalesDbContext context = environment.CreateContext(tenantId);

        Func<Task> act = () => context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO sales."leads"
                ("Id", "TenantId", "Title", "Status", "CreatedOnUtc", "IsDeleted", "TenantKey", "Version")
            VALUES ({0}, {1}, {2}, {3}, {4}, false, {5}, 1)
            """,
            Guid.NewGuid(),
            tenantId,
            NewTitle("Constraint"),
            "New",
            DateTimeOffset.UtcNow,
            wrongTenantKey);

        PostgresException exception = await CatchPostgresExceptionAsync(act);
        exception.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        exception.ConstraintName.Should().Be("CK_leads_tenant_key");
    }

    [DockerAvailableFact]
    public async Task Search_Should_MatchLeadTitle_AndOpportunityTitleOrCustomerId()
    {
        PostgreSqlSalesEnvironment environment = await PostgreSqlSalesEnvironment.GetAsync();
        Guid tenantId = Guid.NewGuid();
        Guid customerA = Guid.NewGuid();
        Guid customerB = Guid.NewGuid();
        string leadTitle = NewTitle("Acme lead");
        Lead lead = TestData.CreateLead(tenantId: tenantId, title: leadTitle);
        Opportunity first = TestData.CreateOpportunity(
            tenantId: tenantId,
            customerId: customerA,
            title: NewTitle("Acme renewal"));
        Opportunity second = TestData.CreateOpportunity(
            tenantId: tenantId,
            customerId: customerB,
            title: NewTitle("Globex expansion"));

        await using (SalesDbContext seed = environment.CreateContext(tenantId))
        {
            await seed.Leads.AddAsync(lead);
            await seed.Opportunities.AddRangeAsync(first, second);
            await seed.SaveChangesAsync();
        }

        await using SalesDbContext context = environment.CreateContext(tenantId);
        var leadRepository = new LeadReadRepository(context);
        var opportunityRepository = new OpportunityReadRepository(context);

        (await leadRepository.ListAsync(tenantId, 1, 20, "ACME")).Items.Should()
            .ContainSingle(item => item.Id == lead.Id.Value);
        (await opportunityRepository.ListAsync(tenantId, 1, 20, "acme")).Items.Should()
            .ContainSingle(item => item.Id == first.Id.Value);
        (await opportunityRepository.ListAsync(tenantId, 1, 20, customerB.ToString())).Items.Should()
            .ContainSingle(item => item.Id == second.Id.Value);
    }

    [DockerAvailableFact]
    public async Task SoftDelete_Should_RetainTheRows_AndExcludeThemFromQueries()
    {
        PostgreSqlSalesEnvironment environment = await PostgreSqlSalesEnvironment.GetAsync();
        Guid tenantId = Guid.NewGuid();
        Lead lead = TestData.CreateLead(tenantId: tenantId, title: NewTitle("Deleted lead"));
        Opportunity opportunity = TestData.CreateOpportunity(tenantId: tenantId, title: NewTitle("Deleted opportunity"));

        await using (SalesDbContext seed = environment.CreateContext(tenantId))
        {
            await seed.Leads.AddAsync(lead);
            await seed.Opportunities.AddAsync(opportunity);
            await seed.SaveChangesAsync();
            seed.Leads.Remove(lead);
            seed.Opportunities.Remove(opportunity);
            await seed.SaveChangesAsync();
        }

        await using SalesDbContext context = environment.CreateContext(tenantId);
        (await context.Leads.CountAsync()).Should().Be(0);
        (await context.Opportunities.CountAsync()).Should().Be(0);
        (await context.Leads.IgnoreQueryFilters().CountAsync(entry => entry.Id == lead.Id)).Should().Be(1);
        (await context.Opportunities.IgnoreQueryFilters().CountAsync(entry => entry.Id == opportunity.Id)).Should().Be(1);
    }

    [DockerAvailableFact]
    public async Task Concurrency_Should_DetectAStaleUpdate_AndPreserveTheWinner()
    {
        PostgreSqlSalesEnvironment environment = await PostgreSqlSalesEnvironment.GetAsync();
        Guid tenantId = Guid.NewGuid();
        Lead lead = TestData.CreateLead(tenantId: tenantId, title: NewTitle("Concurrent lead"));

        await using (SalesDbContext seed = environment.CreateContext(tenantId))
        {
            await seed.Leads.AddAsync(lead);
            await seed.SaveChangesAsync();
        }

        await using SalesDbContext first = environment.CreateContext(tenantId);
        await using SalesDbContext second = environment.CreateContext(tenantId);

        Lead firstCopy = await first.Leads.SingleAsync(entry => entry.Id == lead.Id);
        Lead secondCopy = await second.Leads.SingleAsync(entry => entry.Id == lead.Id);

        firstCopy.ChangeNotes("Postgres First").IsSuccess.Should().BeTrue();
        await first.SaveChangesAsync();

        secondCopy.ChangeNotes("Postgres Second").IsSuccess.Should().BeTrue();
        var unitOfWork = new SalesUnitOfWork(second, new RecordingDomainEventDispatcher());

        Func<Task> act = () => unitOfWork.SaveChangesAsync();
        await act.Should().ThrowAsync<ConcurrencyConflictException>();

        second.ChangeTracker.Clear();
        Lead persisted = await second.Leads.SingleAsync(entry => entry.Id == lead.Id);
        persisted.Notes.Should().Be("Postgres First");
    }

    private static string NewTitle(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..30];

    private static async Task<PostgresException> CatchPostgresExceptionAsync(Func<Task> action)
    {
        Exception exception = (await action.Should().ThrowAsync<Exception>()).Which;
        PostgresException? postgres = exception as PostgresException ?? exception.InnerException as PostgresException;

        postgres.Should().NotBeNull("the failure must be raised by PostgreSQL");
        return postgres!;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BSX.Modules.Crm.Infrastructure.Persistence;

/// <summary>Design-time factory used by the EF Core tools to create migrations.</summary>
public sealed class CrmDbContextFactory : IDesignTimeDbContextFactory<CrmDbContext>
{
    /// <inheritdoc />
    public CrmDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=bsx_crm;Username=postgres;Password=postgres")
            .Options;

        return new CrmDbContext(options);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BSX.Modules.Identity.Infrastructure.Persistence;

/// <summary>Design-time factory used by the EF Core tools to create migrations.</summary>
public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    /// <inheritdoc />
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=bsx_identity;Username=postgres;Password=postgres")
            .Options;

        return new IdentityDbContext(options);
    }
}

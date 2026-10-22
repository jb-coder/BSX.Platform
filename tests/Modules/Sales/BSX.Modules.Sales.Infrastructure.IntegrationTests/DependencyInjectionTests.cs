using BSX.BuildingBlocks.DependencyInjection;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Sales.Application.DependencyInjection;
using BSX.Modules.Sales.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace BSX.Modules.Sales.Infrastructure.IntegrationTests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void Should_RegisterSalesAsModuleUnitOfWork_AndCompositeAsTheSingleUnitOfWork()
    {
        var services = new ServiceCollection();
        services.AddBuildingBlocks();
        services.AddSalesApplication();
        services.AddSalesInfrastructure(
            "Host=localhost;Port=5432;Database=bsx_sales;Username=postgres;Password=postgres");
        services.AddCompositeUnitOfWork();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        IModuleUnitOfWork[] modules = scope.ServiceProvider.GetServices<IModuleUnitOfWork>().ToArray();
        modules.Should().ContainSingle(module => module.ModuleName == "Sales");

        IUnitOfWork[] unitsOfWork = scope.ServiceProvider.GetServices<IUnitOfWork>().ToArray();
        unitsOfWork.Should().ContainSingle();
        unitsOfWork[0].Should().BeOfType<CompositeUnitOfWork>();
    }
}

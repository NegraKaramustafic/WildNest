using eCommerce.Common.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace eCommerce.Services.Database;

/// <summary>
/// Design-time factory so EF tools create the context without running the API host.
/// </summary>
public class ECommerceDbContextFactory : IDesignTimeDbContextFactory<ECommerceDbContext>
{
    public ECommerceDbContext CreateDbContext(string[] args)
    {
        EnvFileLoader.Load();
        var connectionString = EnvVariables.Require("ConnectionStrings__DefaultConnection");
        var options = new DbContextOptionsBuilder<ECommerceDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new ECommerceDbContext(options);
    }
}

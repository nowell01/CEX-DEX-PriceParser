using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CEX_DEX_Parser.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddUserSecrets<DesignTimeDbContextFactory>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            var cs = config.GetConnectionString("Db")
                     ?? throw new InvalidOperationException(
                         "ConnectionStrings:Db not set. Run: dotnet user-secrets set \"ConnectionStrings:Db\" \"...\"");

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(cs)
                .UseSnakeCaseNamingConvention()
                .Options;

            return new AppDbContext(options);
        }
    }
}
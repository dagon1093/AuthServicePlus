using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;

namespace AuthServicePlus.Persistence.Context
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var apiProjectPath = Path.GetFullPath(
                Path.Combine(Directory.GetCurrentDirectory(), "..", "AuthServicePlus.Api"));

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                             ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                             ?? "Development";

            // диагностика
             Console.WriteLine($"API path: {apiProjectPath}");
             Console.WriteLine(File.Exists(Path.Combine(apiProjectPath, "appsettings.json")));

            var config = new ConfigurationBuilder()
                .SetBasePath(apiProjectPath) // КЛЮЧЕВОЕ!
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
                .Build();


            var cs = config.GetConnectionString("DefaultConnection")
                     ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in appsettings.");

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(cs)
                .Options;


            return new ApplicationDbContext(options);
        }
    }
}

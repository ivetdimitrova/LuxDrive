using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore; 
using LuxDrive.Data; 
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace LuxDrive.Services
{
    public class FileCleanupService : BackgroundService
    {
        private readonly IServiceProvider _services;

        public FileCleanupService(IServiceProvider services)
        {
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<LuxDriveDbContext>();
                    var spacesService = scope.ServiceProvider.GetRequiredService<SpacesService>();

                    var threshold = DateTime.UtcNow.AddDays(-30);

                    var expiredFiles = await dbContext.Files
                        .Where(f => f.IsDeleted && f.DeletedOn < threshold)
                        .ToListAsync();

                    foreach (var file in expiredFiles)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(file.StorageUrl))
                            {
                                var endpoint = "https://luxdrive.ams3.digitaloceanspaces.com/";
                                var key = file.StorageUrl.Replace(endpoint, string.Empty);
                                await spacesService.DeleteAsync(key);
                            }

                            dbContext.Files.Remove(file);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error cleaning up file {file.Id}: {ex.Message}");
                        }
                    }

                    await dbContext.SaveChangesAsync();
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
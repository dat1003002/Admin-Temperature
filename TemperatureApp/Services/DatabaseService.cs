// File: Services/DatabaseService.cs
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using TemperatureApp.Data;

namespace TemperatureApp.Services
{
    public class DatabaseService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(ApplicationDbContext context, ILogger<DatabaseService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> CanConnectAsync()
        {
            try
            {
                bool canConnect = await _context.Database.CanConnectAsync();
                if (canConnect)
                {
                    _logger.LogInformation("Kết Nối SQL Server thành công!");
                }
                else
                {
                    _logger.LogWarning("Không kết nối đến SQL Server.");
                }
                return canConnect;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi kết nối SQL Server: {ex.Message}");
                return false;
            }
        }
    }
}

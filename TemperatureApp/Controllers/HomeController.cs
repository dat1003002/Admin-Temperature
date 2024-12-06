// File: Controllers/HomeController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TemperatureApp.Services;
using TemperatureApp.Models;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using TemperatureApp.Data;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace TemperatureApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DatabaseService _databaseService;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, DatabaseService databaseService, ApplicationDbContext context)
        {
            _logger = logger;
            _databaseService = databaseService;
            _context = context;
        }
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                bool canConnect = await _databaseService.CanConnectAsync();
                if (!canConnect)
                {
                    _logger.LogWarning("Không thể kết nối đến SQL Server.");
                }

                IQueryable<MS_1> query = _context.MS_1.AsQueryable();

                if (startDate.HasValue && endDate.HasValue)
                {
                    DateTime adjustedEndDate = endDate.Value.AddSeconds(59).AddMilliseconds(999);
                    query = query.Where(x => x.MS1_Time >= startDate && x.MS1_Time <= adjustedEndDate);
                }

                // Lấy dữ liệu, giới hạn 50 bản ghi nếu không có bộ lọc ngày
                var data = await (startDate.HasValue && endDate.HasValue
                    ? query.OrderByDescending(x => x.MS1_Time).ToListAsync()
                    : query.OrderByDescending(x => x.MS1_Time).Take(50).ToListAsync());

                data.Reverse(); // Đảo ngược danh sách để hiển thị thời gian tăng dần

                var room01Temp = data.Select(x => x.MS1_Temp / 10.0).ToList();
                var room01TempH = data.Select(x => x.MS1_TempH / 10.0).ToList();
                var room01TempL = data.Select(x => x.MS1_TempL / 10.0).ToList();
                var room01Setup = data.Select(x => x.MS1_Setup / 10.0).ToList();
                var labels = data.Select(x => x.MS1_Time.ToString("yyyy-MM-dd HH:mm")).ToList();

                ViewData["Room01_Temp"] = room01Temp;
                ViewData["Room01_TempH"] = room01TempH;
                ViewData["Room01_TempL"] = room01TempL;
                ViewData["Room01_Setup"] = room01Setup;
                ViewData["Labels"] = labels;

                ViewData["StartDate"] = startDate?.ToString("yyyy-MM-ddTHH:mm");
                ViewData["EndDate"] = endDate?.ToString("yyyy-MM-ddTHH:mm");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi: {ex.Message}");
            }

            return View();
        }

        public async Task<IActionResult> ExportToExcel(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                if (!startDate.HasValue || !endDate.HasValue)
                {
                    return BadRequest("Ngày bắt đầu và ngày kết thúc là bắt buộc.");
                }

                IQueryable<MS_1> query = _context.MS_1.AsQueryable();

                // Điều chỉnh điều kiện lọc nếu cần thiết
                query = query.Where(x => x.MS1_Time >= startDate && x.MS1_Time <= endDate);

                var data = await query.OrderByDescending(x => x.MS1_Time).ToListAsync();

                if (!data.Any())
                {
                    return BadRequest("Không có dữ liệu trong khoảng thời gian đã chọn.");
                }

                // Đặt LicenseContext cho EPPlus
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Dữ liệu");

                    // Tiêu đề cột
                    worksheet.Cells[1, 1].Value = "Thời gian";
                    worksheet.Cells[1, 2].Value = "Nhiệt độ hiện tại";
                    worksheet.Cells[1, 3].Value = "Nhiệt độ cao";
                    worksheet.Cells[1, 4].Value = "Nhiệt độ thấp";
                    worksheet.Cells[1, 5].Value = "Nhiệt độ cài đặt";

                    // Đổ dữ liệu
                    for (int i = 0; i < data.Count; i++)
                    {
                        var record = data[i];
                        worksheet.Cells[i + 2, 1].Value = record.MS1_Time.ToString("yyyy-MM-dd HH:mm");
                        worksheet.Cells[i + 2, 2].Value = record.MS1_Temp / 10.0;
                        worksheet.Cells[i + 2, 3].Value = record.MS1_TempH / 10.0;
                        worksheet.Cells[i + 2, 4].Value = record.MS1_TempL / 10.0;
                        worksheet.Cells[i + 2, 5].Value = record.MS1_Setup / 10.0;
                    }

                    // Định dạng Excel
                    worksheet.Cells[1, 1, 1, 5].Style.Font.Bold = true; // In đậm tiêu đề cột
                    worksheet.Cells.AutoFitColumns(); // Tự động điều chỉnh độ rộng cột

                    // Trả file về client
                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    string excelName = $"Data_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ExportToExcel Error: {ex}");
                return BadRequest($"Lỗi khi xuất Excel: {ex.Message}");
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

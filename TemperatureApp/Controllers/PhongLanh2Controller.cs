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
   
    public class PhongLanh2Controller : Controller
    {
        private readonly ILogger<PhongLanh2Controller> _logger;
        private readonly DatabaseService _databaseService;
        private readonly ApplicationDbContext _context;
        public PhongLanh2Controller(ILogger<PhongLanh2Controller> logger, DatabaseService databaseService, ApplicationDbContext context)
        {
            _logger = logger;
            _databaseService = databaseService;
            _context = context;
        }
        public async Task<IActionResult> IndexAsync(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                if (!await _databaseService.CanConnectAsync())
                {
                    _logger.LogWarning("Không thể kết nối đến SQL Server.");
                    return View(); // Hiển thị view trống nếu không kết nối được
                }

                // Tạo câu truy vấn cơ sở
                var query = _context.MS_3.AsQueryable();

                // Lọc dữ liệu theo ngày nếu có
                if (startDate.HasValue && endDate.HasValue)
                {
                    // Điều chỉnh endDate để bao phủ chính xác khoảng thời gian
                    DateTime adjustedEndDate = endDate.Value.AddSeconds(59).AddMilliseconds(999);
                    query = query.Where(x => x.MS3_Time >= startDate && x.MS3_Time <= adjustedEndDate);
                }

                // Lấy dữ liệu, giới hạn 50 bản ghi nếu không lọc theo ngày
                var data = await(startDate.HasValue && endDate.HasValue
                    ? query.OrderByDescending(x => x.MS3_Time).ToListAsync()
                    : query.OrderByDescending(x => x.MS3_Time).Take(50).ToListAsync());

                data.Reverse(); // Đảo ngược danh sách để hiển thị thời gian tăng dần

                // Trích xuất dữ liệu để hiển thị
                var room03Temp = data.Select(x => x.MS3_Temp / 10.0).ToList();
                var room03TempH = data.Select(x => x.MS3_TempH / 10.0).ToList();
                var room03TempL = data.Select(x => x.MS3_TempL / 10.0).ToList();
                var room03Setup = data.Select(x => x.MS3_Setup / 10.0).ToList();
                var labels = data.Select(x => x.MS3_Time.ToString("yyyy-MM-dd HH:mm")).ToList();

                // Truyền dữ liệu qua ViewData
                ViewData["Room03_Temp"] = room03Temp;
                ViewData["Room03_TempH"] = room03TempH;
                ViewData["Room03_TempL"] = room03TempL;
                ViewData["Room03_Setup"] = room03Setup;
                ViewData["Labels"] = labels;
                ViewData["StartDate"] = startDate?.ToString("yyyy-MM-ddTHH:mm");
                ViewData["EndDate"] = endDate?.ToString("yyyy-MM-ddTHH:mm");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi xử lý Room3: {ex.Message}");
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

                var query = _context.MS_3.AsQueryable();
                query = query.Where(x => x.MS3_Time >= startDate && x.MS3_Time <= endDate);

                var data = await query.OrderByDescending(x => x.MS3_Time).ToListAsync();

                if (!data.Any())
                {
                    return BadRequest("Không có dữ liệu trong khoảng thời gian đã chọn.");
                }

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
                        worksheet.Cells[i + 2, 1].Value = record.MS3_Time.ToString("yyyy-MM-dd HH:mm");
                        worksheet.Cells[i + 2, 2].Value = record.MS3_Temp / 10.0;
                        worksheet.Cells[i + 2, 3].Value = record.MS3_TempH / 10.0;
                        worksheet.Cells[i + 2, 4].Value = record.MS3_TempL / 10.0;
                        worksheet.Cells[i + 2, 5].Value = record.MS3_Setup / 10.0;
                    }

                    worksheet.Cells[1, 1, 1, 5].Style.Font.Bold = true;
                    worksheet.Cells.AutoFitColumns();

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

    }
}

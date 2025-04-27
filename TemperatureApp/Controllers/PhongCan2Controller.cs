using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TemperatureApp.Services;
using TemperatureApp.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.IO;
using TemperatureApp.Data;

namespace TemperatureApp.Controllers
{
    public class PhongCan2Controller : Controller
    {
        private readonly ILogger<PhongCan2Controller> _logger;
        private readonly DatabaseService _databaseService;
        private readonly ApplicationDbContext _context;

        public PhongCan2Controller(ILogger<PhongCan2Controller> logger, DatabaseService databaseService, ApplicationDbContext context)
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

                IQueryable<MS_4> query = _context.MS_4.AsQueryable();

                if (startDate.HasValue && endDate.HasValue)
                {
                    DateTime adjustedEndDate = endDate.Value.AddSeconds(59).AddMilliseconds(999);
                    query = query.Where(x => x.MS4_Time >= startDate && x.MS4_Time <= adjustedEndDate);
                }

                // Lấy dữ liệu, giới hạn 50 bản ghi nếu không có bộ lọc ngày
                var data = await (startDate.HasValue && endDate.HasValue
                    ? query.OrderByDescending(x => x.MS4_Time).ToListAsync()
                    : query.OrderByDescending(x => x.MS4_Time).Take(50).ToListAsync());

                data.Reverse(); // Đảo ngược danh sách để hiển thị thời gian tăng dần

                var room04Temp = data.Select(x => x.MS4_Temp / 10.0).ToList();
                var room04TempH = data.Select(x => x.MS4_TempH / 10.0).ToList();
                var room04TempL = data.Select(x => x.MS4_TempL / 10.0).ToList();
                var room04Setup = data.Select(x => x.MS4_Setup / 10.0).ToList();
                var labels = data.Select(x => x.MS4_Time.ToString("yyyy-MM-dd HH:mm")).ToList();

                ViewData["Room04_Temp"] = room04Temp;
                ViewData["Room04_TempH"] = room04TempH;
                ViewData["Room04_TempL"] = room04TempL;
                ViewData["Room04_Setup"] = room04Setup;
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

                IQueryable<MS_4> query = _context.MS_4.AsQueryable();

                query = query.Where(x => x.MS4_Time >= startDate && x.MS4_Time <= endDate);

                var data = await query.OrderByDescending(x => x.MS4_Time).ToListAsync();

                if (!data.Any())
                {
                    return BadRequest("Không có dữ liệu trong khoảng thời gian đã chọn.");
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Dữ liệu Phòng Cân 2");

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
                        worksheet.Cells[i + 2, 1].Value = record.MS4_Time.ToString("yyyy-MM-dd HH:mm");
                        worksheet.Cells[i + 2, 2].Value = record.MS4_Temp / 10.0;
                        worksheet.Cells[i + 2, 3].Value = record.MS4_TempH / 10.0;
                        worksheet.Cells[i + 2, 4].Value = record.MS4_TempL / 10.0;
                        worksheet.Cells[i + 2, 5].Value = record.MS4_Setup / 10.0;
                    }

                    // Định dạng Excel
                    worksheet.Cells[1, 1, 1, 5].Style.Font.Bold = true;
                    worksheet.Cells.AutoFitColumns();

                    // Trả file về client
                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    string excelName = $"Data_PhongCan2_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
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
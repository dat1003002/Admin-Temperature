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
    public class KhoHoaChatController : Controller
    {
        private readonly ILogger<KhoHoaChatController> _logger;
        private readonly DatabaseService _databaseService;
        private readonly ApplicationDbContext _context;

        public KhoHoaChatController(ILogger<KhoHoaChatController> logger, DatabaseService databaseService, ApplicationDbContext context)
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
                    TempData["Warning"] = "Không thể kết nối đến SQL Server.";
                    _logger.LogWarning("Không thể kết nối đến SQL Server.");
                }

                IQueryable<MS_5> query = _context.MS_5.AsQueryable();

                if (startDate.HasValue && endDate.HasValue)
                {
                    DateTime adjustedEndDate = endDate.Value.AddSeconds(59).AddMilliseconds(999);
                    query = query.Where(x => x.MS5_Time >= startDate && x.MS5_Time <= adjustedEndDate);
                }

                var data = await (startDate.HasValue && endDate.HasValue
                    ? query.OrderByDescending(x => x.MS5_Time).ToListAsync()
                    : query.OrderByDescending(x => x.MS5_Time).Take(50).ToListAsync());

                if (!data.Any())
                {
                    TempData["Warning"] = "Không có dữ liệu trong khoảng thời gian đã chọn.";
                }

                data.Reverse();

                var roomTemp = data.Select(x => x.MS5_Temp / 10.0).ToList();
                var roomTempH = data.Select(x => x.MS5_TempH / 10.0).ToList();
                var roomTempL = data.Select(x => x.MS5_TempL / 10.0).ToList();
                var roomSetup = data.Select(x => x.MS5_Setup / 10.0).ToList();
                var labels = data.Select(x => x.MS5_Time.ToString("yyyy-MM-dd HH:mm")).ToList();

                ViewData["MS5_Temp"] = roomTemp;
                ViewData["MS5_TempH"] = roomTempH;
                ViewData["MS5_TempL"] = roomTempL;
                ViewData["MS5_Setup"] = roomSetup;
                ViewData["Labels"] = labels;

                ViewData["StartDate"] = startDate?.ToString("yyyy-MM-ddTHH:mm");
                ViewData["EndDate"] = endDate?.ToString("yyyy-MM-ddTHH:mm");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi tải dữ liệu: {ex.Message}";
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

                IQueryable<MS_5> query = _context.MS_5.AsQueryable();

                query = query.Where(x => x.MS5_Time >= startDate && x.MS5_Time <= endDate);

                var data = await query.OrderByDescending(x => x.MS5_Time).ToListAsync();

                if (!data.Any())
                {
                    return BadRequest("Không có dữ liệu trong khoảng thời gian đã chọn.");
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Dữ liệu Kho Hóa Chất (DRB2)");

                    worksheet.Cells[1, 1].Value = "Thời gian";
                    worksheet.Cells[1, 2].Value = "Nhiệt độ hiện tại";
                    worksheet.Cells[1, 3].Value = "Nhiệt độ cao";
                    worksheet.Cells[1, 4].Value = "Nhiệt độ thấp";
                    worksheet.Cells[1, 5].Value = "Nhiệt độ cài đặt";

                    for (int i = 0; i < data.Count; i++)
                    {
                        var record = data[i];
                        worksheet.Cells[i + 2, 1].Value = record.MS5_Time.ToString("yyyy-MM-dd HH:mm");
                        worksheet.Cells[i + 2, 2].Value = record.MS5_Temp / 10.0;
                        worksheet.Cells[i + 2, 3].Value = record.MS5_TempH / 10.0;
                        worksheet.Cells[i + 2, 4].Value = record.MS5_TempL / 10.0;
                        worksheet.Cells[i + 2, 5].Value = record.MS5_Setup / 10.0;
                    }

                    worksheet.Cells[1, 1, 1, 5].Style.Font.Bold = true;
                    worksheet.Cells.AutoFitColumns();

                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    string excelName = $"KhoHoaChat_Data_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
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
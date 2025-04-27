using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TemperatureApp.Services;
using TemperatureApp.Models;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.IO;
using TemperatureApp.Data;

namespace TemperatureApp.Controllers
{
    public class Lohoi5Controller : Controller
    {
        private readonly ILogger<Lohoi5Controller> _logger;
        private readonly DatabaseService _databaseService;
        private readonly ApplicationDbContext _context;

        public Lohoi5Controller(ILogger<Lohoi5Controller> logger, DatabaseService databaseService, ApplicationDbContext context)
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
                    TempData["Error"] = "Không thể kết nối đến cơ sở dữ liệu. Vui lòng thử lại sau.";
                    return View();
                }

                var query = _context.BTLH5.AsQueryable();

                if (startDate.HasValue && endDate.HasValue)
                {
                    if (startDate > endDate)
                    {
                        TempData["Error"] = "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc.";
                        return View();
                    }

                    DateTime adjustedEndDate = endDate.Value.AddSeconds(59).AddMilliseconds(999);
                    query = query.Where(x => x.TIME >= startDate && x.TIME <= adjustedEndDate);
                }

                var data = await (startDate.HasValue && endDate.HasValue
                    ? query.OrderByDescending(x => x.TIME).ToListAsync()
                    : query.OrderByDescending(x => x.TIME).Take(50).ToListAsync());

                data.Reverse(); // Đảo ngược để hiển thị theo thứ tự thời gian tăng dần

                if (!data.Any())
                {
                    TempData["Warning"] = "Không tìm thấy dữ liệu trong khoảng thời gian đã chọn.";
                }

                // Chia APTC, APTT và MUCNUOC cho 10
                ViewData["APTC"] = data.Select(x => x.APTC / 10.0).ToList();
                ViewData["APTT"] = data.Select(x => x.APTT / 10.0).ToList();
                ViewData["MUCNUOC"] = data.Select(x => x.MUCNUOC / 10.0).ToList();
                ViewData["TRANGTHAI"] = data.Select(x => x.TRANGTHAI ? 1 : 0).ToList();
                ViewData["Labels"] = data.Select(x => x.TIME.ToString("yyyy-MM-dd HH:mm:ss")).ToList();
                ViewData["StartDate"] = startDate?.ToString("yyyy-MM-ddTHH:mm");
                ViewData["EndDate"] = endDate?.ToString("yyyy-MM-ddTHH:mm");

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi xử lý Lohoi5: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải dữ liệu. Vui lòng thử lại.";
                return View();
            }
        }

        public async Task<IActionResult> ExportToExcel(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                if (!startDate.HasValue || !endDate.HasValue)
                {
                    return BadRequest("Vui lòng chọn ngày bắt đầu và ngày kết thúc.");
                }

                if (startDate > endDate)
                {
                    return BadRequest("Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc.");
                }

                DateTime adjustedEndDate = endDate.Value.AddSeconds(59).AddMilliseconds(999);
                var data = await _context.BTLH5
                    .Where(x => x.TIME >= startDate && x.TIME <= adjustedEndDate)
                    .OrderBy(x => x.TIME)
                    .ToListAsync();

                if (!data.Any())
                {
                    return BadRequest("Không tìm thấy dữ liệu trong khoảng thời gian đã chọn.");
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Dữ liệu Lò Hơi 5");

                    var headers = new[] { "Thời gian", "Áp lực tiêu chuẩn", "Áp lực thực tế", "Mực nước", "Trạng thái" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cells[1, i + 1].Value = headers[i];
                        worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    }

                    for (int i = 0; i < data.Count; i++)
                    {
                        var item = data[i];
                        worksheet.Cells[i + 2, 1].Value = item.TIME.ToString("yyyy-MM-dd HH:mm:ss");
                        worksheet.Cells[i + 2, 2].Value = item.APTC / 10.0; // Chia cho 10
                        worksheet.Cells[i + 2, 3].Value = item.APTT / 10.0; // Chia cho 10
                        worksheet.Cells[i + 2, 4].Value = item.MUCNUOC / 10.0; // Chia cho 10
                        worksheet.Cells[i + 2, 5].Value = item.TRANGTHAI ? "Bật" : "Tắt";
                    }

                    worksheet.Cells.AutoFitColumns();

                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    string fileName = $"LoHoi5_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi xuất Excel: {ex.Message}");
                return StatusCode(500, "Có lỗi xảy ra khi xuất Excel.");
            }
        }
    }
}
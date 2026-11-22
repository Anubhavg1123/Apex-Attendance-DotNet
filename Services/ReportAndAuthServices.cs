using EmployeeAttendanceApp.Data;
using EmployeeAttendanceApp.Models;
using EmployeeAttendanceApp.Models.ViewModels;

namespace EmployeeAttendanceApp.Services;

public interface IReportService
{
    Task<AttendanceReportViewModel> GenerateReportAsync(DateTime fromDate, DateTime toDate, string? department, string? status, string? search);
    Task<byte[]> ExportCsvAsync(DateTime fromDate, DateTime toDate, string? department, string? status, string? search);
}

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _db;

    public ReportService(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<AttendanceReportViewModel> GenerateReportAsync(DateTime fromDate, DateTime toDate, string? department, string? status, string? search)
    {
        var from = fromDate.Date;
        var to = toDate.Date.AddDays(1).AddTicks(-1);

        var query = _db.AttendanceRecords
            .Where(r => r.Date >= from && r.Date <= to)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(department) && department != "All")
        {
            query = query.Where(r => r.Department.Equals(department, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            query = query.Where(r => r.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(r => 
                r.EmployeeName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                r.EmployeeCode.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        var records = query.OrderByDescending(r => r.PunchInTime).ToList();
        var totalHours = records.Sum(r => r.TotalWorkHours);
        var avgHours = records.Count > 0 ? Math.Round(totalHours / records.Count, 2) : 0.0;

        var model = new AttendanceReportViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            Department = department,
            Status = status,
            SearchTerm = search,
            Records = records,
            Departments = _db.Departments.OrderBy(d => d.Name).ToList(),
            TotalPunches = records.Count,
            TotalHoursWorked = Math.Round(totalHours, 2),
            AverageHoursPerDay = avgHours
        };

        return Task.FromResult(model);
    }

    public Task<byte[]> ExportCsvAsync(DateTime fromDate, DateTime toDate, string? department, string? status, string? search)
    {
        var report = GenerateReportAsync(fromDate, toDate, department, status, search).Result;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Record ID,Date,Employee Code,Employee Name,Department,Punch In,Punch Out,Total Hours,Status,Match Confidence,Verification Method,Device");

        foreach (var r in report.Records)
        {
            var inTime = r.PunchInTime.ToString("yyyy-MM-dd HH:mm:ss");
            var outTime = r.PunchOutTime.HasValue ? r.PunchOutTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Active Shift";
            sb.AppendLine($"\"{r.Id}\",\"{r.Date:yyyy-MM-dd}\",\"{r.EmployeeCode}\",\"{r.EmployeeName}\",\"{r.Department}\",\"{inTime}\",\"{outTime}\",\"{r.TotalWorkHours:F2}\",\"{r.Status}\",\"{r.MatchConfidence:F1}%\",\"{r.VerificationMethod}\",\"{r.DeviceLocation}\"");
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return Task.FromResult(bytes);
    }
}

public interface IAuthService
{
    Task<UserAccount?> AuthenticateAsync(string username, string password);
    Task<bool> ChangePasswordAsync(string username, string currentPassword, string newPassword);
}

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;

    public AuthService(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<UserAccount?> AuthenticateAsync(string username, string password)
    {
        var user = _db.Users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        if (user == null) return Task.FromResult<UserAccount?>(null);

        if (DbInitializer.VerifyPassword(password, user.PasswordHash))
        {
            return Task.FromResult<UserAccount?>(user);
        }

        return Task.FromResult<UserAccount?>(null);
    }

    public Task<bool> ChangePasswordAsync(string username, string currentPassword, string newPassword)
    {
        var user = _db.Users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        if (user == null) return Task.FromResult(false);

        if (!DbInitializer.VerifyPassword(currentPassword, user.PasswordHash))
        {
            return Task.FromResult(false);
        }

        user.PasswordHash = DbInitializer.HashPassword(newPassword);
        _db.SaveChanges();
        return Task.FromResult(true);
    }
}

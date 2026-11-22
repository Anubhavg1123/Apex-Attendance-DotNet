using EmployeeAttendanceApp.Data;
using EmployeeAttendanceApp.Models;
using EmployeeAttendanceApp.Models.ViewModels;

namespace EmployeeAttendanceApp.Services;

public interface IAttendanceService
{
    Task<PunchResultModel> ProcessPunchAsync(PunchRequestModel request);
    Task<DashboardViewModel> GetDashboardMetricsAsync();
    Task<List<AttendanceRecord>> GetTodayAttendanceAsync();
    Task<List<AttendanceRecord>> GetRecentPunchesAsync(int count = 10);
    Task<AttendanceRecord?> GetTodayRecordForEmployeeAsync(int employeeId);
}

public class AttendanceService : IAttendanceService
{
    private readonly ApplicationDbContext _db;

    public AttendanceService(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<PunchResultModel> ProcessPunchAsync(PunchRequestModel request)
    {
        // 1. Resolve employee
        Employee? employee = null;
        if (request.EmployeeId.HasValue && request.EmployeeId.Value > 0)
        {
            employee = _db.Employees.FirstOrDefault(e => e.Id == request.EmployeeId.Value);
        }
        else if (!string.IsNullOrEmpty(request.EmployeeCode))
        {
            employee = _db.Employees.FirstOrDefault(e => e.EmployeeCode.Equals(request.EmployeeCode, StringComparison.OrdinalIgnoreCase));
        }

        if (employee == null)
        {
            return Task.FromResult(new PunchResultModel
            {
                Success = false,
                Message = "Employee identification not found or biometric profile not matched."
            });
        }

        if (!employee.IsActive)
        {
            return Task.FromResult(new PunchResultModel
            {
                Success = false,
                Message = $"Employee {employee.FullName} is currently inactive in the directory."
            });
        }

        var policy = _db.Policy ?? new AttendancePolicy();
        var requiredConfidence = policy.RequiredMatchConfidence;

        // If confidence is provided and below threshold, reject
        if (request.MatchConfidence > 0 && request.MatchConfidence < requiredConfidence)
        {
            return Task.FromResult(new PunchResultModel
            {
                Success = false,
                Message = $"Biometric face confidence ({request.MatchConfidence:F1}%) is below strict threshold ({requiredConfidence}%). Please align clearly in the viewfinder."
            });
        }

        var now = DateTime.Now;
        var today = DateTime.Today;

        // Check if there is an existing record for today
        var existingRecord = _db.AttendanceRecords
            .FirstOrDefault(r => r.EmployeeId == employee.Id && r.Date.Date == today);

        if (existingRecord == null)
        {
            // First punch of the day: Check-in (PUNCH IN)
            // Calculate status based on policy
            var status = "Present";
            if (TimeSpan.TryParse(policy.ShiftStartTime, out var shiftStart))
            {
                var graceLimit = shiftStart.Add(TimeSpan.FromMinutes(policy.LateGraceMinutes));
                if (now.TimeOfDay > graceLimit)
                {
                    status = "Late";
                }
            }

            var newRecord = new AttendanceRecord
            {
                Id = _db.GetNextAttendanceId(),
                EmployeeId = employee.Id,
                EmployeeCode = employee.EmployeeCode,
                EmployeeName = employee.FullName,
                Department = employee.Department,
                Date = today,
                PunchInTime = now,
                PunchOutTime = null,
                TotalWorkHours = 0,
                Status = status,
                MatchConfidence = request.MatchConfidence > 0 ? request.MatchConfidence : 96.5,
                VerificationMethod = "Biometric Face Scan",
                PunchInPhoto = request.PhotoDataUri,
                DeviceLocation = request.DeviceLocation,
                Notes = status == "Late" ? $"Arrived at {now:HH:mm} (Grace threshold exceeded)" : "On-time biometric verification"
            };

            _db.AttendanceRecords.Add(newRecord);
            _db.SaveChanges();

            return Task.FromResult(new PunchResultModel
            {
                Success = true,
                Message = $"Welcome, {employee.FullName}! Check-In registered successfully.",
                EmployeeName = employee.FullName,
                EmployeeCode = employee.EmployeeCode,
                Department = employee.Department,
                Action = "Check-In (Punch IN)",
                TimeFormatted = now.ToString("hh:mm:ss tt"),
                Status = status,
                MatchConfidence = newRecord.MatchConfidence
            });
        }
        else
        {
            // Already punched in: Check-out (PUNCH OUT) or Re-punch
            // If already punched out, update checkout time
            existingRecord.PunchOutTime = now;
            var duration = (now - existingRecord.PunchInTime).TotalHours;
            existingRecord.TotalWorkHours = Math.Max(0.05, Math.Round(duration, 2));
            existingRecord.PunchOutPhoto = request.PhotoDataUri;

            if (existingRecord.TotalWorkHours < policy.HalfDayMinimumHours && existingRecord.Status != "Late")
            {
                existingRecord.Status = "Half-Day";
            }
            else if (existingRecord.TotalWorkHours >= policy.StandardWorkHours + 1.0)
            {
                existingRecord.Status = "Overtime";
            }

            _db.SaveChanges();

            return Task.FromResult(new PunchResultModel
            {
                Success = true,
                Message = $"Goodbye, {employee.FullName}! Check-Out registered. Total work hours: {existingRecord.TotalWorkHours:F2} hrs.",
                EmployeeName = employee.FullName,
                EmployeeCode = employee.EmployeeCode,
                Department = employee.Department,
                Action = "Check-Out (Punch OUT)",
                TimeFormatted = now.ToString("hh:mm:ss tt"),
                Status = existingRecord.Status,
                MatchConfidence = request.MatchConfidence > 0 ? request.MatchConfidence : existingRecord.MatchConfidence,
                TotalHoursToday = existingRecord.TotalWorkHours
            });
        }
    }

    public Task<DashboardViewModel> GetDashboardMetricsAsync()
    {
        var today = DateTime.Today;
        var totalEmployees = _db.Employees.Count(e => e.IsActive);
        var todayRecords = _db.AttendanceRecords.Where(r => r.Date.Date == today).ToList();

        var presentCount = todayRecords.Count(r => r.Status == "Present" || r.Status == "Overtime");
        var lateCount = todayRecords.Count(r => r.Status == "Late");
        var halfDayCount = todayRecords.Count(r => r.Status == "Half-Day");
        var totalCheckedIn = todayRecords.Count;
        var absentCount = Math.Max(0, totalEmployees - totalCheckedIn);

        var attendanceRate = totalEmployees > 0 
            ? Math.Round(((double)totalCheckedIn / totalEmployees) * 100, 1) 
            : 0.0;

        // Department breakdown
        var deptStats = _db.Departments.Select(d =>
        {
            var deptEmployees = _db.Employees.Count(e => e.Department == d.Name && e.IsActive);
            var deptPresent = todayRecords.Count(r => r.Department == d.Name);
            var pct = deptEmployees > 0 ? Math.Round(((double)deptPresent / deptEmployees) * 100, 1) : 0.0;
            return new DepartmentStat
            {
                DepartmentName = d.Name,
                TotalStaff = deptEmployees,
                PresentCount = deptPresent,
                Percentage = pct
            };
        }).ToList();

        // 7-Day Trend
        var trend = new List<DailyTrendStat>();
        for (int i = 6; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var dayRecords = _db.AttendanceRecords.Where(r => r.Date.Date == date).ToList();
            var dayTotal = dayRecords.Count;
            var dayLate = dayRecords.Count(r => r.Status == "Late");
            var dayPresent = dayTotal - dayLate;
            var dayAbsent = Math.Max(0, totalEmployees - dayTotal);

            trend.Add(new DailyTrendStat
            {
                DayName = date.ToString("ddd"),
                DateFormatted = date.ToString("MMM dd"),
                Present = dayPresent,
                Late = dayLate,
                Absent = dayAbsent
            });
        }

        var recentAttendance = _db.AttendanceRecords
            .OrderByDescending(r => r.PunchInTime)
            .Take(8)
            .ToList();

        var model = new DashboardViewModel
        {
            TotalEmployees = totalEmployees,
            PresentToday = presentCount,
            LateToday = lateCount,
            HalfDayToday = halfDayCount,
            AbsentToday = absentCount,
            AttendanceRatePercentage = attendanceRate,
            RecentAttendance = recentAttendance,
            DepartmentStats = deptStats,
            Last7DaysTrend = trend,
            Policy = _db.Policy ?? new AttendancePolicy()
        };

        return Task.FromResult(model);
    }

    public Task<List<AttendanceRecord>> GetTodayAttendanceAsync()
    {
        var today = DateTime.Today;
        var list = _db.AttendanceRecords
            .Where(r => r.Date.Date == today)
            .OrderByDescending(r => r.PunchInTime)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<List<AttendanceRecord>> GetRecentPunchesAsync(int count = 10)
    {
        var list = _db.AttendanceRecords
            .OrderByDescending(r => r.PunchInTime)
            .Take(count)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<AttendanceRecord?> GetTodayRecordForEmployeeAsync(int employeeId)
    {
        var today = DateTime.Today;
        var record = _db.AttendanceRecords
            .FirstOrDefault(r => r.EmployeeId == employeeId && r.Date.Date == today);
        return Task.FromResult(record);
    }
}

using EmployeeAttendanceApp.Models;

namespace EmployeeAttendanceApp.Models.ViewModels;

public class DashboardViewModel
{
    public int TotalEmployees { get; set; }
    public int PresentToday { get; set; }
    public int LateToday { get; set; }
    public int HalfDayToday { get; set; }
    public int AbsentToday { get; set; }
    public double AttendanceRatePercentage { get; set; }
    public List<AttendanceRecord> RecentAttendance { get; set; } = new();
    public List<DepartmentStat> DepartmentStats { get; set; } = new();
    public List<DailyTrendStat> Last7DaysTrend { get; set; } = new();
    public AttendancePolicy Policy { get; set; } = new();
}

public class DepartmentStat
{
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalStaff { get; set; }
    public int PresentCount { get; set; }
    public double Percentage { get; set; }
}

public class DailyTrendStat
{
    public string DayName { get; set; } = string.Empty;
    public string DateFormatted { get; set; } = string.Empty;
    public int Present { get; set; }
    public int Late { get; set; }
    public int Absent { get; set; }
}

public class PunchRequestModel
{
    public int? EmployeeId { get; set; }
    public string? EmployeeCode { get; set; }
    public string? FaceDescriptorJson { get; set; }
    public string? PhotoDataUri { get; set; }
    public double MatchConfidence { get; set; }
    public string Action { get; set; } = "IN"; // "IN" or "OUT"
    public string DeviceLocation { get; set; } = "Biometric Kiosk";
}

public class PunchResultModel
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string TimeFormatted { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double MatchConfidence { get; set; }
    public double? TotalHoursToday { get; set; }
}

public class EnrollFaceModel
{
    public int EmployeeId { get; set; }
    public string FaceDescriptorJson { get; set; } = string.Empty;
    public string PhotoDataUri { get; set; } = string.Empty;
}

public class LoginViewModel
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; } = true;
    public string? ReturnUrl { get; set; }
}

public class AttendanceReportViewModel
{
    public DateTime FromDate { get; set; } = DateTime.Today.AddDays(-7);
    public DateTime ToDate { get; set; } = DateTime.Today;
    public string? Department { get; set; }
    public string? Status { get; set; }
    public string? SearchTerm { get; set; }
    public List<AttendanceRecord> Records { get; set; } = new();
    public List<Department> Departments { get; set; } = new();
    public int TotalPunches { get; set; }
    public double TotalHoursWorked { get; set; }
    public double AverageHoursPerDay { get; set; }
}

using System.Text.Json.Serialization;

namespace EmployeeAttendanceApp.Models;

public class Employee
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty; // e.g., EMP-101
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Department { get; set; } = "Engineering";
    public string Designation { get; set; } = "Staff";
    public string ShiftType { get; set; } = "Standard (09:00 - 18:00)";
    public DateTime JoiningDate { get; set; } = DateTime.Today;
    public bool IsActive { get; set; } = true;
    
    // Biometric face vector & snapshot
    public bool IsFaceEnrolled { get; set; } = false;
    public string? FaceDescriptorJson { get; set; } // 128-D vector string
    public string? ProfilePhotoUrl { get; set; } // Base64 data URI or image path
    public DateTime? EnrolledAt { get; set; }
}

public class AttendanceRecord
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Today;
    
    public DateTime PunchInTime { get; set; }
    public DateTime? PunchOutTime { get; set; }
    public double TotalWorkHours { get; set; }
    
    public string Status { get; set; } = "Present"; // Present, Late, Half-Day, Overtime
    public double MatchConfidence { get; set; } = 95.0; // Biometric match %
    public string VerificationMethod { get; set; } = "Biometric Face Scan";
    public string? PunchInPhoto { get; set; } // Snapshot captured during punch in
    public string? PunchOutPhoto { get; set; }
    public string DeviceLocation { get; set; } = "HQ Main Entrance";
    public string? Notes { get; set; }
}

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ShiftStartTime { get; set; } = "09:00";
    public string ShiftEndTime { get; set; } = "18:00";
    public int GraceMinutes { get; set; } = 15;
    public int EmployeeCount { get; set; } = 0;
}

public class AttendancePolicy
{
    public int Id { get; set; } = 1;
    public string CompanyName { get; set; } = "Apex Technologies Inc.";
    public string ShiftStartTime { get; set; } = "09:00";
    public string ShiftEndTime { get; set; } = "18:00";
    public int LateGraceMinutes { get; set; } = 15;
    public double HalfDayMinimumHours { get; set; } = 4.5;
    public double StandardWorkHours { get; set; } = 8.0;
    public double RequiredMatchConfidence { get; set; } = 90.0;
    public bool AutoCheckoutAtMidnight { get; set; } = true;
    public bool AllowKioskDirectPunches { get; set; } = true;
}

public class UserAccount
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Admin"; // Admin, Manager, Employee
    public string FullName { get; set; } = string.Empty;
    public int? EmployeeId { get; set; }
}

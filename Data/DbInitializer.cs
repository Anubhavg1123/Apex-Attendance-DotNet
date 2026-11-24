using System.Security.Cryptography;
using System.Text;
using EmployeeAttendanceApp.Models;

namespace EmployeeAttendanceApp.Data;

public static class DbInitializer
{
    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "ApexSalt_2026!#"));
        return Convert.ToBase64String(bytes);
    }

    public static bool VerifyPassword(string enteredPassword, string storedHash)
    {
        return HashPassword(enteredPassword) == storedHash;
    }

    public static Task InitializeAsync(ApplicationDbContext context)
    {
        // 1. Seed or Upgrade Admin User with Secure Password
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            var isProd = string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Production", StringComparison.OrdinalIgnoreCase);
            if (isProd)
            {
                // In production, generate a cryptographically secure random secret if none was explicitly configured
                adminPassword = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)) + "!A1";
            }
            else
            {
                adminPassword = "ApexAdmin@2026!Prod";
            }
        }
        var devHash = HashPassword("Admin@123");
        var adminUser = context.Users.FirstOrDefault(u => u.Username.Equals("admin", StringComparison.OrdinalIgnoreCase));
        if (adminUser == null)
        {
            context.Users.Add(new UserAccount
            {
                Id = context.GetNextUserId(),
                Username = "admin",
                PasswordHash = HashPassword(adminPassword),
                Role = "Admin",
                FullName = "System Administrator"
            });
        }
        else if (adminUser.PasswordHash == devHash || Environment.GetEnvironmentVariable("ADMIN_PASSWORD") != null)
        {
            adminUser.PasswordHash = HashPassword(adminPassword);
        }

        // 2. Seed Policy
        if (context.Policy == null || string.IsNullOrEmpty(context.Policy.CompanyName))
        {
            context.Policy = new AttendancePolicy
            {
                Id = 1,
                CompanyName = "Apex Technologies Biometric Systems",
                ShiftStartTime = "09:00",
                ShiftEndTime = "18:00",
                LateGraceMinutes = 15,
                HalfDayMinimumHours = 4.5,
                StandardWorkHours = 8.0,
                RequiredMatchConfidence = 90.0,
                AutoCheckoutAtMidnight = true,
                AllowKioskDirectPunches = true
            };
        }

        // 3. Seed Departments
        if (!context.Departments.Any())
        {
            var depts = new List<Department>
            {
                new() { Id = context.GetNextDepartmentId(), Name = "Engineering", Description = "Software Engineering, Cloud, & Systems Architecture", ShiftStartTime = "09:00", ShiftEndTime = "18:00", GraceMinutes = 15, EmployeeCount = 3 },
                new() { Id = context.GetNextDepartmentId(), Name = "Product & Design", Description = "UI/UX Architecture & Product Strategy", ShiftStartTime = "09:00", ShiftEndTime = "18:00", GraceMinutes = 15, EmployeeCount = 2 },
                new() { Id = context.GetNextDepartmentId(), Name = "Human Resources", Description = "People Operations, Talent & Culture", ShiftStartTime = "09:00", ShiftEndTime = "18:00", GraceMinutes = 15, EmployeeCount = 1 },
                new() { Id = context.GetNextDepartmentId(), Name = "Finance & Accounts", Description = "Corporate Treasury, Payroll, & Audit", ShiftStartTime = "09:00", ShiftEndTime = "18:00", GraceMinutes = 15, EmployeeCount = 1 },
                new() { Id = context.GetNextDepartmentId(), Name = "Operations & Logistics", Description = "Infrastructure, Supply & Site Operations", ShiftStartTime = "08:30", ShiftEndTime = "17:30", GraceMinutes = 15, EmployeeCount = 1 }
            };
            context.Departments.AddRange(depts);
        }

        // 4. Seed Starter Employees
        if (!context.Employees.Any())
        {
            var emp1 = new Employee
            {
                Id = context.GetNextEmployeeId(),
                EmployeeCode = "APX-101",
                FullName = "Alexander Vance",
                Email = "alexander.vance@apex.io",
                Phone = "+1 (555) 234-8901",
                Department = "Engineering",
                Designation = "Principal Cloud Architect",
                ShiftType = "Standard (09:00 - 18:00)",
                JoiningDate = DateTime.Today.AddMonths(-14),
                IsActive = true,
                IsFaceEnrolled = true,
                ProfilePhotoUrl = null,
                EnrolledAt = DateTime.Today.AddMonths(-14)
            };

            var emp2 = new Employee
            {
                Id = context.GetNextEmployeeId(),
                EmployeeCode = "APX-102",
                FullName = "Sophia Reyes",
                Email = "sophia.reyes@apex.io",
                Phone = "+1 (555) 345-6712",
                Department = "Product & Design",
                Designation = "Lead UX Designer",
                ShiftType = "Standard (09:00 - 18:00)",
                JoiningDate = DateTime.Today.AddMonths(-8),
                IsActive = true,
                IsFaceEnrolled = true,
                ProfilePhotoUrl = null,
                EnrolledAt = DateTime.Today.AddMonths(-8)
            };

            var emp3 = new Employee
            {
                Id = context.GetNextEmployeeId(),
                EmployeeCode = "APX-103",
                FullName = "Marcus Chen",
                Email = "marcus.chen@apex.io",
                Phone = "+1 (555) 892-4411",
                Department = "Engineering",
                Designation = "Senior Full-Stack Engineer",
                ShiftType = "Standard (09:00 - 18:00)",
                JoiningDate = DateTime.Today.AddMonths(-5),
                IsActive = true,
                IsFaceEnrolled = false,
                ProfilePhotoUrl = null,
                EnrolledAt = null
            };

            var emp4 = new Employee
            {
                Id = context.GetNextEmployeeId(),
                EmployeeCode = "APX-104",
                FullName = "Elena Rostova",
                Email = "elena.rostova@apex.io",
                Phone = "+1 (555) 441-2299",
                Department = "Human Resources",
                Designation = "HR Operations Director",
                ShiftType = "Standard (09:00 - 18:00)",
                JoiningDate = DateTime.Today.AddYears(-2),
                IsActive = true,
                IsFaceEnrolled = true,
                ProfilePhotoUrl = null,
                EnrolledAt = DateTime.Today.AddYears(-2)
            };

            var emp5 = new Employee
            {
                Id = context.GetNextEmployeeId(),
                EmployeeCode = "APX-105",
                FullName = "David Kim",
                Email = "david.kim@apex.io",
                Phone = "+1 (555) 773-1902",
                Department = "Finance & Accounts",
                Designation = "Financial Analyst",
                ShiftType = "Standard (09:00 - 18:00)",
                JoiningDate = DateTime.Today.AddMonths(-3),
                IsActive = true,
                IsFaceEnrolled = false,
                ProfilePhotoUrl = null,
                EnrolledAt = null
            };

            context.Employees.AddRange(new[] { emp1, emp2, emp3, emp4, emp5 });

            // 5. Seed Realistic Attendance Records for Past 7 Days + Today
            var today = DateTime.Today;
            for (int i = 6; i >= 1; i--)
            {
                var recordDate = today.AddDays(-i);
                if (recordDate.DayOfWeek == DayOfWeek.Saturday || recordDate.DayOfWeek == DayOfWeek.Sunday)
                    continue;

                // Emp 1 (On Time)
                context.AttendanceRecords.Add(new AttendanceRecord
                {
                    Id = context.GetNextAttendanceId(),
                    EmployeeId = emp1.Id,
                    EmployeeCode = emp1.EmployeeCode,
                    EmployeeName = emp1.FullName,
                    Department = emp1.Department,
                    Date = recordDate,
                    PunchInTime = recordDate.AddHours(8).AddMinutes(52),
                    PunchOutTime = recordDate.AddHours(18).AddMinutes(12),
                    TotalWorkHours = 9.33,
                    Status = "Present",
                    MatchConfidence = 96.8,
                    VerificationMethod = "Biometric Face Scan",
                    DeviceLocation = "HQ Main Entrance Kiosk"
                });

                // Emp 2 (Late)
                context.AttendanceRecords.Add(new AttendanceRecord
                {
                    Id = context.GetNextAttendanceId(),
                    EmployeeId = emp2.Id,
                    EmployeeCode = emp2.EmployeeCode,
                    EmployeeName = emp2.FullName,
                    Department = emp2.Department,
                    Date = recordDate,
                    PunchInTime = recordDate.AddHours(9).AddMinutes(24),
                    PunchOutTime = recordDate.AddHours(18).AddMinutes(30),
                    TotalWorkHours = 9.1,
                    Status = "Late",
                    MatchConfidence = 94.2,
                    VerificationMethod = "Biometric Face Scan",
                    DeviceLocation = "HQ Main Entrance Kiosk"
                });

                // Emp 4 (Present)
                context.AttendanceRecords.Add(new AttendanceRecord
                {
                    Id = context.GetNextAttendanceId(),
                    EmployeeId = emp4.Id,
                    EmployeeCode = emp4.EmployeeCode,
                    EmployeeName = emp4.FullName,
                    Department = emp4.Department,
                    Date = recordDate,
                    PunchInTime = recordDate.AddHours(8).AddMinutes(45),
                    PunchOutTime = recordDate.AddHours(17).AddMinutes(55),
                    TotalWorkHours = 9.17,
                    Status = "Present",
                    MatchConfidence = 98.4,
                    VerificationMethod = "Biometric Face Scan",
                    DeviceLocation = "Executive Suite Terminal"
                });
            }

            // Today's Punches
            context.AttendanceRecords.Add(new AttendanceRecord
            {
                Id = context.GetNextAttendanceId(),
                EmployeeId = emp1.Id,
                EmployeeCode = emp1.EmployeeCode,
                EmployeeName = emp1.FullName,
                Department = emp1.Department,
                Date = today,
                PunchInTime = today.AddHours(8).AddMinutes(50),
                PunchOutTime = null,
                TotalWorkHours = 0,
                Status = "Present",
                MatchConfidence = 97.2,
                VerificationMethod = "Biometric Face Scan",
                DeviceLocation = "HQ Main Entrance Kiosk",
                Notes = "On-time arrival"
            });

            context.AttendanceRecords.Add(new AttendanceRecord
            {
                Id = context.GetNextAttendanceId(),
                EmployeeId = emp4.Id,
                EmployeeCode = emp4.EmployeeCode,
                EmployeeName = emp4.FullName,
                Department = emp4.Department,
                Date = today,
                PunchInTime = today.AddHours(8).AddMinutes(42),
                PunchOutTime = null,
                TotalWorkHours = 0,
                Status = "Present",
                MatchConfidence = 99.1,
                VerificationMethod = "Biometric Face Scan",
                DeviceLocation = "HQ Main Entrance Kiosk",
                Notes = "Biometric verified"
            });

            context.AttendanceRecords.Add(new AttendanceRecord
            {
                Id = context.GetNextAttendanceId(),
                EmployeeId = emp2.Id,
                EmployeeCode = emp2.EmployeeCode,
                EmployeeName = emp2.FullName,
                Department = emp2.Department,
                Date = today,
                PunchInTime = today.AddHours(9).AddMinutes(22),
                PunchOutTime = null,
                TotalWorkHours = 0,
                Status = "Late",
                MatchConfidence = 95.6,
                VerificationMethod = "Biometric Face Scan",
                DeviceLocation = "HQ Main Entrance Kiosk",
                Notes = "Late by 7 mins after grace threshold"
            });
        }

        context.SaveChanges();
        return Task.CompletedTask;
    }
}

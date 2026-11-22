using System.Text.Json;
using EmployeeAttendanceApp.Models;

namespace EmployeeAttendanceApp.Data;

public class DatabaseSchema
{
    public List<Employee> Employees { get; set; } = new();
    public List<AttendanceRecord> AttendanceRecords { get; set; } = new();
    public List<Department> Departments { get; set; } = new();
    public List<UserAccount> Users { get; set; } = new();
    public AttendancePolicy Policy { get; set; } = new();
    public int NextEmployeeId { get; set; } = 1;
    public int NextAttendanceId { get; set; } = 1;
    public int NextDepartmentId { get; set; } = 1;
    public int NextUserId { get; set; } = 1;
}

public class ApplicationDbContext
{
    private readonly string _filePath;
    private readonly ReaderWriterLockSlim _lock = new();
    private DatabaseSchema _data = new();

    public ApplicationDbContext(IWebHostEnvironment env)
    {
        var customDataDir = Environment.GetEnvironmentVariable("DATA_DIR");
        var dataDir = !string.IsNullOrWhiteSpace(customDataDir)
            ? customDataDir
            : Path.Combine(env.ContentRootPath, "App_Data");

        if (!Directory.Exists(dataDir))
        {
            Directory.CreateDirectory(dataDir);
        }
        _filePath = Path.Combine(dataDir, "attendance_database.json");
        LoadData();
    }

    private void LoadData()
    {
        _lock.EnterWriteLock();
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var loaded = JsonSerializer.Deserialize<DatabaseSchema>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (loaded != null)
                {
                    _data = loaded;
                    return;
                }
            }
            _data = new DatabaseSchema();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void SaveChanges()
    {
        _lock.EnterWriteLock();
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_data, options);
            var tempPath = _filePath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _filePath, overwrite: true);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    // Accessors
    public List<Employee> Employees => _data.Employees;
    public List<AttendanceRecord> AttendanceRecords => _data.AttendanceRecords;
    public List<Department> Departments => _data.Departments;
    public List<UserAccount> Users => _data.Users;
    public AttendancePolicy Policy
    {
        get => _data.Policy;
        set => _data.Policy = value;
    }

    public int GetNextEmployeeId() => _data.NextEmployeeId++;
    public int GetNextAttendanceId() => _data.NextAttendanceId++;
    public int GetNextDepartmentId() => _data.NextDepartmentId++;
    public int GetNextUserId() => _data.NextUserId++;
}

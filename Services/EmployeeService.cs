using System.Text.Json;
using EmployeeAttendanceApp.Data;
using EmployeeAttendanceApp.Models;

namespace EmployeeAttendanceApp.Services;

public interface IEmployeeService
{
    Task<List<Employee>> GetAllEmployeesAsync(string? search = null, string? department = null);
    Task<Employee?> GetEmployeeByIdAsync(int id);
    Task<Employee?> GetEmployeeByCodeAsync(string code);
    Task<Employee> CreateEmployeeAsync(Employee employee);
    Task<bool> UpdateEmployeeAsync(Employee employee);
    Task<bool> DeleteEmployeeAsync(int id);
    Task<bool> EnrollFaceAsync(int employeeId, string descriptorJson, string photoDataUri);
    Task<List<Department>> GetDepartmentsAsync();

    // Biometric Verification & Identification
    Task<(bool isDuplicate, Employee? matchedEmployee, double distance, double confidence)> CheckDuplicateFaceAsync(string newDescriptorJson, int? excludeEmployeeId = null);
    Task<(Employee? matchedEmployee, double distance, double confidence, bool isAmbiguous)> IdentifyFaceAsync(string liveDescriptorJson);
}

public class EmployeeService : IEmployeeService
{
    private readonly ApplicationDbContext _db;

    public EmployeeService(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<List<Employee>> GetAllEmployeesAsync(string? search = null, string? department = null)
    {
        var query = _db.Employees.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(e => 
                e.FullName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                e.EmployeeCode.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                e.Email.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(department) && department != "All")
        {
            query = query.Where(e => e.Department.Equals(department, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult(query.OrderBy(e => e.EmployeeCode).ToList());
    }

    public Task<Employee?> GetEmployeeByIdAsync(int id)
    {
        var emp = _db.Employees.FirstOrDefault(e => e.Id == id);
        return Task.FromResult(emp);
    }

    public Task<Employee?> GetEmployeeByCodeAsync(string code)
    {
        var emp = _db.Employees.FirstOrDefault(e => e.EmployeeCode.Equals(code, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(emp);
    }

    public async Task<Employee> CreateEmployeeAsync(Employee employee)
    {
        // 1. Strict Duplicate Face Check: Reject duplicate face registration before database commit
        if (!string.IsNullOrEmpty(employee.FaceDescriptorJson))
        {
            var (isDuplicate, existing, dist, conf) = await CheckDuplicateFaceAsync(employee.FaceDescriptorJson);
            if (isDuplicate && existing != null)
            {
                throw new InvalidOperationException($"DUPLICATE FACE DETECTED: This face is already registered with employee '{existing.FullName}' ({existing.EmployeeCode}) in {existing.Department}. Duplicate registration is not allowed.");
            }
            employee.IsFaceEnrolled = true;
            employee.EnrolledAt = DateTime.Now;
        }

        employee.Id = _db.GetNextEmployeeId();
        
        if (string.IsNullOrWhiteSpace(employee.EmployeeCode))
        {
            employee.EmployeeCode = $"APX-{100 + employee.Id}";
        }

        _db.Employees.Add(employee);

        // Update department count
        var dept = _db.Departments.FirstOrDefault(d => d.Name.Equals(employee.Department, StringComparison.OrdinalIgnoreCase));
        if (dept != null)
        {
            dept.EmployeeCount = _db.Employees.Count(e => e.Department == dept.Name && e.IsActive);
        }

        _db.SaveChanges();
        return employee;
    }

    public async Task<bool> UpdateEmployeeAsync(Employee employee)
    {
        var existing = _db.Employees.FirstOrDefault(e => e.Id == employee.Id);
        if (existing == null) return false;

        // If face descriptor is being updated or changed, enforce duplicate check against OTHER employees
        if (!string.IsNullOrEmpty(employee.FaceDescriptorJson) && employee.FaceDescriptorJson != existing.FaceDescriptorJson)
        {
            var (isDuplicate, otherEmp, dist, conf) = await CheckDuplicateFaceAsync(employee.FaceDescriptorJson, excludeEmployeeId: employee.Id);
            if (isDuplicate && otherEmp != null)
            {
                throw new InvalidOperationException($"DUPLICATE FACE DETECTED: This face is already registered with employee '{otherEmp.FullName}' ({otherEmp.EmployeeCode}) in {otherEmp.Department}. Duplicate registration is not allowed.");
            }
            existing.FaceDescriptorJson = employee.FaceDescriptorJson;
            existing.IsFaceEnrolled = true;
            existing.EnrolledAt = DateTime.Now;
        }

        existing.FullName = employee.FullName;
        existing.Email = employee.Email;
        existing.Phone = employee.Phone;
        existing.Department = employee.Department;
        existing.Designation = employee.Designation;
        existing.ShiftType = employee.ShiftType;
        existing.IsActive = employee.IsActive;

        if (!string.IsNullOrEmpty(employee.ProfilePhotoUrl))
        {
            existing.ProfilePhotoUrl = employee.ProfilePhotoUrl;
        }

        _db.SaveChanges();
        return true;
    }

    public Task<bool> DeleteEmployeeAsync(int id)
    {
        var existing = _db.Employees.FirstOrDefault(e => e.Id == id);
        if (existing == null) return Task.FromResult(false);

        _db.Employees.Remove(existing);
        _db.SaveChanges();
        return Task.FromResult(true);
    }

    public async Task<bool> EnrollFaceAsync(int employeeId, string descriptorJson, string photoDataUri)
    {
        var existing = _db.Employees.FirstOrDefault(e => e.Id == employeeId);
        if (existing == null) return false;

        // Enforce duplicate check against other employees
        var (isDuplicate, otherEmp, dist, conf) = await CheckDuplicateFaceAsync(descriptorJson, excludeEmployeeId: employeeId);
        if (isDuplicate && otherEmp != null)
        {
            throw new InvalidOperationException($"DUPLICATE FACE DETECTED: This face is already registered with employee '{otherEmp.FullName}' ({otherEmp.EmployeeCode}) in {otherEmp.Department}. Duplicate registration is not allowed.");
        }

        existing.FaceDescriptorJson = descriptorJson;
        existing.ProfilePhotoUrl = photoDataUri;
        existing.IsFaceEnrolled = true;
        existing.EnrolledAt = DateTime.Now;

        _db.SaveChanges();
        return true;
    }

    public Task<List<Department>> GetDepartmentsAsync()
    {
        return Task.FromResult(_db.Departments.OrderBy(d => d.Name).ToList());
    }

    // -------------------------------------------------------------
    // Biometric Euclidean-Distance Face Matching & Duplicate Checks
    // -------------------------------------------------------------

    public static double ComputeEuclideanDistance(float[] vecA, float[] vecB)
    {
        if (vecA == null || vecB == null || vecA.Length != vecB.Length) return 999.0;
        double sum = 0.0;
        for (int i = 0; i < vecA.Length; i++)
        {
            double diff = vecA[i] - vecB[i];
            sum += diff * diff;
        }
        return Math.Sqrt(sum);
    }

    public static List<float[]> ParseDescriptors(string json)
    {
        var result = new List<float[]>();
        if (string.IsNullOrWhiteSpace(json)) return result;
        json = json.Trim();
        try
        {
            if (json.StartsWith("[["))
            {
                var multi = JsonSerializer.Deserialize<List<float[]>>(json);
                if (multi != null) result.AddRange(multi);
            }
            else if (json.StartsWith("["))
            {
                var single = JsonSerializer.Deserialize<float[]>(json);
                if (single != null && single.Length > 0) result.Add(single);
            }
        }
        catch { }
        return result;
    }

    public static double DistanceToConfidence(double distance)
    {
        if (distance <= 0.30)
            return Math.Round(Math.Max(90.0, 100.0 - (distance / 0.30) * 10.0), 1);
        if (distance <= 0.50)
            return Math.Round(Math.Max(60.0, 90.0 - ((distance - 0.30) / 0.20) * 30.0), 1);
        return Math.Round(Math.Max(0.0, 60.0 - ((distance - 0.50) / 0.30) * 60.0), 1);
    }

    public Task<(bool isDuplicate, Employee? matchedEmployee, double distance, double confidence)> CheckDuplicateFaceAsync(string newDescriptorJson, int? excludeEmployeeId = null)
    {
        var newVectors = ParseDescriptors(newDescriptorJson);
        if (newVectors.Count == 0)
        {
            return Task.FromResult((false, (Employee?)null, 999.0, 0.0));
        }

        var candidate = newVectors[0];
        Employee? bestMatch = null;
        double lowestDistance = 999.0;

        foreach (var emp in _db.Employees)
        {
            if (excludeEmployeeId.HasValue && emp.Id == excludeEmployeeId.Value) continue;
            if (string.IsNullOrWhiteSpace(emp.FaceDescriptorJson)) continue;

            var existingVectors = ParseDescriptors(emp.FaceDescriptorJson);
            foreach (var vec in existingVectors)
            {
                var dist = ComputeEuclideanDistance(candidate, vec);
                if (dist < lowestDistance)
                {
                    lowestDistance = dist;
                    bestMatch = emp;
                }
            }
        }

        double confidence = DistanceToConfidence(lowestDistance);
        // Strictest Euclidean Distance threshold for duplicate prevention:
        // Any distance <= 0.38 indicates the exact same person.
        bool isDuplicate = (lowestDistance <= 0.38 && bestMatch != null);

        return Task.FromResult((isDuplicate, isDuplicate ? bestMatch : null, lowestDistance, confidence));
    }

    public Task<(Employee? matchedEmployee, double distance, double confidence, bool isAmbiguous)> IdentifyFaceAsync(string liveDescriptorJson)
    {
        var liveVectors = ParseDescriptors(liveDescriptorJson);
        if (liveVectors.Count == 0)
        {
            return Task.FromResult(((Employee?)null, 999.0, 0.0, false));
        }

        var candidate = liveVectors[0];
        Employee? bestMatch = null;
        double lowestDistance = 999.0;
        double secondLowestDistance = 999.0;

        foreach (var emp in _db.Employees)
        {
            if (!emp.IsActive || string.IsNullOrWhiteSpace(emp.FaceDescriptorJson)) continue;

            var existingVectors = ParseDescriptors(emp.FaceDescriptorJson);
            foreach (var vec in existingVectors)
            {
                var dist = ComputeEuclideanDistance(candidate, vec);
                if (dist < lowestDistance)
                {
                    secondLowestDistance = lowestDistance;
                    lowestDistance = dist;
                    bestMatch = emp;
                }
                else if (dist < secondLowestDistance)
                {
                    secondLowestDistance = dist;
                }
            }
        }

        double confidence = DistanceToConfidence(lowestDistance);
        // Ambiguity check: if best and second-best candidates are within 0.08 of each other and not virtually identical
        bool isAmbiguous = (secondLowestDistance - lowestDistance < 0.08 && lowestDistance > 0.20 && secondLowestDistance < 0.50);
        bool isMatch = (lowestDistance <= 0.38 && confidence >= 85.0 && !isAmbiguous && bestMatch != null);

        return Task.FromResult((isMatch ? bestMatch : null, lowestDistance, confidence, isAmbiguous));
    }
}

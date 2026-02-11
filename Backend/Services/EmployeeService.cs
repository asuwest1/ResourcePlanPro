using Microsoft.EntityFrameworkCore;
using ResourcePlanPro.API.Data;
using ResourcePlanPro.API.Models;
using ResourcePlanPro.API.Models.DTOs;

namespace ResourcePlanPro.API.Services
{
    public class EmployeeService
    {
        private readonly ResourcePlanProContext _context;
        private readonly ILogger<EmployeeService> _logger;

        public EmployeeService(ResourcePlanProContext context, ILogger<EmployeeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<EmployeeDto>> GetAllEmployeesAsync(bool includeInactive = false)
        {
            try
            {
                var query = _context.Employees
                    .Include(e => e.Department)
                    .AsQueryable();

                if (!includeInactive)
                {
                    query = query.Where(e => e.IsActive);
                }

                var employees = await query
                    .OrderBy(e => e.LastName)
                    .ThenBy(e => e.FirstName)
                    .ToListAsync();

                return employees.Select(e => new EmployeeDto
                {
                    EmployeeId = e.EmployeeId,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    FullName = $"{e.FirstName} {e.LastName}",
                    Email = e.Email,
                    JobTitle = e.JobTitle,
                    DepartmentId = e.DepartmentId,
                    DepartmentName = e.Department.DepartmentName,
                    HoursPerWeek = e.HoursPerWeek,
                    Skills = e.Skills,
                    IsActive = e.IsActive
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employees");
                throw;
            }
        }

        public async Task<EmployeeDto> GetEmployeeByIdAsync(int employeeId)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.Department)
                    .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

                if (employee == null)
                {
                    throw new KeyNotFoundException($"Employee with ID {employeeId} not found");
                }

                return new EmployeeDto
                {
                    EmployeeId = employee.EmployeeId,
                    FirstName = employee.FirstName,
                    LastName = employee.LastName,
                    FullName = $"{employee.FirstName} {employee.LastName}",
                    Email = employee.Email,
                    JobTitle = employee.JobTitle,
                    DepartmentId = employee.DepartmentId,
                    DepartmentName = employee.Department.DepartmentName,
                    HoursPerWeek = employee.HoursPerWeek,
                    Skills = employee.Skills,
                    IsActive = employee.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee {EmployeeId}", employeeId);
                throw;
            }
        }

        public async Task<List<EmployeeWorkloadDto>> GetEmployeeWorkloadAsync(
            int? employeeId = null,
            DateTime? weekStartDate = null,
            int weekCount = 12)
        {
            if (weekCount < 1 || weekCount > 52)
                throw new ArgumentException("weekCount must be between 1 and 52");

            try
            {
                var startDate = weekStartDate ?? DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);

                var query = _context.EmployeeWorkloads
                    .FromSqlRaw(@"
                        SELECT * FROM vw_EmployeeWorkloadSummary 
                        WHERE WeekStartDate >= {0} 
                        AND WeekStartDate < {1}",
                        startDate,
                        startDate.AddDays(7 * weekCount))
                    .AsQueryable();

                if (employeeId.HasValue)
                {
                    query = query.Where(w => w.EmployeeId == employeeId.Value);
                }

                return await query
                    .OrderBy(w => w.EmployeeId)
                    .ThenBy(w => w.WeekStartDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee workload");
                throw;
            }
        }

        public async Task<List<EmployeeDto>> GetEmployeesByDepartmentAsync(int departmentId, bool includeInactive = false)
        {
            try
            {
                var query = _context.Employees
                    .Include(e => e.Department)
                    .Where(e => e.DepartmentId == departmentId);

                if (!includeInactive)
                {
                    query = query.Where(e => e.IsActive);
                }

                var employees = await query
                    .OrderBy(e => e.LastName)
                    .ThenBy(e => e.FirstName)
                    .ToListAsync();

                return employees.Select(e => new EmployeeDto
                {
                    EmployeeId = e.EmployeeId,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    FullName = $"{e.FirstName} {e.LastName}",
                    Email = e.Email,
                    JobTitle = e.JobTitle,
                    DepartmentId = e.DepartmentId,
                    DepartmentName = e.Department.DepartmentName,
                    HoursPerWeek = e.HoursPerWeek,
                    Skills = e.Skills,
                    IsActive = e.IsActive
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employees for department {DepartmentId}", departmentId);
                throw;
            }
        }
    }
}

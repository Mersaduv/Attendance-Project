using Microsoft.EntityFrameworkCore;
using NewAttendanceProject.Data;
using NewAttendanceProject.Models;

namespace NewAttendanceProject.Services
{
    public class EmployeeService
    {
        private readonly ApplicationDbContext _context;
        private readonly WorkScheduleService _workScheduleService;
        
        public EmployeeService(ApplicationDbContext context, WorkScheduleService workScheduleService)
        {
            _context = context;
            _workScheduleService = workScheduleService;
        }
        
        public async Task<List<Employee>> GetAllAsync()
        {
            return await _context.Employees
                .Include(e => e.Department)
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToListAsync();
        }
        
        public async Task<Employee> GetByIdAsync(int id)
        {
            return await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == id);
        }
        
        public async Task<List<Employee>> GetByDepartmentAsync(int departmentId)
        {
            return await _context.Employees
                .Where(e => e.DepartmentId == departmentId)
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToListAsync();
        }
        
        public async Task<Employee> CreateAsync(Employee employee)
        {
            // Check if employee code is unique
            if (!await IsEmployeeCodeUnique(employee.EmployeeCode))
            {
                throw new InvalidOperationException($"Employee code '{employee.EmployeeCode}' already exists. Please use a unique code.");
            }
            
            // Get the department's work schedule if it exists
            var departmentSchedules = await _workScheduleService.GetByDepartmentAsync(employee.DepartmentId);
            var departmentSchedule = departmentSchedules.FirstOrDefault();
            
            // Assign the department's work schedule to the employee if available, otherwise use the default schedule
            if (departmentSchedule != null)
            {
                employee.WorkScheduleId = departmentSchedule.Id;
            }
            else
            {
                // Get default schedule (ID = 1) if department has no specific schedule
                var defaultSchedule = await _context.WorkSchedules.FindAsync(1);
                if (defaultSchedule != null)
                {
                    employee.WorkScheduleId = defaultSchedule.Id;
                }
            }
            
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
            return employee;
        }
        
        public async Task<Employee> UpdateAsync(Employee employee)
        {
            // Check if employee code is unique
            if (!await IsEmployeeCodeUnique(employee.EmployeeCode, employee.Id))
            {
                throw new InvalidOperationException($"Employee code '{employee.EmployeeCode}' already exists. Please use a unique code.");
            }
            
            // Find the existing entity
            var existingEmployee = await _context.Employees.FindAsync(employee.Id);
            if (existingEmployee == null)
            {
                throw new KeyNotFoundException($"Employee with ID {employee.Id} not found");
            }
            
            // Update properties
            existingEmployee.FirstName = employee.FirstName;
            existingEmployee.LastName = employee.LastName;
            existingEmployee.Email = employee.Email;
            existingEmployee.PhoneNumber = employee.PhoneNumber;
            existingEmployee.Position = employee.Position;
            existingEmployee.EmployeeCode = employee.EmployeeCode;
            existingEmployee.DepartmentId = employee.DepartmentId;
            existingEmployee.HireDate = employee.HireDate;
            
            // Get the department's work schedule if it exists
            var departmentSchedules = await _workScheduleService.GetByDepartmentAsync(employee.DepartmentId);
            var departmentSchedule = departmentSchedules.FirstOrDefault();
            
            // Assign the department's work schedule to the employee if available, otherwise use the default schedule
            if (departmentSchedule != null)
            {
                existingEmployee.WorkScheduleId = departmentSchedule.Id;
            }
            else
            {
                // Get default schedule (ID = 1) if department has no specific schedule
                var defaultSchedule = await _context.WorkSchedules.FindAsync(1);
                if (defaultSchedule != null)
                {
                    existingEmployee.WorkScheduleId = defaultSchedule.Id;
                }
            }
            
            // Save changes
            await _context.SaveChangesAsync();
            return existingEmployee;
        }
        
        public async Task DeleteAsync(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
            }
        }
        
        // Check if employee code is unique
        public async Task<bool> IsEmployeeCodeUnique(string employeeCode, int employeeId = 0)
        {
            // When updating, exclude the current employee from the check
            if (employeeId > 0)
            {
                return !await _context.Employees
                    .AnyAsync(e => e.EmployeeCode == employeeCode && e.Id != employeeId);
            }
            
            // For new employees, check if the code is already used
            return !await _context.Employees.AnyAsync(e => e.EmployeeCode == employeeCode);
        }
    }
} 
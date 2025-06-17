using Microsoft.EntityFrameworkCore;
using NewAttendanceProject.Data;
using NewAttendanceProject.Models;

namespace NewAttendanceProject.Services
{
    public class WorkScheduleService
    {
        private readonly ApplicationDbContext _context;
        
        public WorkScheduleService(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<List<WorkSchedule>> GetAllAsync()
        {
            return await _context.WorkSchedules
                .Include(ws => ws.Department)
                .ToListAsync();
        }
        
        public async Task<WorkSchedule> GetByIdAsync(int id)
        {
            return await _context.WorkSchedules
                .Include(ws => ws.Department)
                .Include(ws => ws.Employees)
                .FirstOrDefaultAsync(ws => ws.Id == id);
        }
        
        public async Task<List<WorkSchedule>> GetByDepartmentAsync(int departmentId)
        {
            return await _context.WorkSchedules
                .Where(ws => ws.DepartmentId == departmentId)
                .ToListAsync();
        }
        
        public async Task<WorkSchedule> GetEmployeeWorkScheduleAsync(int employeeId)
        {
            var employee = await _context.Employees
                .Include(e => e.WorkSchedule)
                .Include(e => e.Department)
                .ThenInclude(d => d.WorkSchedules)
                .FirstOrDefaultAsync(e => e.Id == employeeId);
                
            if (employee == null)
                return null;
                
            // If employee has specific schedule assigned, return it
            if (employee.WorkSchedule != null)
                return employee.WorkSchedule;
                
            // Otherwise, get department's default schedule
            var departmentSchedules = await _context.WorkSchedules
                .Where(ws => ws.DepartmentId == employee.DepartmentId)
                .ToListAsync();
                
            return departmentSchedules.FirstOrDefault() ?? 
                await _context.WorkSchedules.FirstOrDefaultAsync(ws => ws.Id == 1); // Get default schedule
        }
        
        public async Task<WorkSchedule> CreateAsync(WorkSchedule workSchedule)
        {
            _context.WorkSchedules.Add(workSchedule);
            await _context.SaveChangesAsync();
            return workSchedule;
        }
        
        public async Task<WorkSchedule> UpdateAsync(WorkSchedule workSchedule)
        {
            // Find the existing entity
            var existingWorkSchedule = await _context.WorkSchedules.FindAsync(workSchedule.Id);
            if (existingWorkSchedule == null)
            {
                throw new KeyNotFoundException($"Work Schedule with ID {workSchedule.Id} not found");
            }
            
            // Update properties
            existingWorkSchedule.Name = workSchedule.Name;
            existingWorkSchedule.StartTime = workSchedule.StartTime;
            existingWorkSchedule.EndTime = workSchedule.EndTime;
            existingWorkSchedule.IsWorkingDaySunday = workSchedule.IsWorkingDaySunday;
            existingWorkSchedule.IsWorkingDayMonday = workSchedule.IsWorkingDayMonday;
            existingWorkSchedule.IsWorkingDayTuesday = workSchedule.IsWorkingDayTuesday;
            existingWorkSchedule.IsWorkingDayWednesday = workSchedule.IsWorkingDayWednesday;
            existingWorkSchedule.IsWorkingDayThursday = workSchedule.IsWorkingDayThursday;
            existingWorkSchedule.IsWorkingDayFriday = workSchedule.IsWorkingDayFriday;
            existingWorkSchedule.IsWorkingDaySaturday = workSchedule.IsWorkingDaySaturday;
            existingWorkSchedule.FlexTimeAllowanceMinutes = workSchedule.FlexTimeAllowanceMinutes;
            existingWorkSchedule.Description = workSchedule.Description;
            existingWorkSchedule.DepartmentId = workSchedule.DepartmentId;
            
            // Save changes
            await _context.SaveChangesAsync();
            return existingWorkSchedule;
        }
        
        public async Task DeleteAsync(int id)
        {
            var workSchedule = await _context.WorkSchedules.FindAsync(id);
            if (workSchedule != null)
            {
                _context.WorkSchedules.Remove(workSchedule);
                await _context.SaveChangesAsync();
            }
        }
        
        public async Task AssignToEmployeeAsync(int workScheduleId, int employeeId)
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee != null)
            {
                employee.WorkScheduleId = workScheduleId;
                await _context.SaveChangesAsync();
            }
        }
        
        public async Task AssignToDepartmentAsync(int workScheduleId, int departmentId)
        {
            var workSchedule = await _context.WorkSchedules.FindAsync(workScheduleId);
            if (workSchedule != null)
            {
                workSchedule.DepartmentId = departmentId;
                await _context.SaveChangesAsync();
            }
        }
        
        // Method to determine if a specific date is a working day for an employee
        public async Task<bool> IsWorkingDayForEmployeeAsync(int employeeId, DateTime date)
        {
            var schedule = await GetEmployeeWorkScheduleAsync(employeeId);
            return schedule?.IsWorkingDay(date.DayOfWeek) ?? false;
        }
        
        // Method to get expected work hours for an employee on a specific date
        public async Task<double> GetExpectedWorkHoursAsync(int employeeId, DateTime date)
        {
            var schedule = await GetEmployeeWorkScheduleAsync(employeeId);
            return schedule?.CalculateExpectedWorkHours(date) ?? 0;
        }
    }
} 
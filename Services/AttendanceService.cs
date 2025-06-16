using Microsoft.EntityFrameworkCore;
using NewAttendanceProject.Data;
using NewAttendanceProject.Models;

namespace NewAttendanceProject.Services
{
    public class AttendanceService
    {
        private readonly ApplicationDbContext _context;
        
        public AttendanceService(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<List<Attendance>> GetAllAsync()
        {
            return await _context.Attendances
                .Include(a => a.Employee)
                .ThenInclude(e => e.Department)
                .OrderByDescending(a => a.Date)
                .ToListAsync();
        }
        
        public async Task<List<Attendance>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Attendances
                .Include(a => a.Employee)
                .ThenInclude(e => e.Department)
                .Where(a => a.Date >= startDate && a.Date <= endDate)
                .OrderByDescending(a => a.Date)
                .ToListAsync();
        }
        
        public async Task<List<Attendance>> GetByEmployeeAsync(int employeeId)
        {
            return await _context.Attendances
                .Include(a => a.Employee)
                .Where(a => a.EmployeeId == employeeId)
                .OrderByDescending(a => a.Date)
                .ToListAsync();
        }
        
        public async Task<List<Attendance>> GetByEmployeeAndDateRangeAsync(int employeeId, DateTime startDate, DateTime endDate)
        {
            return await _context.Attendances
                .Include(a => a.Employee)
                .Where(a => a.EmployeeId == employeeId && a.Date >= startDate && a.Date <= endDate)
                .OrderByDescending(a => a.Date)
                .ToListAsync();
        }
        
        public async Task<Attendance> GetByIdAsync(int id)
        {
            return await _context.Attendances
                .Include(a => a.Employee)
                .ThenInclude(e => e.Department)
                .FirstOrDefaultAsync(a => a.Id == id);
        }
        
        public async Task<Attendance> GetByEmployeeAndDateAsync(int employeeId, DateTime date)
        {
            return await _context.Attendances
                .FirstOrDefaultAsync(a => 
                    a.EmployeeId == employeeId && 
                    a.Date.Year == date.Year && 
                    a.Date.Month == date.Month && 
                    a.Date.Day == date.Day);
        }
        
        public async Task<Attendance> CheckInAsync(int employeeId)
        {
            var today = DateTime.Today;
            var now = DateTime.Now;
            
            var attendance = await GetByEmployeeAndDateAsync(employeeId, today);
            
            if (attendance == null)
            {
                attendance = new Attendance
                {
                    EmployeeId = employeeId,
                    Date = today,
                    CheckInTime = now,
                    Notes = ""
                };
                
                _context.Attendances.Add(attendance);
            }
            else if (!attendance.CheckInTime.HasValue)
            {
                attendance.CheckInTime = now;
                _context.Entry(attendance).State = EntityState.Modified;
            }
            
            await _context.SaveChangesAsync();
            return attendance;
        }
        
        public async Task<Attendance> CheckOutAsync(int employeeId)
        {
            var today = DateTime.Today;
            var now = DateTime.Now;
            
            var attendance = await GetByEmployeeAndDateAsync(employeeId, today);
            
            if (attendance != null && attendance.CheckInTime.HasValue)
            {
                attendance.CheckOutTime = now;
                _context.Entry(attendance).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            
            return attendance;
        }
        
        public async Task<Attendance> UpdateAsync(Attendance attendance)
        {
            _context.Entry(attendance).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return attendance;
        }
        
        public async Task DeleteAsync(int id)
        {
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance != null)
            {
                _context.Attendances.Remove(attendance);
                await _context.SaveChangesAsync();
            }
        }
    }
} 
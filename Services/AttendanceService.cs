using Microsoft.EntityFrameworkCore;
using NewAttendanceProject.Data;
using NewAttendanceProject.Models;

namespace NewAttendanceProject.Services
{
    public class AttendanceService
    {
        private readonly ApplicationDbContext _context;
        private readonly WorkScheduleService _workScheduleService;
        private readonly WorkCalendarService _workCalendarService;
        
        public AttendanceService(
            ApplicationDbContext context, 
            WorkScheduleService workScheduleService,
            WorkCalendarService workCalendarService)
        {
            _context = context;
            _workScheduleService = workScheduleService;
            _workCalendarService = workCalendarService;
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
        
        // Method to check if an employee is late based on their work schedule
        public async Task<bool> IsLateCheckInAsync(int employeeId, DateTime checkInTime)
        {
            var schedule = await _workScheduleService.GetEmployeeWorkScheduleAsync(employeeId);
            if (schedule == null)
                return false;
                
            // Check if it's a working day according to calendar and schedule
            bool isWorkingDay = await _workCalendarService.IsWorkingDateForEmployeeAsync(
                employeeId, checkInTime.Date, _workScheduleService);
                
            if (!isWorkingDay)
                return false;
                
            // Calculate the expected check-in time for the day
            var expectedCheckInTime = new DateTime(
                checkInTime.Year, 
                checkInTime.Month, 
                checkInTime.Day,
                schedule.StartTime.Hours,
                schedule.StartTime.Minutes,
                0);
                
            // Add grace period (flex time allowance)
            expectedCheckInTime = expectedCheckInTime.AddMinutes(schedule.FlexTimeAllowanceMinutes);
            
            // Compare with actual check-in time
            return checkInTime > expectedCheckInTime;
        }
        
        // Method to check if an employee left early based on their work schedule
        public async Task<bool> IsEarlyCheckOutAsync(int employeeId, DateTime checkOutTime)
        {
            var schedule = await _workScheduleService.GetEmployeeWorkScheduleAsync(employeeId);
            if (schedule == null)
                return false;
                
            // Check if it's a working day according to calendar and schedule
            bool isWorkingDay = await _workCalendarService.IsWorkingDateForEmployeeAsync(
                employeeId, checkOutTime.Date, _workScheduleService);
                
            if (!isWorkingDay)
                return false;
                
            // Calculate the expected check-out time for the day
            var expectedCheckOutTime = new DateTime(
                checkOutTime.Year, 
                checkOutTime.Month, 
                checkOutTime.Day,
                schedule.EndTime.Hours,
                schedule.EndTime.Minutes,
                0);
                
            // Compare with actual check-out time
            return checkOutTime < expectedCheckOutTime;
        }
        
        // Method to calculate attendance statistics for an employee in a date range
        public async Task<AttendanceStatistics> GetAttendanceStatisticsAsync(int employeeId, DateTime startDate, DateTime endDate)
        {
            var statistics = new AttendanceStatistics
            {
                TotalWorkingDays = 0,
                DaysPresent = 0,
                DaysAbsent = 0,
                LateArrivals = 0,
                EarlyDepartures = 0
            };
            
            // Loop through each day in the date range
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                // Check if it's a working day according to calendar and schedule
                bool isWorkingDay = await _workCalendarService.IsWorkingDateForEmployeeAsync(
                    employeeId, date, _workScheduleService);
                    
                if (isWorkingDay)
                {
                    statistics.TotalWorkingDays++;
                    
                    // Get attendance record for this day
                    var attendance = await GetByEmployeeAndDateAsync(employeeId, date);
                    
                    if (attendance != null && attendance.CheckInTime.HasValue)
                    {
                        statistics.DaysPresent++;
                        
                        // Check if employee was late
                        if (await IsLateCheckInAsync(employeeId, attendance.CheckInTime.Value))
                        {
                            statistics.LateArrivals++;
                        }
                        
                        // Check if employee left early
                        if (attendance.CheckOutTime.HasValue && 
                            await IsEarlyCheckOutAsync(employeeId, attendance.CheckOutTime.Value))
                        {
                            statistics.EarlyDepartures++;
                        }
                    }
                    else
                    {
                        statistics.DaysAbsent++;
                    }
                }
            }
            
            return statistics;
        }
    }
    
    // Class to hold attendance statistics
    public class AttendanceStatistics
    {
        public int TotalWorkingDays { get; set; }
        public int DaysPresent { get; set; }
        public int DaysAbsent { get; set; }
        public int LateArrivals { get; set; }
        public int EarlyDepartures { get; set; }
        
        // Calculated properties
        public double AttendancePercentage => TotalWorkingDays > 0 
            ? (double)DaysPresent / TotalWorkingDays * 100 
            : 0;
            
        public double AbsencePercentage => TotalWorkingDays > 0 
            ? (double)DaysAbsent / TotalWorkingDays * 100 
            : 0;
            
        public double PunctualityPercentage => DaysPresent > 0 
            ? (double)(DaysPresent - LateArrivals) / DaysPresent * 100 
            : 0;
    }
} 
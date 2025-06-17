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
                // Find the existing entity to update it
                var existingAttendance = await _context.Attendances.FindAsync(attendance.Id);
                if (existingAttendance != null)
                {
                    existingAttendance.CheckInTime = now;
                }
            }
            
            await _context.SaveChangesAsync();
            
            // Refresh from database to ensure we have the latest version
            if (attendance.Id > 0)
            {
                attendance = await _context.Attendances.FindAsync(attendance.Id);
            }
            
            return attendance;
        }
        
        public async Task<Attendance> CheckOutAsync(int employeeId)
        {
            var today = DateTime.Today;
            var now = DateTime.Now;
            
            var attendance = await GetByEmployeeAndDateAsync(employeeId, today);
            
            if (attendance != null && attendance.CheckInTime.HasValue)
            {
                // Find the existing entity to update it
                var existingAttendance = await _context.Attendances.FindAsync(attendance.Id);
                if (existingAttendance != null)
                {
                    existingAttendance.CheckOutTime = now;
                    
                    // Calculate work duration
                    if (existingAttendance.CheckInTime.HasValue)
                    {
                        existingAttendance.WorkDuration = now - existingAttendance.CheckInTime.Value;
                    }
                    
                    // Update IsComplete flag
                    existingAttendance.IsComplete = true;
                    
                    await _context.SaveChangesAsync();
                    
                    // Refresh from database
                    attendance = await _context.Attendances.FindAsync(attendance.Id);
                }
            }
            
            return attendance;
        }
        
        public async Task<Attendance> UpdateAsync(Attendance attendance)
        {
            try
            {
                // Find the existing entity from the database
                var existingAttendance = await _context.Attendances.FindAsync(attendance.Id);
                
                if (existingAttendance == null)
                {
                    throw new Exception($"Attendance record with ID {attendance.Id} not found.");
                }
                
                // Update properties
                existingAttendance.EmployeeId = attendance.EmployeeId;
                existingAttendance.Date = attendance.Date;
                existingAttendance.CheckInTime = attendance.CheckInTime;
                existingAttendance.CheckOutTime = attendance.CheckOutTime;
                existingAttendance.Notes = attendance.Notes;
                
                // Calculate work duration if both check-in and check-out times are available
                if (existingAttendance.CheckInTime.HasValue && existingAttendance.CheckOutTime.HasValue)
                {
                    existingAttendance.WorkDuration = existingAttendance.CheckOutTime.Value - 
                                                    existingAttendance.CheckInTime.Value;
                }
                else
                {
                    existingAttendance.WorkDuration = null;
                }
                
                // Update IsComplete flag
                existingAttendance.IsComplete = existingAttendance.CheckInTime.HasValue && 
                                              existingAttendance.CheckOutTime.HasValue;
                
                // Save changes
                await _context.SaveChangesAsync();
                
                return existingAttendance;
            }
            catch (Exception)
            {
                // If there's still an issue, try detaching and then updating
                _context.ChangeTracker.Clear();
                
                _context.Attendances.Update(attendance);
                await _context.SaveChangesAsync();
                
                return attendance;
            }
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
            // Check if it's a working day according to calendar and schedule
            bool isWorkingDay = await _workCalendarService.IsWorkingDateForEmployeeAsync(
                employeeId, checkInTime.Date, _workScheduleService);
                
            if (!isWorkingDay)
                return false;
                
            // Get allowed attendance times with grace periods applied
            var (latestAllowedCheckIn, _) = await _workScheduleService.GetAllowedAttendanceTimesAsync(employeeId, checkInTime.Date);
            
            if (!latestAllowedCheckIn.HasValue)
                return false;
                
            // Compare with actual check-in time
            return checkInTime > latestAllowedCheckIn.Value;
        }
        
        // Method to check if an employee left early based on their work schedule
        public async Task<bool> IsEarlyCheckOutAsync(int employeeId, DateTime checkOutTime)
        {
            // Check if it's a working day according to calendar and schedule
            bool isWorkingDay = await _workCalendarService.IsWorkingDateForEmployeeAsync(
                employeeId, checkOutTime.Date, _workScheduleService);
                
            if (!isWorkingDay)
                return false;
                
            // Get allowed attendance times with grace periods applied
            var (_, earliestAllowedCheckOut) = await _workScheduleService.GetAllowedAttendanceTimesAsync(employeeId, checkOutTime.Date);
            
            if (!earliestAllowedCheckOut.HasValue)
                return false;
                
            // Compare with actual check-out time
            return checkOutTime < earliestAllowedCheckOut.Value;
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
        
        // Method to update WorkDuration and IsComplete fields for all attendance records
        public async Task UpdateAllWorkDurationsAndCompleteStatus()
        {
            // Get all attendance records
            var allAttendances = await _context.Attendances.ToListAsync();
            
            foreach (var attendance in allAttendances)
            {
                // Calculate WorkDuration if both check-in and check-out times are available
                if (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue)
                {
                    attendance.WorkDuration = attendance.CheckOutTime.Value - attendance.CheckInTime.Value;
                    attendance.IsComplete = true;
                }
                else
                {
                    attendance.WorkDuration = null;
                    attendance.IsComplete = attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue;
                }
            }
            
            // Save changes to the database
            await _context.SaveChangesAsync();
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
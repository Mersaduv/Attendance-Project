using Microsoft.EntityFrameworkCore;
using NewAttendanceProject.Data;
using NewAttendanceProject.Models;

namespace NewAttendanceProject.Services
{
    public class ReportService
    {
        private readonly ApplicationDbContext _context;
        
        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<List<AttendanceReportItem>> GenerateEmployeeAttendanceReportAsync(
            int employeeId, DateTime startDate, DateTime endDate)
        {
            var attendances = await _context.Attendances
                .Include(a => a.Employee)
                .Where(a => a.EmployeeId == employeeId && a.Date >= startDate && a.Date <= endDate)
                .OrderBy(a => a.Date)
                .ToListAsync();
                
            var report = new List<AttendanceReportItem>();
            
            foreach (var attendance in attendances)
            {
                report.Add(new AttendanceReportItem
                {
                    Date = attendance.Date,
                    EmployeeName = attendance.Employee.FullName,
                    CheckInTime = attendance.CheckInTime,
                    CheckOutTime = attendance.CheckOutTime,
                    WorkDuration = attendance.WorkDuration
                });
            }
            
            // Fill in missing dates in the range
            var currentDate = startDate;
            while (currentDate <= endDate)
            {
                if (!report.Any(r => r.Date.Date == currentDate.Date))
                {
                    report.Add(new AttendanceReportItem
                    {
                        Date = currentDate,
                        EmployeeName = (await _context.Employees.FindAsync(employeeId))?.FullName,
                        CheckInTime = null,
                        CheckOutTime = null,
                        WorkDuration = null,
                        Status = "Absent"
                    });
                }
                currentDate = currentDate.AddDays(1);
            }
            
            return report.OrderBy(r => r.Date).ToList();
        }
        
        public async Task<List<AttendanceReportItem>> GenerateDepartmentAttendanceReportAsync(
            int departmentId, DateTime startDate, DateTime endDate)
        {
            var employees = await _context.Employees
                .Where(e => e.DepartmentId == departmentId)
                .ToListAsync();
                
            var report = new List<AttendanceReportItem>();
            
            foreach (var employee in employees)
            {
                var employeeReport = await GenerateEmployeeAttendanceReportAsync(
                    employee.Id, startDate, endDate);
                report.AddRange(employeeReport);
            }
            
            return report.OrderBy(r => r.Date).ThenBy(r => r.EmployeeName).ToList();
        }
        
        public async Task<List<AttendanceReportItem>> GenerateCompanyAttendanceReportAsync(
            DateTime startDate, DateTime endDate)
        {
            var attendances = await _context.Attendances
                .Include(a => a.Employee)
                .Where(a => a.Date >= startDate && a.Date <= endDate)
                .OrderBy(a => a.Date)
                .ToListAsync();
                
            var report = new List<AttendanceReportItem>();
            
            foreach (var attendance in attendances)
            {
                report.Add(new AttendanceReportItem
                {
                    Date = attendance.Date,
                    EmployeeId = attendance.EmployeeId,
                    EmployeeName = attendance.Employee.FullName,
                    DepartmentName = attendance.Employee.Department?.Name,
                    CheckInTime = attendance.CheckInTime,
                    CheckOutTime = attendance.CheckOutTime,
                    WorkDuration = attendance.WorkDuration,
                    Status = attendance.IsComplete ? "Present" : "Incomplete"
                });
            }
            
            return report;
        }
    }
    
    public class AttendanceReportItem
    {
        public DateTime Date { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string DepartmentName { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public TimeSpan? WorkDuration { get; set; }
        public string Status { get; set; } = "Present";
    }
} 
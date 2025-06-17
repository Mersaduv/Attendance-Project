using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewAttendanceProject.Models
{
    public class WorkSchedule
    {
        [Key]
        public int Id { get; set; }
        
        [Required, StringLength(100)]
        public string Name { get; set; }
        
        [Required]
        public TimeSpan StartTime { get; set; }
        
        [Required]
        public TimeSpan EndTime { get; set; }
        
        // Working days (0 = Sunday, 1 = Monday, ..., 6 = Saturday)
        public bool IsWorkingDaySunday { get; set; } = false;
        public bool IsWorkingDayMonday { get; set; } = true;
        public bool IsWorkingDayTuesday { get; set; } = true;
        public bool IsWorkingDayWednesday { get; set; } = true;
        public bool IsWorkingDayThursday { get; set; } = true;
        public bool IsWorkingDayFriday { get; set; } = true;
        public bool IsWorkingDaySaturday { get; set; } = false;
        
        [StringLength(500)]
        public string Description { get; set; } = "";
        
        // Flexible time allowance in minutes (grace period for late check-ins)
        public int FlexTimeAllowanceMinutes { get; set; } = 15;
        
        // Department assignment (null if schedule applies to specific employees)
        public int? DepartmentId { get; set; }
        
        [ForeignKey("DepartmentId")]
        public Department Department { get; set; }
        
        // Navigation property for employees assigned to this schedule
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
        
        // Methods to check if a specific day is a working day
        public bool IsWorkingDay(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Sunday => IsWorkingDaySunday,
                DayOfWeek.Monday => IsWorkingDayMonday,
                DayOfWeek.Tuesday => IsWorkingDayTuesday,
                DayOfWeek.Wednesday => IsWorkingDayWednesday,
                DayOfWeek.Thursday => IsWorkingDayThursday,
                DayOfWeek.Friday => IsWorkingDayFriday,
                DayOfWeek.Saturday => IsWorkingDaySaturday,
                _ => false
            };
        }
        
        // Calculate expected work hours for a given day
        public double CalculateExpectedWorkHours(DateTime date)
        {
            if (!IsWorkingDay(date.DayOfWeek))
            {
                return 0;
            }
            
            return (EndTime - StartTime).TotalHours;
        }
    }
} 
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewAttendanceProject.Models
{
    public class Employee
    {
        [Key]
        public int Id { get; set; }
        
        [Required(ErrorMessage = "First Name is required"), StringLength(50)]
        public string FirstName { get; set; }
        
        [Required(ErrorMessage = "Last Name is required"), StringLength(50)]
        public string LastName { get; set; }
        
        [Required(ErrorMessage = "Email is required"), StringLength(100), EmailAddress]
        public string Email { get; set; }
        
        [Required(ErrorMessage = "Phone Number is required"), StringLength(20)]
        public string PhoneNumber { get; set; }
        
        [Required(ErrorMessage = "Department is required")]
        public int DepartmentId { get; set; }
        
        [ForeignKey("DepartmentId")]
        public Department Department { get; set; }
        
        // Work Schedule assignment (can be null to use department's default schedule)
        public int? WorkScheduleId { get; set; }
        
        [ForeignKey("WorkScheduleId")]
        public WorkSchedule WorkSchedule { get; set; }
        
        [Required(ErrorMessage = "Employee Code is required"), StringLength(20)]
        public string EmployeeCode { get; set; }
        
        [Required(ErrorMessage = "Position is required"), StringLength(100)]
        public string Position { get; set; }
        
        [Required(ErrorMessage = "Hire Date is required")]
        public DateTime HireDate { get; set; }
        
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
} 
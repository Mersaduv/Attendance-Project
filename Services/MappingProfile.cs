using AutoMapper;
using NewAttendanceProject.Models;
using NewAttendanceProject.Services;

namespace NewAttendanceProject.Services
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Create basic mapping configurations for the models
            CreateMap<Department, Department>();
            CreateMap<Employee, Employee>();
            CreateMap<Attendance, Attendance>();
            
            // Map Attendance to AttendanceReportItem
            CreateMap<Attendance, AttendanceReportItem>()
                .ForMember(dest => dest.EmployeeId, opt => opt.MapFrom(src => src.EmployeeId))
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.Employee.FullName))
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Employee.Department.Name))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.IsComplete ? "Present" : "Incomplete"));
        }
    }
} 
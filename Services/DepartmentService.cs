using Microsoft.EntityFrameworkCore;
using NewAttendanceProject.Data;
using NewAttendanceProject.Models;

namespace NewAttendanceProject.Services
{
    public class DepartmentService
    {
        private readonly ApplicationDbContext _context;
        
        public DepartmentService(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<List<Department>> GetAllAsync()
        {
            return await _context.Departments.OrderBy(d => d.Name).ToListAsync();
        }
        
        public async Task<Department> GetByIdAsync(int id)
        {
            return await _context.Departments.FindAsync(id);
        }
        
        public async Task<Department> CreateAsync(Department department)
        {
            _context.Departments.Add(department);
            await _context.SaveChangesAsync();
            return department;
        }
        
        public async Task<Department> UpdateAsync(Department department)
        {
            _context.Entry(department).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return department;
        }
        
        public async Task DeleteAsync(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department != null)
            {
                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();
            }
        }
    }
} 
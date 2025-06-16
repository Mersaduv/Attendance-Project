using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using NewAttendanceProject.Areas.Identity;
using NewAttendanceProject.Data;
using NewAttendanceProject.Services;
using NewAttendanceProject.Areas.Identity.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();

// Register application services
builder.Services.AddScoped<DepartmentService>();
builder.Services.AddScoped<EmployeeService>();
builder.Services.AddScoped<AttendanceService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<IPrintingService, PrintingService>();

// Configure AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();;

app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Initialize database and create default admin user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    
    // Apply migrations
    context.Database.Migrate();
    
    // Create default admin user if it doesn't exist
    await CreateDefaultAdminUser(userManager, roleManager);
}

app.Run();

// Method to create default admin user
async Task CreateDefaultAdminUser(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
{
    const string adminEmail = "admin@ghalibhr.com";
    const string adminPassword = "Admin123!";
    const string adminRoleName = "Admin";
    
    // Create Admin role if it doesn't exist
    if (!await roleManager.RoleExistsAsync(adminRoleName))
    {
        await roleManager.CreateAsync(new IdentityRole(adminRoleName));
        Console.WriteLine($"Created '{adminRoleName}' role");
    }
    
    // Check if the admin user already exists
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    
    if (adminUser == null)
    {
        // Create a new admin user
        adminUser = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true // Auto-confirm the email for the admin
        };
        
        // Create the user with the specified password
        var result = await userManager.CreateAsync(adminUser, adminPassword);
        
        if (result.Succeeded)
        {
            // Add the user to the Admin role
            await userManager.AddToRoleAsync(adminUser, adminRoleName);
            Console.WriteLine($"Default admin user created and added to '{adminRoleName}' role: {adminEmail}");
        }
        else
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            Console.WriteLine($"Failed to create admin user: {errors}");
        }
    }
    else
    {
        // Check if user is in admin role
        if (!await userManager.IsInRoleAsync(adminUser, adminRoleName))
        {
            // Add to admin role if not already
            await userManager.AddToRoleAsync(adminUser, adminRoleName);
            Console.WriteLine($"Existing user {adminEmail} added to '{adminRoleName}' role");
        }
        else
        {
            Console.WriteLine("Admin user already exists and is in Admin role");
        }
    }
}

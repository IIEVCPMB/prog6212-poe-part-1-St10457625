using Microsoft.EntityFrameworkCore;
using PROG_POE_Part_1.Data;
using PROG_POE_Part_1.Services;

namespace PROG_POE_Part_1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Register DbContext
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Register services for DI
            builder.Services.AddScoped<FileEncryptionService>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<ClaimService>();

            // Add MVC support
            builder.Services.AddControllersWithViews();

            // Add session support
            builder.Services.AddSession();
            builder.Services.AddHttpContextAccessor();

            var app = builder.Build();

            // Configure middleware
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();

            app.UseAuthorization();

            // Map default controller route
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}

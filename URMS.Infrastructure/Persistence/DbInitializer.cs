using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using URMS.Domain.Constants;
using URMS.Domain.Entities;

namespace URMS.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        // ─── 1. Apply Pending Migrations ───
        if ((await context.Database.GetPendingMigrationsAsync()).Any())
        {
            await context.Database.MigrateAsync();
        }

        // ─── 2. Seed Roles ───
        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // ─── 3. Seed Default Permissions per Role ───
        await SeedRolePermissionsAsync(roleManager);

        // ─── 4. Seed Default SuperAdmin User ───
        var superAdminEmail = "admin@urms.edu.eg";
        var superAdminUser = await userManager.FindByEmailAsync(superAdminEmail);

        if (superAdminUser is null)
        {
            superAdminUser = new ApplicationUser
            {
                UserName = superAdminEmail,
                Email = superAdminEmail,
                FirstNameAr = "مدير",
                LastNameAr = "النظام",
                FirstNameEn = "Super",
                LastNameEn = "Admin",
                UserType = URMS.Domain.Enums.UserType.SuperAdmin,
                IsApproved = true,
                IsActive = true,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(superAdminUser, "SuperAdmin@123456");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(superAdminUser, AppRoles.SuperAdmin);
            }
        }
    }

    private static async Task SeedRolePermissionsAsync(RoleManager<IdentityRole> roleManager)
    {
        // SuperAdmin gets all permissions
        var superAdminRole = await roleManager.FindByNameAsync(AppRoles.SuperAdmin);
        if (superAdminRole is not null)
        {
            var existingClaims = await roleManager.GetClaimsAsync(superAdminRole);
            var allPermissions = Permissions.GetAllPermissions();

            foreach (var permission in allPermissions)
            {
                if (!existingClaims.Any(c => c.Type == "Permission" && c.Value == permission))
                {
                    await roleManager.AddClaimAsync(superAdminRole, new Claim("Permission", permission));
                }
            }
        }

        // Student Permissions
        var studentRole = await roleManager.FindByNameAsync(AppRoles.Student);
        if (studentRole is not null)
        {
            var existingClaims = await roleManager.GetClaimsAsync(studentRole);
            var studentPermissions = new[] { Permissions.Requests.ViewOwn, Permissions.Requests.Create };

            foreach (var permission in studentPermissions)
            {
                if (!existingClaims.Any(c => c.Type == "Permission" && c.Value == permission))
                {
                    await roleManager.AddClaimAsync(studentRole, new Claim("Permission", permission));
                }
            }
        }

        // AcademicAdvisor Permissions
        var advisorRole = await roleManager.FindByNameAsync(AppRoles.AcademicAdvisor);
        if (advisorRole is not null)
        {
            var existingClaims = await roleManager.GetClaimsAsync(advisorRole);
            var advisorPermissions = new[]
            {
                Permissions.Requests.View,
                Permissions.Requests.ApproveAdvisor,
                Permissions.Requests.Reject,
                Permissions.Advisors.RequestAvailabilityChange
            };

            foreach (var permission in advisorPermissions)
            {
                if (!existingClaims.Any(c => c.Type == "Permission" && c.Value == permission))
                {
                    await roleManager.AddClaimAsync(advisorRole, new Claim("Permission", permission));
                }
            }
        }

        // CollegeSecretary Permissions
        var secretaryRole = await roleManager.FindByNameAsync(AppRoles.CollegeSecretary);
        if (secretaryRole is not null)
        {
            var existingClaims = await roleManager.GetClaimsAsync(secretaryRole);
            var secretaryPermissions = new[]
            {
                Permissions.Users.View,
                Permissions.Users.ApproveRegistration,
                Permissions.Requests.View,
                Permissions.Requests.ConfirmStaff,
                Permissions.Requests.ProcessPayment,
                Permissions.Advisors.ImportExcel,
                Permissions.Advisors.ApproveAvailabilityChange
            };

            foreach (var permission in secretaryPermissions)
            {
                if (!existingClaims.Any(c => c.Type == "Permission" && c.Value == permission))
                {
                    await roleManager.AddClaimAsync(secretaryRole, new Claim("Permission", permission));
                }
            }
        }
    }
}

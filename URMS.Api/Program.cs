using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi;
using URMS.Domain.Entities;
using URMS.Infrastructure;
using URMS.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ─── 1. Add Infrastructure Services (DbContext, Identity, Cookies, Permissions) ───
builder.Services.AddInfrastructureServices(builder.Configuration);

// ─── 2. Add Session ───
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ─── 3. Add CORS (with Credentials support for Cookies) ───
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ─── 4. Add Controllers & Swagger / OpenAPI ───
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "URMS API",
        Version = "v1",
        Description = "University Request Management System API"
    });
});

var app = builder.Build();

// ─── 5. Seed Database Roles, Permissions, and Default SuperAdmin ───
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await DbInitializer.SeedAsync(context, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// ─── 6. Configure HTTP Middleware Pipeline ───
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "URMS API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("DefaultPolicy");

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi;
using URMS.Api.Middleware;
using URMS.Application;
using URMS.Domain.Entities;
using URMS.Infrastructure;
using URMS.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ─── 1. Add Infrastructure & Application Services (Mapster, FluentValidation, DbContext, Identity) ───
builder.Services.AddApplicationServices();
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
        policy.WithOrigins(
                  "http://localhost:3000",
                  "http://localhost:5174",
                  "https://urms-lake.vercel.app",
                  "http://urms-lake.vercel.app"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ─── 4. Add Controllers & Swagger / OpenAPI ───
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
})
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
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

// ─── 5. Configure HTTP Middleware Pipeline ───
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "URMS API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseExceptionHandler();
app.UseCors("DefaultPolicy");

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

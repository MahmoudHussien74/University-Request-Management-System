using URMS.Api;
using URMS.Application;
using URMS.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ─── 1. Register Services (Application, Infrastructure, API) ───
builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration)
    .AddApiServices(builder.Configuration);

var app = builder.Build();

// ─── 2. Configure HTTP Middleware Pipeline ───
app.UseRequestLocalization();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "URMS API v1");
    c.RoutePrefix = "swagger";
    c.DisplayRequestDuration();
    c.EnablePersistAuthorization();
});

app.UseHttpsRedirection();
app.UseExceptionHandler();
app.UseCors("DefaultPolicy");

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

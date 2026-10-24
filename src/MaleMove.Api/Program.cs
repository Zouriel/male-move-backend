using MaleMove.Api;
using MaleMove.Api.Middleware;
using MaleMove.Infrastructure;
using MaleMove.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMaleMoveInfrastructure(builder.Configuration);
builder.Services.AddMaleMoveApi(builder.Configuration, builder.Environment);

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

if (!app.Environment.IsDevelopment()) app.UseHsts();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapControllers().RequireRateLimiting("api");

app.Run();


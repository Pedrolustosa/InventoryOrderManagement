using InventoryOrderManagement.Api.ExceptionHandling;
using InventoryOrderManagement.Application.DependencyInjection;
using InventoryOrderManagement.Infrastructure.DependencyInjection;
using InventoryOrderManagement.Infrastructure.Persistence;
using InventoryOrderManagement.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDev", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory Order Management API v1");
        options.RoutePrefix = "swagger";
    });

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    dbContext.Database.Migrate();
    await DevelopmentDataSeeder.SeedAsync(dbContext);
}

app.UseExceptionHandler();
app.UseCors("AngularDev");

// Keep HTTP available for the Angular app in Development (avoids 307 → HTTPS self-signed cert failures).
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.MapControllers();
app.Run();

using FirstApi.AOP;
using FirstApi.Models;
using FirstApi.Services;
using FirstApi.Workers;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Register performance action filter and configure controllers to use it globally.
builder.Services.AddScoped<PerformanceActionFilter>();
builder.Services.AddControllers(options => options.Filters.Add<PerformanceActionFilter>());
builder.Services.AddSingleton<InMemoryStore>();
builder.Services.AddSingleton<OrderService>();
builder.Services.AddSingleton<CapacityControlService>();
builder.Services.AddSingleton<BackgroundTaskQueue>();
builder.Services.AddHostedService<OrderProcessingWorker>();
builder.Services.AddHostedService<DailySalesBatchJob>();
builder.Services.AddSingleton<LoadBalancerService>();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379"; 
    options.InstanceName = "SampleStore_";
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreConnection")));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Use performance monitoring middleware early in the pipeline to measure request duration.
app.UseMiddleware<PerformanceMiddleware>();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

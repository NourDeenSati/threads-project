using FirstApi.Services;

var builder = WebApplication.CreateBuilder(args);
var defaultMaxConcurrency = builder.Configuration.GetValue("ThreadLimiter:DefaultMaxConcurrency", 5);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSingleton<InMemoryStore>();
builder.Services.AddSingleton<StoreService>();
builder.Services.AddSingleton<OrderService>();
builder.Services.AddSingleton(sp =>
    new ThreadLimiter(
        defaultMaxConcurrency,
        sp.GetRequiredService<ILogger<ThreadLimiter>>()));
builder.Services.AddSingleton<SimulationService>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// app.UseHttpsRedirection(); // علّقناه مؤقتًا


app.MapControllers();

// أول endpoint بسيط

app.Run();

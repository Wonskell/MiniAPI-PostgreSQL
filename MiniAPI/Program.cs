using MiniAPI;
using MiniAPI.Endpoints;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("MiniApi")
    ?? throw new InvalidOperationException("Задайте ConnectionStrings:MiniApi через dotnet user-secrets или переменную окружения. См. README.md.");
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
builder.Services.AddSingleton<UserManager>();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.SwaggerDoc("v1", new()
{
    Title = "MiniAPI", Version = "v1", Description = "Пользователи и отделы"
}));

var app = builder.Build();
var manager = app.Services.GetRequiredService<UserManager>();
await manager.InitializeAsync();

if (builder.Configuration["import-json"] is { } importPath)
{
    var count = await manager.ImportJsonAsync(importPath);
    app.Logger.LogInformation("Импортировано записей: {Count}", count);
    await app.DisposeAsync();
    return;
}

app.UseExceptionHandler();
app.UseStatusCodePages();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapUserEndpoints();
app.Run();

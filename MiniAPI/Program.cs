using MiniAPI;
using MiniAPI.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Добавляем UserManager
builder.Services.AddSingleton<UserManager>();

// Swagger для тестирования API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Mini API",
        Version = "v1",
        Description = "Простой API для работы с пользователями"
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapUserEndpoints();

app.Run();

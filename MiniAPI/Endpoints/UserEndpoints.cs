using MiniAPI.DTOs;
using Npgsql;

namespace MiniAPI.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var users = app.MapGroup("/user").WithTags("Users");
        users.MapGet("/", (string? name) => Results.Ok(new UserResponse
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Гость" : name.Trim(),
            Message = $"Привет, {(string.IsNullOrWhiteSpace(name) ? "Гость" : name.Trim())}!"
        }));

        users.MapPost("/", async (CreateUserRequest request, UserManager manager, CancellationToken ct) =>
        {
            try
            {
                var user = await manager.AddUserAsync(request, ct);
                return Results.Created($"/user/{user.Id}", user);
            }
            catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
            { return Results.BadRequest(new { error = "Выбранный отдел не существует." }); }
        });

        users.MapGet("/all", async (UserManager manager, CancellationToken ct) => Results.Ok(await manager.GetAllAsync(ct)));
        users.MapGet("/sorted", async (UserManager manager, CancellationToken ct) => Results.Ok(await manager.GetSortedAsync(ct)));
        users.MapGet("/filter", async (int? minAge, int? maxAge, UserManager manager, CancellationToken ct) =>
        {
            var min = minAge ?? 0;
            var max = maxAge ?? 150;
            if (min < 0 || max > 150 || min > max)
                return Results.BadRequest(new { error = "Задайте диапазон от 0 до 150; нижняя граница не должна превышать верхнюю." });
            return Results.Ok(await manager.GetFilteredAsync(min, max, ct));
        });

        users.MapGet("/search", async (string name, UserManager manager, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100)
                return Results.BadRequest(new { error = "Укажите имя длиной от 1 до 100 символов." });
            var user = await manager.FindByNameAsync(name, ct);
            return user is null ? Results.NotFound(new { error = "Пользователь не найден." }) : Results.Ok(user);
        });
        users.MapGet("/{id:guid}", async (Guid id, UserManager manager, CancellationToken ct) =>
        {
            var user = await manager.FindByIdAsync(id, ct);
            return user is null ? Results.NotFound(new { error = "Пользователь не найден." }) : Results.Ok(user);
        });
        users.MapDelete("/{id:guid}", async (Guid id, UserManager manager, CancellationToken ct) =>
            await manager.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound(new { error = "Пользователь не найден." }));
        users.MapGet("/stats", async (UserManager manager, CancellationToken ct) => Results.Ok(await manager.GetStatisticsAsync(ct)));
        users.MapGet("/recent", async (int? count, UserManager manager, CancellationToken ct) =>
        {
            var limit = count ?? 5;
            if (limit is < 1 or > 100)
                return Results.BadRequest(new { error = "Количество должно быть от 1 до 100." });
            return Results.Ok(await manager.GetRecentAsync(limit, ct));
        });
        app.MapGet("/departments", async (UserManager manager, CancellationToken ct) => Results.Ok(await manager.GetDepartmentsAsync(ct)))
            .WithTags("Departments");
        app.MapGet("/departments/stats", async (UserManager manager, CancellationToken ct) => Results.Ok(await manager.GetDepartmentStatisticsAsync(ct)))
            .WithTags("Departments");
        app.MapGet("/old", () => Results.Redirect("/user"));
        app.MapGet("/download", () => Results.Redirect("/download.html"));
    }
}

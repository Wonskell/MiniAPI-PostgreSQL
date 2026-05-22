using System.Text.Json;
using MiniAPI.Models;

namespace MiniAPI.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var userGroup = app.MapGroup("/user").WithTags("Users");

        userGroup.MapGet("/", (string? name, UserManager manager) =>
        {
            var response = manager.GetGreeting(name);
            return Results.Json(response);
        });

        userGroup.MapPost("/", async (HttpContext ctx, UserManager manager) =>
        {
            if (!ctx.Request.HasJsonContentType())
                return Results.BadRequest(new { error = "Expected JSON" });

            try
            {
                var data = await ctx.Request.ReadFromJsonAsync<User>();
                
                if (data == null || string.IsNullOrWhiteSpace(data.Name))
                    return Results.BadRequest(new { error = "Name is required" });

                var user = manager.AddUser(data.Name, data.Age);
                return Results.Json(new
                {
                    name = user.Name,
                    age = user.Age,
                    received = user.Name
                });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (JsonException)
            {
                return Results.BadRequest(new { error = "Invalid JSON" });
            }
        });

        userGroup.MapGet("/all", (UserManager manager) =>
        {
            var allUsers = manager.GetAllUsers();
            return Results.Json(allUsers);
        });

        userGroup.MapGet("/sorted", (UserManager manager) =>
        {
            var sortedUsers = manager.GetUsersSortedByAge();
            return Results.Json(sortedUsers);
        });

        userGroup.MapGet("/filter", (int? minAge, int? maxAge, UserManager manager) =>
        {
            var min = minAge ?? 0;
            var max = maxAge ?? 150;
            var filtered = manager.GetUsersByAgeRange(min, max);
            return Results.Json(filtered);
        });

        userGroup.MapGet("/search", (string name, UserManager manager) =>
        {
            var user = manager.FindUserByName(name);
            if (user == null)
                return Results.NotFound(new { error = "User not found" });
            
            return Results.Json(user);
        });

        userGroup.MapGet("/{id:guid}", (Guid id, UserManager manager) =>
        {
            var user = manager.FindUserById(id);
            if (user == null)
                return Results.NotFound(new { error = "User not found" });
            
            return Results.Json(user);
        });

        userGroup.MapDelete("/{id:guid}", (Guid id, UserManager manager) =>
        {
            var deleted = manager.DeleteUser(id);
            if (!deleted)
                return Results.NotFound(new { error = "User not found" });
            
            return Results.Ok(new { message = "User deleted", id });
        });

        userGroup.MapGet("/stats", (UserManager manager) =>
        {
            var total = manager.GetTotalUsers();
            var avgAge = manager.GetAverageAge();
            var oldest = manager.GetOldestUser();
            var youngest = manager.GetYoungestUser();
            
            return Results.Json(new
            {
                totalUsers = total,
                averageAge = Math.Round(avgAge, 1),
                oldestUser = oldest?.Name,
                youngestUser = youngest?.Name
            });
        });

        userGroup.MapGet("/recent", (int? count, UserManager manager) =>
        {
            var limit = count ?? 5;
            var recentUsers = manager.GetRecentUsers(limit);
            return Results.Json(recentUsers);
        });

        app.MapGet("/old", () => Results.Redirect("/user"));
        app.MapGet("/download", () => Results.Redirect("/download.html"));
    }
}

using System.Text.Json;
using MiniAPI.DTOs;
using MiniAPI.Models;
using Npgsql;
using NpgsqlTypes;

namespace MiniAPI;

public sealed class UserManager(NpgsqlDataSource dataSource)
{
    private const string SelectUsers = """
        SELECT u.id, u.name, u.age, u.department_id, d.name, u.created_at
        FROM users u LEFT JOIN departments d ON d.id = u.department_id
        """;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var stream = typeof(UserManager).Assembly.GetManifestResourceStream("MiniAPI.Data.schema.sql")
            ?? throw new InvalidOperationException("Не найдена схема базы данных.");
        using var reader = new StreamReader(stream);
        var sql = await reader.ReadToEndAsync(ct);
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await command.ExecuteNonQueryAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public static void Validate(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100)
            throw new ArgumentException("Имя должно содержать от 1 до 100 символов.");
        if (request.Age is null or < 0 or > 150)
            throw new ArgumentException("Укажите возраст от 0 до 150.");
        if (request.DepartmentId is <= 0)
            throw new ArgumentException("Некорректный отдел.");
    }

    public async Task<User> AddUserAsync(CreateUserRequest request, CancellationToken ct)
    {
        Validate(request);
        await using var command = dataSource.CreateCommand("""
            WITH inserted AS (
                INSERT INTO users(id, name, age, department_id)
                VALUES ($1, $2, $3, $4) RETURNING *
            )
            SELECT u.id, u.name, u.age, u.department_id, d.name, u.created_at
            FROM inserted u LEFT JOIN departments d ON d.id = u.department_id
            """);
        command.Parameters.AddWithValue(Guid.NewGuid());
        command.Parameters.AddWithValue(request.Name!.Trim());
        command.Parameters.AddWithValue(request.Age!.Value);
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer, Value = (object?)request.DepartmentId ?? DBNull.Value });
        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        return ReadUser(reader);
    }

    public Task<List<User>> GetAllAsync(CancellationToken ct) =>
        QueryUsersAsync(SelectUsers + " ORDER BY u.created_at DESC, u.id", ct);

    public Task<List<User>> GetSortedAsync(CancellationToken ct) =>
        QueryUsersAsync(SelectUsers + " ORDER BY u.age, u.name, u.id", ct);

    public Task<List<User>> GetFilteredAsync(int min, int max, CancellationToken ct) =>
        QueryUsersAsync(SelectUsers + " WHERE u.age BETWEEN $1 AND $2 ORDER BY u.age, u.id", ct, min, max);

    public Task<List<User>> GetRecentAsync(int count, CancellationToken ct) =>
        QueryUsersAsync(SelectUsers + " ORDER BY u.created_at DESC, u.id LIMIT $1", ct, count);

    public async Task<User?> FindByIdAsync(Guid id, CancellationToken ct) =>
        (await QueryUsersAsync(SelectUsers + " WHERE u.id = $1", ct, id)).FirstOrDefault();

    public async Task<User?> FindByNameAsync(string name, CancellationToken ct) =>
        (await QueryUsersAsync(SelectUsers + " WHERE lower(u.name) = lower($1) ORDER BY u.created_at, u.id LIMIT 1", ct, name.Trim())).FirstOrDefault();

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand("DELETE FROM users WHERE id = $1");
        command.Parameters.AddWithValue(id);
        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<UserStatistics> GetStatisticsAsync(CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand("""
            SELECT count(*), coalesce(round(avg(age), 1), 0),
                (SELECT name FROM users ORDER BY age DESC, created_at, id LIMIT 1),
                (SELECT name FROM users ORDER BY age, created_at, id LIMIT 1)
            FROM users
            """);
        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        return new(reader.GetInt64(0), reader.GetDecimal(1),
            reader.IsDBNull(2) ? null : reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3));
    }

    public async Task<List<DepartmentResponse>> GetDepartmentsAsync(CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand("SELECT id, name FROM departments ORDER BY name");
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<DepartmentResponse>();
        while (await reader.ReadAsync(ct)) result.Add(new(reader.GetInt32(0), reader.GetString(1)));
        return result;
    }

    public async Task<List<DepartmentStatistics>> GetDepartmentStatisticsAsync(CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand("""
            SELECT d.id, d.name, count(u.id), round(avg(u.age), 1)
            FROM departments d LEFT JOIN users u ON u.department_id = d.id
            GROUP BY d.id, d.name ORDER BY d.name
            """);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<DepartmentStatistics>();
        while (await reader.ReadAsync(ct)) result.Add(new(reader.GetInt32(0), reader.GetString(1),
            reader.GetInt64(2), reader.IsDBNull(3) ? null : reader.GetDecimal(3)));
        return result;
    }

    public async Task<int> ImportJsonAsync(string path, CancellationToken ct = default)
    {
        await using var file = File.OpenRead(path);
        var users = await JsonSerializer.DeserializeAsync<List<ImportUserRequest?>>(file,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct)
            ?? throw new ArgumentException("Файл должен содержать массив пользователей.");
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var imported = 0;
        foreach (var user in users)
        {
            if (user is null || user.Id is null || user.Id == Guid.Empty || user.CreatedAt is null || user.CreatedAt == default(DateTime))
                throw new ArgumentException("У каждой записи должны быть id и createdAt.");
            Validate(new(user.Name, user.Age, user.DepartmentId));
            await using var command = new NpgsqlCommand("""
                INSERT INTO users(id, name, age, department_id, created_at)
                VALUES ($1, $2, $3, $4, $5) ON CONFLICT (id) DO NOTHING
                """, connection, transaction);
            command.Parameters.AddWithValue(user.Id.Value);
            command.Parameters.AddWithValue(user.Name!.Trim());
            command.Parameters.AddWithValue(user.Age!.Value);
            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer, Value = (object?)user.DepartmentId ?? DBNull.Value });
            command.Parameters.AddWithValue(user.CreatedAt.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(user.CreatedAt.Value, DateTimeKind.Utc) : user.CreatedAt.Value.ToUniversalTime());
            imported += await command.ExecuteNonQueryAsync(ct);
        }
        await transaction.CommitAsync(ct);
        return imported;
    }

    private async Task<List<User>> QueryUsersAsync(string sql, CancellationToken ct, params object[] parameters)
    {
        await using var command = dataSource.CreateCommand(sql);
        foreach (var value in parameters) command.Parameters.AddWithValue(value);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var users = new List<User>();
        while (await reader.ReadAsync(ct)) users.Add(ReadUser(reader));
        return users;
    }

    private static User ReadUser(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetGuid(0), Name = reader.GetString(1), Age = reader.GetInt32(2),
        DepartmentId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
        DepartmentName = reader.IsDBNull(4) ? null : reader.GetString(4), CreatedAt = reader.GetDateTime(5)
    };
}

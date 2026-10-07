namespace MiniAPI.DTOs;

public sealed record CreateUserRequest(string? Name, int? Age, int? DepartmentId);

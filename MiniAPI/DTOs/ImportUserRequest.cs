namespace MiniAPI.DTOs;

public sealed record ImportUserRequest(Guid? Id, string? Name, int? Age, int? DepartmentId, DateTime? CreatedAt);

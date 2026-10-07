namespace MiniAPI.DTOs;

public sealed record UserStatistics(long TotalUsers, decimal AverageAge, string? OldestUser, string? YoungestUser);
public sealed record DepartmentResponse(int Id, string Name);
public sealed record DepartmentStatistics(int Id, string Name, long UserCount, decimal? AverageAge);

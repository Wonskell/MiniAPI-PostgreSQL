using MiniAPI.Models;
using MiniAPI.DTOs;
using System.Text.Json;

namespace MiniAPI;

public class UserManager
{
    private readonly List<User> userList = new();
    private readonly object fileLock = new();
    private readonly string dataFilePath = "users_data.json";

    public UserManager()
    {
        LoadUsersFromFile();
    }

    public UserResponse GetGreeting(string? name)
    {
        var userName = string.IsNullOrWhiteSpace(name) ? "Unknown" : name;
        return new UserResponse
        {
            Name = userName,
            Message = $"Привет, {userName}!"
        };
    }

    public User AddUser(string name, int age)
    {
        // Простая валидация
        if (age < 0 || age > 150)
        {
            throw new ArgumentException("Возраст должен быть от 0 до 150");
        }

        lock (fileLock)
        {
            var newUser = new User
            {
                Id = Guid.NewGuid(),
                Name = name,
                Age = age,
                CreatedAt = DateTime.UtcNow
            };
            userList.Add(newUser);
            SaveUsersToFile();
            return newUser;
        }
    }

    public List<User> GetAllUsers()
    {
        lock (fileLock)
        {
            return userList.ToList();
        }
    }

    public List<User> GetUsersSortedByAge()
    {
        lock (fileLock)
        {
            return userList.OrderBy(u => u.Age).ToList();
        }
    }

    public List<User> GetUsersByAgeRange(int minAge, int maxAge)
    {
        lock (fileLock)
        {
            return userList
                .Where(u => u.Age >= minAge && u.Age <= maxAge)
                .ToList();
        }
    }

    public User? FindUserByName(string name)
    {
        lock (fileLock)
        {
            return userList
                .FirstOrDefault(u => u.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }

    public User? FindUserById(Guid id)
    {
        lock (fileLock)
        {
            return userList.FirstOrDefault(u => u.Id == id);
        }
    }

    public bool DeleteUser(Guid id)
    {
        lock (fileLock)
        {
            var user = userList.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                userList.Remove(user);
                SaveUsersToFile();
                return true;
            }
            return false;
        }
    }

    public int GetTotalUsers()
    {
        lock (fileLock)
        {
            return userList.Count;
        }
    }

    public double GetAverageAge()
    {
        lock (fileLock)
        {
            if (userList.Count == 0)
                return 0;

            return userList.Average(u => u.Age);
        }
    }

    public User? GetOldestUser()
    {
        lock (fileLock)
        {
            return userList.OrderByDescending(u => u.Age).FirstOrDefault();
        }
    }

    public User? GetYoungestUser()
    {
        lock (fileLock)
        {
            return userList.OrderBy(u => u.Age).FirstOrDefault();
        }
    }

    public List<User> GetRecentUsers(int count)
    {
        lock (fileLock)
        {
            return userList
                .OrderByDescending(u => u.CreatedAt)
                .Take(count)
                .ToList();
        }
    }

    private void LoadUsersFromFile()
    {
        try
        {
            if (File.Exists(dataFilePath))
            {
                var jsonData = File.ReadAllText(dataFilePath);
                var loadedUsers = JsonSerializer.Deserialize<List<User>>(jsonData);
                
                if (loadedUsers != null)
                {
                    userList.AddRange(loadedUsers);
                    Console.WriteLine($"Загружено пользователей: {userList.Count}");
                }
            }
            else
            {
                Console.WriteLine("Файл данных не найден, создаем новый список");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки данных: {ex.Message}");
        }
    }

    private void SaveUsersToFile()
    {
        try
        {
            var jsonOptions = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var jsonData = JsonSerializer.Serialize(userList, jsonOptions);
            File.WriteAllText(dataFilePath, jsonData, System.Text.Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка сохранения данных: {ex.Message}");
        }
    }
}

# Mini API - Система управления пользователями

Простой REST API для работы с пользователями на ASP.NET Core.

## Возможности

- ✅ Регистрация пользователей
- ✅ Просмотр списка пользователей
- ✅ Поиск по имени и ID
- ✅ Фильтрация по возрасту
- ✅ Сортировка
- ✅ Удаление пользователей
- ✅ Статистика (средний возраст, самый старший/младший)
- ✅ Сохранение в JSON файл
- ✅ Веб-интерфейс

## Технологии

- C# / .NET 10
- ASP.NET Core Minimal API
- LINQ
- JSON сериализация
- Swagger/OpenAPI

## Запуск

```bash
dotnet run
```

Откройте в браузере:
- http://localhost:5000 - веб-интерфейс
- http://localhost:5000/swagger - документация API

## API Endpoints

### Пользователи
- `GET /user` - приветствие
- `POST /user` - создать пользователя
- `GET /user/all` - все пользователи
- `GET /user/sorted` - отсортированные по возрасту
- `GET /user/filter?minAge=18&maxAge=30` - фильтр по возрасту
- `GET /user/search?name=Иван` - поиск по имени
- `GET /user/{id}` - получить по ID
- `DELETE /user/{id}` - удалить пользователя

### Статистика
- `GET /user/stats` - общая статистика
- `GET /user/recent?count=5` - последние N пользователей

## Структура проекта

```
MiniAPI/
├── Models/              # Модели данных
├── DTOs/                # Data Transfer Objects
├── Endpoints/           # API endpoints
├── wwwroot/             # Статические файлы (HTML/CSS/JS)
├── UserManager.cs       # Бизнес-логика
├── Program.cs           # Точка входа
└── users_data.json      # Хранилище данных
```

## Примеры использования

### Создать пользователя
```bash
curl -X POST http://localhost:5000/user \
  -H "Content-Type: application/json" \
  -d '{"name":"Иван","age":25}'
```

### Получить всех пользователей
```bash
curl http://localhost:5000/user/all
```

### Статистика
```bash
curl http://localhost:5000/user/stats
```

## Автор

Learning project by Wonskell

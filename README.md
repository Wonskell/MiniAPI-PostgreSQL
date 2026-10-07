# MiniAPI

Учебный проект на ASP.NET Core Minimal API и PostgreSQL. Пользователей можно добавлять, искать, фильтровать по возрасту и удалять. Веб-интерфейс показывает список, статистику по отделам и позволяет экспортировать данные в JSON.

SQL-запросы написаны вручную и выполняются через Npgsql. ORM не используется.

## Запуск

Нужны .NET 10 SDK и работающий сервер PostgreSQL 16 или новее. Команды ниже выполняются из корня репозитория. Если PostgreSQL установлен в Windows, но команда psql не найдена, добавьте каталог его bin в PATH или используйте полный путь к psql.exe.

1. Подключитесь к PostgreSQL под администратором:

   ~~~powershell
   psql -U postgres -d postgres
   ~~~

   Создайте отдельного пользователя и базу. Команда \password запросит пароль без сохранения его в SQL-файле:

   ~~~sql
   CREATE ROLE miniapi LOGIN;
   \password miniapi
   CREATE DATABASE miniapi OWNER miniapi;
   ~~~

   Выйдите из psql командой \q. Если база и роль уже существуют, повторять создание не нужно.

2. Сохраните строку подключения в локальных секретах .NET, заменив YOUR_PASSWORD своим паролем:

   ~~~powershell
   dotnet user-secrets set "ConnectionStrings:MiniApi" "Host=localhost;Port=5432;Database=miniapi;Username=miniapi;Password=YOUR_PASSWORD" --project MiniAPI
   ~~~

   Альтернатива для запуска из PowerShell — переменная окружения текущего процесса:

   ~~~powershell
   $env:ConnectionStrings__MiniApi = 'Host=localhost;Port=5432;Database=miniapi;Username=miniapi;Password=YOUR_PASSWORD'
   ~~~

3. Запустите приложение:

   ~~~powershell
   dotnet run --project MiniAPI --launch-profile http
   ~~~

При запуске приложение создаёт отсутствующие таблицы и добавляет три отдела: «Разработка», «Поддержка», «Аналитика». Существующие записи сохраняются. Если база недоступна, приложение завершится с ошибкой; перехода на временное хранилище нет.

- Главная: http://localhost:5000
- Справочник: http://localhost:5000/dashboard.html
- Swagger: http://localhost:5000/swagger — только в режиме Development.

## Данные

Таблица departments хранит отделы, users — пользователей. Поле users.department_id ссылается на departments.id и может быть NULL. При удалении отдела пользователи сохраняются, а их department_id становится NULL.

Имя содержит от 1 до 100 символов после удаления пробелов по краям, возраст — целое число от 0 до 150. Одинаковые имена пользователей допустимы. Дата добавления хранится как timestamptz, API возвращает время UTC.

Схема находится в [MiniAPI/Data/schema.sql](MiniAPI/Data/schema.sql). Это начальная схема: CREATE TABLE IF NOT EXISTS не обновляет структуру уже существующих таблиц. Дальнейшие изменения структуры требуют отдельных миграций.

## API

| Метод | Адрес | Поведение |
| --- | --- | --- |
| GET | /user?name=Анна | Приветствие; сохранения в БД нет |
| POST | /user | Добавление; 201 Created, запись и заголовок Location |
| GET | /user/all | Все записи, сначала новые |
| GET | /user/{id} | Запись по UUID или 404 |
| DELETE | /user/{id} | Удаление: 204 без тела; 404, если записи нет |
| GET | /user/sorted | По возрасту по возрастанию |
| GET | /user/filter?minAge=18&maxAge=30 | Включительный диапазон; по умолчанию 0–150 |
| GET | /user/search?name=Анна | Первое полное совпадение без учёта регистра; при дублях сначала самая ранняя запись |
| GET | /user/recent?count=5 | Последние записи; count от 1 до 100, по умолчанию 5 |
| GET | /user/stats | Количество, средний возраст, имена самого старшего и младшего |
| GET | /departments | Справочник отделов |
| GET | /departments/stats | Количество пользователей и средний возраст по каждому отделу |

Для добавления передайте JSON с обязательными name и age; departmentId необязателен:

~~~json
{ "name": "Анна Смирнова", "age": 24, "departmentId": null }
~~~

Пример для PowerShell:

~~~powershell
$body = @{ name = 'Анна Смирнова'; age = 24; departmentId = $null } | ConvertTo-Json
Invoke-RestMethod -Uri 'http://localhost:5000/user' -Method Post -ContentType 'application/json; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes($body))
Invoke-RestMethod -Uri 'http://localhost:5000/user/stats'
~~~

Ошибки проверки данных возвращают 400 с полем error. Ошибки чтения HTTP-запроса обрабатывает ASP.NET Core. Непредвиденная ошибка базы возвращает 500 и записывается в журнал сервера; интерфейс не показывает её как успешное сохранение.

## Импорт прежнего JSON-хранилища

Старый users_data.json не используется для хранения и не изменяется. Для переноса записей укажите абсолютный путь к файлу:

~~~powershell
dotnet run --project MiniAPI --launch-profile http -- --import-json "C:\data\users_data.json"
~~~

После импорта программа завершится. Импорт выполняется одной транзакцией: ошибка в любой записи отменяет добавление всей партии. Записи с уже существующим UUID пропускаются; их поля не обновляются. Можно импортировать прежний формат с PascalCase и текущий экспорт с camelCase. В каждой записи нужны корректные id, name, age и createdAt. Время без часового пояса трактуется как UTC. Для departmentId, если он указан, должен существовать соответствующий отдел.

## Практикум по SQL

[docs/sql-practice.md](docs/sql-practice.md) объясняет схему и запросы проекта. В [sql/practice.sql](sql/practice.sql) есть примеры SELECT, JOIN, GROUP BY, HAVING, подзапросов, UPDATE, DELETE и транзакций. Изменяющие данные упражнения завершаются ROLLBACK.

## Проверка

~~~powershell
dotnet build MiniAPI.slnx
powershell -ExecutionPolicy Bypass -File scripts/smoke-test.ps1 -BaseUrl http://localhost:5000
~~~

Приложение должно быть запущено. Smoke-тест создаёт временные записи и удаляет их после проверки. Для проверки ограничений самой БД на отдельной тестовой базе:

~~~powershell
psql -U miniapi -d miniapi -v ON_ERROR_STOP=1 -f sql/verify.sql
~~~

## Структура

~~~text
MiniAPI/
  Data/schema.sql        Таблицы, ограничения и индексы
  DTOs/                  Входные данные и ответы API
  Endpoints/             HTTP-маршруты
  Models/                Модель пользователя
  wwwroot/               HTML, CSS и JavaScript
  UserManager.cs         SQL-запросы и импорт
  Program.cs             Настройка и запуск приложения
docs/sql-practice.md     Разбор тем практикума
sql/                     Упражнения и проверка ограничений
scripts/                 Проверка API
~~~

## Ограничения

Авторизация, редактирование пользователей и управление отделами через интерфейс не реализованы. Список и экспорт возвращают все записи без постраничной загрузки. Учебное приложение рассчитано на локальный запуск. Пароли подключения не должны попадать в репозиторий.

Автор: Wonskell.


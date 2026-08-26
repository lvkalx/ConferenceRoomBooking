# Conference Room Booking API

REST API для бронювання конференц-залів: пошук вільних кімнат, створення/скасування бронювань, розрахунок вартості оренди та звіти по завантаженості й доходу.

Проєкт побудований за принципами Clean Architecture: `Domain` → `Application` → `Infrastructure` → `API`.

## Стек

- .NET 8 / ASP.NET Core Web API
- Entity Framework Core 8 + PostgreSQL (Npgsql)
- FluentValidation
- AutoMapper
- Swashbuckle (Swagger / OpenAPI)
- xUnit — модульні тести (`tests/`)

## Структура рішення

```
ConferenceRoomBooking.sln
├── ConferenceRoomBooking.API             # контролери, DI, middleware, Swagger
├── ConferenceRoomBooking.Application     # DTO, сервіси, валідатори, інтерфейси
├── ConferenceRoomBooking.Domain          # сутності, value objects, бізнес-винятки
├── ConferenceRoomBooking.Infrastructure  # EF Core, репозиторії, міграції, звіти
└── tests/
    ├── ConferenceRoomBooking.Domain.Tests
    └── ConferenceRoomBooking.Application.Tests
```

## Запуск

1. Встановіть [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) та PostgreSQL.
2. Пропишіть рядок підключення (`ConferenceRoomBooking.API/appsettings.json` або через `dotnet user-secrets`):
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=conference_room_booking;Username=postgres;Password=postgres"
     }
   }
   ```
3. Застосуйте міграції:
   ```bash
   dotnet ef database update --project ConferenceRoomBooking.Infrastructure --startup-project ConferenceRoomBooking.API
   ```
4. Запустіть API:
   ```bash
   dotnet run --project ConferenceRoomBooking.API
   ```
5. У Development-режимі база автоматично наповнюється тестовими даними (`DbSeeder`).

## Документація API (Swagger)

Swagger вже підключено в проєкті (`Swashbuckle.AspNetCore`, налаштування — у `ConferenceRoomBooking.API/Extensions/SwaggerServiceExtensions.cs`), разом з XML-коментарями над контролерами (`GenerateDocumentationFile` увімкнено в `.csproj`).

Після запуску в Development-режимі Swagger UI доступний за адресою:

```
https://localhost:7076/swagger
```
(порт дивіться у `Properties/launchSettings.json`, у консолі при старті додатку теж виводиться актуальна адреса).

Там же можна відкрити сирий JSON-документ OpenAPI: `/swagger/v1/swagger.json`.

### Основні ендпоінти

| Метод | Маршрут | Опис |
|---|---|---|
| `GET`    | `/api/rooms` | список залів |
| `GET`    | `/api/rooms/{id}` | зал за id |
| `GET`    | `/api/rooms/available` | пошук вільних залів на період |
| `POST`   | `/api/rooms` | створити зал |
| `PUT`    | `/api/rooms/{id}` | повністю замінити зал |
| `PATCH`  | `/api/rooms/{id}` | частково оновити зал |
| `DELETE` | `/api/rooms/{id}` | видалити зал |
| `GET`    | `/api/bookings/{id}` | бронювання за id |
| `POST`   | `/api/bookings` | створити бронювання |
| `POST`   | `/api/bookings/{id}/cancel` | скасувати бронювання |
| `GET`    | `/api/reports/occupancy` | звіт по завантаженості залів |
| `GET`    | `/api/reports/revenue` | звіт по доходу |
| `GET`    | `/api/reports/popular-services` | звіт по популярних послугах |

Повний опис параметрів, тіл запитів/відповідей і кодів помилок — у Swagger UI.

## Тести

```bash
dotnet test
```

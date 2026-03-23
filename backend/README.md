# Dead-Mans Backend

Backend - активная часть проекта, а не заготовка. Сейчас это ASP.NET Core 8 Web API с реальными application/domain/infrastructure границами и временными in-memory adapters для хранения.

## Слои

- `Api/Contracts/` - HTTP DTO и transport-модели.
- `Application/` - use-case сервисы, application models и repository ports.
- `Domain/` - доменные сущности и инварианты.
- `Infrastructure/` - in-memory adapters и DI-регистрация.
- `Controllers/` - HTTP endpoints.
- `Data/` - точка входа для будущей EF Core persistence.
- `openapi/deadmans.v1.yaml` - канонический transport source of truth.

## Что важно в текущем skeleton

- контроллеры зависят от application-сервисов, а не от инфраструктурных in-memory классов;
- in-memory код теперь играет роль adapter storage, а не application-слоя;
- transport DTO отделены от application contracts;
- Swagger и OpenAPI используются как внешний контракт API.

## Endpoint'ы

- `GET /api/health`
- `GET /api/leaderboard`
- `GET /api/loadout`
- `GET /api/modifiers`
- `POST /api/modifiers/activate`
- `GET /api/game-state`
- `POST /api/game-state/start`
- `POST /api/game-state/pause`
- `POST /api/game-state/resume`
- `POST /api/game-state/next-round`
- `POST /api/game-state/reset`

## Локальный запуск

Из корня репозитория:

```bash
npm run dev:backend
```

Или напрямую:

```bash
dotnet run --project backend/backend.csproj
```

## Следующий шаг

Следующий архитектурный шаг - заменить in-memory repository adapters на persistence-backed реализации через EF Core, не меняя контроллеры и минимально затрагивая application-слой.

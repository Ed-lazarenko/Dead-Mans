# Dead-Mans Backend

Backend - активная часть проекта, а не заготовка. Сейчас это ASP.NET Core 8 Web API с реальными application/domain/infrastructure границами: game-срезы пока используют in-memory adapters, а auth/users/roles уже идут через EF Core `ApplicationDbContext`.

## Слои

- `Api/Contracts/` - HTTP DTO и transport-модели.
- `Api/Mapping/` - маппинг application-моделей в transport DTO.
- `Application/` - use-case сервисы, application models и repository ports.
- `Domain/` - доменные сущности и инварианты.
- `Infrastructure/` - persistence adapters, Twitch auth integration и DI-регистрация.
- `Controllers/` - HTTP endpoints.
- `Data/` - EF Core DbContext, entity-конфигурации и миграции.
- `openapi/deadmans.v1.yaml` - канонический transport source of truth.

## Что важно в текущем skeleton

- контроллеры зависят от application-сервисов, а не от инфраструктурных in-memory классов;
- in-memory код играет роль adapter storage, а не application-слоя;
- transport DTO отделены от application contracts;
- Swagger UI показывает канонический `openapi/deadmans.v1.yaml`, а не отдельно сгенерированный runtime-контракт.
- runtime-роли для авторизации подтягиваются из БД на каждом аутентифицированном запросе; cookie хранит identity, а не долгоживущий снимок ролей.

## Endpoint'ы

- `GET /api/health`
- `GET /api/leaderboard`
- `GET /api/loadout`
- `GET /api/modifiers` - требует `moderator` или `admin`
- `POST /api/modifiers/activate`
- `POST /api/modifiers/activate` - требует `moderator` или `admin`
- `GET /api/game-state`
- `POST /api/game-state/start` - требует `moderator` или `admin`
- `POST /api/game-state/pause` - требует `moderator` или `admin`
- `POST /api/game-state/resume` - требует `moderator` или `admin`
- `POST /api/game-state/next-round` - требует `moderator` или `admin`
- `POST /api/game-state/reset` - требует `moderator` или `admin`

## Локальный запуск

Из корня репозитория:

```bash
npm run dev:backend
```

Или напрямую:

```bash
dotnet run --project backend/backend.csproj
```

## Twitch auth - первые шаги (подготовка)

Для работы текущих auth endpoint'ов нужно подготовить OAuth-приложение Twitch и persistence-конфигурацию backend.

1. Создайте приложение в Twitch Developer Console.
2. Добавьте Redirect URI:
   - `http://localhost:5285/auth/twitch/callback` (локально)
   - `https://<your-domain>/auth/twitch/callback` (прод)
3. Заполните переменные из `backend/.env.example`:
   - `TwitchAuth__ClientId`
   - `TwitchAuth__ClientSecret`
   - `TwitchAuth__RedirectUri`
   - `TwitchAuth__Scopes__*`

Backend валидирует секцию `TwitchAuth` на старте. Auth также требует зарегистрированный `ApplicationDbContext`: если `ConnectionStrings:DefaultConnection` не настроен и DbContext не переопределён явно, backend завершит старт с понятной ошибкой конфигурации.

Для локальных секретов без риска утечки в git:

1. Скопируйте `appsettings.Local.example.json` в `appsettings.Local.json`.
2. Заполните `TwitchAuth.ClientId` и `TwitchAuth.ClientSecret` своими значениями.
3. `appsettings.Local.json` уже в `.gitignore`, поэтому файл останется только локально.
4. Для локальной frontend callback-страницы используйте `http://localhost:5180/auth/callback`.

### Текущие auth endpoint'ы (MVP)

- `GET /auth/twitch/login` - редиректит пользователя на Twitch OAuth.
- `GET /auth/twitch/callback` - принимает `code/state`, получает профиль из Twitch, создает/обновляет пользователя в БД и возвращает результат авторизации.
- `GET /auth/me` - возвращает текущую сессию и эффективные роли пользователя.
- `POST /auth/logout` - завершает cookie-сессию.

Если пользователь входит впервые, backend автоматически назначает ему базовую роль `viewer`. Деактивированные пользователи не могут получить новую сессию до повторной активации аккаунта.

## Следующий шаг

Следующий архитектурный шаг - последовательно переводить game repository adapters с in-memory на persistence-backed реализации через EF Core, не меняя контроллеры и минимально затрагивая application-слой.

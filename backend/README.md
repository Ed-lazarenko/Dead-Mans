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

## Twitch auth - первые шаги (подготовка)

Перед реализацией endpoint'ов авторизации нужно подготовить OAuth-приложение Twitch и конфигурацию backend.

1. Создайте приложение в Twitch Developer Console.
2. Добавьте Redirect URI:
   - `http://localhost:5285/auth/twitch/callback` (локально)
   - `https://<your-domain>/auth/twitch/callback` (прод)
3. Заполните переменные из `.env.example`:
   - `TwitchAuth__ClientId`
   - `TwitchAuth__ClientSecret`
   - `TwitchAuth__RedirectUri`
   - `TwitchAuth__Scopes__*`

Backend валидирует секцию `TwitchAuth` на старте, чтобы ошибки конфигурации были видны сразу.

Для локальных секретов без риска утечки в git:

1. Скопируйте `appsettings.Local.example.json` в `appsettings.Local.json`.
2. Заполните `TwitchAuth.ClientId` и `TwitchAuth.ClientSecret` своими значениями.
3. `appsettings.Local.json` уже в `.gitignore`, поэтому файл останется только локально.
4. Для локальной frontend callback-страницы используйте `http://localhost:5180/auth/callback`.

### Текущие auth endpoint'ы (MVP)

- `GET /auth/twitch/login` - редиректит пользователя на Twitch OAuth.
- `GET /auth/twitch/callback` - принимает `code/state`, получает профиль из Twitch, создает/обновляет пользователя в БД и возвращает результат авторизации.

Если пользователь входит впервые, backend автоматически назначает ему базовую роль `viewer`.

## Следующий шаг

Следующий архитектурный шаг - заменить in-memory repository adapters на persistence-backed реализации через EF Core, не меняя контроллеры и минимально затрагивая application-слой.

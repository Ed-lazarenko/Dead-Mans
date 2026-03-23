# Dead-Mans Backend

Текущий backend находится в стадии архитектурного `v1 skeleton`, но уже не является пустой заготовкой.

## Что есть сейчас

- ASP.NET Core 8 Web API
- Swagger / OpenAPI
- CORS под локальный frontend (`http://localhost:5173`)
- базовый composition root в `Program.cs`
- слои:
  - `Application/` — контракты и сервисные абстракции
  - `Domain/` — доменные модели
  - `Infrastructure/` — in-memory реализации и DI
  - `Controllers/` — HTTP API
  - `Data/` — будущая точка входа для EF Core persistence

## Какие endpoint'ы уже есть

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

## Почему сейчас in-memory

Фронтенд пока живёт на mock-данных, а схема БД всё ещё уточняется. Поэтому backend приведён к правильной структуре и контрактам, но текущие сервисы реализованы in-memory. Это позволяет:

- не смешивать HTTP-слой и бизнес-логику;
- держать стабильные контракты для фронтенда;
- позже заменить in-memory реализации на EF Core / SQL без переписывания контроллеров.

## Что дальше

- завести реальные сущности и конфигурации EF Core в `Data/`;
- заменить in-memory сервисы на persistence-backed реализации;
- подключить Twitch auth / роли / аудит;
- синхронизировать frontend data access с реальными endpoint'ами через `httpClient`.

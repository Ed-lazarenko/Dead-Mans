# Dead-Mans Backend

Backend сейчас поддерживает только два прикладных сценария:

- Twitch authentication и cookie session;
- read-only `GET /api/game`, который отдает игровое поле из БД.

## Что есть в коде

- `Controllers/` - `AuthController`, `AuthSessionController`, `GameController`.
- `Application/` - auth session service и game-board service.
- `Infrastructure/` - Twitch auth, EF Core persistence, `DbGameBoardRepository`.
- `Data/` - `ApplicationDbContext`, entities, configurations, migrations.
- `openapi/deadmans.v1.yaml` - канонический контракт для auth + game board.

## Актуальные endpoint'ы

- `GET /api/game`
- `GET /auth/me`
- `POST /auth/logout`
- `GET /auth/twitch/login`
- `GET /auth/twitch/callback`

## Локальный запуск

Bootstrap и сброс: корневой `README.md` (`scripts/setup-local.ps1`, `reset-local.ps1`; на Windows — `setup-local.bat`, `reset-local.bat` в корне репо).

Сервер из каталога `backend/`:

```powershell
dotnet run --project backend.csproj
```

Из корня репозитория: `npm run dev:backend`.

## База данных и storage

Игровое поле читается из PostgreSQL через EF Core. Медиа-URL для ячеек строятся на основе `Storage:PublicBaseUrl`.

Каноничный источник тестовых картинок:

- `backend/assets/test-game-board/elements/`

Uploader:

- `tools/SeedTestGameBoardMedia/`
- `scripts/upload-test-game-board-media.ps1`

Migration создает тестовую игру и записи `media_assets` / `board_cell_media`, а uploader заливает в bucket `deadman` реальные PNG-файлы с теми же object key.

## Twitch auth

Для работы auth нужны:

- `TwitchAuth__ClientId`
- `TwitchAuth__ClientSecret`
- `TwitchAuth__RedirectUri`
- `TwitchAuth__FrontendRedirectUri`
- `TwitchAuth__Scopes__*`

Backend валидирует auth-конфигурацию и наличие рабочего `ApplicationDbContext` на старте.

## Forwarded headers (proxy)

Если backend работает за reverse proxy/load balancer, настройте секцию `ForwardedHeaders`:

- `ForwardedHeaders__Enabled=true` (глобальное включение/отключение обработки forwarded headers)
- `ForwardedHeaders__TrustedProxies__0=203.0.113.10` (отдельные IP прокси)
- `ForwardedHeaders__TrustedNetworks__0=10.0.0.0/24` (доверенные подсети в CIDR)

Локально в `Development` по умолчанию включен совместимый режим:

- `ForwardedHeaders__TrustAllProxiesInDevelopment=true`

Для stage/prod рекомендуется оставить только trusted proxy/network и не полагаться на "trust all".
Для тестовых/специальных окружений можно временно отключить поведение полностью: `ForwardedHeaders__Enabled=false`.

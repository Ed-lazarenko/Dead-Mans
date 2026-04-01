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

Из каталога `backend/`:

```powershell
.\scripts\setup-local.ps1
dotnet run --project backend.csproj
```

Или напрямую:

```powershell
dotnet run --project backend.csproj
```

## База данных и storage

Игровое поле читается из PostgreSQL через EF Core. Медиа-URL для ячеек строятся на основе `Storage:PublicBaseUrl`.

Безопасный backend bootstrap:

```powershell
.\scripts\setup-local.ps1
```

Он не удаляет существующие local volumes. Скрипт:

- поднимает `postgres` и `minio`, если они не запущены;
- применяет EF Core migrations;
- догружает test media в MinIO.

Полный destructive reset:

```powershell
.\scripts\reset-local.ps1
```

Или без интерактивного подтверждения:

```powershell
.\scripts\reset-local.ps1 -Force
```

`reset-local.ps1` удаляет local Docker volumes для PostgreSQL и MinIO, а затем выполняет обычный `setup-local.ps1`.

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

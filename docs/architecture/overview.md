# Dead-Mans Architecture Overview

## Текущий продуктовый скоуп

Приложение состоит из двух вертикалей:

- Twitch auth
- read-only game board

## Поток данных

```mermaid
flowchart LR
  browser[Browser] --> frontend[Frontend SPA]
  frontend --> api[GET /api/game]
  frontend --> auth[/auth/*]
  api --> gameController[GameController]
  gameController --> gameBoardService[IGameBoardService]
  gameBoardService --> gameBoardRepo[DbGameBoardRepository]
  gameBoardRepo --> postgres[(PostgreSQL)]
  gameBoardRepo --> storage[Public media URLs]
  auth --> authControllers[AuthController / AuthSessionController]
  authControllers --> authServices[Twitch auth + EF auth services]
  authServices --> postgres
```

## Frontend

- `features/auth/` - Twitch login, callback, session restore
- `features/game-board/` - read-only экран игрового поля
- `shared/api/client/` - HTTP transport
- `shared/api/contracts/` - generated types из OpenAPI
- `shared/auth/` - auth context и guard

## Backend

- `Controllers/` - `AuthController`, `AuthSessionController`, `GameController`
- `Application/Features/Auth/` - auth session service
- `Application/Features/GameBoard/` - game-board service
- `Infrastructure/Persistence/DbGameBoardRepository.cs` - чтение игрового поля из БД
- `Infrastructure/Auth/` - Twitch auth, роли, claims transformation
- `Data/` - EF Core context, entities, migrations
- `assets/test-game-board/elements/` - source-controlled тестовые PNG для локального bootstrap
- `tools/SeedTestGameBoardMedia/` - uploader этих PNG в MinIO

## Локальное развертывание

Каноничный безопасный backend bootstrap:

```powershell
Set-Location backend
.\scripts\setup-local.ps1
```

Скрипт живет в `backend/scripts/`, не удаляет существующие локальные данные, применяет миграции и заливает test media в storage.

Для полного локального wipe существует отдельный destructive-сценарий:

```powershell
Set-Location backend
.\scripts\reset-local.ps1
```

## Контракты

Source of truth:

- `backend/openapi/deadmans.v1.yaml`

Frontend regeneration:

```bash
npm --prefix frontend run generate:contracts
```

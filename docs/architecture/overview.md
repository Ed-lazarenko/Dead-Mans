# Dead-Mans Architecture Overview

## Текущий продуктовый скоуп

Приложение состоит из нескольких продуктовых вертикалей:

- Twitch auth
- game board с чтением снимка, admin-only открытием ячеек и realtime-синхронизацией
- game setup: один общий admin-черновик в БД, Save + optimistic concurrency (`expectedVersion` / `409`), cell image upload/delete, draft reset через hard-delete только для `draft` (исключение из soft-delete политики), realtime через SignalR (контракт в OpenAPI `x-signalr`, см. `docs/architecture/realtime.md`)
- game modifiers: глобальный каталог, выбор `enabledModifierIds` в draft setup,
  round-scoped покупки игрока и admin proxy activation только в
  `awaiting_modifiers`; immutable activation audit, owner/admin cancellation с
  однократным refund и realtime invalidation через `game-board` hub; определения,
  включённые в active game, доступны только для чтения, а admin emergency disable
  блокирует только новые активации текущей игры, сохраняет actor/time/reason audit и
  публикует versioned `modifierAvailabilityChanged` для полного client resync
- modifier definitions используют revisioned typed `BehaviorV2`; OpenAPI содержит
  закрытые resolution/formula parameter unions, frontend types генерируются из него,
  activation замораживает полный snapshot при покупке, а round result копирует snapshot
  из activation без повторного чтения live catalog; отсутствующий или повреждённый V2 snapshot
  отклоняется fail closed и не пересчитывается из изменяемого каталога
- runtime modifier projection отдаёт только безопасную инструкцию, performer, stacking policy
  и server time; formula parameters/trace остаются moderator/admin projection. Frontend
  восстанавливает countdown по `gameplayStartedAtUtc`, прекращает его в review/terminal state
  и не пишет outcome по локальному timer event
- round lifecycle: `awaiting_modifiers` → `preparing` → `in_progress` →
  `reviewing_results` → terminal state; prepare/begin/review/resume используют
  monotonic `roundVersion` и серверные timestamps
- round summary использует exact resolution groups/instances, authoritative preview hash и
  optimistic version gate; confirmed notes/outcomes видны authenticated visitors, draft и
  formula trace — только moderator/admin. Completed и technically cancelled rounds в history
  агрегируются раздельно, cancelled rounds не влияют на leaderboard
- preparing-round rebuild полностью refund'ит заказ и возвращает ordering в
  `awaiting_modifiers`, сохраняя выбранные карточку и команду; technical cancel — отдельный
  terminal path с нулевым score, retired-card state, освобождением команды и append-only
  transition audit
- game questions (phase 1): каталог вопросов с поиском/фильтрацией и enable/disable в `game-setup`; runtime quiz API для ask/answer/manual awards живёт отдельно на `/api/game/quiz/*`
- game history (phase 1): user-centric API `GET /api/game/history/users/{userId}` возвращает активность пользователя по играм (какие модификаторы активировал и какие вопросы были зачтены как ответы пользователя)
- lifecycle archive (phase 1): `DELETE /api/game/lifecycle/games/{gameId}` выполняет soft-delete для non-draft игр; draft остаётся отдельным hard-delete сценарием через game setup
- game registration: приём заявок в статусе `ready`, команды и инвайты (см. `docs/architecture/game-registration.md`)
- политика удаления и сохранения истории: `docs/architecture/data-retention.md`

## Поток данных

```mermaid
flowchart LR
  browser[Browser] --> frontend[Frontend SPA]
  frontend --> api["GET /api/game"]
  frontend --> openCell["POST /api/game/cells/{cellId}/open"]
  frontend --> realtime["/hubs/game-board"]
  frontend --> auth[/auth/*]
  api --> gameController[GameController]
  openCell --> gameController
  gameController --> gameBoardService[IGameBoardService]
  gameBoardService --> gameBoardRepo[DbGameBoardRepository]
  gameBoardRepo --> postgres[(PostgreSQL)]
  gameBoardRepo --> storage[Public media URLs]
  gameBoardService --> realtimePublisher[IGameBoardEventsPublisher]
  realtimePublisher --> realtimeHub[GameBoardHub]
  realtimeHub --> browser
  frontend --> setupApi["GET/POST/PUT/DELETE /api/game/setup"]
  frontend --> setupMedia["POST/DELETE /api/game/setup/cells/{cellId}/media"]
  frontend --> setupRealtime["/hubs/game-setup"]
  setupApi --> gameSetupController[GameSetupController]
  setupMedia --> gameSetupMediaController[GameSetupCellMediaController]
  gameSetupController --> gameSetupService[IGameSetupService]
  gameSetupMediaController --> gameSetupCellMediaService[IGameSetupCellMediaService]
  gameSetupService --> gameSetupRepo[DbGameSetupRepository]
  gameSetupCellMediaService --> gameSetupRepo
  gameSetupRepo --> postgres
  gameSetupService --> setupPublisher[IGameSetupEventsPublisher]
  setupPublisher --> setupHub[GameSetupHub]
  setupHub --> browser
  auth --> authControllers[AuthController / AuthSessionController]
  authControllers --> authServices[Twitch auth + EF auth services]
  authServices --> postgres
```

## Frontend

- `features/auth/` - Twitch login, callback, session restore
- `features/game-board/` - экран игрового поля, open-cell flow и realtime sync
- `features/game-setup/` - настройка черновика игры, cell media, Save/layout flow, realtime sync
- `features/game-modifiers/` - shared feature API каталога модификаторов для game setup
- `features/game-registration/` - единый typed API-модуль регистрации команд (используют `game-application` и `team-registrations`)
- `features/game-application/` - страница заявки игрока
- `features/team-registrations/` - admin-подтверждение команд
- `app/panel-route-metadata.ts` + `app/panel-route-config.tsx` - panel routes split: metadata/access отдельно от lazy-страниц и realtime-sync
- `app/AppRoutes.tsx` + `app/app-route-tree.tsx` - дерево маршрутов через `useRoutes`
- `routes/app-routes.ts` - re-export метаданных, guard/access helpers и redirects
- `layouts/` - `MainLayout`, `PanelNavigation`
- `shared/ui/` - reusable layers: primitives, patterns, feedback
- `shared/theme/` - единые UI tokens и layout presets
- `shared/api/client/` - `openapi-fetch` transport поверх generated `paths`, общая обработка `ApiError`
- `shared/api/contracts/` - generated types из OpenAPI
- `features/*/api/*-queries.ts` - feature-local query keys и `queryOptions`; повторяемые mutation policies живут рядом в `mutationOptions`
- `shared/realtime/use-signalr-hub-lifecycle.ts` - общий SignalR connect/reconnect/cleanup lifecycle; event-specific handlers остаются в owning feature
- `shared/auth/` - auth context/guard + action-level capability helpers (`panel-capabilities.ts`)

## Backend

- `Controllers/` - `AuthController`, `AuthSessionController`, `GameController`, `GameModifierController`, `GameQuestionController`, `GameQuizController`, `GameHistoryController`, `GameSetupController`, `GameSetupCellMediaController`, `GameLifecycleController`, `GameRegistrationController`
- `Api/Contracts/` + `Api/Mapping/` - transport DTO и явный mapping из application-моделей
- `Application/Features/Auth/` - auth session service
- `Application/Features/GameBoard/` - game-board service
- `Application/Features/GameSetup/` - draft setup, cell media, storage cleanup on reset
- `Application/Features/GameModifiers/` - catalog, activation, authorization-aware
  cancellation/refund, active-game content lock and emergency-disable orchestration
- `Domain/GameModifiers/` - persistence-free BehaviorV2 types, four-formula registry,
  compatibility validation and fail-closed per-instance round calculator
- `Application/Features/GameQuestions/` - question catalog mutation + quiz runtime services
- `Application/Features/GameHistory/` - user activity history
- `Application/Features/GameRegistration/` - registration use-cases
- `Application/Features/GameLifecycle/` - lifecycle transitions and archive
- `Application/Abstractions/IObjectStorage.cs` + `Infrastructure/Storage/` - S3-compatible object storage port
- `Infrastructure/Persistence/DbGameBoardRepository.cs` - чтение игрового поля из БД
- `Infrastructure/Persistence/DbGameSetupRepository.cs` - draft setup persistence
- `Infrastructure/Persistence/DbGameModifierRepository.cs` - transactional,
  round-locked modifier purchase/refund persistence, catalog content locks and
  game-scoped emergency-disable audit; definition revisioning and purchase-time snapshots
- `Infrastructure/Persistence/DbGameQuestionRepository.cs` / `DbGameQuizRepository.cs` - question catalog and quiz runtime persistence
- `Infrastructure/Persistence/DbGameHistoryRepository.cs` - history persistence
- `Infrastructure/Auth/` - Twitch auth, роли, claims transformation
- `Infrastructure/Realtime/` - SignalR hubs и publishers (`GameBoardHub`, `GameSetupHub`)
- `Data/` - EF Core context, entities, migrations
- `assets/test-game-board/cards/` - source-controlled тестовые PNG для локального bootstrap
- `tools/SeedTestGameBoardMedia/` - uploader этих PNG в MinIO

## Локальное развертывание

Общий workflow: [`docs/development.md`](../development.md). Скрипты bootstrap/reset: `backend/scripts/setup-local.ps1` и `backend/scripts/reset-local.ps1`. В Windows в корне — `setup-local.bat` / `reset-local.bat` как обёртки над теми же `.ps1`.

## Контракты

Source of truth:

- `backend/openapi/deadmans.v1.yaml`

Frontend regeneration:

```bash
npm --prefix frontend run generate:transport
```

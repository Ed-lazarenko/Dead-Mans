# Dead-Mans Backend

Backend поддерживает auth, game board, game setup (admin draft), modifiers, questions, user history, game registration и lifecycle.

## Что есть в коде

- `Controllers/` — auth, game board, modifiers, questions, history, game setup, registration и lifecycle.
- `Application/` — use-case сервисы (`GameBoard`, `GameModifiers`, `GameQuestions`, `GameHistory`, `GameSetup`, `GameRegistration`, `GameLifecycle`) и repository ports.
- `Api/` — transport contracts, mapping, HTTP middleware, rate limiting and SignalR hubs/publishers.
- `Infrastructure/` — Twitch auth, EF repositories (`DbGame*Repository`) and object storage.
- `Data/` — `ApplicationDbContext`, entities, configurations, migrations.
- `openapi/deadmans.v1.yaml` — канонический контракт (HTTP + SignalR `x-signalr`); см. `docs/architecture/realtime.md`.
- `Api/Contracts/RealtimeHubContracts.cs` — hub paths и event names (синхронно с OpenAPI).

## Архитектурные границы сборки

Backend разделен на layer-проекты и собирается как единая `backend.slnx`:

- `backend.Domain.csproj` — domain-модели и value objects.
- `backend.Application.csproj` — use-cases и порты (без зависимости на `Microsoft.AspNetCore.App`).
- `backend.Data.csproj` — EF Core persistence model и migrations.
- `backend.Api.csproj` — transport contracts / API mapping helpers.
- `backend.Infrastructure.csproj` — реализации портов, auth, realtime, storage, DI.
- `backend.csproj` — web host (`Program` + `Controllers`), компоновка слоев.

Guardrails:

- тест `BackendProjectDependencyRulesTests` фиксирует допустимую матрицу `ProjectReference`;
- тест фиксирует `ErrorResponse.code.enum` в OpenAPI синхронным с `AppMessages.ErrorCodes`;
- тест запрещает hardcoded `game_*.*` error-code литералы вне `AppMessages.ErrorCodes`;
- runtime-код формирует ошибки через `ErrorResponseFactory` (есть тест-запрет на `new ErrorResponse(...)`);
- контроллеры формируют error `IActionResult` через `ApiErrorResults` helper-методы (есть тест-запрет на прямой `ErrorResponseFactory.Create(...)` в `Controllers/`);
- `DomainErrorHttpPolicy` задает единое отображение domain `*ErrorCode -> HTTP status + payload` и покрыт тестом полноты enum;
- непойманные исключения обрабатываются централизованно в `ApiExceptionHandlingMiddleware` и возвращают единый 500 payload;
- error payload включает `requestId` для трассировки, а `ApiErrorMetrics` публикует счетчики backend ошибок по статусу/коду/источнику;
- CI собирает `backend/backend.slnx`, поэтому нарушение границ ловится на PR.

## Актуальные endpoint'ы

- `GET /api/game`, `POST /api/game/cells/{cellId}/open`
- `GET /api/game/modifiers/catalog`, `POST /api/game/modifiers/{modifierId}/activate`
- `POST /api/game/modifiers`, `PUT /api/game/modifiers/{modifierId}`,
  `DELETE /api/game/modifiers/{modifierId}?expectedRevision=...` (admin-only immutable revisions/archive)
- `GET /api/game/modifiers/history`, `GET /api/game/modifiers/{modifierId}/versions`,
  version detail and related-game endpoints (all authenticated roles, keyset pagination)
- `GET /api/game/questions/catalog`, `GET /api/game/questions/categories`, `POST /api/game/questions/categories`
- `PATCH /api/game/questions/{questionId}/enabled`, `PATCH /api/game/questions/categories/{categoryId}/enabled`
- `POST /api/game/quiz/questions/ask-next`, `POST /api/game/quiz/rounds/{roundId}/answer`
- `DELETE /api/game/questions/{questionId}` (admin): soft-delete вопроса из каталога
- `GET/POST/PUT/DELETE /api/game/setup`, cell media under `/api/game/setup/cells/{cellId}/media`
- `GET /api/game/registration`, team/invitation mutations under `/api/game/registration/*`
- `GET /api/game/registration/teams` (moderator/admin), confirm/reject/disband, disband requests, invitations
- `GET /api/game/history/users/{userId}` (self or moderator/admin): grouped user activity history by game (modifier activations + answered quiz rounds)
- `POST /api/game/lifecycle/open-registration`, `/start`, `GET /api/game/lifecycle/games/{gameId}/finish-preview`, `POST /api/game/lifecycle/games/{gameId}/finish`, `DELETE /api/game/lifecycle/games/{gameId}` (admin lifecycle, immutable final result + non-draft archive workflow)
- `GET /auth/me`, `POST /auth/logout`, Twitch login/callback

Quiz application port distinguishes manual delivery/answers from Twitch delivery/answers. A future
bot can call the application service directly with provider channel/message identity; only the first
correct answer is persisted. Its Twitch principal is created without a login timestamp, and OAuth
later reuses that same `twitch_user_id` row.

## Локальный запуск

Bootstrap и сброс: [`docs/development.md`](../docs/development.md) (`backend/scripts/setup-local.ps1`, `backend/scripts/reset-local.ps1`; на Windows — `setup-local.bat`, `reset-local.bat` в корне репо).

Сервер из каталога `backend/`:

```powershell
dotnet run --project backend.csproj
```

Из корня репозитория: `npm run dev:backend`.

## База данных и storage

Игровое поле читается из PostgreSQL через EF Core. Открытие ячеек выполняется на backend с role-check по admin и публикует realtime-события через SignalR. Медиа-URL для ячеек строятся на основе `Storage:PublicBaseUrl`.

Политика удаления/архивации данных: [`docs/architecture/data-retention.md`](../docs/architecture/data-retention.md).

Game setup (admin draft):

- `GET/POST/PUT/DELETE /api/game/setup` — черновик и пакетное сохранение текстовых полей.
- `POST/DELETE /api/game/setup/cells/{cellId}/media` — загрузка/удаление изображения ячейки (multipart, admin only).
- Object key: `{Storage:GamesPrefix}/{gameId}/{Storage:CardsGroup}/{col}-{row}.{ext}` (см. `GameMediaObjectKeyFormat`).
- `DELETE /api/game/setup` выполняет hard-delete только для текущего черновика (`draft`) и очищает связанные draft media-артефакты; это исключение из общей soft-delete политики.

Обязательные ключи `Storage` для media: `PublicBaseUrl`, `BucketName`, `GamesPrefix`, `CardsGroup`. Для записи в MinIO в dev также `AccessKey` / `SecretKey` (или `MINIO_ROOT_*`).

Каноничный источник тестовых картинок:

- `backend/assets/test-game-board/cards/`

Uploader:

- `tools/SeedTestGameBoardMedia/`
- `backend/scripts/upload-test-game-board-media.ps1`

Локальные тестовые данные:

- `backend/scripts/seed-local-test-data.ps1`
- `backend/scripts/seed-local-test-data.sql`

`setup-local.ps1` применяет миграции, заливает PNG-файлы в bucket `deadman`, затем идемпотентно пересоздает локальную активную тестовую игру `c6c6a0da-0bd1-4f0b-bb2f-9a4c9c8b7f6a` с `media_assets` / `board_cell_media`, командами, quiz questions, enabled catalog selections, quiz points и несколькими активными модификаторами.

## Twitch auth

Для работы auth нужны:

- `TwitchAuth__ClientId`
- `TwitchAuth__ClientSecret`
- `TwitchAuth__RedirectUri`
- `TwitchAuth__FrontendRedirectUri`
- `TwitchAuth__Scopes__*`

Backend валидирует auth-конфигурацию и наличие рабочего `ApplicationDbContext` на старте.

В production соединение PostgreSQL обязано использовать `SSL Mode=VerifyFull`. Режимы
`Disable`, `Allow`, `Prefer`, `Require` и `VerifyCA` не проходят стартовую проверку: только
`VerifyFull` одновременно шифрует соединение, проверяет сертификат и имя хоста.

`Storage:PublicBaseUrl` принимается только как чистый `http`/`https` origin без credentials,
query string и fragment. В production используйте HTTPS URL и не публикуйте MinIO admin console.
Readiness endpoint `/health/ready` проверяет и PostgreSQL, и возможность прочитать bucket object
storage; liveness endpoint `/health/live` не зависит от внешних сервисов.

Для любого общего/stage/prod-окружения обязательно задайте явный список допустимых host names
через `AllowedHosts` (в переменной окружения значения разделяются `;`). Значение `*` не используйте:
оно отключает фильтрацию заголовка `Host`. Локальный default разрешает только `localhost` и
`127.0.0.1`.

В production `AllowedHosts` проходит строгую стартовую проверку: wildcard, `localhost` и loopback
адреса запрещены. Также задайте абсолютный путь `DataProtection__KeysDirectory` и смонтируйте его
как постоянный каталог с доступом только для процесса backend. Иначе ключи auth-cookie не переживут
пересоздание контейнера; production-запуск без этого параметра блокируется.

Все изменяющие cookie-authenticated запросы `/api/*` должны содержать
`X-Dead-Mans-Api-Client: 1`. Общий frontend API client добавляет его автоматически; проверка на
backend является CSRF-границей и не должна отключаться для отдельных mutation endpoint-ов.

Глобальный rate limiter раздельно ограничивает `/auth`, читающие `/api` запросы и изменения
`/api`, а также realtime transport `/hubs`. Лимиты задаются в `RateLimiting:Auth`,
`RateLimiting:Reads`, `RateLimiting:Mutations` и `RateLimiting:Realtime`; полностью отключать их
допустимо только в изолированном тестовом окружении.

## Forwarded headers (proxy)

Если backend работает за reverse proxy/load balancer, настройте секцию `ForwardedHeaders`:

- `ForwardedHeaders__Enabled=true` (глобальное включение/отключение обработки forwarded headers)
- `ForwardedHeaders__TrustedProxies__0=203.0.113.10` (отдельные IP прокси)
- `ForwardedHeaders__TrustedNetworks__0=10.0.0.0/24` (доверенные подсети в CIDR)

Локально в `Development` по умолчанию включен совместимый режим:

- `ForwardedHeaders__TrustAllProxiesInDevelopment=true`

Для stage/prod рекомендуется оставить только trusted proxy/network и не полагаться на "trust all".
Для тестовых/специальных окружений можно временно отключить поведение полностью: `ForwardedHeaders__Enabled=false`.

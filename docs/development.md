# Development Workflow

Единая operational-инструкция для локальной разработки, проверки и синхронизации generated-артефактов.

## Prerequisites

- Docker (Compose support)
- .NET 10 SDK (точная feature band закреплена в `global.json`)
- Node.js LTS

Восстановите закреплённые .NET tools из корня репозитория:

```bash
dotnet tool restore
```

Команды EF запускайте через локальный manifest, чтобы версия CLI совпадала с runtime-пакетами:

```bash
dotnet tool run dotnet-ef migrations has-pending-model-changes --project backend/backend.csproj --startup-project backend/backend.csproj --no-build
```

## First-Time Setup

### Windows (bat wrappers in repo root)

```bat
setup-local.bat
```

### macOS/Linux (or cross-platform PowerShell)

```bash
pwsh backend/scripts/setup-local.ps1
```

`setup-local` поднимает docker-инфраструктуру, применяет миграции и подготавливает тестовые media.
На Windows `setup-local.bat` и `dev-full.bat` сначала запускают
`backend/scripts/ensure-docker-desktop.ps1`: если Docker Engine недоступен, скрипт завершает
только процессы Docker Desktop, переносит повреждённые transient socket-каталоги `run` и
`docker-secrets-engine` в `%LOCALAPPDATA%\Docker\runtime-quarantine`, запускает Desktop скрыто
и ждёт готовности Engine. Volumes, images и project data не удаляются.

## Daily Development

### Full stack from repo root

```bash
npm install
npm run dev
```

### Windows alternative

```bat
dev-full.bat
```

## Reset Local Data (Destructive)

### Windows

```bat
reset-local.bat
```

### macOS/Linux (or cross-platform PowerShell)

```bash
pwsh backend/scripts/reset-local.ps1
```

## Transport Contract Workflow

Source of truth: `backend/openapi/deadmans.v1.yaml` (HTTP + SignalR in `x-signalr`).

After transport contract changes:

```bash
npm --prefix frontend run generate:transport
```

This regenerates:

- `frontend/src/shared/api/contracts/generated.ts`
- `frontend/src/shared/realtime/generated.ts`

Optional partial regeneration:

- `npm --prefix frontend run generate:contracts` (HTTP/OpenAPI schemas only)
- `npm --prefix frontend run generate:realtime` (SignalR hubs/events only)

Do not hand-edit generated files.

## Verification Before PR

- Backend tests:
  - `dotnet build backend/backend.slnx --configuration Release`
  - `dotnet test backend/backend.slnx --configuration Release --no-build`
  - `dotnet tool run dotnet-ef migrations has-pending-model-changes --project backend/backend.csproj --startup-project backend/backend.csproj --configuration Release --no-build`
- Frontend quality gate:
  - `npm --prefix frontend run check`
  - включает Prettier check, строгий TypeScript, ESLint, locale consistency и поиск hardcoded UI-текста, Vitest с V8 coverage для критичных модулей, Knip и production build
  - отдельный coverage-прогон: `npm --prefix frontend run test:coverage`
- Generated artifacts are up to date:
  - run `npm --prefix frontend run generate:transport`
  - ensure no unexpected git diff in generated paths

CI собирает и тестирует backend в `Release`, блокирует расхождение EF-модели с миграциями,
устанавливает frontend dependencies через `npm --prefix frontend ci` и запускает тот же
`npm --prefix frontend run check`, поэтому локальная проверка совпадает с pull request pipeline.

Playwright smoke tests используют перехват HTTP на уровне браузера и не зависят от Twitch,
локальной БД или секретов. Локальный запуск после установки Chromium:

```powershell
npx --prefix frontend playwright install chromium
npm --prefix frontend run test:e2e
```

CI запускает Chromium smoke для anonymous redirect, role-based routing и доступа администратора
к каталогу вопросов после основного frontend quality gate.

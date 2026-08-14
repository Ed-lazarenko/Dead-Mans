# Development Workflow

Единая operational-инструкция для локальной разработки, проверки и синхронизации generated-артефактов.

## Prerequisites

- Docker (Compose support)
- .NET 8 SDK
- Node.js LTS

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
  - `dotnet test backend/backend.slnx`
- Frontend quality gate:
  - `npm --prefix frontend run check`
  - включает Prettier check, строгий TypeScript, ESLint, locale consistency и поиск hardcoded UI-текста, Vitest с V8 coverage для критичных модулей, Knip и production build
  - отдельный coverage-прогон: `npm --prefix frontend run test:coverage`
- Generated artifacts are up to date:
  - run `npm --prefix frontend run generate:transport`
  - ensure no unexpected git diff in generated paths

CI устанавливает frontend dependencies через `npm --prefix frontend ci` и запускает тот же
`npm --prefix frontend run check`, поэтому локальная проверка совпадает с pull request pipeline.

Playwright smoke tests будут добавлены отдельным этапом после появления стабильных test data и
auth fixture; плановый минимальный набор — 3–5 ключевых пользовательских сценариев.

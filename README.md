# Dead-Mans

Текущий scope проекта предельно узкий:

- Twitch authentication;
- одна защищенная страница `game-board`;
- read-only загрузка игрового поля из БД через `GET /api/game`.

Все другие игровые панели и моковые срезы выведены из активного кода.

## Активные части репозитория

- `frontend/` - React SPA с Twitch auth и страницей `game-board`
- `backend/` - ASP.NET Core Web API с auth endpoint'ами и `GET /api/game`
- `backend/openapi/deadmans.v1.yaml` - канонический контракт API

`legacy-v1/` остается только как reference и не участвует в развитии текущего приложения.

## Быстрый старт

Backend:

```powershell
Set-Location backend
.\scripts\setup-local.ps1
dotnet run --project backend.csproj
```

Frontend:

```bash
cd frontend
npm install
npm run dev
```

## Локальный bootstrap

Безопасный backend bootstrap:

```powershell
Set-Location backend
.\scripts\setup-local.ps1
```

Что делает команда:

1. создает `.env` из `.env.example`, если его нет;
2. поднимает `postgres` и `minio`, если они не запущены;
3. дожидается готовности контейнеров;
4. накатывает EF Core migrations;
5. загружает тестовые картинки игрового поля в MinIO.

Существующие локальные данные при этом не удаляются.

Для полного destructive reset backend:

```powershell
Set-Location backend
.\scripts\reset-local.ps1
```

Или без интерактивного подтверждения:

```powershell
Set-Location backend
.\scripts\reset-local.ps1 -Force
```

Тестовые картинки лежат в репозитории в `backend/assets/test-game-board/elements/`.

## Contract workflow

- source of truth: `backend/openapi/deadmans.v1.yaml`
- после изменения API регенерируйте frontend-типы:

```bash
npm --prefix frontend run generate:contracts
```


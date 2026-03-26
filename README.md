# Dead-Mans

Dead-Mans - интерактивная панель управления для стрима и зрительских ивентов. Проект позволяет вести игру с командами, считать очки, применять модификаторы от зрителей и управлять ходом раунда в реальном времени.

## Активный контур

В активной разработке находятся только две части:

- `frontend/` - React SPA для панели управления.
- `backend/` - ASP.NET Core Web API для игровых сценариев и будущих интеграций.

`legacy-v1/` оставлен в репозитории только как reference-источник старой логики и UX-идей. Новый код в него не добавляется и напрямую оттуда не копируется.

## Репозиторий сейчас

- `frontend/` - feature-based SPA на React + TypeScript + Vite.
- `backend/` - layered ASP.NET Core Web API: game-срезы пока используют in-memory adapters, auth и пользовательские роли уже работают через EF Core `ApplicationDbContext`.
- `backend/openapi/deadmans.v1.yaml` - канонический transport-контракт API.
- `docs/architecture/overview.md` - краткая карта текущей архитектуры.
- `CONTEXT.md` - живой контекст проекта и история решений.
- `STACK.md` - выбранный стек и инфраструктурные ориентиры.

## Быстрый старт

Локальный запуск всего активного контура:

```bash
npm install
npm --prefix frontend install
npm run dev
```

Команда поднимет backend и frontend параллельно. По умолчанию frontend будет доступен на `http://localhost:5180`, backend - на `http://localhost:5285`.
Twitch auth требует настроенный `ConnectionStrings:DefaultConnection` и заполненную секцию `TwitchAuth`; без persistence-конфигурации backend завершит старт с понятной ошибкой.

Если нужен отдельный запуск:

```bash
npm run dev:backend
npm run dev:frontend
```

## Docker: инфраструктура для локалки

Первый шаг по DO-first курсу - поднимаем только инфраструктуру (`postgres + minio`) через Docker Compose:

```bash
copy .env.example .env
docker compose up -d
docker compose ps
```

Альтернатива через `npm scripts`:

```bash
npm run docker:up
npm run docker:ps
```

Проверка:

- PostgreSQL: `localhost:5432`
- MinIO API: `http://localhost:9000`
- MinIO Console: `http://localhost:9001`

Остановка:

```bash
docker compose down
```

Сброс инфраструктуры вместе с данными volume (осторожно, удаляет локальные данные):

```bash
docker compose down -v
```

Если хочется быстро посмотреть статус и логи:

```bash
docker compose ps
docker compose logs -f postgres
docker compose logs -f minio
```

## Что уже есть

- лоадауты, таблица лидеров, модификаторы и game controls;
- role-aware маршруты на frontend;
- HTTP API с OpenAPI/Swagger;
- единый transport-контракт через `backend/openapi/deadmans.v1.yaml` и генерацию TS-типов для frontend.

## Документация

- общий стек: `STACK.md`
- текущий контекст: `CONTEXT.md`
- фронтенд: `frontend/README.md`
- backend: `backend/README.md`
- архитектурный обзор: `docs/architecture/overview.md`

## Contract workflow

- source of truth для transport-контрактов: `backend/openapi/deadmans.v1.yaml`;
- при любом изменении API сначала обновляйте OpenAPI, затем запускайте `npm run generate:contracts`;
- после регенерации держите в sync и публичные re-export'ы в `frontend/src/shared/api/contracts/index.ts`;
- auth endpoints (`/auth/*`) описаны в том же OpenAPI, но с отдельным base URL (`/`), тогда как игровые API работают под `/api`.


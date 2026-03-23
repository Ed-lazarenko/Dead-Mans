# Dead-Mans

Dead-Mans - интерактивная панель управления для стрима и зрительских ивентов. Проект позволяет вести игру с командами, считать очки, применять модификаторы от зрителей и управлять ходом раунда в реальном времени.

## Активный контур

В активной разработке находятся только две части:

- `frontend/` - React SPA для панели управления.
- `backend/` - ASP.NET Core Web API для игровых сценариев и будущих интеграций.

`legacy-v1/` оставлен в репозитории только как reference-источник старой логики и UX-идей. Новый код в него не добавляется и напрямую оттуда не копируется.

## Репозиторий сейчас

- `frontend/` - feature-based SPA на React + TypeScript + Vite.
- `backend/` - layered backend skeleton с реальными application/domain/infrastructure границами и in-memory адаптерами хранения.
- `backend/openapi/deadmans.v1.yaml` - канонический transport-контракт API.
- `docs/architecture/overview.md` - краткая карта текущей архитектуры.
- `CONTEXT.md` - живой контекст проекта и история решений.
- `STACK.md` - выбранный стек и инфраструктурные ориентиры.

## Быстрый старт

Локальный запуск всего активного контура:

```bash
npm install
npm run dev
```

Команда поднимет backend и frontend параллельно. По умолчанию frontend будет доступен на `http://localhost:5180`, backend - на `http://localhost:5285`.

Если нужен отдельный запуск:

```bash
npm run dev:backend
npm run dev:frontend
```

## Что уже есть

- лоадауты, таблица лидеров, модификаторы и game controls;
- role-aware маршруты на frontend;
- HTTP API с OpenAPI/Swagger;
- единый transport-контракт через `backend/openapi/deadmans.v1.yaml` и генерацию TS-типов для frontend;
- mock/http переключение на frontend через `VITE_API_MODE`.

## Документация

- общий стек: `STACK.md`
- текущий контекст: `CONTEXT.md`
- фронтенд: `frontend/README.md`
- backend: `backend/README.md`
- архитектурный обзор: `docs/architecture/overview.md`


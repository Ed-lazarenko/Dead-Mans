# Dead-Mans

Веб-приложение: авторизация через Twitch, защищённая панель с игровым полем (`game-board`), админским разделом настройки (`game-setup`), регистрацией команд (`game-application` / `team-registrations`), модификаторами игры и админским открытием ячеек. Снимок поля читается из PostgreSQL (`GET /api/game`), открытие ячейки выполняется через `POST /api/game/cells/{cellId}/open`, модификаторы активируются через `POST /api/game/modifiers/{modifierId}/activate`, а realtime-обновления приходят через SignalR. Медиа для ячеек — через S3-совместимое хранилище (локально MinIO).

**Стек:** React + TypeScript (Vite) · ASP.NET Core 10 · PostgreSQL · MinIO. Контракт HTTP API: [`backend/openapi/deadmans.v1.yaml`](backend/openapi/deadmans.v1.yaml).

| Каталог | Роль |
|---------|------|
| `frontend/` | SPA |
| `backend/` | Web API, EF Core, миграции |
| `legacy-v1/` | reference, не участвует в активной разработке |

Дополнительно: [`CONTEXT.md`](CONTEXT.md) · [`docs/development.md`](docs/development.md) · [`STACK.md`](STACK.md) · [`docs/architecture/overview.md`](docs/architecture/overview.md) · [`docs/architecture/realtime.md`](docs/architecture/realtime.md) · [`docs/architecture/data-retention.md`](docs/architecture/data-retention.md) · [`backend/README.md`](backend/README.md) · [`frontend/README.md`](frontend/README.md).

## Требования для локального запуска

- **Docker** (с поддержкой Compose) — PostgreSQL и MinIO
- **.NET 10 SDK** — backend (точная feature band закреплена в `global.json`)
- **Node.js** (LTS) — frontend

Перед настройкой и запуском должен быть запущен Docker (daemon / Docker Desktop).

| Сервис | Адрес |
|--------|--------|
| Frontend | http://localhost:5180 |
| Backend | http://localhost:5285 |
| MinIO API | http://localhost:9000 |
| MinIO Console | http://localhost:9001 |

## Локальный запуск на Windows (`.bat` в корне репозитория)

Запускайте файлы из **корневой папки клона** (рядом с `docker-compose.yml`).

**Порядок работы**

1. Один раз после клона или на новой машине: **`setup-local.bat`** — поднимает контейнеры, создаёт/обновляет базу, заливает тестовые данные в MinIO. Уже существующие данные в Docker volumes не удаляет.
2. Для ежедневной разработки: **`dev-full.bat`** — поднимает backend и frontend вместе. Окно с логами не закрывайте, пока работаете.
3. Сайт открывать по адресу http://localhost:5180

**Какой батник для чего**

| Файл | Назначение |
|------|------------|
| **`setup-local.bat`** | Первичная подготовка окружения (БД, MinIO, миграции, тестовые картинки). Запускать до первого `dev-full`, если окружение ещё не собирали на этой машине. |
| **`dev-full.bat`** | Обычный режим: API + интерфейс одновременно. |
| **`dev-backend.bat`** | Только API (порт 5285), если frontend запускаете отдельно. |
| **`dev-frontend.bat`** | Только интерфейс (порт 5180), если API уже запущен. |
| **`dev-stop.bat`** | Освободить порты 5285 и 5180, если процесс «завис» или окно закрылось некорректно. После этого снова **`dev-full.bat`**. |
| **`reset-local.bat`** | Полный сброс локальных данных Postgres и MinIO в Docker (volumes проекта). Затем снова выполняется полная настройка как после `setup-local`. Использовать осознанно. |
| **`dev-common.bat`** | Служебный файл для остальных `dev-*.bat`, **вручную не запускать**. |

## Не Windows

Локальный запуск на macOS/Linux и общий cross-platform workflow — в [`docs/development.md`](docs/development.md).

## Контракт API и типы на frontend

Источник правды: `backend/openapi/deadmans.v1.yaml`. После изменений контракта transport-артефакты frontend перегенерируются командой `npm --prefix frontend run generate:transport` (см. [`docs/development.md`](docs/development.md)). HTTP-вызовы используют `openapi-fetch` поверх generated `paths`, поэтому endpoint, параметры, body и response проверяются TypeScript.

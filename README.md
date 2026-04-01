# Dead-Mans

Веб-приложение: авторизация через Twitch, защищённая страница игрового поля (`game-board`), данные поля отдаются read-only из PostgreSQL (`GET /api/game`). Медиа для ячеек — через S3-совместимое хранилище (локально MinIO).

**Стек:** React + TypeScript (Vite) · ASP.NET Core 8 · PostgreSQL · MinIO. Контракт HTTP API: [`backend/openapi/deadmans.v1.yaml`](backend/openapi/deadmans.v1.yaml).

| Каталог | Роль |
|---------|------|
| `frontend/` | SPA |
| `backend/` | Web API, EF Core, миграции |
| `legacy-v1/` | reference, не участвует в активной разработке |

Дополнительно: [`STACK.md`](STACK.md) · [`docs/architecture/overview.md`](docs/architecture/overview.md) · [`backend/README.md`](backend/README.md) · [`frontend/README.md`](frontend/README.md).

## Требования для локального запуска

- **Docker** (с поддержкой Compose) — PostgreSQL и MinIO
- **.NET 8 SDK** — backend
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

Локальный запуск на macOS/Linux и прочие детали по частям приложения — в **`backend/README.md`** и **`frontend/README.md`**.

## Контракт API и типы на frontend

Источник правды: `backend/openapi/deadmans.v1.yaml`. После изменений контракта типы для frontend перегенерируются командой из `frontend` (см. [`frontend/README.md`](frontend/README.md), скрипт `generate:contracts`).

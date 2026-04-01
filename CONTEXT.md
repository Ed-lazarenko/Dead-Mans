## Dead-Mans - контекст проекта

Этот файл нужен как короткая актуальная карта репозитория: что уже реализовано, где проходят архитектурные границы и на что опираться при следующих изменениях.

---

## Принятый курс

- Проект на ранней стадии, критичных данных в БД нет, поэтому смена технологического baseline выполняется сейчас.
- Целевой курс: **DigitalOcean-first** окружение с максимально близкой локалкой.
- Зафиксированный стек и инфраструктурные правила: `STACK.md`.
- Базовый принцип: локальная разработка с инфраструктурой в контейнерах (`postgres + minio`), приложение запускается из исходников; прод в DO App Platform через Dockerfile.

---

## 1. Что это за проект

**Dead-Mans** - панель управления для стрима и интерактивных игровых сценариев.

Основные сценарии:
- авторизация через Twitch.
- read-only просмотр игрового поля из БД.

---

## 2. Активный контур

В активной разработке находятся только:
- `frontend/`
- `backend/`

`legacy-v1/` хранится в репозитории только как reference-источник старой логики и UX-идей. Новый код туда не добавляется.

---

## 3. Текущее устройство фронтенда

Фронтенд живёт в `frontend/` и представляет собой React SPA с feature-first структурой:

- `src/app/` - composition root, providers, theme;
- `src/routes/` - route config;
- `src/features/*` - feature UI, hooks/model и feature-facing data access;
- `src/shared/api/client/` - общий HTTP transport;
- `src/shared/api/config.ts` - единая env-конфигурация (`VITE_API_BASE_URL`, `VITE_BACKEND_ORIGIN`);
- `src/shared/api/contracts/` - generated transport types из OpenAPI и стабильные alias-типы;
- `src/shared/auth/` - auth context, API и route guards;
- `src/locales/` - языковые ресурсы.

### Что важно архитектурно

- auth HTTP использует тот же общий `httpClient`, что и игровые API, только с другим `baseUrl` для `/auth/*`;
- transport-типы импортируются централизованно из `src/shared/api/contracts/index.ts`.

### Основные страницы

- `AuthLandingPage` - вход через Twitch.
- `TwitchAuthCallbackPage` - завершение OAuth flow и восстановление сессии.
- `GameBoardPage` - игровое поле из БД (read-only).

Фронтенд работает через backend HTTP API.

---

## 4. Текущее устройство backend

Backend живет в `backend/` и представляет собой layered ASP.NET Core Web API:

- `Api/Contracts/` - HTTP DTO;
- `Api/Mapping/` - mapping application-моделей в transport DTO;
- `Application/` - use-case сервисы и repository/auth ports;
- `Domain/` - доменные модели и правила;
- `Infrastructure/` - adapters и DI;
- `Data/` - EF Core `ApplicationDbContext`, entities, configurations, migrations (Npgsql/PostgreSQL baseline);
- `Controllers/` - thin HTTP layer;
- `openapi/deadmans.v1.yaml` - канонический transport source of truth.

### Важный текущий split

- игровое поле (`IGameBoardRepository` / `GET /api/game`) работает через persistence-backed adapter (EF Core/PostgreSQL);
- auth, users и roles уже DB-backed через `ApplicationDbContext` и EF Core;
- медиа ячеек отдаются как публичные URL, собранные backend из storage metadata.

### Auth flow

- `GET /auth/twitch/login` - редиректит на Twitch OAuth;
- `GET /auth/twitch/callback` - завершает Twitch login, upsert'ит пользователя и ставит auth cookie; при отклонённом входе или неактивном аккаунте редиректит frontend на callback-страницу с `reason=*`;
- `GET /auth/me` - возвращает текущую сессию;
- `POST /auth/logout` - завершает сессию.

Runtime-роли для backend-авторизации считаются из БД на каждом аутентифицированном запросе. Cookie хранит identity/session claims, а не долгоживущий snapshot ролей.
Деактивированные пользователи не могут получить новую auth-сессию. Права на игровые endpoint'ы задаются выборочно на уровне самих контроллеров/действий.

Auth требует корректно настроенный `ApplicationDbContext`. Если backend запущен без persistence-конфигурации, старт должен завершаться явной ошибкой, а не оставлять приложение в полу-рабочем состоянии.

---

## 5. Контракты и Swagger

Канонический source of truth для transport-контрактов:

- `backend/openapi/deadmans.v1.yaml`

Frontend генерирует типы через:

```bash
npm --prefix frontend run generate:contracts
```

Swagger UI в development должен смотреть на тот же YAML-файл, чтобы документация и generation не расходились.

---

## 6. Что уже реализовано

- feature-first фронтенд с `app/`, `routes/`, `features/`, `shared/`, `locales/`;
- auth context и protected route для панели;
- Twitch login flow с backend cookie session;
- game API на панели: `GET /api/game`;
- централизованный query key слой на frontend;
- общий `httpClient` для frontend API;
- OpenAPI contract generation для frontend;
- безопасный локальный backend bootstrap через `backend/scripts/setup-local.ps1` без удаления БД и storage;
- отдельный destructive reset через `backend/scripts/reset-local.ps1` с подтверждением;
- backend tests для auth transport-контрактов;
- документация по стеку и архитектуре.

---

## 7. Что важно не забыть

- активный код живёт только в `frontend/` и `backend/`;
- `legacy-v1/` можно читать, но нельзя использовать как источник прямого копирования;
- backend controllers должны зависеть от application services/ports, а не от EF/infrastructure напрямую;
- transport DTO нельзя смешивать с application/domain моделями;
- user-facing строки должны проходить через i18n;
- при изменении transport-контракта сначала обновляется OpenAPI, потом регенерируются frontend types.

---

## 8. Быстрый возврат в проект

Если открываешь репозиторий после паузы:

1. Прочитай `README.md`.
2. Посмотри `docs/architecture/overview.md`.
3. Проверь `frontend/src/features/*` и `backend/Application/`.
4. Убедись, что OpenAPI и generated contracts синхронизированы.
5. Локальный bootstrap: `backend/scripts/setup-local.ps1` (см. README; Windows — `setup-local.bat` при необходимости).
6. Разработка: из корня `npm install`, `npm run dev` (или `dev-full.bat` на Windows).
7. Сброс данных: `backend/scripts/reset-local.ps1` или `reset-local.bat`.
8. Для auth-проверок не забывай, что backend требует настроенный DB-backed `ApplicationDbContext`.

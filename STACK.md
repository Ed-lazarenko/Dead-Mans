## Общий стек проекта

Проект разделен на несколько частей:

- **legacy-v1**: старый прототип (vanilla JS + socket.io + localStorage) — только как reference-источник идей.
- **frontend**: активный SPA на React + TypeScript.
- **backend**: активный ASP.NET Core Web API.
- **OpenAPI contract**: `backend/openapi/deadmans.v1.yaml` - канонический transport-контракт для frontend/backend.

Ниже — актуальный стек проекта и инфраструктурный курс разработки.

---

## Актуальный курс разработки

- Разрабатываем локально максимально близко к будущему прод-окружению в DigitalOcean.
- Базовая модель окружения: контейнеры для инфраструктуры (`postgres`, `minio`), приложение запускается из исходников.
- Production-путь остаётся DigitalOcean-first и env-driven.

---

## Frontend

- **Vite 7 + React 19 + TypeScript**
  - **Vite**: быстрый dev-сервер и сборщик, удобен для SPA, быстрые перезапуски.
  - **React**: основа клиентского UI, декларативные компоненты и экосистема.
  - **TypeScript**: статическая типизация, защита от типичных багов, само-документация кода.

- **React Router v7 (`react-router-dom`)**
  - Навигация и роутинг внутри SPA: login, callback и защищённая панель `/panel` (`game-board`, `game-setup`, `game-application`, `team-registrations`).

- **TanStack Query (`@tanstack/react-query`)**
  - Управление серверным состоянием: запросы к API, кеш, рефетч, статусы загрузки/ошибок.
  - Позволяет не тащить данные с сервера в Redux/глобальный стейт.
  - Query keys и `queryOptions` находятся в owning feature; общие mutation invalidation/error policies задаются через `mutationOptions`.

- **OpenAPI TypeScript + openapi-fetch**
  - `openapi-typescript` генерирует `paths` из канонического OpenAPI-контракта.
  - `openapi-fetch` выводит endpoint, path/query/body и response-типы без ручных generic-типов и динамической сборки URL.
  - Отдельные domain adapters остаются только для `404 → null`, mapping, multipart и optimistic cache updates.

- **MUI (`@mui/material`, `@emotion/*`)**
  - Готовые UI-компоненты и тема для панели.
  - App theme собирается из отдельных palette, typography и component override модулей; повторяемые стили принадлежат theme/shared UI.
  - Ускоряет разработку, не надо верстать все с нуля.

- **React Hook Form + Zod**
  - Transactional submitted-формы используют типизированное состояние и декларативную валидацию через `zodResolver`; схемы вынесены из компонентов, а values выводятся через `z.infer`.
  - Плотные интерактивные draft-редакторы остаются controlled React state и не превращаются в одну большую RHF-форму.
  - Zod также применяется выборочно для runtime-проверки критичных API-ответов; generated OpenAPI-типы остаются compile-time source of truth.

- **Интернационализация (`react-i18next`, `i18next`, `i18next-browser-languagedetector`)**
  - **i18next/react-i18next**: i18n-движок, feature-owned ресурсы, переключение языков и TypeScript-проверка ключей через module augmentation.
  - **browser-languagedetector**: автоматический выбор языка по браузеру/настройкам.
  - Locale parity check сравнивает `en/ru/uk/pl` внутри каждого feature translation module.

- **SignalR клиент (`@microsoft/signalr`)**
  - Используется для realtime-синхронизации `game-board` (`cellOpened`, `modifierActivated`) и `game-setup` (`draftChanged`).
  - Shared hook владеет connection lifecycle, а фичи — регистрацией и обработкой своих событий.

- **Тестирование (Vitest + React Testing Library + V8 coverage)**
  - Юнит-тесты для model/shared logic и поведенческие тесты hooks/components в `jsdom`.
  - Coverage thresholds применяются к критичным setup, registration, realtime, route и capability-модулям; формальный глобальный процент для всего SPA не используется.
  - Playwright запланирован отдельным smoke-слоем из 3–5 сценариев после подготовки стабильных test fixtures.

- **Качество кода (Prettier + ESLint + Knip)**
  - Prettier задаёт единый формат, ESLint проверяет кодовые правила, Knip обнаруживает неиспользуемые файлы, exports и dependencies.
  - `@tanstack/eslint-plugin-query` в strict recommended режиме проверяет TanStack Query hooks и dependency stability.
  - TypeScript включает `noUncheckedIndexedAccess` и `exactOptionalPropertyTypes` для app и tooling-кода.
  - Каноническая локальная проверка: `npm --prefix frontend run check`.

- **Client-only state и UI add-ons — по потребности**
  - Локальное UI-state остаётся в React; Zustand вводится только для реального cross-tree состояния и не дублирует TanStack Query/auth.
  - Framer Motion, Lucide, SVGR и отдельные toast/icon packs добавляются вместе с использующей их фичей, а не как неиспользуемый baseline.

---

## Backend (.NET)

- **ASP.NET Core 10 (Web API / Controllers)**
  - Основной backend, REST/HTTP API для фронта, хорошая производительность и экосистема.
  - Архитектурный baseline: thin controllers поверх Application/Domain/Infrastructure split.

- **Entity Framework Core + Npgsql**
  - ORM и провайдер под PostgreSQL как целевой persistence слой.
  - Миграции — через EF Core migrations.

- **PostgreSQL 16**
  - Единый baseline для локальной разработки и будущего прода.
  - Auth/users/roles и game board уже DB-backed.

- **OpenAPI / Swagger**
  - Контракт API и документация endpoint'ов.
  - `backend/openapi/deadmans.v1.yaml` используется как transport source of truth для генерации frontend-типов.

- **SignalR (серверная часть)**
  - Реал-тайм канал для синхронизации игрового поля без полного перезапроса после открытия ячеек.

- **Cookie auth + собственные роли**
  - Текущая auth-модель: cookie authentication, Twitch OAuth и собственные EF-сущности пользователей/ролей.

- **OAuth2 Twitch**
  - Авторизация через Twitch, чтобы привязывать пользователей к их Twitch-аккаунтам.

- **ImageSharp / SkiaSharp**
  - Потенциальный стек для серверной генерации составных изображений (оверлеи, итоги игр, карточки).

- **Message Queue (на будущее: RabbitMQ / Kafka / Azure Service Bus)**
  - Асинхронные задачи и интеграции (например, тяжелая обработка, батчевые операции, межсервисные сообщения).

---

## Файлы и хранилище

- **S3-compatible API через AWS SDK for .NET**
  - Локально: **MinIO**.
  - Прод: **DigitalOcean Spaces**.
  - В БД храним object key; URL формируем на лету.
  - Presigned URLs используем для приватных файлов, публичные ассеты отдаем обычными URL.

---

## Инфраструктура и деплой

- **Docker Compose (локально)**
  - Базовый набор сервисов: `postgres`, `minio`.
  - Приложение локально запускается из исходников поверх этой инфраструктуры.

- **DigitalOcean App Platform (целевая платформа)**
  - Деплой из GitHub или container image.
  - Настройки окружения и секреты ведутся через env variables.

- **Конфигурация**
  - Приложение должно уметь работать от env variables.
  - Порядок для БД: сначала `DATABASE_URL`, иначе локальный connection string.

- **Git + GitHub**
  - Хранение кода, история изменений, code review, CI/CD.

- **CI/CD (GitHub Actions)**
  - Автоматическая сборка и тесты при `push` в `main` и на всех `pull_request`; frontend устанавливается через `npm ci` и проходит единый `npm run check`.
  - Деплой настраивается отдельным workflow.

- **Логирование и мониторинг**
  - ASP.NET logging + наблюдаемость (Application Insights / Grafana + Prometheus — по мере развития проекта).

---

## Что сознательно не выбираем

- SQL Server как основную БД при целевом проде на DO + Postgres.
- SQL Server-специфичные типы/SQL в новой бизнес-логике.
- Локальное файловое хранение "на диске" как основной путь.
- Скрытые альтернативные runtime-path'ы, которые расходятся с env-driven и Postgres/S3-first baseline.
- Раннее встраивание локальной LLM в основной сервис.

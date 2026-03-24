## Общий стек проекта

Проект разделён на несколько частей:
- **legacy-v1**: старый прототип (vanilla JS + socket.io + localStorage) - только как reference-источник идей.
- **frontend**: активный SPA на React + TypeScript.
- **backend**: активный ASP.NET Core Web API.
- **OpenAPI contract**: `backend/openapi/deadmans.v1.yaml` - канонический transport-контракт для frontend/backend.

Ниже — выбранный стек с кратким описанием, зачем нужна каждая технология.

---

## Frontend

- **Vite + React + TypeScript**
  - **Vite**: быстрый dev‑сервер и сборщик, удобен для SPA, очень быстрые перезапуски.
  - **React 18/19**: основа клиентского UI, декларативные компоненты и экосистема.
  - **TypeScript**: статическая типизация, защита от типичных багов, само‑документация кода.

- **React Router v7 (`react-router-dom`)**
  - Навигация и роутинг внутри SPA: разные URL для вкладок (лоадауты, таблица, модификаторы, управление).

- **TanStack Query (`@tanstack/react-query`)**
  - Управление **серверным состоянием**: запросы к API, кеш, рефетч, статус загрузки/ошибок.
  - Позволяет не тащить данные с сервера в Redux/глобальный стейт.

- **MUI (`@mui/material`, `@mui/icons-material`, `@emotion/*`)**
  - Готовые UI‑компоненты (кнопки, таблицы, модалки, вкладки) + тема (dark/light), кастомизация под бренд.
  - Ускоряет разработку, не надо верстать всё с нуля.

- **Формы и валидация (`react-hook-form`, `zod`, `@hookform/resolvers`)**
  - **react-hook-form**: управление состоянием форм с высокой производительностью.
  - **zod**: декларативные схемы валидации.
  - **@hookform/resolvers**: связывает `react-hook-form` и `zod` для типизированной валидации.

- **Международзация (`react-i18next`, `i18next`, `i18next-browser-languagedetector`)**
  - **i18next/react-i18next**: удобный i18n‑движок, переключение языков и ключи переводов.
  - **browser-languagedetector**: автоматический выбор языка по браузеру/настройкам.

- **SignalR клиент (`@microsoft/signalr`, будет добавлен позже)**
  - Реал‑тайм обновления (изменения очков, модификаторы, активная команда) через WebSocket/SignalR.

- **Тестирование (на перспективу: Vitest + React Testing Library)**
  - Юнит‑ и компонентные тесты без привязки к конкретному рантайму браузера.

---

## Backend (.NET)

- **ASP.NET Core 8+ (Web API / Minimal APIs)**
  - Основной backend, REST/HTTP‑API для фронта и бота, хорошая производительность и экосистема.

- **Entity Framework Core**
  - ORM для работы с SQL базой. Сейчас в коде подключён SQL Server provider; PostgreSQL остаётся возможным вариантом на будущее.

- **SQL база (SQL Server или PostgreSQL)**
  - Сейчас используется для auth/users/roles. Игровые срезы пока ещё живут на in-memory adapters и будут переводиться на persistence поэтапно.

- **OpenAPI / Swagger**
  - Контракт API и документация endpoint'ов.
  - `backend/openapi/deadmans.v1.yaml` используется как transport source of truth для генерации frontend-типов.

- **SignalR (серверная часть)**
  - Планируемый реал‑тайм канал для фронта и, при желании, для бота (обновления без перезагрузки страницы).

- **Cookie auth + собственные роли**
  - Текущая auth-модель: cookie authentication, Twitch OAuth и собственные EF-сущности пользователей/ролей.

- **OAuth2 Twitch**
  - Авторизация через Twitch, чтобы привязывать пользователей к их Twitch‑аккаунтам.

- **ImageSharp / SkiaSharp**
  - Потенциальный стек для серверной генерации составных изображений (оверлеи, итоги игр, карточки).

- **Message Queue (на будущее: RabbitMQ / Kafka / Azure Service Bus)**
  - Асинхронные задачи и интеграции (например, тяжёлая обработка, батчевые операции, меж‑сервисные сообщения).

---

## Инфраструктура и деплой

- **Git + GitHub**
  - Хранение кода, история изменений, code review, CI/CD.

- **CI/CD (GitHub Actions)**
  - Автоматическая сборка и тесты при `push` в `main` и на всех `pull_request`; деплой будет добавлен отдельно.

- **Хостинг фронта (варианты)**
  - **Vercel / Netlify / Azure Static Web Apps / S3+CloudFront**: деплой собранного SPA (`frontend`).

- **Хостинг backend**
  - **Azure App Service / Kubernetes / Docker на VPS**: хостинг ASP.NET Core API + SignalR.

- **Managed SQL DB**
  - Azure SQL / RDS / Cloud SQL — управляемая SQL база, бэкапы и масштабирование.

- **Логирование и мониторинг**
  - ASP.NET logging + Application Insights / Grafana + Prometheus (в будущем) для метрик и логов.



# Dead-Mans Frontend

Frontend - активный SPA-пакет проекта Dead-Mans. Он работает вместе с backend как часть единого активного контура.

## Стек

- React 19 + TypeScript
- Vite
- React Router
- TanStack Query
- MUI
- i18next / react-i18next

## Структура

- `src/app/` - composition root, theme, providers.
- `src/routes/` - route config и role-aware navigation helpers.
- `src/layouts/` - общие app-shell/layout компоненты.
- `src/features/` - фичи по доменам (`loadout`, `leaderboard`, `modifiers`, `controls`, `auth`).
- `src/shared/api/client/` - HTTP transport.
- `src/shared/api/config.ts` - единая env-конфигурация transport/base URLs.
- `src/shared/api/contracts/` - generated и aliased transport types.
- `src/shared/api/queryKeys.ts` - иерархические query keys.
- `src/shared/auth/` - auth context и route guards.
- `src/shared/session/` - UI persistence helpers.
- `src/locales/` - переводы по языкам.

## Источник контрактов

Frontend не держит transport-контракты вручную как отдельную правду. Канонический источник - `../backend/openapi/deadmans.v1.yaml`.

Сгенерировать типы:

```bash
npm run generate:contracts
```

## Режим API

Frontend работает через backend и использует общий `httpClient`.

Auth-запросы используют тот же общий `httpClient`, но с отдельным `baseUrl` на backend origin для `/auth/*`.
Все HTTP-запросы frontend отправляют `credentials: 'include'`, поэтому frontend готов к credentialed CORS-сценарию. Для реального cross-site cookie deployment этого недостаточно само по себе: backend cookie policy (`SameSite`, `Secure`) и CORS должны быть настроены под конкретную схему размещения.

## Локальный запуск

Из корня репозитория:

```bash
npm run dev:frontend
```

Или внутри пакета:

```bash
npm install
npm run dev
```

По умолчанию dev-server проксирует `/api` на `http://localhost:5285`.

## Сборка

```bash
npm run build
```

## Принцип развития

Новый код должен добавляться через feature-папки и feature-facing data access. `legacy-v1` используется только как reference для логики и UX, но не как источник прямого копирования кода.

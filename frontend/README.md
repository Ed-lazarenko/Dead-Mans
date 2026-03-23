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
- `src/features/` - фичи по доменам (`loadout`, `leaderboard`, `modifiers`, `controls`, `auth`).
- `src/shared/api/client/` - HTTP transport.
- `src/shared/api/contracts/` - generated и aliased transport types.
- `src/shared/api/mocks/` - mock implementations.
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

## Режимы API

Frontend умеет переключаться между mock и HTTP без изменения UI-кода:

- `VITE_API_MODE=mock` - использовать `src/shared/api/mocks/*`
- `VITE_API_MODE=http` - использовать backend через `httpClient`

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

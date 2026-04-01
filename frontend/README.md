# Dead-Mans Frontend

Frontend - активный SPA-пакет проекта Dead-Mans. Он работает вместе с backend как часть единого активного контура.

## Стек

- React 19 + TypeScript
- Vite
- React Router
- TanStack Query
- MUI
- i18next / react-i18next

## Что есть в приложении

- вход через Twitch;
- восстановление сессии через `/auth/me`;
- одна защищенная страница `game-board`, которая читает данные из `GET /api/game`.

## Источник контрактов

Frontend не держит transport-контракты вручную как отдельную правду. Канонический источник - `../backend/openapi/deadmans.v1.yaml`.

Сгенерировать типы:

```bash
npm run generate:contracts
```

## Режим API

Frontend использует общий `httpClient`.

- `GET /api/game` идёт через относительный `/api` base URL;
- auth-запросы идут на backend origin для `/auth/*`;
- все запросы отправляют `credentials: 'include'`.

## Локальный запуск

Backend должен быть поднят по инструкции в корневом `README.md` (`setup-local.ps1` / `setup-local.bat`).

```bash
npm install
npm run dev
```

Из корня репозитория: `npm run dev:frontend` или `npm run dev`.

По умолчанию dev-server проксирует `/api` на `http://localhost:5285`.

## Сборка

```bash
npm run build
```

## Ограничение текущего скоупа

На текущем этапе frontend должен содержать только Twitch auth и read-only экран игрового поля. Любые дополнительные панели и моковые игровые срезы считаются вне скоупа.

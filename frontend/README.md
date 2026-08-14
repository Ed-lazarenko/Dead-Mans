# Dead-Mans Frontend

Frontend - активный SPA-пакет проекта Dead-Mans. Он работает вместе с backend как часть единого активного контура.

## Стек

- React 19 + TypeScript
- Vite
- React Router
- TanStack Query
- MUI
- React Hook Form + Zod
- i18next / react-i18next
- Vitest + React Testing Library + V8 coverage
- Prettier + Knip

## Что есть в приложении

- вход через Twitch;
- восстановление сессии через `/auth/me`;
- защищённая панель под `/panel` с role-aware navigation;
- страница `game-board`, которая читает данные из `GET /api/game`, позволяет admin-пользователям открывать ячейки через `POST /api/game/cells/{cellId}/open` и получает realtime-обновления через SignalR;
- страница `game-setup` (admin): общий черновик в БД (`GET/POST/PUT/DELETE /api/game/setup`), выбор enabled modifiers в draft (`enabledModifierIds`), медиа ячеек (`POST/DELETE /api/game/setup/cells/{cellId}/media`), Save + layout confirm, realtime через `/hubs/game-setup`;
- каталог модификаторов в `game-setup`: администратор выбирает доступные для игры модификаторы; runtime activation UI пока не входит в frontend;
- блок вопросов в `game-setup`: каталог (`GET /api/game/questions/catalog`) с поиском/фильтрацией и enable/disable вопросов/категорий; runtime ask/answer/history endpoints пока доступны только на backend и через generated-контракты;
- страницы регистрации: `game-application` (игроки) и `team-registrations` (moderator/admin) — HTTP через `src/features/game-registration/api/`; confirmed-команды нельзя покинуть напрямую, игрок отправляет заявку на роспуск, а модератор или администратор видит заметное уведомление и подтверждает роспуск в панели команд.

## Структура API-слоя

- `src/shared/api/client/openApiClient.ts` — `openapi-fetch` клиенты поверх generated `paths`, общие credentials/header и перевод error-result в `ApiError`;
- `src/shared/api/contracts/` — generated transport types;
- `src/shared/api/fetch-not-found-as-null.ts` — 404 → `null` для snapshot-read endpoints;
- `src/shared/api/parse-api-response.ts` — единая fail-fast обёртка для выборочной Zod-валидации критичных API-ответов;
- `src/features/*/api/*-queries.ts` — feature-local query keys и `queryOptions`;
- feature mutation modules используют `mutationOptions` для общих invalidation/error policies;
- `src/shared/realtime/use-signalr-hub-lifecycle.ts` — общий connect/reconnect/start/stop lifecycle; event handlers остаются в `features/*/realtime/`;
- `src/features/game-registration/api/` — registration transport (не routed page; используют `game-application` и `team-registrations`);
- `src/features/game-registration/index.ts` — public API registration feature (без deep imports из соседних фич);
- `src/features/game-modifiers/index.ts` — public API modifiers feature;
- `src/app/panel-route-metadata.ts` — metadata/source of truth for route ids, paths, labels and access;
- `src/app/panel-route-config.tsx` — lazy page wiring and optional realtime-sync on top of route metadata;
- `src/app/AppRoutes.tsx` + `src/app/app-route-tree.tsx` — дерево маршрутов (`useRoutes`);
- `src/routes/app-routes.ts` — re-export метаданных, guards и access helpers;
- `src/layouts/` — shell-компоненты панели (`MainLayout`, `PanelNavigation` + `PanelPrimaryNavigation`/`PanelProfileMenu`);
- `src/shared/auth/panel-capabilities.ts` — capability-level access helpers поверх route-level role checks;
- `src/features/*` — feature-first модули; page entrypoints остаются в корне фичи, а нетривиальные внутренности разделяются на `ui/`, `model/`, `api/`, `realtime/`, `theme/` и `lib/` по необходимости.
- Крупные экраны раскладываются на section-компоненты в `features/<feature>/ui/` (например, `game-application/ui/*`, `game-setup/ui/GameSetupSyncActions|BoardNotices|EmptyState`), а крупные orchestration-хуки делятся на focused hooks по одной зоне ответственности (`game-setup`: `use-game-setup-draft` / `use-game-setup-save` / `use-game-setup-cell-media`, собранные тонким `use-game-setup-page`). Одноразовые компоненты не оборачиваются в абстракции.

## Инженерный baseline

- TanStack Query владеет server state. Ответы запросов не дублируются в context/Zustand; обновления проходят через invalidation или `setQueryData`.
- Query keys и `queryOptions` принадлежат фиче-владельцу данных. Повторяемые mutation policies оформляются через `mutationOptions`, а не копируются между hooks.
- `@tanstack/eslint-plugin-query` в strict recommended режиме проверяет стабильность dependencies и использование query options.
- OpenAPI-generated `paths` остаются compile-time source of truth для endpoint, params, body и response. Feature API использует статические path templates через `openapi-fetch`, без ручных response generics и динамической сборки URL.
- Отдельный domain adapter остаётся только там, где есть поведение: `404 → null`, mapping, multipart или optimistic cache update. Пустые `api → data-access` прокси не создаются.
- Zod применяется выборочно на критичных runtime-границах; сейчас так валидируется auth session.
- Transactional submitted-формы с валидацией используют React Hook Form + `zodResolver`. Схемы находятся в feature model вне компонентов, типы values выводятся через `z.infer`, а для MUI-полей используется общий `ControlledFormTextField`.
- Интерактивные ячейки game-setup draft остаются controlled React state: это редактор, а не одна transactional-форма.
- Локальное UI-state остаётся в React. Zustand добавляется только при реальном cross-tree client-state, а не заранее.
- MUI + Emotion и `AppToast` остаются единым UI/feedback baseline. Иконки, Framer Motion, SVG-компоненты и брендовые icon packs добавляются вместе с использующей их фичей.
- Весь user-facing текст проходит через feature-owned или общий i18n resource. Module augmentation i18next проверяет ключи в TypeScript, а `check:i18n` сохраняет parity `en/ru/uk/pl` и не пропускает новые литералы интерфейса.
- TypeScript работает с `noUncheckedIndexedAccess` и `exactOptionalPropertyTypes`; optional-поля не заполняются явным `undefined`, а индексный доступ требует проверки.
- Vitest покрывает setup draft/save/conflict, registration mutations, realtime models, route access и ключевые loading/error/empty/success состояния страниц. Coverage thresholds применяются к критичным модулям по отдельности, а не как формальная глобальная цель.

## UI и стили (единый стандарт)

Фронтенд использует один визуальный baseline в стиле Hunt: Showdown (мрачный фронтир, латунь, мох, пергамент):

- `src/shared/theme/hunt-palette.ts` — каноническая палитра (единственный источник raw colors);
- `src/shared/theme/tokens.ts` — `huntTypography`, spacing и brand tokens;
- `src/shared/theme/surface-sx.ts` — переиспользуемые surface/title/auth presets (`huntPanelSx`, `huntAuthCardSx`, …);
- `src/app/theme/palette.ts` — MUI palette и app gradients;
- `src/app/theme/typography.ts` — typography options;
- `src/app/theme/component-overrides.ts` — глобальные MUI component overrides;
- `src/app/theme/appTheme.ts` — тонкая композиция theme modules и `theme.custom.gradients`;
- feature-local presets живут в `features/<feature>/theme/`:
  - `game-board/theme/board-cell-sx.ts`
  - `game-setup/theme/layout-sx.ts`, `setup-cell-sx.ts`, `cell-image-sx.ts`;
- `src/shared/ui/` - переиспользуемый UI-слой с явной вложенной структурой:
  - `primitives/` - базовые контролы и атомарные building blocks;
  - `patterns/` - типовые секции/компоновка страниц;
  - `feedback/` - диалоги, toast, загрузка и state-panels;
  - `index.ts` - единый публичный barrel для импортов в фичах.

Ключевые reusable-компоненты:

- primitives: `AppButton`, `AppLinkButton`, `FormTextField`, `ControlledFormTextField`, `FormSelect`, `SectionCard`;
- patterns: `PageShell`, `SectionHeader`, `BoardMatrix`, `AsyncSection`, `AuthScreenShell`;
- feedback: `AppDialog`, `ConfirmDialog`, `AppToast`, `PageStatePanel`, `CenteredProgress`.

Правило миграции и дальнейшей разработки:

- layout-уникальность — локально в `sx`;
- повторяемые visual patterns — только через theme override или `shared/ui`;
- межфичевые импорты — через public API (`features/<feature>/index.ts`), без deep-import в `api/`/`model/` соседа;
- не вводим второй styling-подход параллельно MUI (`CSS Modules`, `Tailwind`, отдельный runtime-styling).

## Локализация

- каждая фича хранит переводы в собственном `i18n/*-translations.ts`;
- повторно используемые действия, форматы и доменные подписи хранятся в `src/shared/i18n/common-translations.ts`; одинаковый текст с разной семантикой остаётся в feature resource, чтобы языки могли переводить его независимо;
- `src/locales/index.ts` только собирает feature resources в общий i18next resource;
- `src/i18next.d.ts` связывает английский resource с `CustomTypeOptions`, поэтому неизвестные ключи ломают TypeScript-check;
- `npm run check:locales` рекурсивно проверяет одинаковый набор ключей `en/ru/uk/pl` в каждом feature module.
- `npm run check:hardcoded-ui` анализирует production TS/TSX и запрещает непереведённый JSX, пользовательские `label`/`title`/`message`/`placeholder`/ARIA-строки и locale-sensitive форматирование без выбранной локали;
- `npm run check:i18n` объединяет обе обязательные проверки, а `npm run audit:i18n-duplicates` выводит кандидатов на безопасное объединение переводов для ручного semantic review;
- клиент показывает локализованные сообщения по стабильному API error code и не выводит английский текст backend как fallback.

## Источник контрактов

Frontend не держит transport-контракты вручную как отдельную правду. Канонический источник - `../backend/openapi/deadmans.v1.yaml`.

Сгенерировать transport-артефакты:

```bash
npm run generate:transport
```

(`generate:contracts` — HTTP/OpenAPI schemas; `generate:realtime` — hub paths и event names из `x-signalr`.)

## Режим API

Frontend использует `openapi-fetch` поверх generated `paths`.

- `GET /api/game` идёт через относительный `/api` base URL;
- `POST /api/game/cells/{cellId}/open` идёт через тот же API transport;
- auth-запросы идут на backend origin для `/auth/*`;
- path/query/body передаются структурированно; значения path-параметров сериализует клиент;
- non-2xx ответы централизованно преобразуются в `ApiError`;
- realtime hubs: пути и события из OpenAPI `x-signalr`, код в `src/shared/realtime/generated.ts` (`buildRealtimeHubUrl`);
- все запросы отправляют `credentials: 'include'`.

`VITE_BACKEND_ORIGIN`:

- в `dev` можно не задавать (используется `http://localhost:5285`);
- вне `dev` рекомендуется задать явно;
- значение должно быть абсолютным origin (например, `https://api.example.com`, без пути);
- если не задан, frontend использует `window.location.origin` (same-origin deployment).

## Локальный запуск

Backend должен быть поднят по инструкции в [`docs/development.md`](../docs/development.md) (`backend/scripts/setup-local.ps1` / `setup-local.bat`).

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

## Локальная проверка

```bash
npm run check
```

`check` последовательно проверяет форматирование, строгий TypeScript, ESLint, локали, Vitest с
V8 coverage для критичных модулей, неиспользуемый код/экспорты через Knip и
production-сборку. Отдельный локальный прогон coverage: `npm run test:coverage`. CI
устанавливает зависимости через `npm ci` и запускает тот же quality gate.

Playwright пока не входит в обязательный frontend gate. Следующий e2e-этап — добавить 3–5
стабильных smoke-сценариев для auth/panel routing, game board, game setup save/conflict и
registration flow после подготовки управляемых test data и auth fixture.

## Ограничение текущего скоупа

На текущем этапе frontend содержит Twitch auth, panel shell, role-aware routing, game board, admin game setup и registration UI. Lifecycle-кнопки, runtime activation UI модификаторов, admin invite UI и форма registration settings пока не реализованы; будущие задачи описываются в документации и issue tracker, а не в production-интерфейсе.

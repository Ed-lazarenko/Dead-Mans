## Dead-Mans - контекст проекта

Этот файл нужен как короткая актуальная карта репозитория: что уже реализовано, где проходят архитектурные границы и на что опираться при следующих изменениях.

---

## Как использовать этот файл

Если возвращаешься в проект после паузы, этот файл должен быть первой точкой входа. Он не заменяет `README.md`, `STACK.md` и архитектурные доки, а сжимает их в рабочую карту:

- что является активным контуром;
- какие части приложения уже живы;
- где проходят архитектурные границы;
- какие инварианты нельзя ломать при дальнейшей разработке;
- в каком порядке заново входить в проект.

---

## Принятый курс

- Проект на ранней стадии, критичных данных в БД нет, поэтому смена технологического baseline выполняется сейчас.
- Целевой курс: **DigitalOcean-first** окружение с максимально близкой локалкой.
- Зафиксированный стек и инфраструктурные правила: `STACK.md`.
- Базовый принцип: локальная разработка с инфраструктурой в контейнерах (`postgres + minio`), приложение запускается из исходников; production-путь остаётся DigitalOcean-first и env-driven.

---

## Источники правды

Чтобы проект не "расползался" по нескольким конкурирующим описаниям, держим явные источники правды:

- архитектурный и продуктовый контекст: `CONTEXT.md`;
- инфраструктурный и технологический курс: `STACK.md`;
- общий локальный запуск и обзор репозитория: `README.md`;
- обзор потоков и границ: `docs/architecture/overview.md`;
- политика retention/удаления: `docs/architecture/data-retention.md`;
- настройка игры (глобальный каталог vs текущая игра): `docs/architecture/game-configuration.md`;
- transport-контракт: `backend/openapi/deadmans.v1.yaml` (HTTP + SignalR в `x-signalr`);
- frontend transport types: generated из OpenAPI в `frontend/src/shared/api/contracts/` и `frontend/src/shared/realtime/generated.ts`.

Если меняется один из этих слоёв, изменения должны пройти по всей цепочке зависимых файлов и документации, а не остаться локальным исключением.

---

## 1. Что это за проект

**Dead-Mans** - панель управления для стрима и интерактивных игровых сценариев.

Основные сценарии:

- авторизация через Twitch.
- просмотр игрового поля из БД.
- открытие ячеек администраторами с realtime-синхронизацией для подключённых клиентов.

---

## 2. Активный контур

В активной разработке находятся только:

- `frontend/`
- `backend/`

`legacy-v1/` хранится в репозитории только как reference-источник старой логики и UX-идей. Новый код туда не добавляется.

---

## 3. Текущее устройство фронтенда

Фронтенд живёт в `frontend/` и представляет собой React SPA с feature-first структурой:

- `src/app/` - composition root, providers, theme, единый panel route config (`panel-route-config`) и route tree (`AppRoutes`, `app-route-tree`);
- `src/routes/` - re-export route metadata, guard/access helpers и redirects;
- `src/layouts/` - shell-компоненты панели и контейнер навигации;
- `src/features/*` - feature UI, hooks/model и feature API/domain adapters;
- `src/shared/ui/` - единый переиспользуемый слой: primitives, patterns, feedback;
- `src/shared/theme/` - общие UI tokens и layout presets для единообразных стилей;
- `src/shared/api/client/` - общие `openapi-fetch` клиенты поверх generated `paths` и перевод non-2xx ответов в `ApiError`;
- `src/shared/api/config.ts` - единая env-конфигурация (`VITE_API_BASE_URL`, `VITE_BACKEND_ORIGIN`);
- `src/shared/api/parse-api-response.ts` - fail-fast runtime parsing для критичных Zod-схем;
- `src/shared/api/contracts/` - generated transport types из OpenAPI и стабильные alias-типы;
- `src/shared/realtime/` - generated hub constants, URL helpers и общий SignalR lifecycle;
- `src/shared/auth/` - auth context, API и route guards;
- `src/**/i18n/*-translations.ts` - feature-owned языковые ресурсы;
- `src/locales/index.ts` - композиция ресурсов всех фич и список поддерживаемых языков;
- `src/app/theme/` - композиция MUI theme из palette, typography и component overrides.

### Что важно архитектурно

- auth HTTP использует тот же typed `openapi-fetch` transport, что и игровые API, но отдельный backend-origin client для `/auth/*`;
- endpoint templates, path/query/body и response-типы выводятся из generated `paths`; стабильные domain aliases при необходимости экспортируются через `src/shared/api/contracts/index.ts`;
- feature API и data access не дублируются: отдельный domain adapter остаётся только для `404 → null`, mapping, multipart и optimistic cache updates;
- TanStack Query keys и `queryOptions` живут внутри owning feature; повторяемые invalidation/error policies оформляются через `mutationOptions`;
- shared SignalR код управляет connect/reconnect/start/stop, а event-specific handlers и resync остаются внутри owning feature;
- registration HTTP живёт в `src/features/game-registration/api/`; UI — `game-application/` и `team-registrations/`.
- крупные экраны и orchestration-хуки декомпозированы по зонам ответственности: страницы собираются из section-компонентов в `features/<feature>/ui/`, а `game-setup` разделён на focused hooks (`use-game-setup-draft`, `use-game-setup-save`, `use-game-setup-cell-media`) под тонким `use-game-setup-page`; `PanelNavigation` разложен на `PanelPrimaryNavigation` и `PanelProfileMenu`.
- capability-level проверки (`gameSetup`, `openGameBoardCell`) задаются через `src/shared/auth/panel-capabilities.ts`, а не через локальные матрицы ролей в фичах.
- TanStack Query владеет server state; React Hook Form + Zod используются для transactional submitted-форм, схемы живут вне компонентов, а типы значений выводятся через `z.infer`. Плотный game-setup draft остаётся controlled React state.
- Критичный auth response дополнительно валидируется runtime-схемой; generated OpenAPI types остаются compile-time transport source of truth.
- Локали принадлежат фичам, ключи i18next проверяются TypeScript через module augmentation, а parity всех `en/ru/uk/pl` модулей входит в общий quality gate.
- Vitest + React Testing Library покрывают setup draft/save/conflict, registration mutations, realtime models, routes/capabilities и ключевые page states. V8 coverage thresholds заданы для критичных модулей, а не глобальным процентом по всему SPA.
- TypeScript baseline включает `noUncheckedIndexedAccess` и `exactOptionalPropertyTypes` в app и tooling-конфигах.
- Единый frontend quality gate — `npm --prefix frontend run check`: Prettier, строгий TypeScript, ESLint, locale consistency, Vitest coverage, Knip и production build. CI использует `npm ci` и запускает тот же gate.
- Playwright пока остаётся следующим отдельным этапом: 3–5 smoke-сценариев после подготовки стабильных test data и auth fixture.

### Основные страницы

- `AuthLandingPage` - вход через Twitch.
- `TwitchAuthCallbackPage` - завершение OAuth flow и восстановление сессии.
- `GameBoardPage` - игровое поле из БД с admin-only открытием ячеек.
- `GameSetupPage` - admin-настройка общего черновика игры (Save, layout, cell media, SignalR sync).
- `GameApplicationPage` - заявка игрока и управление командой.
- `TeamRegistrationsPage` - admin-модерация команд и инвайтов.

### Актуальный shell панели

- Внутренний раздел приложения живёт под `panelRootPath` (`/panel`).
- Панель использует общий `MainLayout`.
- Все panel routes описаны в одном месте: `src/app/panel-route-config.tsx`. Навигация, access helpers и `useRoutes` берут данные из этого конфига через `panelRoutes` / `panelRouteConfig`.
- Основная навигация показывает player-разделы, а доступные admin-разделы находятся в profile menu.
- Даже если frontend скрывает пункт навигации, доступ к маршруту всё равно должен проверяться через общие route/access helpers, а не только через UI.
- Маршрутный доступ и capability-доступ разделены: route visibility (`hasAccessToPanelRoute`) и действия внутри страницы (`hasPanelCapability`) не должны смешиваться.

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
- `Controllers/` - thin HTTP-слой;
- `openapi/deadmans.v1.yaml` - канонический transport source of truth.

### Важный текущий split

- игровое поле (`IGameBoardRepository`, `GET /api/game`, `POST /api/game/cells/{cellId}/open`) работает через persistence-backed adapter (EF Core/PostgreSQL);
- auth, users и roles уже DB-backed через `ApplicationDbContext` и EF Core;
- медиа ячеек отдаются как публичные URL, собранные backend из storage metadata;
- изменения состояния ячеек рассылаются подключённым клиентам через SignalR hub.

### Auth flow

- `GET /auth/twitch/login` - редиректит на Twitch OAuth;
- `GET /auth/twitch/callback` - завершает Twitch login, upsert'ит пользователя и ставит auth cookie; при отклонённом входе или неактивном аккаунте редиректит frontend на callback-страницу с `reason=*`;
- `GET /auth/me` - возвращает текущую сессию;
- `POST /auth/logout` - завершает сессию.

Runtime-роли для backend-авторизации считаются из БД на каждом аутентифицированном запросе. Cookie хранит identity/session claims, а не долгоживущий snapshot ролей.
Деактивированные пользователи не могут получить новую auth-сессию; существующая cookie-сессия отклоняется middleware на `/api/*`, `/auth/*` и `/hubs/*` (401 + sign-out), а не только на `/auth/me`. `POST /auth/logout` требует заголовок `X-Dead-Mans-Api-Client: 1` (его выставляет общий frontend `openapi-fetch` client). Права на игровые endpoint'ы задаются выборочно на уровне самих контроллеров/действий.
Закрытые ячейки в `GET /api/game` не отдают `title`/`description` в JSON (только после открытия).

Auth требует корректно настроенный `ApplicationDbContext`. Если backend запущен без persistence-конфигурации, старт должен завершаться явной ошибкой, а не оставлять приложение в полу-рабочем состоянии.

---

## 5. Контракты и Swagger

Канонический source of truth для transport-контрактов:

- `backend/openapi/deadmans.v1.yaml`

Frontend генерирует transport-артефакты через:

```bash
npm --prefix frontend run generate:transport
```

Swagger UI в development должен смотреть на тот же YAML-файл, чтобы документация и generation не расходились.

---

## 6. Архитектурные инварианты

Это те правила, которые должны переживать отдельные задачи и чаты:

- активный код живёт только в `frontend/` и `backend/`;
- `legacy-v1/` можно читать, но нельзя использовать как место для нового кода;
- frontend остаётся feature-first, а не превращается в слой случайных util-файлов;
- page/layout-компоненты остаются тонкими, а orchestration/API adapters/role logic уходит в hooks, route helpers и feature modules;
- backend остаётся layered: controllers не ходят напрямую в EF/infrastructure;
- transport DTO, application-модели и domain-модели не смешиваются;
- OpenAPI сначала обновляется как контракт, затем синхронизируются backend mapping/endpoint'ы и frontend generated types;
- frontend role gating - это UX, а не security boundary; реальные права проверяются на backend, включая admin-only открытие ячеек;
- конфигурационные и persistence-проблемы должны проявляться явно на старте, а не оставлять приложение в полу-рабочем состоянии;
- новые решения должны расширять уже выбранный паттерн, а не создавать второй конкурирующий способ делать то же самое.
- зависимости для state, animation, icons, toast или asset transforms добавляются только вместе с реальным использованием, а не как обязательный неиспользуемый baseline.

---

## 7. Качество решений

При следующих изменениях ориентир такой:

- одинаковые задачи должны решаться одинаковыми по стилю способами;
- новые абстракции вводятся только если они реально очищают границу или убирают повтор;
- `shared/` не должен становиться свалкой feature-логики;
- user-facing текст проходит через i18n;
- повторяемые стили живут в theme/shared UI, а feature-specific стили остаются локальными;
- security и role checks не размазываются между несколькими слоями без необходимости;
- при структурных изменениях обновляются docs, а не только код;
- если уже есть хороший паттерн в проекте, его лучше продолжить, чем изобретать новый «чуть удобнее» локально.

---

## 8. Что уже реализовано

- feature-first фронтенд с `app/`, `routes/`, `features/`, `shared/`, `locales/`;
- auth context и protected route для панели;
- Twitch login flow с backend cookie session;
- game API на панели: `GET /api/game`, `POST /api/game/cells/{cellId}/open`, realtime sync через SignalR (после успешной записи в БД publish best-effort, см. `docs/architecture/realtime.md`);
- game setup для admin: `GET/POST/PUT/DELETE /api/game/setup` — один общий черновик в БД; `PUT` с `expectedVersion` и `409` при конфликте; `DELETE /api/game/setup` — осознанное исключение и делает hard-delete только draft-игры (с cleanup draft media); `POST/DELETE /api/game/setup/cells/{cellId}/media` — изображения сразу в bucket; realtime: hub/event в OpenAPI `x-signalr` (`/hubs/game-setup`, `draftChanged`); на frontend текстовые поля сохраняются кнопкой Save, layout — при подтверждении в диалоге, без `localStorage`;
- game modifiers (phase 1): каталог модификаторов хранится в БД (`modifier_definitions`) с полями для расчёта (`mechanicType`, `effect` в `MetadataJson`, `scoringType`, `defaultLimitPerGame`) и редактируемыми конфликтами (`modifier_conflicts`, `conflictingModifierIds` в create/update); первичный ключ модификатора — `Id` (Guid); форма каталога показывает русские категории механик (`rule_only`, `restriction_with_reward`, `kill_counter`, `multiplier`, `mentor`) и сохраняет структурированное влияние на будущий расчёт наград; в draft admin выбирает `enabledModifierIds` через `PUT /api/game/setup`; в active-игре `admin/moderator` может активировать модификатор `POST /api/game/modifiers/{modifierId}/activate`; состояние (`enabledModifierIds`, `activeModifiers`) входит в `GET /api/game`; realtime по `/hubs/game-board` событием `modifierActivated`;
- game questions (phase 1): каталог вопросов (`/api/game/questions/catalog`) с поиском/фильтрацией и массовым enable/disable по категориям управляется в `game-setup`; gameplay endpoints (`ask-next`, `answer`, game history) реализованы на backend и в контрактах, но без отдельного panel UI на `game-board`;
- game history (phase 1): endpoint `GET /api/game/history/users/{userId}` (self или `admin/moderator`) возвращает историю пользователя по играм: активации модификаторов и ответы на вопросы, сгруппированные по игре;
- soft-delete workflow: вопросы удаляются через soft-delete (`DELETE /api/game/questions/{questionId}`), не-draft игры архивируются через soft-delete (`DELETE /api/game/lifecycle/games/{gameId}`); draft-игра остаётся исключением и удаляется hard-delete через `DELETE /api/game/setup`;
- глобальный каталог + настройка на игру (phase 2): admin-навигация разделена на «Текущая игра» и «Глобальный каталог» (`PanelAdminNavigation`, `adminSection`); каталог модификаторов имеет CRUD `POST/PUT/DELETE /api/game/modifiers` (DELETE = архивация через `IsArchived`), каталог вопросов — `POST/PUT /api/game/questions` (+ существующие soft-delete/enable); per-game выбор вопросов хранится в `game_question_selections` и задаётся через `enabledQuestionIds` в `PUT /api/game/setup` (зеркало `enabledModifierIds`); `ask-next` берёт вопросы только из выбранных для игры (пустой выбор = вопросов нет); детали — `docs/architecture/game-configuration.md`;
- категории вопросов по суррогатному `Id`: `question_categories` имеет PK `Id` (Guid) и редактируемое отображаемое `Name` (уникальное); вопрос ссылается через `CategoryId` (FK `Restrict`). Кода у категории больше нет — переименование = правка `Name` без каскада. Зашитые категории мигрированы в русские имена (`Лор`, `Локации`, `Оружие и предметы`, `Статистика`). Контракт: `categoryId` в create/update вопроса, фильтр `GET /game/questions/catalog?categoryId`, `PATCH /game/questions/categories/{categoryId}/enabled`; в каталоге вопрос несёт `categoryId` + `categoryName` (имя). Миграция `SwitchQuestionCategoriesToIdKey`;
- feature-local query keys/`queryOptions` и переиспользуемые `mutationOptions` для cache/error policies;
- typed frontend API на `openapi-fetch` поверх generated `paths`, со статическими endpoint templates и общей обработкой `ApiError`;
- OpenAPI contract generation для frontend;
- role-aware panel routing, компактная player navigation и admin-разделы в profile menu;
- безопасный локальный backend bootstrap через `backend/scripts/setup-local.ps1` без удаления БД и storage;
- отдельный destructive reset через `backend/scripts/reset-local.ps1` с подтверждением;
- backend integration и contract tests для auth, game board/setup, registration и lifecycle;
- документация по стеку и архитектуре;
- регистрация команд: статусы игры `draft → ready → active → finished`, слоты/команды/инвайты, API `/api/game/registration` и `/api/game/lifecycle/*`, страницы `/panel/game-application` и `/panel/team-registrations` (см. `docs/architecture/game-registration.md`).

---

## 9. Что важно не забыть

- активный код живёт только в `frontend/` и `backend/`;
- `legacy-v1/` можно читать, но нельзя использовать как источник прямого копирования;
- backend controllers должны зависеть от application services/ports, а не от EF/infrastructure напрямую;
- transport DTO нельзя смешивать с application/domain моделями;
- user-facing строки должны проходить через i18n;
- при изменении transport-контракта сначала обновляется OpenAPI, потом регенерируются frontend types.

---

## 10. Быстрый возврат в проект

Если открываешь репозиторий после паузы:

1. Прочитай `README.md`.
2. Посмотри `docs/architecture/overview.md`.
3. Прочитай этот `CONTEXT.md` целиком и восстанови в голове инварианты.
4. Проверь `frontend/src/features/*`, `frontend/src/routes/` и `backend/Application/`.
5. Убедись, что OpenAPI и generated contracts синхронизированы.
6. Локальный bootstrap: `backend/scripts/setup-local.ps1` (см. README; Windows — `setup-local.bat` при необходимости).
7. Разработка: из корня `npm install`, `npm run dev` (или `dev-full.bat` на Windows).
8. Сброс данных: `backend/scripts/reset-local.ps1` или `reset-local.bat`.
9. Для auth-проверок не забывай, что backend требует настроенный DB-backed `ApplicationDbContext`.

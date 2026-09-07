# Game configuration: глобальный каталог и настройка текущей игры

Этот документ описывает архитектуру настройки модификаторов и вопросов: как
разделены «глобальный каталог» (мастер-данные) и «настройка на текущую игру»
(подмножество для конкретной игры).

## 1. Две зоны ответственности

Раньше admin-разделы смешивали два разных понятия. Теперь они явно разделены, и
у админа в навигации две группы (`PanelAdminNavigation`):

- **Текущая игра** (`adminSection: 'current-game'`): настройка черновика игры —
  поле (`game-setup`), выбор модификаторов (`admin-modifiers`), выбор вопросов
  (`admin-questions`), команды (`team-registrations`). Здесь админ выбирает, какое
  подмножество каталога будет участвовать в создаваемой игре.
- **Глобальный каталог** (`adminSection: 'catalog'`): мастер-данные —
  `catalog-modifiers` и `catalog-questions`. Здесь модификаторы и вопросы
  создаются, редактируются и удаляются (soft-delete) независимо от какой-либо игры.

## 2. Модель данных

Глобальный каталог:

- `modifier_definitions` — стабильная идентичность модификатора и указатель на текущую
  неизменяемую редакцию. Полная модель: [`modifier-versioning.md`](modifier-versioning.md).
  Soft-delete через `is_archived`.
  Первичный ключ — суррогатный `Id` (Guid). Модификатор больше не требует
  человекочитаемого кода: идентичность и связи держатся на `Id`, а админ
  редактирует только смысловые поля. Для будущего расчёта наград каталог несёт
  типизированное revisioned-поведение `BehaviorV2`: kind, phase, performer,
  host monitoring, rule, stacking policy, resolution, reward и ссылку на одну из
  versioned built-in formulas с закрытыми параметрами. Лимит хранится как
  `max_activations_per_round`, конфликты — через `conflictingModifierIds`.
  UI показывает этапы по-русски (`Перед раундом`, `Во время раунда`,
  `На итог раунда`), а транспорт использует стабильные V2-коды.
- `question_definitions` — каталог вопросов. Soft-delete через `is_deleted` /
  `deleted_at_utc` (+ check-constraint, что флаг и метка времени согласованы).
  Каждому вопросу назначается категория через `CategoryId` (FK на
  `question_categories.Id`, `Restrict`).
- `question_categories` — каталог категорий. Первичный ключ — суррогатный
  `id` (Guid), `name` — отображаемое название (уникальное, ≤64, читаемое). Кода
  у категории нет: имя редактируемо, и переименование не ломает ссылки вопросов
  (они держат `CategoryId`, а не строку). Зашитые категории мигрированы в
  читаемые русские имена (`Лор`, `Локации`, `Оружие и предметы`, `Статистика`).
  Системная fallback-категория `БЕЗ КАТЕГОРИИ` создаётся автоматически и
  используется bulk-import'ом, если в записи не указан `categoryId`.
  Транспорт: `categoryId` в `Create/UpdateGameQuestionRequest`, фильтр каталога
  `GET /game/questions/catalog?categoryId`, массовый toggle
  `PATCH /game/questions/categories/{categoryId}/enabled`; в ответах вопрос несёт
  `categoryId` + `categoryName` (отображаемое имя). Создание/обновление вопроса с
  несуществующим `categoryId` → `404 game_question.category_not_found`.

Привязка к конкретной игре (подмножество каталога):

- `game_enabled_modifiers (game_id, modifier_id)` — какие модификаторы включены
  в игру; `modifier_version_id` пуст в draft/ready и атомарно заполняется для всего набора
  при старте.
- `game_enabled_questions (game_id, question_id)` — аналог для вопросов:
  какие вопросы участвуют в игре. FK на игру — `Cascade`, на вопрос — `Restrict`
  (вопросы удаляются soft-delete, поэтому жёсткого удаления записи каталога нет).

Оба enabled-набора живут на игре и переживают переходы статуса
`draft → ready → active → finished` (это та же запись `games`, меняется только
`status`).

## 3. Поведение во время игры

`AskNextQuizQuestionAsync` выбирает кандидатов только из вопросов, **выбранных для этой
игры** (`game_enabled_questions`), и дополнительно среди не удалённых,
глобально включённых (`IsEnabled`), ещё не заданных в этой игре. Сначала берутся
вопросы с минимальным `AskedTotalCount`, затем среди них приоритет получают
вопросы с максимальным `Priority`; если кандидатов всё ещё несколько, один из них
выбирается случайно. **Пустой выбор =
вопросов нет** (ask-next вернёт `NoAvailableQuestions`) —
выбор вопросов для игры обязателен.

## 4. API (см. `backend/openapi/deadmans.v1.yaml`)

Глобальный каталог (только admin):

- Модификаторы: `POST /api/game/modifiers`, `PUT /api/game/modifiers/{modifierId}`,
  `DELETE /api/game/modifiers/{modifierId}?expectedRevision=...` (архивация). Update принимает
  `expectedRevision` и необязательный `changeNote`; каждое содержательное изменение создаёт
  новую редакцию. Create/update принимают
  карточку, `category`, `behaviorV2`, `tags`, `maxActivationsPerRound`,
  `activationCommand` и `conflictingModifierIds`. Типизированный `behaviorV2`
  задаёт фазу, исполнителя, наблюдение ведущего, stacking, resolution и одну из
  поддерживаемых versioned formulas; произвольных выражений и ручной поправки
  результата в V2-контракте нет.
  Чтение — существующий `GET /api/game/modifiers/catalog` (исключает архивные).
  Полная keyset-история доступна всем авторизованным пользователям через
  `/api/game/modifiers/history` и version/detail/games endpoints.
- Вопросы: `POST /api/game/questions`, `PUT /api/game/questions/{id}`,
  `DELETE /api/game/questions/{id}` (soft-delete, существовал). Чтение —
  `GET /api/game/questions/catalog`. Bulk-import `POST /api/game/questions/import`
  принимает JSON/JSONC, где обязательны только `text`, `answer`, `reward`; если
  `categoryId` не указан, используется fallback-категория `БЕЗ КАТЕГОРИИ`, если
  `isEnabled` не указан — импортируется `false`, если `priority` не указан —
  используется `0`. Невалидные записи не валят весь импорт: backend пропускает их
  и возвращает список пропущенных строк с причинами; нераспознанная категория
  мапится в fallback-категорию `БЕЗ КАТЕГОРИИ`.

Настройка текущей игры:

- `PUT /api/game/setup` принимает `enabledModifierIds` и **`enabledQuestionIds`**;
  снапшот (`GET /api/game/setup`) возвращает оба набора.

## 5. Инварианты

- Каталог редактируется только через catalog-эндпоинты; per-game выбор — только
  через `PUT /api/game/setup`.
- Удаление в каталоге — всегда soft-delete (история игр не теряется).
- Транспорт сначала меняется в OpenAPI, затем регенерируются frontend-типы
  (`npm --prefix frontend run generate:transport`).
- Валидация и нормализация входных данных живут в Application-слое
  (`GameModifierValidator`, `GameQuestionValidator`, `GameSetupDraftValidator`),
  репозитории только persist'ят.

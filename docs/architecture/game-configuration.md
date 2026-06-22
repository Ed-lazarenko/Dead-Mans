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

- `modifier_definitions` — каталог модификаторов. Soft-delete через `IsArchived`.
  Код (`Code`) — человекочитаемый первичный ключ, неизменяемый после создания.
- `question_definitions` — каталог вопросов. Soft-delete через `IsDeleted` /
  `DeletedAtUtc` (+ check-constraint, что флаг и метка времени согласованы).
  Каждому вопросу назначается категория из `question_categories`.

Привязка к конкретной игре (подмножество каталога):

- `game_modifier_selections (GameId, ModifierCode)` — какие модификаторы включены
  в игру. Существовало ранее.
- `game_question_selections (GameId, QuestionId)` — **новое**. Аналог для вопросов:
  какие вопросы участвуют в игре. FK на игру — `Cascade`, на вопрос — `Restrict`
  (вопросы удаляются soft-delete, поэтому жёсткого удаления записи каталога нет).

Оба selection-набора живут на игре и переживают переходы статуса
`draft → ready → active → finished` (это та же запись `games`, меняется только
`status`).

## 3. Поведение во время игры

`AskNextQuestionAsync` выбирает кандидатов только из вопросов, **выбранных для этой
игры** (`game_question_selections`), и дополнительно среди не удалённых,
глобально включённых (`IsEnabled`), ещё не заданных в этой игре. **Пустой выбор =
вопросов нет** (ask-next вернёт `NoAvailableQuestions`) —
выбор вопросов для игры обязателен.

## 4. API (см. `backend/openapi/deadmans.v1.yaml`)

Глобальный каталог (только admin):

- Модификаторы: `POST /api/game/modifiers`, `PUT /api/game/modifiers/{code}`,
  `DELETE /api/game/modifiers/{code}` (архивация). Чтение — существующий
  `GET /api/game/modifiers/catalog` (исключает архивные).
- Вопросы: `POST /api/game/questions`, `PUT /api/game/questions/{id}`,
  `DELETE /api/game/questions/{id}` (soft-delete, существовал). Чтение —
  `GET /api/game/questions/catalog`.

Настройка текущей игры:

- `PUT /api/game/setup` принимает `enabledModifierCodes` и **`enabledQuestionIds`**;
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

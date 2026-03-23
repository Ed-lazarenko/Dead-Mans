## Dead-Mans — черновик схемы БД (v1)

Цель: зафиксировать сущности/связи для первой версии API и подсветить места для расширений. Названия полей — ориентир для EF миграций, не окончательный контракт.

### Базовые сущности
- **User**: `id (guid)`, `twitch_id`, `display_name`, `avatar`, `roles (json/flags: super_admin, admin, participant)`, `created_at`.
- **Session** (игровой ивент): `id`, `owner_id -> User`, `status (draft/registration/open/active/finished)`, `team_size_min`, `team_size_max`, `modifiers_mode (next_team | select_team)`, `starts_at`, `ends_at`, `description`.
- **Registration** (только игроки): `id`, `session_id -> Session`, `user_id -> User`, `slot_from`, `slot_to`, `status (pending/approved/declined)`, `comment`, `created_at`.
- **Team**: `id`, `session_id -> Session`, `name`, `color_hex`, `is_open`, `description`, `created_at`.
- **TeamMember**: `team_id -> Team`, `user_id -> User`, `joined_at`; состав может меняться между матчами, но внутри матча фиксирован.

### Матчи и лоадауты
- **LoadoutBoard**: `id`, `session_id -> Session`, `rows (<=9)`, `cols (<=9)`, `row_labels (json)`, `col_labels (json)`, `version`, `created_at`. Фиксируется перед стартом сессии.
- **LoadoutCell**: `id`, `board_id -> LoadoutBoard`, `row`, `col`, `label`, `points`, `image_url`, `state (available/locked)`.
- **Match**: `id`, `session_id -> Session`, `team_id -> Team`, `index` (попытка №), `map_name`, `started_at`, `ended_at`, `total_score`, `total_penalty`, `is_best` (лучший результат команды в сессии).
- **MatchRoster** (snapshot состава): `match_id -> Match`, `user_id -> User`. Фиксирует кто играл в конкретном матче.
- **LoadoutCellPlay**: `id`, `match_id -> Match`, `cell_id -> LoadoutCell`, `opened_by_team_id -> Team`, `base_points` (стоимость клетки), `opened_at`.

### Очки и правила
- **ScoreRuleDefinition**: `id`, `code` (kill/boss/extract/bonus/penalty/...),
  `name`, `description`, `payload_schema` (json), `is_active`.
- **ScoreEntry**: `id`, `match_id -> Match`, `team_id -> Team`, `rule_id -> ScoreRuleDefinition`,
  `value` (+/-), `payload` (json: counts, multipliers), `created_by -> User`, `created_at`.
  Источник правды; по ним считаем итог матча/лидерборда.

### Зрители и баланс
- **ViewerBalance**: `session_id -> Session`, `user_id -> User`, `balance`, `updated_at` (обнуляется на новую сессию).
- **ViewerTransaction**: `id`, `session_id`, `user_id`, `type (earn/spend/adjust)`,
  `source (quiz/modifier/manual)`, `amount`, `balance_after`, `created_by -> User?`, `created_at`.

### Вопросы для викторин
- **Question**: `id`, `text_ru`, `text_en`, `answer_ru`, `answer_en`, `cost`, `times_asked`, `was_ever_asked`, `is_active`.
- **QuestionAnswer**: `id`, `question_id -> Question`, `session_id -> Session`, `user_id -> User`,
  `provided_answer`, `was_correct`, `awarded_points`, `answered_at`.
  Позволяет видеть, на какие вопросы отвечал пользователь и сколько баллов получил в конкретной сессии.

### Модификаторы
- **ModifierDefinition**: `id`, `name`, `cost`, `description`, `global_limit_per_team`, `personal_limit_per_viewer_per_team`, `conflicts (json: ids)`, `is_active`.
- **ActiveModifier** (событие применения): `id`, `session_id`, `definition_id -> ModifierDefinition`,
  `triggered_by -> User` (viewer), `target_mode (next_team/select_team)`, `target_team_id?`,
  `applied_match_id?`, `status (queued/applied/blocked/expired)`, `created_at`, `note`.
  Лимиты/конфликты проверяются при записи; блокированные храним для истории.

### Управление и аудит
- **ControlActionLog**: `id`, `session_id`, `actor_id -> User`, `action (start/pause/resume/reset/next)`, `created_at`.

### Лидерборд (расчёт)
- Итог команды в сессии = лучший `Match.total_score - Match.total_penalty` (или пересчёт из ScoreEntry).
- Для быстрых ответов можно держать materialized view/кэш `LeaderboardSnapshot(session_id, team_id, best_score, best_match_id, updated_at)`, но истина остаётся в `ScoreEntry`/`Match`.

### Индексы и ограничения (минимум)
- FK + уникальные: `Team(session_id, name)`; `TeamMember(team_id, user_id)`; `Match(session_id, team_id, index)`; `LoadoutCell(board_id, row, col)`; `ViewerBalance(session_id, user_id)` unique; `ViewerTransaction(session_id, user_id, created_at)` index; `ActiveModifier(session_id)` index.
- Частые выборки: по `session_id` в большинстве таблиц; по `match_id` в `ScoreEntry`/`LoadoutCellPlay`; по `team_id` в `Match`/`ScoreEntry`.

### История и личный кабинет
- Игрок: через `MatchRoster` → `Match(map_name)` + `ScoreEntry` (детализация очков по правилам); можно агрегировать по картам.
- Зритель: `ViewerTransaction` + `ActiveModifier` (что активировал, куда, когда).

### Расширения (позже)
- Twitch OAuth токены/refresh; бан-лист.
- Queue модификаторов с приоритетами; delayed-apply.
- Версионность LoadoutBoard (если решим менять до старта) — поле `version` уже заложено.
- Кэши/проекции для API чтения (CQRS), но пишем всегда через первичные таблицы выше.

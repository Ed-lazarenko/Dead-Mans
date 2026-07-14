# Data Retention And Delete Policy

Этот документ фиксирует единую политику удаления данных в Dead-Mans.

## Цель

- сохранить игровую и пользовательскую историю;
- исключить случайное каскадное удаление глобальных справочников;
- явно определить, где допустим hard-delete как осознанное исключение.

## Матрица удаления

- `DELETE /api/game/setup` -> **hard-delete** только для `draft`-игры.
- `DELETE /api/game/setup/cells/{cellId}/media` -> hard-delete media-объекта для draft-ячейки + unlink из draft snapshot.
- `DELETE /api/game/lifecycle/games/{gameId}` -> **soft-delete** для non-draft игр (`games.IsDeleted`, `games.DeletedAtUtc`).
- `DELETE /api/game/questions/{questionId}` -> **soft-delete** вопроса (`question_definitions.IsDeleted`, `question_definitions.DeletedAtUtc`).
- каталог модификаторов -> **archive-ready модель** через `modifier_definitions.IsArchived` (HTTP archive endpoint пока не реализован).
- история отыгрышей карточек (`game_card_runs`, `game_card_run_participants`, `game_card_run_modifier_results`) -> **исторические факты**, не удалять каскадно из-за изменений справочников или состава команд.
- пользователи -> **deactivate** через `users.IsActive`.

## Инварианты безопасности

- Удаление/архивация игры не должно затрагивать глобальные сущности (`users`, `question_definitions`, `modifier_definitions`).
- История завершённых карточек должна оставаться воспроизводимой даже если каталог модификаторов, профиль пользователя или состав команды позже изменятся.
- Связи игровых фактов с каталогами должны использовать безопасную политику (`Restrict`), чтобы история не терялась из-за удаления справочников.
- Все read-запросы активного runtime-контура должны фильтровать soft-deleted записи (`!IsDeleted`) там, где это влияет на поведение.

## Правило для новых изменений

- Для временных рабочих сущностей допускается hard-delete только по явно задокументированному исключению.
- Для исторически значимых бизнес-сущностей — только soft-delete/archive/deactivate.
- Любое изменение политики удаления должно сопровождаться:
  - обновлением OpenAPI;
  - обновлением backend/frontend generated contracts;
  - обновлением архитектурной документации и Cursor rules.

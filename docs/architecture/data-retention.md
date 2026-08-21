# Data Retention And Delete Policy

Этот документ фиксирует единую политику удаления данных в Dead-Mans.

## Цель

- сохранить игровую и пользовательскую историю;
- исключить случайное каскадное удаление глобальных справочников;
- явно определить, где допустим hard-delete как осознанное исключение.

## Матрица удаления

- `DELETE /api/game/setup` -> **hard-delete** только для `draft`-игры.
- `DELETE /api/game/setup/cells/{cellId}/media` -> hard-delete media-объекта для draft-ячейки + unlink из draft snapshot.
- `DELETE /api/game/lifecycle/games/{gameId}` -> **soft-delete** для non-draft игр (`games.is_deleted`, `games.deleted_at_utc`).
- `DELETE /api/game/questions/{questionId}` -> **soft-delete** вопроса (`question_definitions.is_deleted`, `question_definitions.deleted_at_utc`).
- каталог модификаторов -> **soft archive** через `modifier_definitions.is_archived` и
  admin `DELETE /api/game/modifiers/{modifierId}`; definition из active game content-locked.
- история отыгрышей карточек (`game_rounds`, `game_round_participants`, `game_round_cell_media`, `game_round_modifier_results`) -> **исторические факты**, не удалять каскадно из-за изменений справочников, медиа карточки или состава команды.
- пользователи -> **deactivate** через `users.is_active`.

## Инварианты безопасности

- Удаление/архивация игры не должно затрагивать глобальные сущности (`users`, `question_definitions`, `modifier_definitions`).
- История завершённых карточек должна оставаться воспроизводимой даже если каталог модификаторов, профиль пользователя или состав команды позже изменятся.
- Медиа карточки, использованной в раунде, фиксируется отдельным snapshot (`game_round_cell_media`) при создании раунда; история и лидерборды читают этот snapshot и не зависят от будущих изменений live-карточки.
- Финальный счёт раунда является backend-authoritative: клиент может прислать preview/override поле для совместимости UI, но backend сохраняет результат, вычисленный из `kills`, `bounty` и проверенных результатов модификаторов.
- Флаг `game_rounds.empty_card_penalty_applied` хранится вместе с раундом как исторический факт применения штрафа за пустую карточку: backend ставит его только когда у завершённой карточки нет положительного результата ни от базовых исходов, ни от очков модификаторов. UI может показывать сумму штрафа из `base_score`, но не должен заново решать, применялось ли правило.
- Связи игровых фактов с каталогами должны использовать безопасную политику (`Restrict`), чтобы история не терялась из-за удаления справочников.
- Все read-запросы активного runtime-контура должны фильтровать soft-deleted записи (`!IsDeleted`) там, где это влияет на поведение.

## Правило для новых изменений

- Для временных рабочих сущностей допускается hard-delete только по явно задокументированному исключению.
- Для исторически значимых бизнес-сущностей — только soft-delete/archive/deactivate.
- Любое изменение политики удаления должно сопровождаться:
  - обновлением OpenAPI;
  - обновлением backend/frontend generated contracts;
  - обновлением архитектурной документации и Cursor rules.

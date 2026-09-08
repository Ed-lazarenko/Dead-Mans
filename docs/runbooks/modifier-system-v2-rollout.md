# Modifier system V2 rollout runbook (archived pre-baseline plan)

> Этот runbook больше не является production-инструкцией. Он описывал перенос одноразовых
> pre-release данных. Первый публичный деплой теперь начинается с пустой миграции
> `20260908003848_ProductionBaseline`; актуальные правила находятся в архитектуре БД и
> отчёте baseline. Остальной текст сохранён только как история проектных решений.

Этот runbook применяется к цепочке миграций `20260820*`, которая вводит lifecycle раунда,
аудит покупок/refund, content lock, immutable BehaviorV2 snapshots и новый расчёт итогов.
Миграции намеренно работают fail closed: неоднозначные или несовместимые legacy-данные не
исправляются автоматически.

## Ответственные и окно

- Назначить одного оператора миграции и отдельного человека для проверки результата.
- Остановить backend workers/instances, которые могут писать в игровую БД.
- Не начинать rollout при незавершённом игровом раунде.
- Зафиксировать текущий application image/commit, последнюю EF migration и размер БД.
- Сделать проверяемый PostgreSQL backup и записать путь/идентификатор восстановления.

## 1. Preflight inventory

Запросы выполняются read-only на production-копии и затем на production перед окном. Любая
непустая выборка ниже блокирует rollout до явного решения оператора.

```sql
-- Активные custom definitions: сопоставить с BehaviorV2 либо осознанно архивировать.
SELECT id, name, mechanic_type, scoring_type, default_limit_per_game
FROM modifier_definitions
WHERE NOT is_archived
  AND id::text NOT LIKE '10000000-0000-0000-0000-00000000000_'
ORDER BY name, id;

-- Противоречивые activation limits.
SELECT id, name, default_limit_per_game,
       metadata_json #>> '{activationLimit,count}' AS metadata_limit
FROM modifier_definitions
WHERE metadata_json #> '{activationLimit,count}' IS NOT NULL
  AND metadata_json #>> '{activationLimit,count}' ~ '^[0-9]+$'
  AND (metadata_json #>> '{activationLimit,count}')::integer
      IS DISTINCT FROM default_limit_per_game;

-- Активные custom expressions не имеют автоматического V2 mapping.
SELECT id, name
FROM modifier_definitions
WHERE NOT is_archived
  AND metadata_json #>> '{effect,scoreImpact,scoreFormula,mode}' = 'custom_expression';

-- Несколько nonterminal rounds одной игры нарушают lifecycle-инвариант.
SELECT game_id, array_agg(id ORDER BY started_at_utc) AS round_ids
FROM game_rounds
WHERE status IN ('awaiting_modifiers', 'preparing', 'in_progress', 'reviewing_results')
GROUP BY game_id
HAVING count(*) > 1;

-- Одна покупка не может принадлежать результатам разных раундов.
SELECT modifier_activation_id, array_agg(DISTINCT round_id) AS round_ids
FROM game_round_modifier_results
GROUP BY modifier_activation_id
HAVING count(DISTINCT round_id) > 1;

-- Покупки без результата должны иметь ровно один детерминированный nonterminal round.
SELECT activation.id, activation.game_id, count(round.id) AS candidate_rounds
FROM game_modifier_activations AS activation
LEFT JOIN game_round_modifier_results AS result
  ON result.modifier_activation_id = activation.id
LEFT JOIN game_rounds AS round
  ON round.game_id = activation.game_id
 AND round.status IN ('awaiting_modifiers', 'preparing', 'in_progress', 'reviewing_results')
WHERE result.id IS NULL
GROUP BY activation.id, activation.game_id
HAVING count(round.id) <> 1;
```

Не редактировать historical score/result snapshots. Для custom definition допустимы только
явное V2-сопоставление, осознанная архивация либо перенос rollout до появления поддерживаемой
formula library. Решение и затронутые IDs сохраняются в change record.

## 2. Репетиция на клоне

1. Восстановить свежий backup в изолированную БД с теми же расширениями и PostgreSQL version.
2. Указать clone connection string только процессу миграции.
3. Выполнить:

```powershell
dotnet ef migrations list --project backend/backend.csproj --startup-project backend/backend.csproj
dotnet ef database update --project backend/backend.csproj --startup-project backend/backend.csproj
dotnet test backend/tests/Backend.Tests/Backend.Tests.csproj --no-restore
npm --prefix frontend run check
```

4. Проверить старую completed history, cancelled history, leaderboard и один полный lifecycle:
   purchase → prepare → gameplay → review → preview → finalize.
5. Повторный `database update` должен завершаться без изменений.

## 3. Production cutover

1. Включить maintenance window и остановить все старые backend instances.
2. Повторить preflight inventory; результаты должны совпадать с утверждённым change record.
3. Сделать финальный backup и проверить, что restore-команда доступна оператору.
4. Выполнить `dotnet ef database update` новым application image.
5. Запустить ровно один новый backend instance, проверить startup/health logs, затем остальные.
6. Развернуть frontend, сгенерированный из того же `deadmans.v1.yaml`.

## 4. Post-migration verification

Все запросы должны вернуть `0`.

```sql
SELECT count(*)
FROM modifier_definitions
WHERE revision < 1
   OR behavior_v2_json IS NULL
   OR behavior_v2_json ->> 'schemaVersion' <> '2';

SELECT count(*)
FROM game_modifier_activations
WHERE round_id IS NULL
   OR initiated_by_user_id IS NULL
   OR status NOT IN ('active', 'consumed', 'cancelled')
   OR definition_revision_snapshot < 1
   OR behavior_v2_snapshot_json ->> 'schemaVersion' <> '2';

SELECT count(*)
FROM game_rounds
WHERE version < 1
   OR (status = 'completed' AND (final_score IS NULL OR finished_at_utc IS NULL))
   OR (status = 'cancelled' AND (final_score <> 0 OR finished_at_utc IS NULL));
```

Затем выполнить authenticated smoke:

- viewer видит board, runtime-инструкции и confirmed history, но не draft/trace/formula parameters;
- moderator проходит prepare/begin/review/resume и получает authoritative preview;
- stale `expectedRoundVersion` даёт `409`, повторная refund-команда не начисляет очки дважды;
- завершённый раунд попадает в leaderboard, технически отменённый — только в отдельную секцию;
- reconnect восстанавливает countdown от server timestamps, offline state явно помечен;
- catalog item активной игры read-only, emergency disable блокирует только новые покупки.

## 5. Rollback

- Если EF migration завершилась ошибкой, PostgreSQL transaction должна откатить её целиком;
  приложение не запускается, пока причина inventory не устранена.
- После успешного cutover не запускать `database update <old migration>` на живой БД: новые
  lifecycle/audit данные могут не иметь безопасного обратного преобразования.
- При критическом дефекте остановить writers, сохранить проблемную БД для расследования,
  восстановить pre-cutover backup в новую БД и переключить connection string на предыдущий
  application image. Проверить history/quiz points перед снятием maintenance.

## 6. Cleanup gate

Legacy columns/readers и compatibility labels удаляются отдельной миграцией только после
наблюдаемого стабильного периода и письменного подтверждения всех пунктов:

- нет активных catalog rows без BehaviorV2;
- clean DB и production-like restored DB проходят migration/tests;
- pre-V2 completed history читается без повторного выполнения формул;
- новый round flow и refund/cancellation audit подтверждены на реальных lifecycle-сценариях;
- в логах нет `modifier.legacy_calculation_failed`, snapshot decode или lifecycle invariant errors;
- backup/restore и rollback были проверены оператором.

До этого gate legacy reader остаётся read-only. Новые/редактируемые definitions и новые
activations используют только BehaviorV2; возвращать legacy write path запрещено.

### Локальное выполнение 2026-08-20 — 2026-08-23

Для единственной development-среды владелец явно разрешил обойти период наблюдения и
пересоздать базу без сохранения pre-V2 данных. База `deadmans` пересоздана, затем обновлена по всей
цепочке из 13 миграций до `20260823100000_EnforceSingleNonterminalGameRound`; повторный update
идемпотентен. Legacy columns/readers удалены, все 15 seed definitions имеют BehaviorV2, а partial
unique index гарантирует не более одного nonterminal round на игру. Это исключение не отменяет
cleanup gate для будущих shared или production-сред.

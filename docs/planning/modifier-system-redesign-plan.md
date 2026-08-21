# План переработки системы модификаторов

Статус: **Implemented and locally rolled out; manual exploratory testing pending**

Пакеты A–I завершены и прошли автоматические quality gates. Единственная локальная база
пересоздана на чистой V2-схеме; физический legacy compatibility path удалён. Остаётся ручная
проверка игровых сценариев владельцем приложения.
Дата фиксации решений: **2026-08-17**
Область: `frontend/`, `backend/`, OpenAPI, persistence, история и игровой lifecycle
Связанный общий backlog: `docs/planning/functional-review-work-plan.md`

## 1. Назначение документа

Это основной рабочий план переработки модификаторов. Он нужен, чтобы после паузы можно было:

- восстановить причины переработки;
- увидеть принятые продуктовые решения без повторного обсуждения;
- проверить математику всех текущих модификаторов;
- выполнять изменения небольшими reviewable-пакетами;
- не принимать новую продуктовую семантику случайно во время реализации;
- сверять итоговый код с критериями приёмки.

Документ описывает целевое состояние. Пока соответствующий этап не реализован, фактическое поведение определяется кодом и OpenAPI.

Открытых продуктовых вопросов для первой итерации нет. Если решение меняется, сначала обновляется этот документ с причиной и датой, затем код.

Как вернуться к работе:

1. Прочитать разделы 5–9, 14–17 и 25 — это продуктовая семантика.
2. Для реализации открыть раздел 20 и журнал в разделе 24.
3. Формулы и каталог не выводить заново из чата: source of truth — разделы 7 и 8.
4. Canvas `modifier-system-review.canvas.tsx` — вспомогательная схема; при расхождении приоритет у этого файла.

## 2. Почему система перерабатывается

Текущая форма заставляет администратора собирать технический payload движка:

- около 29 полей;
- пять `mechanicType`;
- отдельный `scoringType`;
- внутренние `traits`, `resolutionInputs`, `killDeltaMode`, `multiplierTarget`;
- свободные expressions;
- шесть frontend-only типов формы итогов.

Параллельные классификации описывают один и тот же смысл разными словами. Часть настроек хранится, но не исполняется runtime. Пользователь вынужден понимать внутреннее устройство backend вместо того, чтобы описывать игровое правило.

Аудит также обнаружил технические разрывы:

- миграция формулы «Жажды» не зарегистрирована в EF migration chain локальной БД;
- одинаковые активации местами получают один групповой input и повторно умножают общий результат;
- rule-only модификаторы скрываются из формы итогов и превращаются в `cancelled`;
- `defaultLimitPerGame` фактически сбрасывается после раунда;
- formula/effect snapshot читается из изменяемого каталога слишком поздно;
- отмена активации удаляет запись и ослабляет audit trail;
- score preview возвращает агрегаты без понятного вклада каждого модификатора;
- один DTO раскрывает одинаковые технические данные всем ролям;
- отдельной стадии подготовки между заказом и фактическим gameplay нет.

## 3. Цели первой итерации

1. Сделать создание и редактирование понятным пользователю, впервые открывшему форму.
2. Убрать свободные формулы из обычного flow.
3. Представить модификатор через понятные блоки поведения.
4. Покрыть все 15 текущих карточек четырьмя встроенными formulas.
5. Сделать backend единственным источником расчёта.
6. Хранить неизменяемый результат каждого использованного modifier instance.
7. Показывать прямое влияние каждого модификатора в итогах раунда и игры.
8. Отделить подготовку внешней игры от фактического gameplay.
9. Сделать отмены, refunds, права и realtime воспроизводимыми и проверяемыми.
10. Удалить старый путь только после миграции и проверки истории.

## 4. Что не входит в первую итерацию

- UI-конструктор произвольных expressions;
- универсальный workflow-builder;
- отдельный каталог сущностей тегов;
- лимит активаций на всю игру;
- автоматическая проверка игровых событий внутри Hunt;
- server-side ticks для таймеров;
- автоматическое выставление результата по окончании таймера;
- новые speculative formulas, не используемые текущими 15 карточками.

Будущие custom formulas создаются отдельным expert-инструментом и появляются в том же отфильтрованном списке после публикации versioned formula.

## 5. Пользовательская модель

### 5.1 Первый выбор

Пользователь выбирает один из двух понятных вариантов:

1. **Правило без изменения счёта**.
2. **Влияет на итог**.

Термин «пассивный» не используется как основной: многие правила требуют активного наблюдения ведущего.

### 5.2 Расчётная ветка

Для модификатора, влияющего на итог, форма последовательно спрашивает:

1. Что меняется: `points` или `bonusKills`.
2. Как получается факт:
   - автоматически из показателей раунда;
   - ведущий выбирает да/нет;
   - ведущий вводит неотрицательное количество.
3. Как считать: dropdown показывает только совместимые встроенные formulas.

Один модификатор имеет только один reward type. У текущего каталога нет отдельного multiplier output.

### 5.3 Теги и системные признаки

Структурированные настройки автоматически создают системные badges:

- правило без счёта / влияет на итог;
- стадия действия;
- тип результата;
- способ фиксации;
- требуется наблюдение ведущего.

Пользовательские теги:

- необязательны;
- используются только для поиска, фильтрации и объяснения;
- не меняют validator, formula или runtime;
- максимум пять тегов;
- максимум 32 Unicode-символа;
- пробелы по краям удаляются, повторные пробелы схлопываются;
- дубликаты сравниваются без учёта регистра;
- custom tags не переводятся автоматически.

Ограничения `5 × 32`, trim/collapse и case-insensitive dedupe являются утверждёнными
техническими defaults первой итерации. Suggested tags берутся из небольшого curated-списка,
составленного по текущим 15 карточкам: бой, ментор, движение, снаряжение, коммуникация,
оживление, окружение, ограничение, оружие, бонус, штраф, таймер. Отдельная таблица или CRUD
справочника тегов не создаётся. Suggested и custom значения сохраняются одинаково как
нормализованные строки. Длина считается по Unicode grapheme clusters; normalization/dedupe
использует NFKC и invariant case-fold, а отображение сохраняет пользовательский casing первой
записи.

Отдельный badge «Шанс» не создаётся. Это обычное boolean-условие с результатом «Удалось / Не удалось».

## 6. Пошаговый мастер

### Шаг 1 — Карточка

- правило без счёта / влияет на итог;
- название;
- описание;
- иконка;
- предложенные или custom tags.

### Шаг 2 — Правило и активация

- стоимость в очках викторины;
- максимум активаций на один раунд;
- стадия: preparation / gameplay / result;
- участник эффекта;
- текст правила;
- требуется ли наблюдение ведущего;
- окно действия или длительность;
- конфликты;
- команда активации: если поле пустое, генерируется как `!активировать {lowercase name}` и показывается в UI; admin может переопределить вручную. Это display/search field, не behavior.

### Шаг 3 — Влияние

Показывается только для расчётного типа:

- reward type;
- resolution kind;
- совместимая formula;
- typed parameters formula.

### Шаг 4 — Проверка

- карточка глазами игрока;
- карточка глазами ведущего;
- пример результата;
- пример calculation breakdown;
- conflicts;
- итоговые параметры активации.

Пример и проверка формируются через backend validation/preview endpoint. Frontend не содержит
второй реализации formulas даже внутри wizard.

Ограничения UX:

- не больше 5–7 основных controls одновременно;
- технические коды скрыты;
- при смене preset старые несовместимые значения удаляются;
- draft хранится в памяти формы;
- закрытие формы с изменениями требует подтверждения;
- backend draft/autosave не входит в первую итерацию;
- включённый в активную игру контент открывается read-only.

Обязательный UX smoke: новый администратор без знания `mechanicType`, `scoringType` и expressions
должен создать rule-only и scoring modifier, вернуться между шагами, исправить validation error и
понять итоговый preview без обращения к технической документации.

## 7. Каталог текущих модификаторов

### 7.1 Общие правила каталога

- payer/owner — пользователь, активировавший modifier; он не может состоять в active team;
- target всех текущих карточек — active team текущего раунда;
- performer — active team, если ниже явно не указан mentor;
- admin proxy action сохраняет выбранного игрока как owner/payer, а admin — как initiator;
- указанные cost/limit являются migration baseline текущих seed rows, а не навсегда зашитым балансом;
- до глобального запуска игры admin может менять cost/limit и остальные разрешённые настройки;
- повторные rule-only активации объединяются в один усиленный effect и один общий outcome;
- параметры rule-only effects складываются линейно;
- расчётные активации остаются отдельными effect instances;
- все rule-only карточки используют единые UI outcomes: «Выполнено / Нарушено / Условие не возникло»;
- `notTriggered` означает, что ситуация применения не возникла; это не нарушение;
- для `violated` обязателен публичный comment;
- host monitoring marker управляет выделением правила в runtime host panel, но не правами и не обязательностью outcome в summary;
- панель наблюдения во время gameplay показывает только активированные карточки с `requiresHostMonitoring = true` из этого каталога; набор флагов ниже — продуктовое решение, а не эвристика по стадии;
- conflict relation симметрична независимо от направления legacy row;
- команда активации хранится в definition, копируется в snapshot и показывается на карточке/в поиске.

### 7.2 Правила без изменения счёта

#### Чирик

- cost: `3`;
- max activations per round: `5`;
- стадия: gameplay;
- performer: active team;
- host monitoring marker: `false`;
- правило: первые `60 × A` секунд разрешено перемещаться только на корточках;
- timer: автоматический countdown от `gameplayStartedAtUtc`;
- stacking: две активации дают 120 секунд, пять — 300;
- теги: движение, приседание, таймер;
- reward: none;
- conflicts: none;
- resolution: один общий rule outcome группы.

#### Расходник

- cost: `4`;
- max activations per round: `4`;
- стадия: preparation;
- performer: active team;
- host monitoring marker: `false`;
- правило: команда может заменить `A` расходников на свой выбор;
- stacking: одна дополнительная разрешённая замена на активацию;
- теги: снаряжение, расходники, замена;
- reward: none;
- conflicts: none;
- resolution: один общий rule outcome группы.

#### Трупы

- cost: `4`;
- max activations per round: `1`;
- стадия: gameplay;
- performer: active team;
- host monitoring marker: `true`;
- правило: запрещено сжигать трупы весь раунд;
- теги: трупы, огонь, запрет;
- reward: none;
- conflicts: none;
- resolution: rule outcome.

#### Навыки

- cost: `4`;
- max activations per round: `5`;
- стадия: preparation;
- performer: active team;
- host monitoring marker: `true` в target model;
- правило: внешний лимит навыков уменьшается на `min(20 × A, 100)%`;
- stacking: две активации дают `−40%`, пять — `−100%`;
- приложение хранит и показывает только суммарный процент;
- приложение не рассчитывает исходные или доступные skill points;
- теги: навыки, подготовка, ограничение;
- reward: none;
- conflicts: none;
- resolution: один общий rule outcome группы.

#### Диарея

- cost: `7`;
- max activations per round: `1`;
- стадия: gameplay;
- performer: active team;
- host monitoring marker: `true`;
- trigger: упоминание или обнаружение туалета при отсутствии врага в поле зрения;
- правило: при trigger игрок обязан зайти в туалет;
- теги: окружение, туалет, триггер;
- reward: none;
- conflicts: none;
- resolution: rule outcome; если trigger не возник, выбирается `notTriggered`.

#### Кэп

- cost: `10`;
- max activations per round: `1`;
- стадия: gameplay;
- performer: active team;
- host monitoring marker: `true`;
- правило: пользоваться голосовым чатом может только капитан;
- теги: коммуникация, капитан, голос;
- reward: none;
- conflicts: none;
- resolution: rule outcome.

#### Подъём

- cost: `14`;
- max activations per round: `1`;
- стадия: gameplay;
- performer: active team;
- host monitoring marker: `true`;
- правило: нельзя поднимать союзника, пока команда не убила врага;
- теги: оживление, союзник, условие;
- reward: none;
- conflicts: none;
- resolution: rule outcome; если ситуация подъёма не возникла, допустим `notTriggered`.

### 7.3 Менторские события без начислений

#### Проказник

- cost: `6`;
- max activations per round: `2`;
- стадия: gameplay;
- performer: mentor;
- host monitoring marker: `true`;
- loadout: обманки и полтергейст;
- правило: mentor мешает active team в течение `300 × A` секунд;
- stacking: две активации дают один 600-секундный effect;
- timer: завершается автоматически; host stop/acknowledgement отсутствует;
- если обманки кончились раньше, приложение не останавливает timer; ведущий учитывает это только в итогах как `completed` / `violated`;
- mentor нельзя убить или поднять;
- mentor kills не начисляются команде;
- теги: ментор, помеха, обманки, полтергейст, таймер;
- reward: none;
- conflicts: Менторбайт, Крыса, Шот;
- resolution: один общий rule outcome группы.

#### Менторбайт

- cost: `8`;
- max activations per round: `1`;
- стадия: gameplay;
- performer: mentor;
- host monitoring marker: `true`;
- loadout: набор шумелок;
- правило: mentor действует 300 секунд, а команда сама решает, как его использовать;
- timer: завершается автоматически; host stop/acknowledgement отсутствует;
- mentor можно убить, но нельзя поднять;
- mentor kills не начисляются команде;
- теги: ментор, шум, приманка, таймер;
- reward: none;
- conflicts: Проказник, Крыса;
- resolution: rule outcome.

#### Фейерверк

- cost: `11`;
- max activations per round: `1`;
- стадия: gameplay;
- performer: mentor;
- host monitoring marker: `true`;
- loadout: оружие с осветительными снарядами;
- обязательное расписание: `t=0`, `60`, `120`, `180`, `240`;
- первый выстрел выполняется при нажатии «Игра началась»;
- timer и расписание завершаются автоматически без host acknowledgement;
- mentor нельзя убить или поднять;
- mentor kills не начисляются команде;
- теги: ментор, сигналы, осветительные снаряды, таймер;
- reward: none;
- conflicts: none;
- resolution: rule outcome.

### 7.4 Модификаторы, влияющие на итог

#### Жажда

- cost: `3`;
- max activations per round: `2`;
- стадия: result;
- performer: active team;
- host monitoring marker: `true` как migration baseline;
- input: `killsCount` автоматически из frozen round facts;
- output: points;
- параметры: increment `5`, zero-kill penalty `25`;
- stacking: каждый instance считается отдельно, затем points суммируются;
- теги: убийства, очки, бонус, штраф, риск;
- formula: `growing_kill_value@1`;
- conflicts: none;
- manual resolution: отсутствует.

#### Патрон

- cost: `4`;
- max activations per round: `1`;
- стадия: result;
- performer: active team;
- host monitoring marker: `true`;
- условие: враг убит первой пулей;
- исключённое оружие: лук, арбалет, дробовик;
- input: boolean от ведущего;
- output: `+1 bonusKill` при success;
- теги: оружие, точность, первая пуля, исключения;
- formula: `bonus_kill_on_condition@1`;
- conflicts: none;
- resolution: `succeeded / notSucceeded`.

#### Крыса

- cost: `12`;
- max activations per round: `1`;
- стадия: result;
- performer: mentor;
- host monitoring marker: `true`;
- loadout: полный набор ловушек;
- mentor можно убить, но нельзя поднять;
- input: неотрицательное количество убийств mentor;
- output: один bonus kill на единицу count;
- mentor kills считаются вкладом active team;
- теги: ментор, ловушки, убийства;
- formula: `bonus_kills_by_count@1`;
- conflicts: Проказник, Менторбайт;
- resolution: count, где `0` валиден.

#### Шот

- cost: `13`;
- max activations per round: unlimited (`null`);
- стадия: result;
- performer: mentor;
- host monitoring marker: `true`;
- правило: каждая активация даёт mentor оружие с одним выстрелом;
- mentor можно убить, но нельзя поднять;
- input: отдельный boolean для каждой активации;
- output: `+1 bonusKill` за успешную активацию;
- три активации и два успешных выстрела дают ровно `+2 bonusKills`;
- теги: ментор, оружие, один выстрел, убийства;
- formula: `bonus_kill_on_condition@1`;
- conflicts: Проказник;
- resolution: `succeeded / notSucceeded` по каждому instance.

#### Хард75

- cost: `18`;
- max activations per round: `1`;
- стадия: result;
- performer: active team;
- host monitoring marker: `true`;
- active window: от gameplay start до восстановления полосок здоровья;
- input: количество подходящих убийств внутри окна;
- допустимый диапазон: `0 <= count <= killsCount + resolved bonusKills`;
- bonus kills Патрона, Крысы и Шота разрешено включать в верхнюю границу;
- output: points;
- bonus rate: `0.75`;
- два подходящих убийства на карточке 100 дают `+150 points`;
- это не множитель всего раунда;
- каждое подходящее убийство вместе с базовой стоимостью даёт `1.75 × cardValue`;
- теги: здоровье, убийства, окно действия, бонус;
- formula: `window_kill_bonus_points@1`;
- conflicts: none;
- resolution: count, где `0` валиден.

## 8. Четыре встроенные formulas

### `growing_kill_value@1`

- input: `round.killsCount`;
- output: points;
- parameters:
  - `incrementPointsPerKill: int32 >= 0`;
  - `zeroKillPenaltyPoints: int32 >= 0`;
- один instance:
  - если `K > 0`: `incrementPointsPerKill × K²`;
  - если `K = 0`: `-zeroKillPenaltyPoints`;
- используется: Жажда.

Итоговая интерпретация Жажды:

```text
base kills score = B × K
modifier delta   = A × 5 × K²
total kills part = B × K + A × 5 × K²
```

При `K = 0`:

```text
empty-card penalty = -B
modifier penalty   = -25 × A
empty-card subtotal = -B - 25A
```

Для карточки 100 и одной активации subtotal равен `-125`, если нет bounty, bonus kills или
других положительных modifier points.

### `bonus_kill_on_condition@1`

- input: boolean;
- output: bonusKills;
- parameter: `successBonusKills: int32 >= 1`;
- formula: `success ? N : 0`;
- используется: Патрон, Шот.

### `bonus_kills_by_count@1`

- input: non-negative count;
- output: bonusKills;
- parameter: `bonusKillsPerUnit: int32 >= 1`;
- formula: `count × N`;
- используется: Крыса.

### `window_kill_bonus_points@1`

- input: non-negative count;
- output: points;
- parameter: `bonusRate: decimal > 0`;
- formula: `round(count × cardValue × bonusRate)`;
- используется: Хард75.

### Общие правила engine

- каждый расчётный instance вычисляется отдельно;
- результат группы равен сумме instance outputs;
- bonusKills хранятся как raw output и переводятся в point equivalent через frozen `cardValue`;
- дробные points округляются для каждого instance через `MidpointRounding.AwayFromZero`;
- intermediate arithmetic использует `decimal`/`Int64`, `bonusRate` хранится с precision не ниже `decimal(18,6)`;
- каждый опубликованный aggregate безопасно ограничивается диапазоном `Int32`;
- ошибка formula/config блокирует весь preview/finalize;
- partial scoring запрещён;
- preview и finalize используют одну реализацию.

`AwayFromZero`, wide intermediates и saturation являются утверждёнными техническими defaults,
сохраняющими текущую backend policy.

### Каноническая формула раунда

```text
B  = frozen cardValue
K  = killsCount
Q  = bountyCount
BK = Σ bonusKillsDelta
P  = Σ pointsDelta

cardOutcomeUnits = K + Q + BK
cardOutcomeScore = B × cardOutcomeUnits
emptyCardPenalty = -B, если cardOutcomeUnits = 0 и P <= 0; иначе 0
finalScore       = saturate(cardOutcomeScore + P + emptyCardPenalty)
```

Следствия:

- bounty, хотя бы один bonus kill или положительный aggregate `P` отменяет empty-card penalty;
- отрицательный или нулевой aggregate `P` не отменяет penalty при нулевых outcome units;
- empty-card penalty применяется один раз на раунд, а не на modifier instance;
- technical cancelled round всегда публикует нулевой score независимо от введённых draft facts;
- displayed breakdown обязан арифметически воспроизводить `finalScore`.

### Порядок resolution/calculation

1. Проверить exact set manual resolution groups/instances.
2. Разрешить rule outcomes.
3. Разрешить boolean/count inputs Патрона, Крысы и Шота.
4. Рассчитать полный `resolvedBonusKills`.
5. Проверить Hard75 count против `killsCount + resolvedBonusKills`.
6. Рассчитать per-instance points formulas Жажды и Хард75.
7. Суммировать `BK` и `P`.
8. Применить каноническую формулу раунда и saturation.

Этот порядок является частью domain contract и покрывается golden tests.

### Сквозной пример одного раунда

Карточка `B = 100`. Активированы: Жажда ×2, Патрон, Шот ×3, Хард75, Чирик ×2.

Факты ведущего:

- `K = 3`, `Q = 0`;
- Патрон: удалось;
- Шот: удалось, не удалось, удалось;
- Хард75: `H = 2`;
- Чирик ×2: выполнено.

Расчёт:

```text
BK = 1 (Патрон) + 2 (Шот) = 3
Жажда P = 2 × 5 × 3² = 90
Хард75 P = round(2 × 100 × 0.75) = 150
cardOutcomeScore = 100 × (3 + 0 + 3) = 600
finalScore = 600 + 90 + 150 = 840
Чирик: 120 секунд, numerical contribution 0
```

Игрок после finalize видит понятные строки вклада. Formula trace Жажды и Хард75 видит только ведущий/admin.

## 9. Lifecycle раунда

Целевой основной flow:

```text
awaiting_modifiers
  -> preparing
  -> in_progress
  -> reviewing_results
  -> completed

reviewing_results -> in_progress  (ошибочно открытый review)
any nonterminal   -> cancelled    (technical cancel)
```

### `awaiting_modifiers`

- карточка открыта;
- команда выбрана;
- приём модификаторов открыт;
- активация получает обязательный `roundId`;
- новые активации разрешены.

Игрок может отменить только свою неиспользованную покупку:

- только в `awaiting_modifiers`;
- только owner, не чужие активации;
- полный refund;
- reason не требуется;
- остальные активации сохраняются;
- повторная команда идемпотентна.

Admin может отменить любую ошибочную покупку в `awaiting_modifiers`:

- полный refund владельцу;
- обязательная audit reason;
- остальные активации сохраняются;
- раунд остаётся в `awaiting_modifiers`;
- повторная команда идемпотентна.

### `preparing`

- действие UI: **«Начать раунд»**;
- приём новых модификаторов закрыт;
- ведущий видит итоговый набор правил;
- команда готовится и загружается во внешнюю игру;
- фиксируется `preparedAtUtc`;
- таймеры ещё не запущены.

Admin может отменить отдельную покупку и на этой стадии:

- выбранная активация полностью refund'ится;
- остальные активации сохраняются;
- раунд остаётся в `preparing`;
- `preparedAtUtc` сохраняется, `roundVersion` увеличивается;
- добавить замену нельзя, потому что ordering закрыт;
- если нужна новая покупка, используется полная пересборка заказа.

### Пересборка заказа

Это не отмена раунда.

- доступна только из `preparing`;
- требует подтверждения;
- отменяет все покупки текущего раунда;
- полностью возвращает очки викторины;
- сохраняет выбранную карточку и команду;
- возвращает раунд в `awaiting_modifiers`;
- набор собирается заново.

### `in_progress`

- действие UI: **«Игра началась»**;
- фиксируется `gameplayStartedAtUtc`;
- возврат в `preparing`/`awaiting_modifiers` запрещён;
- начинаются countdown timers;
- definition snapshots внутри result records копируются из activation snapshots;
- definition snapshots immutable, но outcome/input части result records заполняются позже до finalize;
- live-каталог не читается.

### `reviewing_results`

- внешняя игра завершена;
- таймеры останавливаются;
- ведущий заполняет все обязательные outcomes и scoring inputs;
- draft preview доступен только moderator/admin.

Если ведущий открыл review преждевременно:

- с подтверждением разрешён возврат в `in_progress`;
- `gameplayStartedAtUtc` не меняется;
- current `reviewedAtUtc` очищается; новый review запишет новое значение;
- переход и инициатор остаются в lifecycle audit;
- таймеры продолжают вычисляться от исходного времени и не перезапускаются;
- уже истёкший timer остаётся истёкшим;
- frontend review draft очищается как потенциально устаревший;
- round version увеличивается и клиенты выполняют resync.

### `completed`

- authoritative preview подтверждён;
- score и calculation breakdown заморожены;
- completed раунд не переоткрывается и не редактируется в первой итерации;
- будущий audited adjustment-flow является отдельной задачей;
- после успешного finalize UI сохраняет текущий выбор `continue` / `finish`;
- это не поле finalize и не новая round-семантика;
- `finish` вызывает уже существующий played-state API;
- `continue` оставляет команду выбранной для следующей карточки;
- результаты доступны игрокам и истории.

### Техническая отмена

Техническая отмена разрешена из любой незавершённой стадии.

- требуется machine-readable reason и текстовое пояснение;
- раунд получает terminal `cancelled`;
- карточка получает статус «Отменена: техническая причина»;
- карточка не возвращается в доступные и не разыгрывается повторно;
- итоговый игровой score равен нулю;
- очки викторины возвращаются полностью, даже если gameplay уже начался;
- refund выполняется атомарно и идемпотентно;
- таймеры останавливаются;
- активная команда освобождается;
- та же команда может выбрать другую карточку;
- запись остаётся в аудите.

В первой итерации отмена раунда после gameplay является техническим действием. Обычный неуспешный раунд завершается через форму итогов, а не через cancellation.

Initial technical reason codes:

- `external_game_failure`;
- `stream_or_infrastructure_failure`;
- `application_error`;
- `operator_error`;
- `other`.

Для всех codes требуется internal detail. Для `other` дополнительно требуется public summary.
Authenticated visitors видят локализованный короткий reason/public summary; internal detail доступен
только moderator/admin.

## 10. Таймеры

- backend хранит только server timestamps и snapshot расписания;
- projection возвращает `serverNowUtc`;
- UI вычисляет оставшееся время относительно server clock;
- reconnect восстанавливает состояние из `gameplayStartedAtUtc`;
- periodic server events не создаются;
- истечение таймера не меняет outcome автоматически;
- временный effect автоматически получает runtime-состояние `expired`, когда расчётное время вышло;
- у admin нет start/stop/acknowledge controls отдельных modifiers;
- Проказник не завершается приложением досрочно при окончании обманок;
- Firework schedule является инструкцией: приложение не воспроизводит пропущенные действия и не требует отметки каждого выстрела;
- результат правила ведущий отмечает только при подведении итогов;
- таймер прекращается при `reviewing_results`, `completed` или `cancelled`.

Frontend:

- вычисляет server clock offset;
- clamp'ит remaining time к нулю;
- показывает stale/offline state при потере синхронизации;
- после возврата review -> gameplay продолжает исходный timeline;
- не записывает outcome по локальному timer event.

## 11. Блокировка игрового контента

Lock включается при глобальном переходе игры в статус `active`, то есть при запуске новой доски.
Статус `ready` ещё допускает административную подготовку. После запуска весь контент, включённый в
текущую игру, становится immutable:

- вопросы;
- карточки;
- модификаторы.

Редактирование — ответственность администратора на этапе создания и подготовки игрового поля.

Правила:

- включённый в active game контент нельзя редактировать или архивировать;
- не включённые в эту игру catalog items можно редактировать;
- selection активной игры также нельзя расширить новым catalog item;
- mutation возвращает отдельный `409 content_locked_by_active_game`;
- UI показывает read-only форму с причиной;
- завершение или архивирование игры снимает lock.

### Emergency disable

Для найденного во время игры бага существует отдельное admin-действие:

- запрещает только новые активации;
- не изменяет уже купленные активации;
- не изменяет текущие snapshots;
- не пересчитывает историю;
- сохраняет инициатора, время и причину;
- отображает понятную unavailable-причину;
- необратимо для этой игры: re-enable разрешён только в следующей game setup.

## 12. Неизменяемость и versioning

### Built-in formulas

- stable code;
- monotonic integer version;
- опубликованная версия immutable;
- изменение математики создаёт новую version;
- modifier явно pin'ит нужную version;
- новая version не обновляет карточки автоматически.

### Будущая custom formula library

- опубликованные versions append-only;
- edit создаёт новую version;
- referenced version нельзя hard-delete;
- library entry можно archive;
- назначение новой version модификатору выполняется явно;
- editor library доступен только admin;
- editor не встраивается в основной modifier wizard.

Существующие parser/evaluator, whitelist variables, syntax validation и formula tests не
выбрасываются. До появления library они используются только legacy reader. В будущей library это
ядро переиспользуется с save-time validation и test preview.

### Historical snapshots

- история не зависит от live catalog или formula library;
- retroactive recalculation запрещён;
- result snapshot хранит достаточно данных для объяснения сохранённого результата;
- legacy history может строить display breakdown только из сохранённых score/kill deltas и frozen round facts, но не повторно выполнять formula;
- backend хранит stable codes и числа;
- frontend локализует объяснение через i18n;
- formula expressions и internal variables игроку не возвращаются.

Activation snapshot фиксируется в момент покупки. Это технический invariant даже при content lock:
он защищает audit, emergency-disable сценарий и воспроизводимость данных. Begin gameplay не
перечитывает definition, а только копирует уже зафиксированную definition snapshot в result record.

## 13. Целевая форма данных

### `ModifierDefinition`

- revision;
- name, description, icon;
- activation settings;
- normalized tags;
- typed `behaviorV2`;
- `emergencyDisabledAtUtc`;
- archive fields.

`backend/openapi/deadmans.v1.yaml` является source of truth для transport shape.

Activation constraints:

- cost — `int32 >= 0`;
- finite `maxActivationsPerRound` — `1..Int32.MaxValue`;
- unlimited кодируется только `null`, не sentinel number;
- `activationCommand` — optional override; default `!активировать {lowercase name}`; max 128;
- revision начинается с `1` и монотонно увеличивается при каждом update.

### `BehaviorV2`

- `schemaVersion`;
- `kind: rule | scoring`;
- `phase`;
- `performer: activeTeam | mentor`;
- `requiresHostMonitoring`;
- `rule`;
- optional typed time window/schedule;
- `stackingPolicy: aggregateParameters | independentInstances`;
- discriminated `resolution`;
- discriminated `reward`;
- optional `formulaReference`.

Закрытые resolution variants:

- `ruleStatus`;
- `boolean`;
- `nonNegativeCount`;
- `automaticRoundMetric`.

Закрытые reward variants:

- `none`;
- `points`;
- `bonusKills`.

### `GameModifierActivation`

- `roundId`;
- `modifierId`;
- owner user ID;
- initiator user ID для admin proxy action;
- `costSnapshot`;
- `definitionRevision`;
- full behavior/formula/parameter snapshot;
- `status: active | consumed | cancelled`;
- creation audit fields;
- `cancelledByUserId`, `cancelledAtUtc`, reason;
- `refundAmount`, где `0 <= refundAmount <= costSnapshot`.

Активация не удаляется при refund.

### `GameRoundModifierResult`

- `activationId`;
- immutable definition snapshot;
- optional `resolutionGroupId`;
- `resolutionKind`;
- typed resolution input, mutable только до finalize;
- outcome;
- optional `violationComment`;
- `pointsDelta`;
- `bonusKillsDelta`;
- calculation breakdown, immutable после finalize;
- resolver and timestamp audit.

Для каждой activation существует не более одного result record. Automatic results создаёт backend;
клиент не отправляет для них искусственный acknowledgement.

### `GameRound`

- status;
- monotonic version;
- `preparedAtUtc`;
- `gameplayStartedAtUtc`;
- `reviewedAtUtc`;
- `finishedAtUtc`;
- typed technical cancellation reason;
- public cancellation summary;
- internal cancellation detail.

### `GameRoundTransitionAudit`

- round ID and monotonically ordered sequence;
- from/to status;
- action code;
- initiatedByUserId;
- occurredAtUtc;
- optional reason;
- resulting roundVersion.

### Summary DTO

- base round facts;
- score totals;
- modifier groups;
- activation drilldown;
- authenticated confirmed projection;
- privileged draft/formula trace projection.

## 14. Outcomes

Единые labels используются внутри rule-only группы, но не смешиваются с boolean/count/automatic
resolution.

### Правило без начисления

- `completed` — правило выполнено;
- `violated` — правило нарушено;
- `notTriggered` — условие применения не возникло.

Для `violated` обязателен trimmed comment длиной 1–1000 символов.

Все три варианта доступны каждой rule-only карточке по зафиксированному продуктовому решению.
UI обязан объяснять:

- «Выполнено» — правило применялось и было соблюдено;
- «Нарушено» — правило применялось и было нарушено;
- «Условие не возникло» — за раунд не возникла ситуация применения или разрешённая возможность не использовалась.

Для агрегированной группы `×N` выбирается один outcome и один comment на весь усиленный effect.
Per-instance override отсутствует; смешанный исход отдельных покупок не моделируется.

### Boolean condition

- `succeeded`;
- `notSucceeded`.

Промах «Шота» или невыполненное условие «Патрона» не является нарушением и не требует комментария.

### Count

- обязательное неотрицательное целое число;
- `0` является заполненным валидным результатом.

### Automatic metric

- input берётся из frozen round facts;
- ведущий не вводит дублирующее значение;
- outcome `calculated` и breakdown вычисляются backend.

## 15. Новая форма подведения итогов раунда

Форма полностью перерабатывается вместе с behavior v2.

### Секция 1 — Контекст

Read-only:

- команда;
- участники;
- карточка;
- frozen `cardValue`;
- gameplay duration;
- завершившиеся countdown timers.

### Секция 2 — Базовые факты

- `killsCount >= 0`;
- `bountyCount >= 0`;
- optional general round note; после finalize она видна authenticated visitors вместе с confirmed summary.

### Секция 3 — Правила без изменения счёта

- показываются только реально активированные modifiers;
- одинаковые rule instances группируются как `×N`;
- группа получает stable `resolutionGroupId` и полный `memberResultIds`;
- ведущий ставит один общий статус группе;
- backend разворачивает его в отдельные result records;
- numerical contribution всегда 0;
- нарушение требует комментарий;
- комментарий виден всем authenticated visitors в итогах и истории.

### Секция 4 — Условия и счётчики

- boolean modifiers показывают «Удалось / Не удалось»;
- count modifiers принимают число;
- «Шот» показывает отдельную строку на каждую активацию;
- остальные scoring inputs также принадлежат конкретному activation/result instance;
- automatic modifiers не требуют ручного input.

Hard75 validation выполняется после разрешения bonusKills:

```text
0 <= hard75Count <= killsCount + resolvedBonusKills
```

### Секция 5 — Authoritative preview

Backend возвращает:

- базовые очки за убийства;
- очки за награды;
- empty-card penalty;
- raw bonusKills;
- point equivalent bonusKills;
- direct points каждого modifier group;
- activation drilldown;
- final score.

Frontend ничего не пересчитывает.

Preview:

- debounce/cancel предыдущих запросов не позволяет старому response перезаписать новый;
- возвращает `roundVersion` и normalized input echo/hash;
- имеет loading, incomplete, error и stale states;
- formula/config error показывает стабильный domain code;
- не изменяет persistence.

### Секция 6 — Подтверждение

Finalize разрешён, только если:

- передан ровно один manual resolution для каждой ожидаемой resolution unit:
  - один `resolutionGroupId` для агрегированной rule group;
  - один `modifierResultId` для каждого boolean/count scoring instance;
- automatic instances отсутствуют в manual request и рассчитываются backend;
- `memberResultIds` rule group точно совпадают с server projection;
- нет missing, extra или duplicate group/result IDs;
- заполнены все обязательные outcomes;
- violation содержит comment;
- counts валидны;
- последний preview успешен и соответствует текущему draft;
- round version не устарела.

Незаполненные результаты не превращаются автоматически в `cancelled` или `notTriggered`.
Finalize повторно валидирует и рассчитывает тот же request внутри transaction; frontend preview не
является доверенной записью score.

### Видимость

- draft preview доступен только moderator/admin;
- authenticated visitors не видят заполнение формы в реальном времени;
- после finalize любой authenticated visitor видит понятный confirmed breakdown в истории игры и карточки;
- violation comment доступен всем authenticated visitors;
- technical cancellation показывает всем authenticated visitors только короткий reason/public summary;
- internal cancellation detail доступен moderator/admin;
- full formula trace и parameters доступны moderator/admin;
- role filtering выполняется backend;
- realtime event не содержит sensitive breakdown.

Стоимость активаций в очках викторины в игровые итоги не выводится и не смешивается с финальным счётом.

## 16. Итоги игры и история

### Completed rounds

- modifier summary группируется по stable `modifierId`;
- если legacy snapshots одного modifier имеют разные revision/behavior hash, внутри группы создаются отдельные version subgroups;
- показываются activation count, outcomes, raw bonusKills и point contribution;
- группа раскрывается до раундов и individual instances;
- name, rule и formula берутся из snapshots;
- live-каталог не используется;
- formula version может быть показана moderator/admin;
- completed record read-only; correction/reopen в первой итерации отсутствует.

### Cancelled rounds

Отдельный раздел:

- карточка;
- команда;
- стадия отмены;
- короткий technical reason/public summary;
- факт полного refund без суммы quiz points.

Cancelled rounds:

- не входят в leaderboard;
- не входят в completed round count;
- не входят в modifier score totals;
- не скрываются из аудита.

Весь игровой сайт, board view, confirmed game history и played-card history требуют авторизацию.
Confirmed history доступна любому authenticated visitor, а не только roster конкретной игры.

## 17. Права

- весь игровой сайт закрыт авторизацией;
- любой authenticated visitor читает board, confirmed game history и played-card history;
- authenticated player читает доступный каталог и активирует modifier для себя только в `awaiting_modifiers`;
- обычные limits, conflicts, balance и запрет active-team member проверяются backend;
- owner может отменить свою activation только в `awaiting_modifiers` с полным refund и без reason;
- admin может активировать от имени игрока;
- admin может отменить чужую activation в `awaiting_modifiers` и `preparing` с полным refund и обязательной audit reason;
- в `preparing` игрок уже не отменяет покупки сам; для новой покупки нужна полная пересборка заказа;
- владельцем активации остаётся выбранный игрок;
- списание и refund относятся к балансу владельца;
- admin сохраняется как initiator;
- moderator/admin управляют prepare, rebuild, begin gameplay, review, preview, finalize и technical cancel;
- emergency disable и catalog CRUD доступны admin;
- draft summary, formula trace и internal cancellation detail доступны только moderator/admin;
- confirmed outcomes, violation comments и public cancellation summary доступны любому authenticated visitor;
- frontend gating является только UX;
- backend authorization является обязательной границей.

## 18. Concurrency и realtime

- в одной игре допускается только один nonterminal round;
- инвариант защищается partial unique index;
- activation `roundId` и round `gameId` должны принадлежать одной игре;
- для activation допускается не более одного result record;
- timestamp ordering и non-negative refund защищаются constraints;
- lifecycle и activation mutations используют общий transactional lock;
- mutation принимает `expectedRoundVersion`;
- stale command возвращает `409`;
- повторный refund или technical cancel идемпотентен;
- idempotent already-applied state распознаётся до stale-version rejection;
- realtime event содержит только `gameId`, `roundId`, `status`, `roundVersion`;
- event является invalidation signal;
- клиент после события перечитывает HTTP projection;
- full state не вычисляется из порядка событий.

### 18.1 Contract-first command set

Целевые commands в существующем `deadmans.v1.yaml`:

- `prepare`;
- `rebuild`;
- `begin-gameplay`;
- `review`;
- `resume-gameplay`;
- `score-preview`;
- `finalize`;
- `technical-cancel`;
- player self-cancel in awaiting;
- admin activation cancel with reason;
- emergency disable.

Это v1 transport contract с `BehaviorV2` schemas, а не новый API namespace/version.

### 18.2 Error taxonomy

- `400` — malformed request, missing/extra/duplicate resolution IDs, invalid count/comment;
- `401` — отсутствует действующая auth session;
- `403` — недостаточно capability/role;
- `409` — неверный lifecycle state, stale version, content lock, conflict/limit, already unavailable;
- `422` — сохранённая behavior/formula config несовместима или не может быть вычислена;
- каждый ответ содержит стабильный domain error code;
- formula/config error блокирует preview/finalize целиком и не создаёт partial score.

### 18.3 Projection split

Используются отдельные server projections:

- authenticated confirmed summary;
- moderator/admin review draft;
- moderator/admin formula trace.

Privileged fields не возвращаются как nullable-поля общего confirmed DTO.

## 19. Migration policy

### Baseline fix

Сначала:

1. Зарегистрировать migration «Жажды» в EF chain.
2. Проверить `migrations list`.
3. Проверить clean database.
4. Проверить существующую local database.
5. Добавить golden tests согласованной формулы.

### Behavior v2 backfill

- все 15 stable seed IDs получают явный behavior v2;
- migration преобразует текущую семантику row-by-row;
- canonical behavior из раздела 7 имеет приоритет над противоречивыми legacy flags/text;
- admin-edited name и balance values не перезаписываются seed defaults;
- известный старый seed description обновляется до canonical copy;
- отличающийся custom description сохраняется, но попадает в migration report для ручной проверки согласованности;
- добавляется `schemaVersion`;
- если `defaultLimitPerGame` и metadata limit расходятся, rollout блокируется до явного admin resolution;
- после reconciliation остаётся один `maxActivationsPerRound`;
- historical result snapshots не пересчитываются;
- legacy reader сохраняется до подтверждённой миграции.

### Custom и admin-created modifiers

Перед rollout выполняется inventory всех rows, а не только 15 seeds и custom expressions.

- автоматически map'ятся только однозначно совместимые behavior;
- несовместимый row блокирует rollout;
- admin должен явно:
  - сопоставить его с behavior v2 и одной из четырёх formulas;
  - осознанно архивировать;
  - либо отложить rollout до отдельной custom formula library;
- silent archive или бесконечный legacy editable path запрещены;
- существующий expression parser/evaluator остаётся legacy read-only;
- historical snapshots остаются самодостаточными;
- неизвестная legacy shape вызывает явную migration error, а не молчаливый fallback.

### Legacy activations и rounds

Backfill `roundId` выполняется только детерминированно:

1. Если result record уже ссылается на activation, используется round этого result.
2. Для unarchived activation допустима привязка к единственному nonterminal round той же игры.
3. Любая неоднозначность блокирует rollout и требует operator remediation.

Перед partial unique index выполняется validation query на несколько nonterminal rounds одной игры.
Найденные конфликты не удаляются автоматически.

### Порядок rollout

1. Expand schema nullable-полями и новыми tables/index candidates.
2. Inventory legacy data.
3. Reconcile custom rows, limits, activations и duplicate rounds.
4. Backfill behavior/snapshots/round links.
5. Validate invariants.
6. Добавить NOT NULL, unique и check constraints.
7. Переключить application contract.
8. Удалить legacy write path только после наблюдаемого стабильного периода.

### Cleanup gate

Старые fields, duplicate fallback logic и grouped scoring path удаляются только когда:

- catalog rows мигрированы;
- clean и existing DB проходят;
- old history читается;
- новый round flow покрыт integration tests;
- rollback/runbook задокументирован.

### Existing documentation/contract reconciliation

До переключения projections нужно устранить уже существующие расхождения:

- controller и OpenAPI по доступу к active-round projection;
- architecture docs и фактический self-activation endpoint;
- data-retention docs и существующий modifier archive endpoint.

Целевое правило задаёт этот документ: board/confirmed history доступны authenticated visitors,
privileged draft/trace — moderator/admin, catalog mutations/emergency disable/individual activation
cancel — admin.

## 20. План реализации

Каждый пакет должен быть отдельным reviewable intent. Не объединять backend engine, migration и новый UI в один большой change set.

### A. Baseline fixes

Результат:

- исправлена migration «Жажды»;
- current formula закреплена golden tests;
- clean/existing DB parity подтверждена.

Не смешивать с новым UI.

### B. Lifecycle и content lock

Результат:

- preparing;
- rebuild;
- begin gameplay;
- resume gameplay from review;
- technical cancel;
- player self-cancel in awaiting;
- individual admin cancellation before gameplay;
- refunds и audit;
- board-card cancelled state;
- enabled-content lock;
- emergency disable;
- round versioning и concurrency.

### C. Domain engine

Результат:

- behavior v2 types;
- four-formula registry;
- compatibility validator;
- per-instance resolver;
- calculator;
- typed outcomes;
- snapshot schema.

Repositories не содержат formula branching.

### D. Contract и data

Результат:

- v1 OpenAPI contract with BehaviorV2 schemas;
- generated frontend types;
- persistence entities/configurations;
- migrations;
- seed backfill;
- legacy reader.

### E. Round scoring backend

Результат:

- per-activation resolution;
- grouped rule resolution units;
- backend-generated automatic results;
- preview/finalize parity;
- required result validation;
- violation comments;
- modifier breakdown DTO;
- role-safe projections;
- immutable history.

### F. Modifier wizard

Результат:

- четыре шага;
- two-kind first choice;
- tags;
- filtered formulas;
- examples;
- unsaved-change protection;
- read-only content lock;
- accessibility and mobile behavior.

### G. Round summary frontend

Результат:

- новая секционная форма;
- grouped rule outcomes;
- independent Shot inputs;
- authoritative debounced preview;
- submit gate;
- player/admin views;
- localized breakdown.

### H. Runtime и history UX

Результат:

- activation surfaces;
- host monitoring;
- countdown timers;
- played-card breakdown;
- game-level modifier summary;
- separate cancelled-round section.

### I. Legacy cleanup и rollout

Результат:

- старые fields и labels удалены;
- duplicate fallback paths удалены;
- architecture docs обновлены;
- migration/runbook готов;
- полный quality gate и manual smoke пройдены.

## 21. Обязательные тесты

### Formula unit tests

- все четыре built-in formulas;
- Жажда: `K=0`, `K=1`, `K=3`, несколько активаций;
- empty-card penalty применяется один раз;
- bounty, bonus kill и положительный aggregate points отменяют empty-card penalty;
- нулевой/отрицательный aggregate points не отменяет empty-card penalty;
- Хард75 округляется `AwayFromZero`;
- Hard75 принимает count до `killsCount + resolvedBonusKills` и отклоняет большее значение;
- saturation;
- invalid config fail-fast;
- bonus kill conversion использует frozen card value.

### Activation и stacking

- каждая activation принадлежит round;
- maxActivationsPerRound;
- conflicts;
- два Шота: success + failure дают ровно `+1 bonusKill`;
- три Шота, два success дают ровно `+2`;
- два Чирика дают один 120-секундный effect и один resolution group;
- два Расходника дают две разрешённые замены;
- две активации Навыков дают `−40%`, пять — `−100%`;
- два Проказника дают один 600-секундный timer;
- grouped rule `×N` не допускает per-instance outcome и не дублирует numerical score;
- emergency disable блокирует новые и сохраняет existing instances.

### Lifecycle

- awaiting -> preparing -> in_progress -> reviewing -> completed;
- activation запрещена после preparing;
- owner self-cancel разрешён только в awaiting и refund'ит ровно один раз без reason;
- owner self-cancel в preparing запрещён;
- player не может отменить чужую activation;
- admin individual cancel в preparing сохраняет status preparing и остальные purchases;
- rebuild полностью refund'ит и сохраняет card/team;
- begin gameplay необратим;
- review -> gameplay сохраняет исходный gameplayStartedAtUtc и очищает review draft;
- technical cancel из каждой nonterminal стадии;
- technical cancel после gameplay полностью refund'ит;
- card после technical cancel недоступна;
- команда после technical cancel может выбрать другую карточку;
- после finalize `finish` идёт существующим played-state API, `continue` не пишет новое round-поле;
- timer восстанавливается после reconnect;
- timer прекращается на review/cancel;
- временные effects истекают автоматически без admin controls.

### Summary contract

- missing group/result ID -> 400;
- extra group/result ID -> 400;
- duplicate group/result ID -> 400;
- altered memberResultIds resolution group -> 400;
- automatic modifier отсутствует в manual request и всё равно рассчитывается;
- violated без comment -> 400;
- count `0` валиден;
- negative count -> 400;
- failed preview не изменяет БД;
- preview и finalize идентичны;
- player projection не содержит draft/trace/parameters;
- anonymous request к игровому сайту -> 401;
- confirmed comment доступен любому authenticated visitor после finalize;
- public technical reason не раскрывает internal detail.

### History

- history читает frozen breakdown;
- catalog rename не меняет played result;
- formula version change не меняет played result;
- completed и cancelled rounds агрегируются раздельно;
- cancelled round не влияет на score/leaderboard;
- technical reason и refund audit сохраняются.

### Concurrency

- параллельные activate/prepare;
- параллельные prepare/rebuild;
- повторный begin gameplay;
- повторный technical cancel;
- повторный refund;
- stale expected version;
- out-of-order realtime invalidation.

### Frontend

- wizard branching;
- preset change clears hidden values;
- edit round-trip;
- tag normalization;
- dirty close;
- content-lock read-only state;
- required round results;
- resolution group member display;
- Shot activation rows;
- preview race, stale response и stale round version;
- loading/incomplete/error/stale preview states;
- emergency-disabled item;
- reconnect timer и expired state;
- long text and many modifiers;
- keyboard/focus;
- mobile viewport;
- locale parity `ru/en/uk/pl`;
- first-time-admin smoke для rule-only и scoring modifier без технических терминов.

### Migration

- clean DB;
- existing DB без migration Жажды;
- existing DB с изменёнными catalog texts;
- legacy snapshots;
- legacy custom expressions;
- unmappable admin-created modifier блокирует rollout;
- limit field mismatch блокирует rollout;
- ambiguous activation roundId блокирует rollout;
- duplicate nonterminal rounds блокируют unique-index migration;
- repeat-safe local setup.

## 22. Definition of Ready для каждого пакета

- пакет имеет один reviewable intent;
- перечислены изменяемые source-of-truth files;
- OpenAPI change спроектирован до backend/frontend DTO;
- migration rollback/compatibility понятны;
- fixtures и expected formulas определены;
- права и lifecycle states перечислены;
- пакет не требует нового product decision.

## 23. Definition of Done

- критерии пакета выполнены;
- добавлены unit/integration/frontend tests;
- OpenAPI и generated contracts синхронизированы;
- все user-facing strings добавлены в `ru/en/uk/pl`;
- backend authorization проверена прямыми API tests;
- history не зависит от live catalog;
- migration проверена на clean и existing DB;
- проходят применимые quality gates;
- этот документ и architecture docs соответствуют фактической реализации;
- статус пакета и ссылка на commit/PR добавлены в журнал ниже.

## 24. Журнал реализации

### A. Baseline fixes

- Статус: `Done 2026-08-20`
- Commit/PR: локальное рабочее дерево, commit/PR ещё не создан.
- Проверки: EF migration chain и idempotent SQL generation; clean PostgreSQL; clone существующей
  PostgreSQL с более поздней миграцией, но без миграции «Жажды»; golden tests `K=0/1/3`, две
  активации, preview/finalize parity; полный backend suite `274/274 passed`.
- Примечания: `20260809121000_DeclareZhazhdaScoreFormula` зарегистрирована в EF chain и переведена
  на исполняемый PostgreSQL `jsonb` update. Existing-DB проверка подтвердила сохранение изменённых
  admin name/cost; основная локальная база не изменялась.

### B. Lifecycle и content lock

- Статус: `Done 2026-08-20 — B1/B2/B3/B4/B5 implemented`
- Commit/PR: —
- Проверки: lifecycle/API integration, authorization and idempotent refund tests;
  PostgreSQL clean migration suite; clone существующей БД с `31` activation rows;
  frontend modifier/admin/recovery/content-lock/emergency-disable scenarios и полный frontend
  gate `231/231 passed`; OpenAPI + SignalR contract generation; полный backend suite
  `290/290 passed`; clone существующей PostgreSQL с применённой миграцией
  `20260820151225_AddGameModifierContentLockEmergencyDisable` (3/3 columns, CHECK + restrictive FK).
- Примечания: добавлены `preparing`, versioned prepare/begin/review/resume и lifecycle
  timestamps. Activation теперь round-scoped и immutable; owner self-cancel разрешён
  только в awaiting, admin cancel — в awaiting/preparing с audit reason; refund
  сохраняется один раз и защищён constraints. Rebuild атомарно refund'ит весь заказ и
  возвращает preparing → awaiting; technical cancel работает из каждой nonterminal стадии,
  обнуляет score, refund'ит active/consumed activation, освобождает команду и переводит
  карточку в недоступный `cancelled`. Все переходы пишутся в упорядоченный append-only
  `GameRoundTransitionAudit`. B4 добавил транзакционный content lock определений, включённых
  в active game, стабильный `409 content_locked_by_active_game`, read-only catalog UX и
  game-scoped emergency disable. Отключение хранит первый actor/time/reason, идемпотентно,
  сохраняет существующие activation/snapshot/history и блокирует только новые активации.
  B5 подтвердил на реальном PostgreSQL сериализацию параллельных `activate/prepare` и
  `prepare/rebuild`, сохранил idempotency повторных begin/cancel/refund и stale-version guards,
  добавил versioned `modifierAvailabilityChanged` в SignalR и тест out-of-order realtime
  invalidation. Пакет B завершён.

### C. Domain engine

- Статус: `Done 2026-08-20`
- Commit/PR: —
- Проверки: `13/13` focused formula/engine tests; полный backend suite `303/303 passed`;
  formatter/style gate без изменений.
- Примечания: добавлены persistence-free BehaviorV2 discriminated types, immutable activation
  snapshot schema, registry ровно из четырёх formulas `@1`, compatibility validator, typed
  rule/boolean/count/automatic inputs и outcomes, двухфазный per-instance resolver (сначала
  bonus kills, затем Hard75/points) и канонический round calculator. Ошибки config/input и
  duplicate activation ID fail closed без partial score; wide arithmetic, `AwayFromZero` и
  Int32 saturation закреплены golden tests. Интеграция контрактов/хранения выполняется в D,
  runtime preview/finalize — в E.

### D. Contract и data

- Статус: `Done 2026-08-20`
- Commit/PR: —
- Проверки: strict codec/catalog unit tests; typed OpenAPI endpoint tests; полный backend
  suite `306/306 passed`; полный frontend gate `231/231 passed`; EF pending model changes —
  none; clean PostgreSQL migration; clone существующей БД (`15` definitions, `31`
  activations, `27` historical results); реальные negative rollout checks для custom
  expression, limit mismatch и active unmapped custom definition.
- Примечания: добавлен v1 BehaviorV2 transport contract с закрытыми resolution и formula
  parameter unions и перегенерированы frontend contracts. Definition хранит revision,
  normalized tags и strict BehaviorV2; update монотонно увеличивает revision. Activation
  замораживает полное определение/behavior/formula/parameters при покупке, result копирует
  только activation snapshot. Миграция
  `20260820162234_AddModifierBehaviorV2Snapshots` backfill'ит все 15 seeds и 31 legacy
  activation, сохраняет изменённые balance parameters и оставляет 27 historical result
  snapshots на legacy reader без пересчёта. Legacy custom-expression write path закрыт
  предсказуемым `400`; unmappable rollout fail closed с operator-readable ошибкой.

### E. Round scoring backend

- Статус: `Done 2026-08-20`
- Commit/PR: —
- Проверки: `dotnet test backend/tests/Backend.Tests/Backend.Tests.csproj --no-restore` —
  `310/310 passed`; `npm run check` — `231/231 passed`, typecheck, ESLint, i18n,
  coverage, Knip и production build; `dotnet format backend/backend.csproj --no-restore`.
- Примечания: BehaviorV2 resolution выполняется per activation, rule-only instances объединяются
  стабильным `resolutionGroupId` и принимаются только exact-set запросы. Automatic/boolean/count
  resolution, violation comments и все четыре formula работают через единый domain engine;
  bonus kills разрешаются до Hard75. Preview не пишет в БД, возвращает round version, normalized
  input hash и privileged calculation trace; finalize повторяет тот же расчёт в transaction.
  Formula/config failures fail closed без partial score с `422` и стабильным domain code, ошибки
  resolution — с `400`, stale version — с `409`. Confirmed breakdown/comment сохраняются в frozen
  history, а formula trace не попадает в viewer projection.

### F. Modifier wizard

- Статус: `Done 2026-08-20`
- Commit/PR: —
- Проверки: backend `311/311 passed`; frontend `240/240 passed`; OpenAPI generation,
  format, typecheck, ESLint, locale/hardcoded-text checks, coverage, Knip и production build.
- Примечания: старую техническую форму заменил четырёхшаговый мастер `Карточка → Правило и
  активация → Влияние → Проверка`. Первый выбор использует только rule/scoring; rule-ветка
  пропускает расчётный шаг, scoring-ветка показывает только совместимые resolution/formula и
  typed parameters четырёх встроенных formulas. Custom expressions из flow удалены. Теги
  нормализуются NFKC с сохранением casing первой записи, ограничениями `5 × 32 grapheme` и
  case-insensitive dedupe. Добавлен admin-only backend `POST /game/modifiers/preview`, который
  нормализует карточку/команду и строит authoritative calculation example через domain engine.
  Реализованы edit round-trip, очистка скрытых preset values, dirty-close confirmation,
  read-only content lock, keyboard labels и полноэкранный mobile dialog; локализация en/ru/uk/pl.

### G. Round summary frontend

- Статус: `Done 2026-08-20`
- Commit/PR: —
- Проверки: frontend `246/246 passed`; format, typecheck, ESLint, locale/hardcoded-text
  checks, coverage, Knip и production build.
- Примечания: итоговая форма переведена на V2 resolution contract. Rule-модификаторы
  разрешаются одной группой с точным набором участников и обязательным комментарием при
  нарушении; boolean/count scoring activations вводятся независимо, включая отдельные строки
  каждого Шота; automatic results отображаются, но не отправляются как ручной input. Preview
  выполняется сервером с debounce, `expectedRoundVersion`, защитой от out-of-order responses,
  проверкой version/hash и явными incomplete/loading/error/stale states. Finalize доступен только
  для последнего успешно рассчитанного draft. Добавлены локализованный score breakdown и
  privileged calculation trace; transport finalize передаёт rule groups и optimistic version.

### H. Runtime и history UX

- Статус: `Done 2026-08-20`
- Commit/PR: —
- Проверки: backend `313/313 passed`; frontend `254/254 passed`; formatter, strict
  typecheck, ESLint, locale/hardcoded-text checks, coverage, Knip и production build;
  PostgreSQL post-migration invariants `0/0/0` invalid definitions/activations/rounds.
- Примечания: active-round projection теперь содержит server time, frozen card context и
  только безопасный runtime behavior без formula parameters. Реализованы server-offset
  countdown, reconnect/offline/stale states, host-monitoring panel и отсутствие локальных
  timer outcome writes. Summary context показывает карточку, frozen value, gameplay duration,
  завершившиеся timers и optional authenticated note. Played-card breakdown разделяет revision,
  показывает violation comments; game-level summary агрегирует activation/outcome/points по
  stable modifier/revision. Completed и technically cancelled rounds разделены; отмены не
  попадают в leaderboard и показывают только public reason/stage/refund fact.

### I. Legacy cleanup и rollout

- Статус: `Done 2026-08-20 — clean local cutover complete`
- Commit/PR: —
- Проверки: полный backend `292/292 passed`; полный frontend `249/249 passed`, включая coverage;
  EF `has-pending-model-changes` — none; formatter, typecheck, lint, i18n и contracts проходят.
- Rollout: локальная БД `deadmans` пересоздана и вся цепочка из 12 миграций применена с нуля до
  `20260820184215_RemoveLegacyModifierCompatibility`. Повторный `database update` идемпотентен;
  все 15 definitions имеют schema V2, legacy columns отсутствуют. Backend live/ready/OpenAPI и
  frontend root возвращают `200`, anonymous session probe возвращает ожидаемый `204`.
- Примечания: удалены legacy API/DTO/effect/formula types, expression evaluator, compatibility
  readers, snapshots, write path и произвольная ручная корректировка очков. Definitions,
  activations, results и history используют строгие revisioned BehaviorV2 snapshots. Миграция
  удаления fail-closed очищает локальные pre-V2 result rows без полного V2 snapshot; это принято
  осознанно для единственной development-базы. Production-политика runbook остаётся обязательной
  для любого будущего многопользовательского развёртывания.

## 25. Зафиксированные решения

### 2026-08-17

- выбран пошаговый мастер;
- лимит трактуется как лимит на раунд;
- Жажда использует `5 × K²` points delta на каждую активацию;
- при нуле убийств действует empty-card penalty и отдельный штраф Жажды;
- host monitoring является наблюдением, а не permission;
- одна карточка имеет один reward type;
- custom tags не влияют на поведение;
- первая версия содержит четыре built-in formulas;
- Шот считается отдельно по каждой активации;
- Навыки складываются линейно по `20%` с cap `100%` и не рассчитывают skill points;
- Hard75 count может включать base kills и resolved bonus kills;
- empty-card penalty сохраняет текущее backend-правило и отменяется bounty, bonus kills или положительным aggregate points;
- Фейерверк запускается от фактического gameplay start;
- временные modifiers истекают автоматически без admin stop/acknowledgement;
- подготовка отделена от gameplay;
- rebuild отменяет все покупки и полностью refund'ит;
- admin может отменить одну activation до gameplay с полным refund;
- individual cancel в preparing не открывает ordering и сохраняет status preparing;
- review можно вернуть в gameplay без изменения gameplayStartedAtUtc;
- completed round не переоткрывается в первой итерации;
- после completed ведущий сохраняет выбор continue/finish;
- после technical cancel команда может выбрать другую карточку;
- technical cancellation всегда полностью refund'ит и делает карточку недоступной;
- emergency disable блокирует только новые активации;
- content lock начинается при глобальном status `active`, а emergency disable необратим до конца игры;
- правила, условия и counts используют разные outcome semantics;
- все rule-only карточки используют единые labels completed/violated/notTriggered;
- repeated rule-only activations агрегируют параметры и имеют один общий outcome;
- «Шанс» не является отдельным типом или тегом;
- все activated modifiers обязательны в round summary;
- violation comment виден любому authenticated visitor;
- draft закрыт, confirmed result виден любому authenticated visitor после finalize;
- весь игровой сайт и история требуют авторизацию;
- public technical cancellation показывает короткий reason, internal detail только moderator/admin;
- cancelled rounds показаны отдельно;
- quiz-point costs не выводятся в игровые итоги;
- всё включённое в active game содержимое заблокировано от редактирования;
- unmappable custom/admin-created modifiers блокируют rollout до ручного решения;
- игрок отменяет свою покупку только в awaiting, без reason, с полным refund;
- admin отменяет чужие покупки в awaiting/preparing с reason;
- панель наблюдения показывает только карточки с явным `requiresHostMonitoring = true`;
- Проказник не останавливается приложением, если обманки кончились раньше таймера;
- continue/finish после раунда остаётся текущим UI + existing played-state API;
- команда активации генерируется как `!активировать {name}`, показывается в UI и может быть переопределена admin.

## 26. Глоссарий экранов и кнопок

Чтобы не восстанавливать термины из чата:

| Действие               | Кто                       | Когда                                | Результат                                              |
| ---------------------- | ------------------------- | ------------------------------------ | ------------------------------------------------------ |
| Активировать           | игрок или admin за игрока | `awaiting_modifiers`                 | списание очков викторины, snapshot покупки             |
| Отменить свою покупку  | owner                     | только `awaiting_modifiers`          | полный refund, без reason                              |
| Отменить чужую покупку | admin                     | `awaiting_modifiers` или `preparing` | полный refund, reason обязателен                       |
| Начать раунд           | moderator/admin           | `awaiting_modifiers` → `preparing`   | заказ закрыт, таймеры ещё не идут                      |
| Пересобрать заказ      | moderator/admin           | `preparing` → `awaiting_modifiers`   | все покупки раунда refund, карточка и команда остаются |
| Игра началась          | moderator/admin           | `preparing` → `in_progress`          | `gameplayStartedAtUtc`, snapshots, таймеры             |
| Вернуться в игру       | moderator/admin           | `reviewing_results` → `in_progress`  | ошибочный review; таймер не сбрасывается               |
| Подвести итоги         | moderator/admin           | `in_progress` → `reviewing_results`  | форма итогов, draft только ведущему                    |
| Завершить раунд        | moderator/admin           | `reviewing_results` → `completed`    | заморозка score; игроки видят confirmed breakdown      |
| Продолжить с командой  | moderator/admin           | после finalize                       | существующий UI, команда остаётся выбранной            |
| Команда отыграла       | moderator/admin           | после finalize                       | существующий played-state API                          |
| Техническая отмена     | moderator/admin           | любая незавершённая стадия           | карточка cancelled, полный refund, score 0             |
| Emergency disable      | admin                     | active game                          | запрет новых активаций этой карточки                   |

Панель наблюдения ведущего во время gameplay: Трупы, Навыки, Диарея, Кэп, Подъём, Проказник, Менторбайт, Фейерверк, Жажда, Патрон, Крыса, Шот, Хард75. Чирик и Расходник в эту панель не входят.

# Первый production-запуск базы данных

Этот runbook применяется только к первому публичному развёртыванию Dead-Mans. Целевая
PostgreSQL должна быть пустой: перенос pre-release данных и поддержка удалённой цепочки
миграций не предусмотрены.

Единственная исходная миграция — `20260908003848_ProductionBaseline`. Она создаёт схему и
три технические роли доступа. Игры, пользователи, вопросы, ответы и модификаторы не
загружаются.

## Неизменяемые правила

- Миграцию выполняет отдельный release-шаг до запуска backend. Сам backend не изменяет
  схему при старте.
- Release и migration runner используют один commit и один набор артефактов.
- Connection string передаётся через секрет окружения и не записывается в команду, лог или
  репозиторий.
- Production-соединение PostgreSQL использует `SSL Mode=VerifyFull` и доверенный корневой
  сертификат.
- До первого пользовательского трафика ошибочно созданную пустую БД можно удалить и создать
  заново. После первого трафика baseline считается неизменяемой: только новые forward-
  миграции либо восстановление backup.
- Никогда не применять миграции `202608*` из истории Git и не выполнять downgrade на живой БД.

## 1. Preflight

1. Зафиксировать release commit, версию container image и имя целевой БД в change record.
2. Проверить, что target — новая пустая БД, а не база другого окружения.
3. Создать отдельного runtime-пользователя приложения с минимально необходимыми правами.
   Если платформа позволяет, DDL выполняет отдельный migration user; его credentials не
   передаются runtime-контейнеру.
4. Настроить проверяемый автоматический backup PostgreSQL и выполнить пробное восстановление
   в отдельную БД до открытия доступа пользователям.
5. Подготовить persistent volume для `DataProtection__KeysDirectory`; доступ к каталогу должен
   иметь только процесс backend.
6. Проверить production-конфигурацию: точные `AllowedHosts`, trusted proxy/network,
   HTTPS Twitch redirect URL, HTTPS `Storage__PublicBaseUrl`, закрытый MinIO admin console,
   включённые rate limits и отсутствие development-секретов.
7. Проверить доступ backend к приватному bucket object storage. Production bucket не должен
   разрешать listing или запись анонимным пользователям.

Локально, из корня release checkout:

```powershell
dotnet tool restore
dotnet restore backend/backend.slnx
dotnet build backend/backend.slnx --no-restore
dotnet tool run dotnet-ef migrations has-pending-model-changes `
  --project backend/backend.csproj `
  --startup-project backend/backend.csproj `
  --no-build
dotnet tool run dotnet-ef migrations list `
  --project backend/backend.csproj `
  --startup-project backend/backend.csproj `
  --no-build
```

`has-pending-model-changes` должен сообщить, что model changes отсутствуют, а список должен
содержать только `20260908003848_ProductionBaseline`.

## 2. Применение baseline

1. Не запускать backend и другие writers.
2. Передать production connection string процессу миграции через секрет окружения.
3. Выполнить:

```powershell
dotnet tool run dotnet-ef database update `
  --project backend/backend.csproj `
  --startup-project backend/backend.csproj `
  --no-build
```

4. Повторно выполнить `migrations list`: baseline должна иметь статус applied.
5. Проверить каталог PostgreSQL штатным интеграционным gate:

```powershell
dotnet test backend/tests/Backend.Tests/Backend.Tests.csproj `
  --filter "FullyQualifiedName~ProductionBaselineMigrationTests"
```

Тест создаёт собственные временные БД. Он не принимает production connection string и не
должен запускаться от production migration user.

## 3. Запуск и smoke-проверка

1. Запустить один backend instance и проверить startup logs.
2. Убедиться, что `/health/live` возвращает успех.
3. Убедиться, что `/health/ready` подтверждает доступность PostgreSQL и object storage.
4. Проверить Twitch login/callback/logout через публичный HTTPS origin.
5. Проверить, что каталог игр пуст и в БД отсутствуют тестовые пользователи, вопросы,
   модификаторы и игровые данные.
6. Создать и удалить тестовый draft через production UI до открытия общего доступа. Не
   запускать полноценную игру, если запись должна остаться в production history.
7. Только после smoke-проверки открыть внешний трафик и проверить readiness каждого следующего
   backend instance.

## 4. Критерии остановки

Не открывать пользователям доступ, если выполняется хотя бы одно условие:

- применена неизвестная миграция либо есть pending model changes;
- PostgreSQL не использует `VerifyFull`;
- `/health/ready` неуспешен;
- Data Protection keys находятся в эфемерной файловой системе;
- публично доступен MinIO admin console или bucket позволяет анонимную запись/listing;
- callback Twitch ведёт не на точный production origin;
- в целевой БД обнаружены pre-release/test данные;
- не проверено восстановление backup.

## 5. Восстановление

До первого публичного трафика безопасный rollback — остановить writers, удалить только точно
идентифицированную пустую целевую БД, создать её заново и повторить baseline после устранения
причины. Перед удалением оператор повторно сверяет host, database name и отсутствие данных.

После первого пользовательского трафика удаление или downgrade запрещены. Остановить writers,
сохранить проблемную БД для расследования, восстановить последний проверенный backup в новую
БД и переключить приложение на неё. Исправление схемы поставляется новой forward-миграцией.

## 6. После первого запуска

- Сохранить в change record commit, image digest, applied migration и результаты smoke/restore.
- Включить мониторинг readiness, ошибок БД, заполнения диска, backup jobs и срока действия TLS.
- Проверить фактическую возможность восстановления по расписанию, а не только создание backup.
- Любое дальнейшее изменение физической схемы сопровождается новой миграцией, обновлением
  `docs/architecture/database.md` и проверкой `ProductionBaselineMigrationTests`.
